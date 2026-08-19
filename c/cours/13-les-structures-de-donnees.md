# 13 — Les structures de données, écrites à la main

Le chapitre 07 a construit un vecteur d'`int` ; reste à lui donner un type quelconque, puis à
écrire les deux structures qu'un programme C finit toujours par réclamer. Tous les chiffres
viennent d'un programme compilé sur la machine de référence : arm64, macOS, Apple clang 21.

## Le vecteur générique : trois façons

Le C n'a pas de générique. Il a trois substituts, et le choix n'est pas affaire de goût : ils n'ont
ni le même coût à l'exécution, ni la même sûreté de typage, ni la même expérience de débogage.

### 1. `void *` et une taille d'élément

```c
typedef struct {
    unsigned char *donnees;      // et pas void * : on veut faire de l'arithmétique dessus
    size_t taille, capacite, taille_element;
} Vecteur;                       // sizeof : 32 octets, contre 24 pour la version typée

static int vecteur_agrandir(Vecteur *v);          // doublement, chapitre 07

int vecteur_ajouter(Vecteur *v, const void *element) {
    if (v->taille == v->capacite && !vecteur_agrandir(v)) return 0;
    memcpy(v->donnees + v->taille * v->taille_element, element, v->taille_element);
    v->taille++; return 1;
}
void *vecteur_case(const Vecteur *v, size_t i) { return v->donnees + i * v->taille_element; }
```

Un seul exemplaire du code, utilisable pour tout : c'est la solution de `qsort`, et elle en a les
défauts. **Aucun typage** d'abord — passer `&d`, où `d` est un `double` valant `1.0`, à un vecteur
d'`int` compile sans un mot de `-Wall -Wextra`, d'ASan ni d'UBSan, et imprime `0` : les 32 bits de
poids faible de sa représentation. Avec `3.14` on obtient `1374389535`, ce qui est pire, parce que
ça ressemble à une donnée.

**Un appel de fonction par élément** ensuite, dès que le vecteur est compilé dans une autre unité
que son utilisateur, c'est-à-dire toujours. Sur quatre millions d'entiers, `-O2`, sans LTO : **12,2
ms contre 1,8 ms** pour les ajouts, **3,2 ms contre 0,38 ms** pour la somme. Ce qui coûte n'est pas
le `i * taille_element`, c'est l'appel non inlinable et le `memcpy` de taille inconnue.

**Ce qui n'est pas un problème** : l'alignement. `donnees + i * taille_element` tombe toujours
juste, la norme garantissant que `sizeof(T)` est un multiple de `_Alignof(T)` — sinon les tableaux
ne seraient pas contigus — et `malloc` rendant un bloc aligné pour tout type **à alignement
fondamental**. Un type sur-aligné par `_Alignas` sort de cette garantie et réclame `aligned_alloc`.

### 2. Les macros

```c
#define DEFINIR_VECTEUR(Nom, Type)                                          \
    typedef struct { Type *donnees; size_t taille; size_t capacite; } Nom;  \
    static int Nom##_ajouter(Nom *v, Type valeur) {                         \
        if (v->taille == v->capacite) {                                     \
            size_t nouvelle = v->capacite == 0 ? 4 : v->capacite * 2;       \
            Type *agrandi = realloc(v->donnees, nouvelle * sizeof(Type));   \
            if (agrandi == NULL) return 0;                                  \
            v->donnees = agrandi; v->capacite = nouvelle;                   \
        }                                                                   \
        v->donnees[v->taille++] = valeur; return 1;                         \
    }
```

Le code produit est celui qu'on écrirait à la main, et il a trois pièges, dont deux sérieux.

**Le message d'erreur reste lisible**, contrairement à la légende : `VecteurPoint_ajouter(&v, 3)`
donne `error: passing 'int' to parameter of incompatible type 'Point'`, la ligne d'appel, puis
`note: expanded from macro 'DEFINIR_VECTEUR'`. Trois lignes, la faute au bon endroit.

