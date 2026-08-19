# 12 — Écrire son allocateur

`malloc` est un généraliste : il ne sait rien de ton programme, sert tous les fils d'exécution à la
fois, et rend n'importe quelle taille à n'importe quel moment. Un allocateur écrit pour un cas
précis en sait beaucoup plus, et c'est de là que vient le gain — pas d'une astuce de codage.

## Pourquoi écrire un allocateur

Les chiffres de ce chapitre viennent tous de la même machine : arm64, Apple clang 21, macOS 27,
compilé `-O2` sans sanitizer, boucles d'un million d'itérations, trois exécutions.

| Opération | Coût mesuré |
|---|---|
| `malloc(64)` puis `free` immédiat | ~7 ns le couple |
| `malloc(64)`, un million de blocs vivants | ~7 ns |
| le `free` correspondant | ~8 ns |
| un million de `malloc(64)` puis un million de `free` | ~15 ms |

Sept nanosecondes, c'est une trentaine de cycles : ce n'est **pas** lent. Le bump ci-dessous tombe
à 1,2 ns, six fois moins, ce qui ne renverse aucun profil à soi seul. Le vrai gain est ailleurs.

**La durée de vie.** Rendre dix mille objets veut dire dix mille `free`, donc avoir gardé dix mille
pointeurs sans en oublier un. Une arène rend tout d'un coup, par une affectation : la boucle d'un
million d'allocations ci-dessus, refaite dans une arène remise à zéro à la fin, prend **1,2 ms au
lieu de 15 ms**, et l'essentiel du facteur douze vient de la libération.

**Le nombre d'appels.** Un million d'appels devient un seul. Chaque appel évité, c'est un
aller-retour vers la bibliothèque, une synchronisation entre fils, une recherche de classe de
taille.

**La disposition**, qu'on oublie de compter. Les objets d'une arène sont contigus, dans l'ordre de
création ; ceux de `malloc` sont là où il y avait de la place — d'où des lignes de cache pleines
plutôt qu'à moitié vides.

## Ce que fait `malloc`, en gros

Un allocateur généraliste tient quatre exigences à la fois, et aucune n'est gratuite.

**Retrouver un bloc libre de la bonne taille.** Les blocs libres sont rangés par **classes de
taille** : une liste des 16 octets, une des 32, une des 48, puis des structures plus lourdes ou des
pages demandées au système. Allouer, c'est trouver la classe et retirer la tête de sa liste.

**Se souvenir de la taille de chaque bloc**, puisque `free` ne la reçoit pas — soit en la rangeant
juste avant le bloc rendu, soit en la déduisant de la région où tombe l'adresse. Ici, un million de
`malloc(16)` consécutifs sortent des adresses **espacées de 16 octets**, sauf aux changements de
région : la métadonnée
des petits blocs est ailleurs. La glibc fait l'autre choix, un en-tête en ligne.

**Arrondir.** `malloc_size` donne le grain : 16 octets utilisables pour toute demande de 1 à 16, 32
de 17 à 32, 48 de 33 à 48, 112 pour 100, 1024 pour 1000, et une adresse toujours multiple de 16. La
**norme** garantit moins : au moins `alignof(max_align_t)`, 8 ici et 16 sur x86-64.

**Résister à la fragmentation.** Des allocations et libérations mêlées de tailles différentes
laissent des trous : de la mémoire libre, en morceaux trop petits pour la demande suivante. C'est
le problème dur d'un allocateur général, et il le résout pour tout le monde, y compris pour le code
qui alloue depuis un autre fil. Le spécialiste le bat parce qu'il connaît la réponse à l'avance.

## L'allocateur à pointeur qui avance

Le plus simple de tous : un bloc, un curseur.

```c
typedef struct { unsigned char *base; size_t capacite; size_t utilise; } Bump;

void *bump_allouer(Bump *b, size_t taille) {
    if (taille > b->capacite - b->utilise) { return NULL; }
    void *bloc = b->base + b->utilise;
    b->utilise += taille;
    return bloc;
}
```

Compilé `-O2` sur arm64, le chemin qui réussit fait **dix instructions**, test de capacité compris.

Le test s'écrit `taille > capacite - utilise` et **pas** `utilise + taille > capacite`. L'invariant
`utilise <= capacite` garantit que la soustraction ne déborde pas, alors que l'addition peut
déborder sur une taille venant d'une entrée — et un `size_t` qui déborde repasse par zéro en
silence, ce qui laisse passer un bloc gigantesque.

