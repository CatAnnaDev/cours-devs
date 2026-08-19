# 08 — Les structures, l'alignement et les unions

Une structure regroupe des champs sous un seul nom. C'est le seul moyen qu'a le C de dire « ces
octets vont ensemble », et la façon dont il les range décide de la place occupée et de la vitesse.

## Déclarer, nommer, initialiser

```c
struct Point { int x; int y; };
struct Point origine;             // sans typedef, le mot-clé struct est obligatoire partout

typedef struct {
    int x;
    int y;
} Point;                          // la forme qu'on écrit en pratique
Point a = {3, 4};                 // dans l'ordre de déclaration
Point b = {0};                    // tous les champs à zéro
Point c = { .y = 4, .x = 3 };     // champs désignés : l'ordre est libre
Point d;                          // NON INITIALISÉ : contenu indéterminé (chapitre 05)
```

Les **champs désignés** (C99) survivent à un ajout au milieu, et ce qu'on ne nomme pas vaut zéro.

## Le point et la flèche

```c
Point p = { .x = 3, .y = 4 };
Point *ptr = &p;
p.x          // 3
ptr->x       // 3
(*ptr).x     // 3, exactement la même chose
```

`->` n'est pas une deuxième manière d'accéder à un champ : c'est **une abréviation**, définie par
la norme comme `(*ptr).champ`. Elle existe parce que `.` lie plus fort que `*` — sans parenthèses,
`*ptr.x` signifie `*(ptr.x)`, qui ne compile pas. La règle tient donc en une ligne : **à gauche une
valeur, `.` ; à gauche une adresse, `->`**. Et comme `->` déréférence pour de bon, `ptr->x` sur un
pointeur nul ou pendouillant fait exactement ce que fait `*ptr`.

## Par valeur ou par pointeur

Passer une structure par valeur **copie toute la structure** à chaque appel ; passer un pointeur
copie une adresse.

| Ce qu'on passe | Octets copiés à l'appel |
|---|---|
| `Point` (8 octets) | 8, et ça passe en registres |
| une structure de 200 octets | 200, recopiés sur la pile |
| `const Point *` | 8, toujours |

Sur x86-64 et arm64, une structure d'au plus 16 octets voyage dans deux registres et ne coûte rien
; au-delà, le compilateur émet une vraie copie : 200 octets pour 10 000 entités à 60 images par
seconde, ça fait 120 Mo par seconde de pure recopie. D'où la règle, sans exception utile : **`const
T *` en lecture, `T *` en écriture** — `double aire(const Rectangle *r);` promet à l'appelant que
son objet ressortira intact.

## La disposition en mémoire : alignement et remplissage

```c
struct Mauvaise { char a; int b; char c; };     // 6 octets de données, sizeof : 12
```

```
  0    1    2    3    4    5    6    7    8    9   10   11
+----+----+----+----+----+----+----+----+----+----+----+----+
| a  | remplissage  |      b (int)      | c  | remplissage  |
+----+----+----+----+----+----+----+----+----+----+----+----+
```

Chaque type a un **alignement** : l'adresse d'un objet doit être un multiple de ce nombre. Sur les
ABI 64 bits de macOS et Linux : `char` 1, `short` 2, `int` et `float` 4, `double`, `long` et tout
pointeur 8 — un `long` fait 4 octets sur Windows, et les chiffres suivants avec lui. Deux outils
pour le constater :

```c
offsetof(struct Mauvaise, b)      // 4 — le décalage réel du champ, <stddef.h>
_Alignof(struct Mauvaise)         // 4 — C11 ; alignof avec <stdalign.h>
```

De là, deux règles qui produisent tout le reste. **Chaque champ commence à un décalage multiple de
son alignement** : `b` ne peut pas être à l'octet 1. **La taille totale est un multiple de
l'alignement de la structure** : sans les trois octets finaux, le deuxième élément de
`struct Mauvaise tableau[2];` commencerait à l'octet 9, désaligné.

**Pourquoi le compilateur remplit.** Le matériel ne lit pas la mémoire octet par octet, mais par
mots alignés — 8 ou 16 octets par accès, 64 par ligne de cache. Un `int` à cheval sur deux mots
demande deux lectures et un recollage : des cycles perdus sur x86-64, une exception ailleurs, un
refus net des instructions atomiques et SIMD. Et le contenu du remplissage est **indéterminé** :
ne compare jamais deux structures avec `memcmp`, ne les écris jamais telles quelles dans un
fichier.

## Réordonner les champs

Le compilateur n'a **pas le droit** de trier les champs : la norme impose que les décalages
croissent dans l'ordre de déclaration. C'est donc à toi de le faire.

```c
typedef struct {
    char drapeau;  double poids;  char lettre;  char *nom;  int identifiant;
} Desordre;                       // sizeof : 40