**Le type doit être un préfixe de déclarateur**, puisque la macro écrit `Type *donnees` et `Type
valeur`. `DEFINIR_VECTEUR(V, int (*)(int, int))` est une erreur de syntaxe ; avec `typedef int
Ligne[4]`, `DEFINIR_VECTEUR(V, Ligne)` donne `array type 'Ligne' is not assignable` ;
`DEFINIR_VECTEUR(V, struct { int x, y; })`, `too many arguments provided`. Parade : un `typedef`.

**Le débogueur ne voit qu'une ligne.** Vérifié dans la table DWARF d'un binaire compilé avec `-g` :
les neuf entrées du corps de `VecteurInt_ajouter` pointent toutes vers la ligne du
`DEFINIR_VECTEUR`, colonne 1. Pas de point d'arrêt, pas de pas à pas. C'est le vrai prix.

### 3. Le code généré, et celle que le cours utilise

Le même corps, mais dans un fichier que le compilateur numérote pour de bon : un modèle `.h` inclus
une fois par type, précédé de `#define VECTEUR_TYPE int` et `#define VECTEUR_NOM VecteurInt`, qui
colle les noms par `##` et se termine par les `#undef` de ses paramètres. La table de lignes nomme
alors le modèle et ses lignes 15 à 18 : le débogueur y entre normalement, contre un fichier de
plus. Le cours retient **la macro** pour ce qui est court : typée, gratuite, contenue dans un
en-tête. `void *` reste
pour les frontières hétérogènes — API de greffon, comparateur de `qsort` —, le modèle pour ce qui
dépasse la cinquantaine de lignes.

## L'invalidation, et pourquoi on garde des indices

Un vecteur qui grandit déplace ses données. Sur un million d'ajouts avec doublement à partir de 1,
l'allocateur système de macOS fait **20 réallocations, dont 13 déplacements réels**, de façon
stable : sept des vingt réallocations n'ont rien déplacé — ce qui ne sauve **aucun** pointeur,
puisque la dernière déplace, et parce qu'un pointeur vers un bloc réalloué est de toute façon
indéterminé, même quand l'adresse rendue se trouve être la même. Sinon :

```
ERROR: AddressSanitizer: heap-use-after-free on address 0x602000000970
READ of size 4 at 0x602000000970 thread T0
freed by thread T0 here:                 #0 ... in realloc
previously allocated by thread T0 here:  #0 ... in malloc
```

**Dans un tableau qui peut grandir, on stocke des indices, jamais des pointeurs** : un `size_t`
survit à toutes les réallocations. Refuser la règle coûte l'un de ces prix : renoncer à agrandir —
capacité fixe réservée d'avance, défendable pour un pool d'entités —, ou passer par un tableau de
pointeurs vers des blocs séparés, un défaut de cache par accès et une allocation par élément.

## La liste chaînée : ce qu'elle promet, ce qu'elle tient

Un nœud `struct Noeud { int valeur; struct Noeud *suivant; };` pèse 16 octets pour 4 utiles. La
liste promet l'insertion et la suppression sans déplacer personne, et des adresses stables ; elle
tient les deux promesses. Le problème est ailleurs — somme de dix millions d'`int` :

| Disposition | Temps | Rapport |
|---|---|---|
| vecteur `int[10 000 000]` | 0,6 ms | 1 |
| liste dont les nœuds sont contigus et dans l'ordre | 7,5 ms | 12 |
| liste dont les nœuds sont contigus mais mélangés | 870 ms | 1400 |
| liste d'un `malloc` par nœud, en ordre aléatoire | 870 ms | 1400 |

La ligne 2 est le **meilleur cas absolu** d'une liste, et elle perd déjà d'un facteur douze : 16
octets lus par élément au lieu de 4, un pointeur au lieu d'un indice. La ligne 3 est le cas
réaliste, et le facteur est **mille quatre cents** : la ligne de cache fait 128 octets sur Apple
Silicon (`sysctl hw.cachelinesize`) et 64 sur x86-64, donc 32 entiers d'un coup dans un vecteur
contre un seul dans une liste éparpillée, où le préchargeur ne devine rien. Et 40 Mo contre 160.