La contrepartie est énorme : **il n'y a pas de `bump_liberer`**, le curseur ne sachant pas reculer
sur un bloc du milieu. D'où ses trois usages : ce qui vit jusqu'à la fin du programme (table des
symboles, chaînes internées, configuration — le système récupère tout à la sortie) ; le tampon
d'une passe qu'on jette en entier ; un sous-allocateur de structure.

## L'alignement, le détail qui fait tout casser

Un allocateur rend des adresses brutes. C'est **lui** qui doit garantir ce que le chapitre 08
appelait l'alignement : un `double` à une adresse multiple de 8, un `int` à un multiple de 4. Le
compilateur ne peut plus rien ici, il ne voit qu'un `void *`.

```c
size_t arrondir(size_t v, size_t a)    { return (v + a - 1) / a * a; }
size_t arrondir_p2(size_t v, size_t a) { return (v + a - 1) & ~(a - 1); }   // a puissance de deux
```

La seconde forme n'est valable **que** pour un alignement puissance de deux — ce que sont tous les
alignements du C — et évite une division. Les deux donnent le même résultat pour toutes les valeurs
de 0 à 199 et tous les alignements **puissance de deux** de 1 à 64, vérifié par boucle exhaustive ;
hors puissances de deux elles divergent, `arrondir_p2(1, 3)` rendant 1. Et on arrondit toujours
**le décalage courant avant de servir**, jamais l'adresse après coup.

Un invariant est caché là-dedans, et il se paie cher quand on l'oublie : arrondir le décalage ne
donne une adresse alignée que si **la base l'est déjà**. Un tampon déclaré `unsigned char zone[64]`
a un alignement de 1 : déclare-le `alignas(max_align_t)`, ou l'arène rendra des adresses que le
type n'accepte pas.

`alignof(T)` (`<stdalign.h>` en C11 et C17, mot-clé en C23) donne l'alignement exigé par `T`, et
`alignof(max_align_t)` (`<stddef.h>`) le plus grand alignement fondamental : le minimum promis par
`malloc`.

| | arm64 macOS | x86-64 macOS |
|---|---|---|
| `sizeof(max_align_t)` | 8 | 16 |
| `alignof(max_align_t)` | 8 | 16 |
| `sizeof(long double)` | 8 | 16 |

Même norme, deux chiffres : sur arm64 le `long double` est un `double`, sur x86-64 c'est le format
étendu 80 bits logé dans 16 octets, et c'est lui qui tire `max_align_t` vers le haut. Aligner tout
sur `alignof(max_align_t)` gaspille un peu ; prendre l'alignement en paramètre ne gaspille rien.

Ce n'est pas une question de performance : écrire un `int` à une adresse impaire est un
comportement indéfini au sens du chapitre 11, et UBSan l'attrape avec les options de `clings` —
`runtime error: store to misaligned address 0x6060000001a1 for type 'int', which requires 4 byte
alignment`. Sur arm64 l'accès non aligné passe souvent en silence, ce qui est le pire cas : ça
marche, jusqu'à la première instruction atomique ou au portage.

## L'arène et le marqueur

L'arène, c'est le bump plus deux choses : l'alignement, et un moyen de revenir en arrière.

```c
typedef size_t Marqueur;

void *arene_allouer(Arene *arene, size_t taille, size_t alignement) {
    size_t debut = arrondir_p2(arene->utilise, alignement);
    if (debut > arene->capacite || taille > arene->capacite - debut) { return NULL; }
    arene->utilise = debut + taille;
    return arene->base + debut;
}
Marqueur arene_marquer(const Arene *arene) { return arene->utilise; }
void arene_revenir(Arene *arene, Marqueur marqueur) { arene->utilise = marqueur; }

#define ARENE_NOUVEAU(a, type)  ((type *)arene_allouer((a), sizeof(type), alignof(type)))
```

La macro supprime l'oubli d'alignement. Une exécution vérifie la mécanique, `Point3` valant trois
`double` : un `char` sort au décalage 0, le `Point3` suivant au décalage **8** et non 1, `utilise`
vaut 32, un tableau de 100 `int` le porte à 432, et le retour au marqueur le ramène à 32.

**Le marqueur rend en O(1) tout ce qui a été pris depuis un point** : une copie du curseur pour le
prendre, une affectation pour y revenir. Et les marqueurs s'emboîtent.

```c
Marqueur m = arene_marquer(&image);
dessiner_scene(&image);              // alloue tout ce qu'elle veut, sans rien libérer
arene_revenir(&image, m);
```

Une arène **par image** dans un jeu, **par requête** dans un serveur, **par niveau** détruite au
chargement du suivant : la durée de vie cesse d'être une propriété de chaque objet, elle devient
celle du **cycle**.

