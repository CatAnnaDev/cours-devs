# 04 — Les pointeurs

C'est le chapitre qui fait peur, et il ne devrait pas. Un pointeur, c'est **un nombre : l'adresse
d'un octet en mémoire**. Tout le reste est de la notation.

## Deux opérateurs, et c'est tout

```c
int nombre = 42;
int *adresse = &nombre;    // & : « l'adresse de »
int copie = *adresse;      // * : « la valeur à l'adresse »
```

`&` et `*` sont réciproques. `*&nombre` est `nombre`.

La déclaration `int *adresse` se lit **« `*adresse` est un `int` »** — donc `adresse` est un
pointeur vers un `int`. C'est pour ça qu'on écrit l'étoile collée au nom :

```c
int *a, *b;     // deux pointeurs
int* a, b;      // un pointeur et un int. Le même code, une lecture trompeuse.
```

Une déclaration par ligne évite le problème définitivement.

## À quoi ça sert, concrètement

Le C n'a ni références, ni objets, ni retour multiple. Quatre besoins, un seul outil :

| Besoin | Sans pointeur |
|---|---|
| qu'une fonction modifie ma variable | impossible |
| passer une grosse structure sans la copier | impossible |
| une taille décidée à l'exécution | impossible |
| une structure qui se référence (liste, arbre) | impossible |

Ce n'est pas une commodité, c'est le seul mécanisme d'indirection du langage.

## L'arithmétique compte en éléments

```c
int nombres[4] = {10, 20, 30, 40};
int *curseur = nombres;

curseur + 1     // avance de 4 octets : un int
*(curseur + 2)  // vaut 30
curseur[2]      // exactement la même chose
```

`p + 1` avance d'**un élément**, pas d'un octet. Le compilateur multiplie par `sizeof` du type
pointé.

Et l'égalité fondamentale, qui explique le C tout entier :

```c
p[i]  ≡  *(p + i)
```

L'indexation **est** de l'arithmétique de pointeur. Ce qui a une conséquence amusante et vraie :
`i[p]` compile et fait la même chose, puisque l'addition est commutative. Ne l'écris jamais, mais
sache pourquoi ça marche : ça prouve qu'il n'y a pas de « type tableau » à l'exécution.

**Le piège associé** : `curseur + taille * sizeof(int)` avance de bien trop loin. Le `sizeof` est
déjà appliqué par le compilateur. C'est une erreur classique et ASan la voit tout de suite.

## Le pointeur nul

```c
int *pointeur = NULL;
if (pointeur != NULL) {
    *pointeur = 1;
}
```

`NULL` est l'adresse « qui ne désigne rien ». La déréférencer plante — et c'est une bonne nouvelle
: c'est le seul bug de pointeur qui échoue **bruyamment et immédiatement**.

Trois habitudes :

**Toute fonction qui reçoit un pointeur décide s'il peut être nul**, et le documente. Soit elle
teste, soit son contrat est « je ne prends pas NULL ».

**Toute fonction qui peut échouer à allouer renvoie NULL**, et l'appelant teste. `malloc` renvoie
NULL quand il n'y a plus de mémoire.

**Après `free`, mets le pointeur à NULL.** `free(NULL)` ne fait rien, donc un double `free` devient
inoffensif, et une utilisation après libération plante franchement au lieu de corrompre en silence.

## `const`, et comment le lire

```c
const int *a;        // on ne peut pas modifier *a — le pointeur, lui, bouge
int * const b;       // on ne peut pas faire pointer b ailleurs — *b est modifiable
const int * const c; // ni l'un ni l'autre
```

La règle de lecture : **de droite à gauche, en partant du nom**. `b` est un `const` pointeur vers
`int`. `a` est un pointeur vers `int` `const`.

Dans 95 % des cas, c'est la première forme qu'on veut, sur les paramètres :

```c
size_t longueur(const char *texte);
int maximum(const int *valeurs, size_t taille);
```

Ça dit à l'appelant « je ne toucherai à rien », et le compilateur le vérifie. Un `const` bien placé
est de la documentation exécutable.

## Le pointeur pendouillant

Un pointeur qui désigne une zone qui n'existe plus. Trois façons de s'en fabriquer un :

```c
int *fabriquer(void) {
    int local = 42;
    return &local;             // 1. adresse d'une locale
}

int *bloc = malloc(4);
free(bloc);
*bloc = 1;                     // 2. utilisation après libération

int *element = &vecteur[0];
agrandir(vecteur);             // realloc a peut-être déplacé le bloc
*element = 1;                  // 3. le pointeur pointe sur l'ancienne adresse
```

Le troisième est le plus vicieux, parce qu'il ne se manifeste que **parfois** : `realloc` ne
déplace le bloc que s'il ne peut pas l'agrandir sur place. Le programme marche pendant des mois,
puis casse le jour où la mémoire est fragmentée.

La règle qui les évite tous les trois : **ne garde jamais un pointeur plus longtemps que ce qu'il
désigne**. Quand tu ranges une adresse quelque part, demande-toi qui possède la zone, et qui la
libérera.

## `void *`

```c
void *anonyme = &nombre;
int *retrouve = anonyme;      // conversion implicite, légale en C
```

Un `void *` est une adresse **sans type** : n'importe quel pointeur de données s'y convertit et en
revient. C'est le mécanisme de généricité du C : `malloc` renvoie `void *`, `qsort` prend des
`void *`, `memcpy` travaille sur des `void *`.

Trois règles :

**On ne peut pas le déréférencer** — le compilateur ne sait pas combien d'octets lire.

**On ne peut pas faire d'arithmétique dessus** en C standard (GCC et Clang l'autorisent en
extension, en comptant un octet).

**Ne mets pas de cast sur le retour de `malloc` en C.** `int *p = malloc(...)` est correct et
suffisant ; le cast est nécessaire en C++, et en C il masque l'oubli d'un `#include <stdlib.h>`.

## Un pointeur sur un pointeur

```c
void reserver(int **destination, size_t taille) {
    *destination = malloc(taille * sizeof(int));
}

int *tableau = NULL;
reserver(&tableau, 10);
```

C'est la même règle qu'au chapitre 03 : pour qu'une fonction modifie une variable, il lui faut son
adresse. Ici la variable **est** un pointeur, donc son adresse est un pointeur de pointeur.

`*destination` est le pointeur, `**destination` est la valeur pointée. On s'y perd la première
fois ; ensuite c'est mécanique.

L'autre usage courant est `char **argv` : un tableau de pointeurs vers des chaînes.

## Ce qu'un pointeur n'est pas

**Ce n'est pas un entier.** On peut le convertir en `uintptr_t` pour l'afficher, mais faire de
l'arithmétique dessus comme sur un entier est indéfini.

**Ce n'est pas un tableau.** On y revient au chapitre suivant, c'est la confusion la plus
tenace du C.

**Ce n'est pas une propriété.** Le C ne dit nulle part qui doit libérer un bloc. C'est une
convention que **tu** dois écrire et tenir : ce que le langage ne vérifie pas, la documentation
doit le dire.

## À retenir

1. Un pointeur est une adresse. `&` la prend, `*` la suit.
2. `p[i]` est exactement `*(p + i)` ; l'arithmétique compte en éléments.
3. `NULL` est le seul bug de pointeur qui plante honnêtement.
4. `const int *` : on ne modifie pas la cible. C'est ce qu'on veut presque toujours.
5. Un pointeur ne dit pas qui possède la mémoire. C'est ton travail de le décider.

**Exercices : `04_pointeurs`.**
