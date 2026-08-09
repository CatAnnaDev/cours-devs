# 01 — Les types

## Un type, c'est deux choses

**Combien d'octets**, et **comment les lire**. Rien d'autre. Le C n'a pas de types au sens des
langages modernes : il a des tailles et des conventions d'interprétation.

Les mêmes 32 bits en mémoire valent `1078530011` si tu les lis comme un `int`, et `3.14159` si tu
les lis comme un `float`. Le processeur ne sait pas lequel est « juste » : c'est le type qui
décide.

## Les tailles, et ce qui est garanti

```c
sizeof(char)   // 1, toujours, par définition
sizeof(short)  // 2 en pratique
sizeof(int)    // 4 en pratique
sizeof(long)   // 8 sur Linux/macOS 64 bits, 4 sur Windows 64 bits
sizeof(void *) // 8 sur une machine 64 bits
```

**La norme ne garantit presque rien** : elle impose des minimums (`int` au moins 16 bits, `long`
au moins 32) et un ordre (`char` ≤ `short` ≤ `int` ≤ `long` ≤ `long long`). Tout le reste dépend
de la plateforme.

Ce qui explique `long` : 8 octets sur macOS et Linux, **4 sur Windows**. Un code qui suppose 8 se
casse en le portant, et le bug est silencieux.

Quand la taille compte vraiment — un format de fichier, un protocole réseau, un registre matériel —
on utilise `<stdint.h>` :

```c
#include <stdint.h>

int32_t compteur;   // exactement 32 bits, signé
uint8_t octet;      // exactement 8 bits, non signé
uint64_t horloge;   // exactement 64 bits
```

**Règle pratique** : `int` pour compter des choses ordinaires, `size_t` pour des tailles et des
indices, `intN_t` quand la largeur fait partie du contrat.

## `sizeof` est calculé à la compilation

```c
size_t taille = sizeof(int);        // 4, connu par le compilateur
size_t autre = sizeof nombres;      // la taille du tableau, pas besoin de parenthèses
```

`sizeof` n'exécute rien. `sizeof(f())` ne fait **pas** l'appel : le compilateur regarde le type de
retour et s'arrête là. Et il renvoie un `size_t`, qui est **non signé** — ce qui nous amène au
piège suivant.

## Signé contre non signé

C'est le piège numéro un des types en C.

```c
int indice = -1;
size_t taille = 4;

if (indice < taille) {
    // ce bloc n'est PAS exécuté
}
```

Quand on compare un type signé et un type non signé de même rang, **le signé est converti vers le
non signé**. `-1` devient `18446744073709551615`. La comparaison est donc fausse, et le compilateur
n'a rien fait d'illégal.

Les symptômes typiques : une boucle qui ne s'exécute jamais, ou une qui ne s'arrête jamais :

```c
for (size_t i = taille - 1; i >= 0; i--) {
    // boucle infinie : un size_t est toujours >= 0
}
```

**Les parades**, par ordre de préférence :

1. Compiler avec `-Wsign-compare` (inclus dans `-Wextra`) et corriger les avertissements.
2. Ne pas mélanger : indices signés partout, ou non signés partout.
3. Convertir explicitement, et dans le bon sens : `(size_t)indice < taille` **après** avoir vérifié
   que `indice >= 0`.

## Le débordement

```c
int grand = 2147483647;
grand = grand + 1;      // comportement indéfini
```

Le débordement d'un entier **signé** est un comportement indéfini. Pas « ça repart à
-2147483648 » : indéfini, avec tout ce que le chapitre 00 dit de grave.

Le débordement d'un entier **non signé**, lui, est parfaitement défini : il enveloppe modulo 2ⁿ.
C'est pour ça que les fonctions de hachage et les générateurs pseudo-aléatoires utilisent des
`unsigned`.

Le cas qui mord en pratique :

```c
int a = 100000;
int b = 100000;
long long produit = a * b;    // FAUX
```