### Ce que ça supprime par construction

À l'intérieur du cycle, trois des quatre fautes du chapitre 07 deviennent inécrivables. **La
fuite** : rien à oublier de libérer, le rendu est global. **Le double `free`** : pas de `free`
individuel à appeler deux fois. **L'utilisation après libération** : rien n'est libéré tant qu'on
ne revient pas à un marqueur — `arene_revenir`, lui, la fait revenir. Non par discipline :
l'opération n'existe pas. Le **dépassement**, lui, reste entier.

## Le pool à taille fixe et la liste libre

Quand tous les objets ont la même taille — particules, entités, nœuds d'un arbre — on retrouve la
libération individuelle sans rien perdre : un bloc libre ne contenant rien d'utile, on y range le
pointeur vers le libre suivant, **dans le bloc lui-même**. La liste des libres ne coûte rien.

```c
typedef struct BlocLibre { struct BlocLibre *suivant; } BlocLibre;

void *pool_allouer(Pool *pool) {
    BlocLibre *tete = pool->libres;
    if (tete == NULL) { return NULL; }
    pool->libres = tete->suivant;
    return tete;
}
void pool_rendre(Pool *pool, void *bloc) {
    BlocLibre *libre = bloc;
    libre->suivant = pool->libres;
    pool->libres = libre;
}
```

Compilé `-O2` sur arm64, le cœur de l'allocation fait **trois accès mémoire** — lire la tête, lire
son `suivant`, ranger le nouveau `suivant` — soit six instructions avec le test de liste vide ;
`pool_rendre` en fait quatre. Sur un pool de 512 blocs de 64 octets, soit 32 Ko, qui tient donc
largement dans le cache L1 de données : **moins d'une nanoseconde** le couple allouer + rendre,
contre 7 ns pour `malloc` + `free`. Sur 128 Mo parcourus une fois, il monte à 3 ns : c'est le
défaut de cache, pas le code.

À la création, deux corrections obligatoires : une taille de bloc inférieure à `sizeof(BlocLibre)`
est remontée à cette valeur, sinon le chaînage déborde ; puis elle est arrondie par
`arrondir_p2(taille_bloc, alignof(max_align_t))` — d'où **24 octets sur arm64 macOS et 32 sur
x86-64** pour un `Point3` de 24 : même code, même type, un tiers de mémoire en plus.

Réutiliser un bloc comme `BlocLibre` puis comme `Point3` est légal : la mémoire allouée n'a pas de
type déclaré, l'écriture lui donne son type effectif. Et **le dernier rendu est le premier
repris**.

## L'en-tête de bloc

Pourquoi `free(bloc)` suffit-il, alors que `malloc` avait reçu une taille ? Parce que l'allocateur
l'a rangée quelque part — le plus simple étant **juste avant** l'adresse rendue.

```c
typedef struct { size_t taille; } EnTete;
#define DECALAGE  arrondir_p2(sizeof(EnTete), alignof(max_align_t))

void *allouer_avec_entete(size_t taille) {
    unsigned char *brut = malloc(DECALAGE + taille);
    if (brut == NULL) { return NULL; }
    ((EnTete *)brut)->taille = taille;
    return brut + DECALAGE;               // on rend l'APRÈS-en-tête
}
size_t taille_de(void *bloc) { return ((EnTete *)((unsigned char *)bloc - DECALAGE))->taille; }
```

Le décalage n'est pas `sizeof(EnTete)` : l'adresse **rendue** doit rester alignée pour n'importe
quel type. `sizeof(EnTete)` vaut 8 sur les deux plateformes du tableau, le décalage **8 ici et 16
sur x86-64**.

C'est là tout le prix : huit ou seize octets par bloc, quelle que soit sa taille. Pour un million
de nœuds de 24 octets, cela fait 8 Mo de plus ici et 16 sur l'autre plateforme, entre un tiers et
deux tiers de surcoût — et la raison pour laquelle un pool, qui connaît la taille à l'avance, n'a
pas d'en-tête. La norme n'impose pas ce schéma : elle exige seulement que le pointeur passé à
`free` soit nul, ou vienne de `malloc`, `calloc`, `realloc` ou `aligned_alloc`.

## Les quatre allocateurs côte à côte