typedef struct {
    double poids;  char *nom;  int identifiant;  char drapeau;  char lettre;
} Ordonne;                        // sizeof : 24
```

| Champ | Décalage dans `Desordre` | Décalage dans `Ordonne` |
|---|---|---|
| `poids` (8) | 8 | 0 |
| `nom` (8) | 24 | 8 |
| `identifiant` (4) | 32 | 16 |
| `drapeau` (1) | 0 | 20 |
| `lettre` (1) | 16 | 21 |
| **remplissage total** | **18 octets** | **2 octets** |

Mêmes champs, 40 % de moins. La recette : **du plus exigeant au moins exigeant** — 8, puis 4, 2, 1.

**Ce que ça change vraiment.** Rien, sur une structure. Sur un tableau d'un million d'éléments :
40 Mo contre 24 Mo, soit **40 % de lignes de cache en moins** à traverser — 625 000 contre 375 000
avec les lignes de 64 octets de x86-64, moitié moins sur Apple Silicon qui les fait de 128. Sur ce
genre de boucle, c'est le nombre de lignes chargées qui décide du temps, pas le nombre
d'instructions.

**Quand ça n'en vaut pas la peine.** Une structure instanciée trois fois : jamais. Un ordre imposé
par un format de fichier, un protocole ou une ABI : interdit. Ailleurs, si le regroupement logique
aide à lire, garde la lisibilité — c'est une optimisation de tableaux chauds, et `clang -Wpadded`
montre les trous quand tu veux les voir.

## Tableau de structures, structure de tableaux

Quand une boucle chaude ne lit qu'un champ sur cinq, ranger les champs en tableaux séparés
(`float x[N]; float y[N];`) plutôt qu'en tableau de structures peut diviser le temps par trois,
parce qu'aucun octet inutile ne traverse le cache. C'est la section `17_perf`, mesures à l'appui.

## Les unions

```c
union Nombre { int entier; float reel; };       // sizeof : 4, pas 8
```

Tous les membres commencent au même octet et se partagent une seule case mémoire, dimensionnée sur
le plus grand : écrire `entier` écrase `reel`.

**Relire un membre autre que le dernier écrit n'est pas interdit en C** — la norme autorise
explicitement cette réinterprétation des octets. Mais **la valeur obtenue n'est pas spécifiée** :
elle dépend de l'ordre des octets, de la représentation des flottants, de ce que le compilateur a
gardé en registre, et le comportement devient indéfini si ces octets ne forment pas une valeur
valide du type relu. En C++, la même relecture est un comportement indéfini franc.

Pour réinterpréter volontairement, l'outil correct reste `memcpy` : il dit l'intention, il est
valable dans les deux langages, et le compilateur le réduit à un simple déplacement de registre,
sans appel de fonction.

Le vrai usage des unions est ailleurs : le **type étiqueté**, une union accompagnée d'un `enum` qui
dit lequel des membres est vivant.

```c
typedef enum { VALEUR_ENTIER, VALEUR_REEL, VALEUR_TEXTE } TypeValeur;
typedef struct {
    TypeValeur type;
    union { int entier; double reel; char *texte; } comme;
} Valeur;