Reste « oui, mais l'insertion au milieu ». Dix mille insertions à une position tirée au sort dans
cent mille éléments : **32 ms pour le vecteur** qui déplace la queue au `memmove`, **700 ms pour la
liste**. Le vecteur gagne d'un facteur vingt-deux tout en recopiant des milliers d'éléments à
chaque coup, parce que `memmove` est séquentiel là où la liste doit marcher jusqu'à la position.
Son insertion en O(1) suppose qu'on **tient déjà le lien**.

Deux cas où elle gagne encore. **Les adresses doivent être stables** : un vecteur invalide tout à
chaque agrandissement. **On tient déjà le nœud et on le déplace** : le retirer, le passer d'une
liste à une autre, c'est du O(1) vrai — la liste **intrusive**, dont le chaînage est un champ de
l'objet, comme le `list_head` du noyau Linux ou la liste des blocs libres d'un allocateur.

## Le pointeur de pointeur

La suppression dans une liste simplement chaînée a un cas particulier : la tête n'a pas de
prédécesseur. À gauche la version qui le traite, à droite celle qui manipule **le lien** plutôt que
le nœud et n'en a pas besoin, parce que `&tete` et `&noeud->suivant` sont tous deux des `Noeud **`.

```c
int supprimer_naif(Noeud **tete, int valeur) {          int supprimer(Noeud **lien, int valeur) {
    Noeud *courant = *tete, *precedent = NULL;              while (*lien != NULL) {
    while (courant != NULL) {                                   if ((*lien)->valeur == valeur) {
        if (courant->valeur == valeur) {                            Noeud *mort = *lien;
            if (precedent == NULL)                                  *lien = mort->suivant;
                *tete = courant->suivant;                           free(mort);
            else                                                    return 1;
                precedent->suivant = courant->suivant;          }
            free(courant);                                      lien = &(*lien)->suivant;
            return 1;                                       }
        }                                                   return 0;
        precedent = courant;                            }
        courant = courant->suivant;
    }
    return 0;
}
```

Seize lignes contre douze, deux variables locales contre une, et le test `precedent == NULL` en
moins ; les deux ont été compilées et donnent le même résultat sur la tête, au milieu et sur une
clé absente. L'insertion triée perd le même cas : on avance par `lien = &(*lien)->suivant`, puis
`neuf->suivant = *lien; *lien = neuf;`.

L'idée à garder : **`lien` est l'adresse de la case qui contient le pointeur à modifier** — `&tete`
au premier tour, `&quelque_chose->suivant` ensuite, et le code n'a pas à savoir lequel.

## La table de hachage à adressage ouvert

Tout dans un seul tableau, une seule allocation, un seul pointeur à suivre au lieu de deux. La
capacité est une
puissance de deux, ce qui remplace le `%` par un `&`, et une seule fonction cherche et insère :
elle rend la case de la clé, ou la case où il faudrait la mettre.

```c
typedef struct { char *cle; int valeur; } Case;   // NULL = vide, PIERRE_TOMBALE = effacée.
typedef struct { Case *cases; size_t capacite, occupees, effacees; } Table;
static char PIERRE_TOMBALE[1];                    // sizeof(Case) = 16 : 8 cases par ligne de cache

uint32_t hachage(const char *cle);                // voir notions/hachage.md

Case *table_trouver_case(Case *cases, size_t capacite, const char *cle) {
    size_t masque = capacite - 1, i = hachage(cle) & masque;
    Case *tombe = NULL;
    for (;;) {
        Case *c = &cases[i];
        if (c->cle == NULL) return tombe != NULL ? tombe : c;   // fin du sondage
        if (c->cle == PIERRE_TOMBALE) { if (tombe == NULL) tombe = c; }
        else if (strcmp(c->cle, cle) == 0) return c;
        i = (i + 1) & masque;
    }
}
```