| | `malloc` général | bump | arène | pool |
|---|---|---|---|---|
| **Allocation** | classe de taille, ~7 ns | une addition, ~1,2 ns | comme le bump | 3 accès mémoire, < 1 ns à chaud |
| **Libération individuelle** | oui | impossible | impossible | oui, O(1) |
| **Libération groupée** | N appels à `free` | jamais | une affectation | recréer la liste |
| **Mémoire perdue** | arrondi de classe, métadonnée, fragmentation | la fin du bloc | idem, plus le pic non atteint | `taille_bloc - sizeof(objet)` par bloc |
| **Cas d'usage** | tout le reste | ce qui vit jusqu'à la fin | un cycle : image, requête, niveau | beaucoup d'objets identiques |

Rien de cela ne remplace `malloc` : un programme sérieux en a trois ou quatre, chacun à sa place.

## Ce que ça coûte vraiment

### La fragmentation interne d'un pool

Un pool arrondit chaque objet à sa taille de bloc : des objets de 40 octets dans des blocs de 64,
c'est **24 octets perdus sur 64, soit 37,5 %**, sur tous les blocs, y compris ceux jamais alloués.
Cette perte est fixée à la création du pool, là où celle d'un allocateur général se voit à
l'exécution ;
un pool par taille exacte, lui, laisse des réserves à moitié vides.

### L'arène surdimensionnée

Une arène réserve son pic, pas sa moyenne : taillée pour la scène la plus lourde, elle occupe 64 Mo
pendant les 99 % du temps où la scène en demande 3. Sur une machine seule, c'est de l'adressage
virtuel et le système ne donne des pages physiques que sur premier accès — 16 Ko par page ici, 4 Ko
sur x86-64. Et la question qui décide de tout : **que fait ton arène quand elle est pleine ?**
Rendre `NULL` oblige à tester partout et personne ne le fait ; grandir en chaînant casse la
contiguïté et le marqueur naïf ; planter est défendable, si c'est écrit.

### La durée de vie qu'on croit maîtriser

C'est le danger central. Le jour où quelqu'un garde le pointeur d'un objet d'arène dans une
structure qui, elle, survit au cycle, il obtient un pointeur pendouillant qui **fonctionne
parfaitement** : la mémoire est toujours à toi, toujours lisible, et contient les données de
l'image suivante. Pas de plantage, pas de rapport ASan : le bug du chapitre 07 sans aucun des
outils qui l'attrapent. Trois règles : **nommer l'arène par sa durée de vie** (`arene_image`,
`arene_requete`, `arene_permanente`), pour que le nom du paramètre dise ce qu'on a le droit de
garder ; **ne jamais faire sortir un pointeur d'arène d'une frontière de module** en silence ;
**copier explicitement** ce qui doit survivre.

### Les sanitizers deviennent aveugles

Une arène, pour ASan, c'est **un seul bloc `malloc`** : tout ce qui s'y passe lui est invisible. Ce
programme sort avec le code 0 sous `-fsanitize=address` :

```c
char *premier = arene_allouer(&a, 16, alignof(char));
char *second  = arene_allouer(&a, 16, alignof(char));
memset(second, 'B', 16);
memset(premier, 'A', 32);       // écrase second : ASan ne dit rien
```

Tu as désarmé l'outil qui attrapait la dernière faute. ASan expose `__asan_poison_memory_region` et
`__asan_unpoison_memory_region` : on empoisonne l'arène à la création, on désempoisonne chaque
allocation, et on laisse **une zone rouge empoisonnée entre deux blocs** — sans elle, deux blocs
voisins valides ne sont séparés par rien. Le dépassement ci-dessus donne alors `AddressSanitizer:
use-after-poison`, et le même geste sur `arene_revenir` restaure la détection de l'utilisation
après libération.

## À retenir

1. `malloc` coûte ~7 ns ici : on écrit un allocateur pour la durée de vie, pas pour la vitesse.
2. Bump : un curseur, une addition, dix instructions, et aucune libération individuelle possible.
3. C'est l'allocateur qui doit aligner : `(valeur + a - 1) & ~(a - 1)` pour `a` puissance de deux,
   et une adresse mal alignée est un comportement indéfini qu'UBSan signale.
4. `alignof(max_align_t)` vaut 8 sur arm64 macOS, 16 sur x86-64 : mêmes types, pas la même perte.
5. Arène plus marqueur : tout ce qui a été pris depuis un point est rendu par une affectation, et
   fuite, double `free` et utilisation après libération deviennent inécrivables dans le cycle.
6. Pool à taille fixe : la liste des libres vit dans les blocs eux-mêmes, zéro octet de plus.
7. Le vrai danger n'est pas la performance mais le pointeur d'arène gardé au-delà du cycle : il
   marche, il ne plante pas, et ASan ne voit rien à l'intérieur d'une arène.

**Exercices : `12_allocateurs`.**