double en_reel(const Valeur *valeur) {
    switch (valeur->type) {
        case VALEUR_ENTIER: return (double)valeur->comme.entier;
        case VALEUR_REEL:   return valeur->comme.reel;
        case VALEUR_TEXTE:  return atof(valeur->comme.texte);
    }
    return 0.0;
}
```

L'étiquette dit lequel des membres est vivant : exactement ce que la norme ne peut pas savoir à ta
place. `sizeof(Valeur)` vaut 16, contre 24 pour les trois champs côte à côte.

Une variante existe, souvent confondue avec celle-ci : une union **de structures** commençant
toutes par les mêmes champs dans le même ordre — leur *séquence initiale commune*. Là, et là
seulement, la norme garantit qu'on peut lire cette partie commune par n'importe lequel des
membres, donc y loger l'étiquette. La forme ci-dessus, étiquette dehors, ne dépend d'aucune
règle subtile : préfère-la.

Deux disciplines rendent le motif sûr. **Un seul endroit écrit le couple étiquette + valeur** : une
fonction par variante. Et **le `switch` n'a pas de `default`** — `-Wswitch`, actif dans `-Wall`,
signale alors la variante oubliée le jour où l'`enum` grandit.

## Les champs de bits

```c
typedef struct {
    unsigned version  : 4;
    unsigned longueur : 4;
    unsigned service  : 8;
    unsigned total    : 16;
} EnTete;                         // 32 bits déclarés, sizeof : 4
```

Le nombre après le deux-points est une largeur **en bits** ; le compilateur génère les décalages et
les masques, et `en_tete.version = 4;` s'écrit comme un champ normal. C'est fait pour les en-têtes
de protocole et les jeux de drapeaux. Leurs limites, réelles :

- **L'ordre d'attribution dans le mot est défini par l'implémentation** — certains compilateurs
  commencent par le bit de poids fort, d'autres par le faible : **aucune portabilité binaire**, ce
  qui est fatal pour l'usage qu'on est tenté d'en faire, décrire un en-tête réseau.
- **Pas d'adresse** : `&en_tete.version` ne compile pas. Ni passage par pointeur, ni `scanf`
  dedans.
- **Pas de `sizeof` sur un champ**, et pas de tableau de champs de bits.
- **Le type de base compte** : `int drapeau : 1;` peut valoir `0` ou `-1` selon que
  l'implémentation traite l'`int` nu comme signé. Écris toujours `unsigned` ou `signed`
  explicitement.

**Préfère un masque à la main dès que les bits traversent une frontière** — fichier, socket,
registre matériel :

```c
uint32_t mot = lire_grand_boutiste_32(tampon);
unsigned version  = (mot >> 28) & 0xF;
unsigned longueur = (mot >> 24) & 0xF;
```

Deux lignes de plus, et les mêmes bits sur toutes les plateformes. Garde les champs de bits pour ce
qui reste à l'intérieur d'un seul programme compilé d'un seul coup.

## Une structure qui possède de la mémoire

```c
typedef struct { char *nom; size_t longueur; } Etiquette;

Etiquette a = { .nom = malloc(8), .longueur = 7 };
memcpy(a.nom, "bonjour", 8);
Etiquette b = a;                  // copie SUPERFICIELLE
free(a.nom);
free(b.nom);                      // AddressSanitizer: attempting double-free
```

`b = a` recopie les **seize octets de la structure** — donc le pointeur, et pas les sept caractères
qu'il désigne. L'affectation d'une structure est un `memcpy` de `sizeof`, rien de plus : le C n'a
ni constructeur de copie ni emprunt.

Les deux structures désignent le même bloc, ce qui donne trois fautes du chapitre 07 pour le prix
d'une : le double `free` ci-dessus, une écriture dans `a.nom` qui modifie `b`, un `b.nom`
pendouillant dès que `a` est détruite. La copie profonde s'écrit à la main :

```c
int etiquette_copier(Etiquette *destination, const Etiquette *source) {
    char *bloc = malloc(source->longueur + 1);
    if (bloc == NULL) {
        return 0;
    }
    memcpy(bloc, source->nom, source->longueur + 1);
    destination->nom = bloc;
    destination->longueur = source->longueur;
    return 1;
}
```

Et la convention du chapitre 07 s'applique telle quelle, en paire :

```c
Etiquette *etiquette_creer(const char *texte);
void etiquette_detruire(Etiquette *etiquette);
```

**Fournis toujours la fonction de destruction.** Détruire une `Etiquette` veut dire libérer `nom`
**puis** la structure, dans cet ordre, et cet ordre n'est écrit nulle part ailleurs. Le jour où la
structure gagne un deuxième pointeur, un seul fichier change.

## À retenir

1. `ptr->champ` est exactement `(*ptr).champ` — une abréviation, rien d'autre.
2. Passer par valeur copie toute la structure : `const T *` en lecture, `T *` en écriture.
3. Le compilateur remplit pour aligner : `char / int / char` fait 12 octets, pas 6.
4. Trie les champs du plus exigeant au moins exigeant : 40 octets deviennent 24.
5. Une union sans étiquette est un piège ; `enum` + `union` est le motif à retenir.
6. Les champs de bits n'ont ni adresse ni disposition binaire portable.
7. `b = a` partage le bloc pointé : copie profonde, ou double `free`.

**Exercices : `08_structs`.**
