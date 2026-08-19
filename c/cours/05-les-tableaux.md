# 05 — Les tableaux

## Un tableau n'est pas un pointeur

C'est la confusion la plus tenace du C, entretenue par des tutoriels qui répètent « en C, un
tableau est un pointeur ». C'est faux, et la différence se voit :

```c
int tableau[10];
int *pointeur = tableau;

sizeof(tableau)     // 40 : le tableau entier
sizeof(pointeur)    // 8 : une adresse

&tableau            // type int (*)[10]
&pointeur           // type int **
```

Un tableau est un **objet de 40 octets**. Un pointeur est un objet de 8 octets qui contient une
adresse. Ce sont deux choses différentes.

Ce qui est vrai, c'est que **dans presque toutes les expressions, un tableau se convertit
automatiquement en pointeur sur son premier élément**. C'est la *décomposition*, et c'est elle qui
donne l'illusion.

Les trois exceptions, à connaître :

| Contexte | Ce qui se passe |
|---|---|
| `sizeof(tableau)` | pas de décomposition : la taille totale |
| `&tableau` | pas de décomposition : pointeur vers le tableau entier |
| chaîne littérale utilisée pour initialiser | `char mot[] = "abc"` copie, ne pointe pas |

Et la conséquence pratique la plus importante :

```c
size_t nombre_de_cases = sizeof(tableau) / sizeof(tableau[0]);
```

Cette astuce ne marche **que là où le tableau est déclaré**. Passée à une fonction, elle donne 8/4
= 2, silencieusement. C'est une des erreurs les plus coûteuses du langage, et la raison pour
laquelle on passe toujours la taille en paramètre.

## Les bornes n'existent pas

```c
int nombres[4] = {10, 20, 30, 40};
nombres[4] = 99;      // hors bornes, aucune vérification
nombres[-1] = 99;     // hors bornes aussi, et ça compile
```

Le C ne vérifie **jamais** les indices. `nombres[4]` calcule `*(nombres + 4)` et écrit là où ça
tombe : une autre variable, l'adresse de retour de la fonction, n'importe quoi.

C'est la source de la moitié des failles de sécurité de l'histoire de l'informatique. Ce n'est pas
une exagération : c'est le mécanisme du dépassement de tampon.

Sans sanitizer, le symptôme est presque toujours **ailleurs** que la cause : une variable change
toute seule, le programme plante dans une fonction sans rapport, ou il marche parfaitement
jusqu'au jour où l'on ajoute une variable et que le décor change.

Avec ASan :

```
ERROR: AddressSanitizer: stack-buffer-overflow
WRITE of size 4 at 0x00016f61ddec
    #0 in main tab2.c:8
```

Le fichier, la ligne, la nature. C'est tout le sujet du chapitre 00.

**La règle** : un tableau de `n` cases a les indices `0` à `n - 1`. Et la condition de boucle
s'écrit `i < taille`, jamais `i <= taille`.

## L'initialisation

```c
int a[5] = {1, 2, 3, 4, 5};   // complet
int b[5] = {1, 2};            // les trois derniers sont mis à zéro
int c[5] = {0};               // tout à zéro — l'idiome standard
int d[5];                     // NON INITIALISÉ : contenu indéterminé
int e[] = {1, 2, 3};          // taille déduite : 3
```

**`int d[5];` ne contient pas des zéros.** Il contient ce qui traînait sur la pile. Lire une valeur
non initialisée est un comportement indéfini, et le résultat change selon l'optimisation, la
plateforme et ce qui a été appelé avant.

Le cas particulier de `{0}` : dès qu'un seul initialisateur est donné, **tout le reste est mis à
zéro** par la norme. C'est pour ça que `= {0}` remplit un tableau entier, et ça marche aussi sur
les structures.

C99 ajoute les initialisateurs désignés, très lisibles :

```c
int scores[10] = { [3] = 42, [7] = 13 };
```

## Deux dimensions

```c
int grille[3][4];
```

Ce n'est **pas** un tableau de pointeurs. C'est un bloc continu de 12 entiers, rangés ligne par
ligne :

```
[0][0] [0][1] [0][2] [0][3] [1][0] [1][1] ... [2][3]
```

D'où deux conséquences.

**On peut le parcourir à plat** :

```c
int *plat = &grille[0][0];
plat[ligne * 4 + colonne]     // ≡ grille[ligne][colonne]
```

**Et l'ordre de parcours a un vrai coût.** Parcourir ligne par ligne suit la mémoire ; parcourir
colonne par colonne saute de 16 octets à chaque pas et rate le cache. Sur de grandes matrices,
l'écart est d'un facteur cinq ou plus. C'est le premier contact avec le sujet du chapitre
`17_perf`.

En paramètre, seule la **première** dimension peut être omise :

```c
void traiter(int grille[][4], int lignes);   // le 4 est obligatoire
```

Parce que le compilateur a besoin de la largeur pour calculer `ligne * largeur + colonne`.

Pour une grille dont les deux dimensions sont connues à l'exécution, l'approche qui marche partout
est un **tableau à plat** avec la largeur portée à côté. C'est aussi la plus rapide : un seul
`malloc`, une seule zone continue, pas de double indirection.

## Copier

```c
int source[4] = {1, 2, 3, 4};
int copie[4];

copie = source;                        // erreur de compilation
memcpy(copie, source, sizeof source);  // correct
```

Un tableau ne s'affecte pas. `memcpy` compte en **octets** : `memcpy(copie, source, 4)` ne copie
que le premier `int`. Utilise `sizeof` plutôt qu'un nombre écrit à la main — c'est plus court et
ça ne se désynchronise jamais.

Et sa cousine à connaître : `memmove` fait la même chose mais gère le cas où les deux zones se
chevauchent. `memcpy` sur des zones qui se recouvrent est indéfini.

## Les tableaux de taille variable

```c
void traiter(size_t taille) {
    int tampon[taille];    // taille décidée à l'exécution
}
```

C99 les autorise. Ils sont pratiques et **dangereux** : la taille vient souvent d'une entrée, et
une valeur énorme fait exploser la pile sans aucun message. Ils sont optionnels depuis C11, et
absents de MSVC.

En pratique : au-delà de quelques centaines d'octets, `malloc`. Et pour un tampon local de taille
fixe, un tableau normal.

## À retenir

1. Un tableau n'est pas un pointeur ; il se **convertit** en pointeur presque partout.
2. `sizeof(t)/sizeof(t[0])` ne marche que là où `t` est déclaré.
3. Les indices vont de `0` à `n-1`, et personne ne vérifie.
4. `int t[5];` n'est pas initialisé. `int t[5] = {0};` l'est entièrement.
5. Un tableau 2D est un bloc continu : l'ordre de parcours a un coût.
6. `memcpy` compte en octets ; utilise `sizeof`.

**Exercices : `05_tableaux`.**