**Pourquoi un sondage linéaire, et pas quadratique ni double hachage ?** Parce que `i + 1` reste
dans la même ligne de cache : huit cases de 16 octets tiennent dans les 128 octets d'une ligne sur
Apple Silicon, quatre dans les 64 d'une ligne x86-64. La théorie compte les sondages, le matériel
les défauts de cache. Un million de clés, cinq millions de recherches, tas fragmenté :

| Table | Mémoire | Temps par recherche |
|---|---|---|
| adressage ouvert, 2 097 152 cases de 16 octets, charge 0,48 | 33,6 Mo | 6,7 ns |
| chaînage, 1 048 576 seaux + 1 000 000 maillons de 24 octets | 32,4 Mo | 10,7 ns |

Un facteur 1,6 à mémoire égale, et la raison tient en un compte : le chaînage paie **deux** défauts
de cache au minimum — le tableau de seaux, puis le maillon ailleurs dans le tas —, l'adressage
ouvert **un de moins**. Avec des clés `char *` comme ici, le `strcmp` en coûte encore un : mesuré,
24,3 ns par recherche contre 15,9 avec la clé rangée en ligne dans la case. C'est pourquoi les
tables plates stockent le haché dans la case et ne comparent la chaîne qu'en dernier recours. La
théorie avantage le chaînage ; le matériel tranche dans l'autre sens, et c'est pour ça que les
tables modernes sont plates.

## La suppression et la pierre tombale

Remettre une case à `NULL` casse la recherche, et le scénario tient en trois clés. Table de 16
cases, hachage FNV-1a, mesuré : `"anna"`, `"tom"` et `"luc"` tombent **tous les trois** sur la case
11. Insérées dans cet ordre, elles occupent 11, 12 et 13.

| Case | 11 | 12 | 13 |
|---|---|---|---|
| après insertion | `anna` | `tom` | `luc` |
| après suppression naïve de `tom` | `anna` | *vide* | `luc` |
| avec une pierre tombale | `anna` | *effacée* | `luc` |

Recherche de `"luc"` sur la deuxième ligne : case 11, ce n'est pas lui ; case 12, **vide, donc on
s'arrête**, et la clé est déclarée absente alors qu'elle est une case plus loin — vérifié, la
fonction rend `-1`. Sur la troisième ligne, la même recherche la retrouve en 13.