La multiplication se fait **en `int`**, déborde, et le résultat indéfini est ensuite converti en
`long long`. Le type de la variable de destination ne change rien à l'arithmétique. La correction :

```c
long long produit = (long long)a * b;    // juste
```

Un seul opérande converti suffit : l'autre suit.

## Les promotions, en trois règles

Tu n'as pas besoin de connaître la table complète. Trois règles couvrent 95 % des cas :

**1. Tout ce qui est plus petit qu'un `int` devient un `int` avant tout calcul.** Additionner deux
`char` produit un `int`. C'est pourquoi `sizeof('a')` vaut 4 en C (et 1 en C++).

**2. Entre deux types de rangs différents, le plus petit monte vers le plus grand.**
`int + long` donne un `long`. `int + double` donne un `double`.

**3. À rang égal, le signé devient non signé.** C'est la règle qui produit le bug de la section
précédente.

## Les flottants

```c
double somme = 0.1 + 0.2;
if (somme == 0.3) {
    // faux
}
```

Ce n'est pas un bug du C, c'est le binaire. `0.1` n'a pas de représentation exacte en base 2, pas
plus que `1/3` n'en a en base 10. La somme vaut `0.30000000000000004`.

**Ne compare jamais deux flottants avec `==`.** Compare leur écart à une tolérance :

```c
if (fabs(somme - 0.3) < 1e-9) { ... }
```

Le choix de la tolérance dépend de l'ordre de grandeur : `1e-9` convient pour des nombres autour
de 1, pas pour des nombres autour d'un milliard.

Trois autres choses à savoir :

**`float` a environ 7 chiffres significatifs, `double` environ 16.** En dessous de ces seuils, les
valeurs sont indistinguables. Un compteur de temps en `float` devient granuleux après quelques
heures — le même problème que dans les shaders.

**La division par zéro n'est pas une erreur en flottant** : elle donne `inf` ou `nan`. En entier,
c'est un comportement indéfini, et UBSan l'attrape.

**`nan` n'est égal à rien, pas même à lui-même.** `x != x` est le test standard pour détecter un
`nan`.

## Les conversions qui perdent

```c
double precis = 3.9;
int tronque = precis;      // 3, pas 4 : on tronque, on n'arrondit pas

int grand = 300;
char petit = grand;        // implémentation définie, souvent 44
```

Le C convertit **silencieusement** dans les deux sens. Un `double` vers un `int` tronque vers zéro.
Un grand entier vers un petit garde les bits de poids faible.

`-Wconversion` signale ces pertes, mais il est très bavard. La discipline habituelle : écrire les
conversions volontaires **explicitement**, avec un cast, pour qu'on voie qu'elles sont voulues.

## `char` est un nombre

```c
char lettre = 'A';
printf("%d\n", lettre);    // 65
```

Un `char` est un petit entier. Les lettres se suivent dans la table ASCII, ce qui permet :

```c
char majuscule = minuscule - ('a' - 'A');
int chiffre = caractere - '0';
```

Et un détail sournois : **le C ne dit pas si `char` est signé ou non**. Il l'est sur x86 et ARM
sous Linux et macOS, il ne l'est pas sur ARM sous d'autres systèmes. Quand le signe compte, écris
`signed char` ou `unsigned char`.

Enfin : un `char` n'est **pas** un caractère. C'est un octet. « é » en UTF-8 en occupe deux, un
emoji quatre. On y revient au chapitre 06.

## À retenir

1. Un type, c'est une taille et une convention de lecture.
2. `size_t` est non signé : ne le compare pas à un entier signé sans réfléchir.
3. Le débordement signé est indéfini ; le non signé enveloppe.
4. Le type de la destination n'influence pas l'arithmétique : convertis **avant**.
5. `==` sur des flottants ne veut rien dire.
6. `char` est un octet, pas un caractère.

**Exercices : `01_types`.**