Une case vide n'est pas une absence de donnée, c'est **une information** : « aucune clé dont le
sondage passe ici n'a été insérée au-delà de ce point ». La vider efface cette information, d'où le
marqueur « occupée autrefois, continue de sonder ». Supprimer, c'est `free(c->cle); c->cle =
PIERRE_TOMBALE; table->occupees--; table->effacees++;` — et pas un `occupees--` tout seul.
L'insertion réutilise les tombales : `tombe` sonde jusqu'au bout, mais écrit dans la première
effacée.

## Le facteur de charge

Sondages moyens, mesurés sur une table de 1 048 576 cases avec un hachage à bonne avalanche, contre
les formules classiques `(1 + 1/(1-a))/2` et `(1 + 1/(1-a)²)/2` :

| Charge | Recherche réussie, mesuré / théorie | Recherche infructueuse, mesuré / théorie |
|---|---|---|
| 0,50 | 1,50 / 1,50 | 2,51 / 2,50 |
| 0,75 | 2,50 / 2,50 | 8,53 / 8,50 |
| 0,875 | 4,52 / 4,50 | 32,1 / 32,5 |
| 0,95 | 10,30 / 10,50 | 188 / 200 |

La colonne qui décide est **la deuxième** : une recherche réussie se dégrade lentement, une
recherche infructueuse — tous les tests de présence qui rendent faux — explose.

**Le seuil habituel est de trois quarts.** À 0,75, une recherche infructueuse coûte 8,5 sondages,
soit 136 octets avec des cases de 16 : environ une ligne de cache, et c'est là qu'est le coude ; à
0,875 on en est à 32 sondages. Les seuils publiés encadrent ce chiffre — 0,75 pour `HashMap` en
Java, deux tiers pour le `dict` de CPython, sept huitièmes pour les tables Swiss
(`absl::flat_hash_map`, `hashbrown`) qui sondent seize cases en SIMD sur x86-64, huit sur arm64.
Ces trois-là sont documentés,
pas mesurés ici.

Au-dessus du seuil, on **rehache** : allouer une table plus grande et replacer tout un par un,
puisque les indices dépendent de la capacité. Ce rehachage coûte O(n), étalé sur n insertions donc
O(1) amorti par insertion, comme le doublement d'un
vecteur, et le moment où les tombales disparaissent. Elles **comptent dans la charge**, et c'est le
piège du chapitre : voici 1024 cases contenant 256 clés — charge 0,25 — sous un cycle
suppression-insertion.

| Remplacements | Clés | Pierres tombales | Sondages, réussie | Sondages, infructueuse |
|---|---|---|---|---|
| 0 | 256 | 0 | 1,16 | 1,40 |
| 1024 | 256 | 546 | 1,32 | 5,36 |
| 2048 | 256 | 723 | 1,36 | 28,7 |

Le nombre de clés n'a pas bougé d'un pouce et la recherche infructueuse a été multipliée par vingt,
les cases vraiment vides — les seules qui arrêtent un sondage — ayant presque disparu. Le remède :
compter `occupees + effacees` contre les trois quarts de la capacité, et rehacher **à capacité
constante** quand ce sont les tombales qui débordent.

## Choisir

| Ce que tu veux faire | Ce que tu écris | Pourquoi |
|---|---|---|
| ajouter à la fin, indexer, tout parcourir | vecteur | contigu, préchargé, une allocation amortie |
| retrouver par clé, beaucoup de clés | table à adressage ouvert | un défaut de cache de moins que le chaînage |
| retrouver par clé, moins de ~64 clés | **tableau trié + dichotomie** | plus rapide que la table, et zéro code |
| ensemble construit une fois, lu souvent | **tableau trié + dichotomie** | rien à maintenir, mémoire exacte |
| « toutes les clés entre X et Y » | **tableau trié + dichotomie** | une table de hachage ne sait pas faire |
| clés = petits entiers denses | tableau indexé directement | pas de hachage du tout |
| adresses qui survivent aux insertions | liste chaînée ou intrusive | l'invalidation est le vrai argument |

La ligne à ne pas survoler est **le tableau trié et la dichotomie**. Sur des `uint32_t`, recherches
réussies, `-O2` :

| Nombre d'éléments | Dichotomie | Table de hachage | Balayage linéaire |
|---|---|---|---|
| 64 | 3,8 ns | 4,5 ns | 14,2 ns |
| 4 096 | 9,5 ns | 4,3 ns | 526 ns |
| 1 000 000 | 33,6 ns | 6,6 ns | — |

Jusqu'à une soixantaine d'éléments les deux se valent, et la dichotomie passe même devant : pas de
hachage à calculer, tout tient dans quelques lignes de cache. À quatre mille elle est deux fois
plus lente, ce qui reste dérisoire au regard de la table qu'il aurait fallu écrire, dimensionner,
rehacher et déboguer. Il faut le million pour un facteur cinq, et elle répond aux questions de
plage.

## À retenir

1. Générique en C : `void *` lent et non typé ; macro pour ce qui est court, modèle inclus au-delà.
2. Le `void *` coûte sept fois la version typée à l'ajout, huit à la lecture, et ne voit rien.
3. Un million d'ajouts : 20 réallocations, 13 déplacements réels. Garde des indices.
4. Une liste chaînée éparpillée se parcourt mille quatre cents fois plus lentement qu'un vecteur.
5. La liste a deux arguments et pas trois : adresses stables, et déplacement d'un nœud tenu.
6. `Noeud **lien` supprime le cas particulier de la tête, à l'insertion comme à la suppression.
7. Vider une case au lieu d'y poser une tombale perd les clés décalées, et les tombales chargent la
   table.

**Exercices : `13_structures`.**
