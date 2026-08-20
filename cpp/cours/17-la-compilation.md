# 17 — Calculer à la compilation

Tout ce qui est dit « mesuré » ici a été compilé et lancé sur la machine de référence : arm64
macOS, Apple clang 21 (`clang-2100.3.30.1`), libc++ `_LIBCPP_VERSION` 220106. Les limites chiffrées
de l'évaluateur sont celles de **ce clang**. La norme n'en impose aucune — elle autorise seulement
une implémentation à renoncer — mais elle en *recommande* en annexe, et clang reprend exactement
ces chiffres. Ce qui vient de la norme est daté par sa version.

## Déplacer le travail

Un programme calcule à deux moments : pendant que le compilateur tourne, et pendant qu'il
s'exécute. La frontière n'est pas fixée par le langage, elle est **choisie**. Une table de 256
entrées peut être remplie au démarrage ou déjà siéger dans le binaire : même contenu, autre prix.

Ce qu'on y gagne. **Le calcul quitte l'exécution** : à `-O0`, un `crc32("123456789")` appelé
normalement produit deux `bl`, le constructeur de `string_view` puis `crc32` ; forcé à la
compilation, la fonction entière devient `mov w0, #14630` + `movk w0, #52212, lsl #16`, soit
`0xCBF43926`. **Le résultat change de section** : la table CRC atterrit dans `__TEXT,__const`,
`size 0x400`, soit 256 × 4 octets en lecture seule. **L'UB devient une erreur de compilation**,
sans avoir à lancer le programme sur la bonne entrée.

```cpp
constexpr int debordement()    { int x = 2147483647; return x + 1; }
constexpr int hors_bornes()    { int t[3]{1,2,3}; return t[5]; }
constexpr int non_initialise() { int x; return x; }
// note: value 2147483648 is outside the range of representable values of type 'int'
// note: cannot refer to element 5 of array of 3 elements in a constant expression
// note: read of uninitialized object is not allowed in a constant expression
```

Ce qu'on y perd. Le compilateur interprète un arbre au lieu d'exécuter du code machine : 2700 fois
plus lent qu'à `-O2`, mesuré plus bas. Les plafonds s'atteignent, et le résultat est figé.

## `constexpr` sur une fonction : une permission

`constexpr` devant une fonction ne dit pas « cette fonction est évaluée à la compilation », mais
« elle **a le droit** de l'être, si on l'appelle là où c'est exigé ». Le reste du temps, c'est une
fonction ordinaire, appelée à l'exécution.

```cpp
constexpr int carre(int x) { return x * x; }
int obtenir() { return carre(7); }
// -O0 : mov w0, #7  puis  bl __Z5carrei     (un vrai appel, carre est dans le binaire)
// -O2 : mov w0, #49                          (l'optimiseur, pas le mot-cle)
```

Mesuré. La constante n'est pas apparue grâce à `constexpr` : elle vient de l'**optimiseur**, qui
aurait fait le même travail sans le mot-clé. La même fonction sert dans les deux mondes, et c'est
tout l'intérêt : une définition, une logique.

```cpp
static_assert(carre(7) == 49);     // monde 1 : evalue par le compilateur
constexpr int a = carre(7);        // monde 1 : exige, donc garanti
int b = carre(lu());               // monde 2 : lu() n'est pas constexpr, appel normal
```

Ce fichier compile et affiche `49 81`. La troisième ligne est légale précisément parce que
`constexpr` est une permission, pas une contrainte sur les appels.

### `static_assert`, la seule preuve

Il n'existe aucun avertissement pour « tu croyais que c'était figé, ça ne l'est pas ». Le seul
moyen de **prouver** qu'un appel s'évalue à la compilation est de le placer là où c'est exigé :
`static_assert`, une variable `constexpr`, une taille de tableau, un argument template. Sinon la
compilation échoue sur `constexpr variable 'b' must be initialized by a constant expression`, suivi
de la cause exacte.

Deux relaxations, mesurées. Une fonction `constexpr` qui ne **peut jamais** produire une constante
est une erreur en C++20 (`constexpr function never produces a constant expression`) et devient
légale en **C++23** (P2448). Un `static constexpr` local dans une fonction `constexpr` est du
C++23 : `-Wc++23-extensions` en `-std=c++20`. Enfin `constexpr` implique `inline` : dans un en-tête
inclus par deux unités de traduction, la fonction se lie sans symbole dupliqué.

## `constexpr` sur une variable : une exigence

Sur une variable, le mot change de sens. Il n'autorise plus, il **oblige** : l'initialisateur doit
être une expression constante, sinon la compilation s'arrête. C'est la seule des trois formes qui
garantit sans qu'on ait à le prouver ailleurs. `const` ne dit rien de tel : il dit « je ne
modifierai pas cet objet ». Il se trouve qu'un `const` entier initialisé par une constante est
**aussi** utilisable comme constante — d'où la confusion, car dans le cas le plus fréquent les deux
se comportent pareil.

```cpp
const int a = 4;
int t1[a]{};              // ok : a est une constante integrale
static_assert(a == 4);    // ok
const int b = lu();       // legal : b ne sera pas modifie
static_assert(b == 3);    // ERREUR : initializer of 'b' is not a constant expression
```

La règle tient en une phrase : `const` porte sur l'**écriture**, `constexpr` sur le **moment**. Un
`constexpr` est toujours `const` en plus ; l'inverse est faux. Hors des entiers la différence se
voit dans le binaire : mesuré, un `const std::string` de plus de 22 caractères ajoute un
`__GLOBAL__sub_I_...`, donc du code au démarrage, alors qu'en deçà clang l'initialise statiquement.

## `consteval` : exiger

`consteval` (C++20 ; `__cpp_consteval` vaut 202211 ici) déclare une **fonction immédiate** : chaque
appel doit s'évaluer à la compilation, sauf s'il se trouve déjà dans un **contexte de fonction
immédiate** — le corps d'une autre fonction immédiate, ou un bloc `if consteval`. Elle n'existe pas
dans le binaire.

```cpp
consteval int cube(int x) { return x * x * x; }
constexpr int a = cube(3);   // ok
int n = 4;
int r = cube(n);             // error: call to consteval function 'cube' is not a
                             // constant expression ; note: read of non-const variable 'n'
```

C'est le bon outil quand l'appel à l'exécution serait un **bug silencieux** plutôt qu'un repli
acceptable : validation d'une chaîne de format, construction d'une table, identifiant figé. Mesuré
à `-O0` sur le `crc32` plus haut : appelée en `constexpr` la fonction produit deux `bl`, en
`consteval` elle se réduit à `mov`, `movk`, `ret`. La locale `constexpr auto v = f(...); return v;`
donne le même corps, mais c'est une discipline d'appelant, pas une propriété de la fonction.

Sur ce clang, un `constexpr` **non modèle** qui appelle un `consteval` avec son propre paramètre
est refusé, en `-std=c++20` comme en `-std=c++23` : c'est ce que dit la norme. Un **modèle** ou une
**lambda** devient lui-même immédiat (P2564, appliqué ici même en `-std=c++20`), l'erreur ne
tombant qu'à l'appel.

## `constinit` : ni l'un ni l'autre

`constinit` (C++20 ; `__cpp_constinit` vaut 201907 ici) ne rend rien constant. Il exige que la
variable soit **initialisée avant que le programme démarre**, par une constante, et laisse ensuite
le droit de la modifier. Il ne vaut que pour une durée statique ou `thread_local` ; sur une locale,
`local variable cannot be declared 'constinit'`.

Le problème qu'il résout coûte cher. Une globale dont l'initialisateur n'est pas une constante est
initialisée par du **code**, exécuté avant `main` : la norme garantit l'ordre dans une unité de
traduction, et **rien du tout** entre unités.

```cpp
// a.cpp                                  // b.cpp
int fabriquer();                          int lire_table();
int table[4] = {fabriquer(), 0, 0, 0};    int fabriquer() { return 42; }
int lire_table() { return table[0]; }     int depend = lire_table();
// c++ a.cpp b.cpp -o ab && ./ab  ->  depend=42
// c++ b.cpp a.cpp -o ba && ./ba  ->  depend=0
```

Mesuré : seul l'ordre des fichiers change. Aucun diagnostic, aucun sanitizer : `depend` lit une
variable pas encore initialisée et récupère le zéro de la zone statique.

`constinit` en fait une erreur de compilation, `variable does not have a constant initializer` : la
variable est initialisable statiquement, donc bonne avant tout initialisateur dynamique.

```cpp
constexpr int fabriquer() { return 42; }
constinit int table[4] = {fabriquer(), 0, 0, 0};   // dans le binaire, pas dans du code
```

Les deux ordres de liaison donnent alors `depend=42`, et la variable vit dans `__DATA,__data` sans
fonction d'initialisation. Ce que `constinit` ne fait pas : il ne protège pas en écriture
(`constinit int x = 10;` puis `x = 99;` compile et marche), et il ne dit rien de l'ordre entre deux
variables **dynamiquement** initialisées. Il retire une variable du problème, sans le résoudre.

## Ce que C++20 a ouvert

Avant C++20, une évaluation constante ne pouvait ni allouer, ni contenir `try`, ni faire d'appel
virtuel. Les trois ont sauté, avec des conditions.

**L'allocation** est permise à condition d'être **libérée avant la fin de l'évaluation**, par le
même mécanisme. D'où `constexpr std::vector` et `constexpr std::string` — les deux macros
`__cpp_lib_constexpr_vector` et `__cpp_lib_constexpr_string` valent 201907 ici, et `std::sort` sur
un `vector` local s'évalue sans rien demander. Ce qui est interdit, c'est de **survivre** :

```cpp
constexpr int *fuite() { return new int(7); }
constexpr int *p = fuite();
// error: constexpr variable 'p' must be initialized by a constant expression
// note: pointer to heap-allocated object is not a constant expression
// note: heap allocation performed here
```

La conséquence surprend : une `constexpr std::string` ne survit que si elle **n'a pas alloué**. Sur
cette libc++ arm64 (`sizeof(std::string)` vaut 24), mesuré au caractère près : 22 caractères
passent, 23 échouent sur `pointer to subobject of heap-allocated object`. Le seuil est celui de la
SSO, pas du langage ; dans une fonction, une chaîne de 53 caractères passe sans problème.

**`try`/`catch`** est autorisé dans une fonction `constexpr`, et `throw` peut y figurer tant qu'il
n'est **pas atteint** : `avec_try(21)` passe un `static_assert`, une branche qui lance donne
`subexpression not valid in a constant expression` sur le `throw`. Lancer et rattraper à la
compilation est du **C++26** : `__cpp_constexpr_exceptions` n'est défini ni en `-std=c++20` ni en
`-std=c++23` ici.

**Le virtuel** marche si les fonctions virtuelles et le destructeur sont `constexpr`. Un tableau de
`const Forme*` parcouru en appelant `aire()` sur deux dérivés donne 29 dans un `static_assert` :
l'évaluateur connaît le type dynamique exact de chaque objet qu'il a construit lui-même.

## `if consteval` et `std::is_constant_evaluated`

Les deux répondent à « suis-je en train d'être évalué par le compilateur ? », mais pas au même
niveau. `std::is_constant_evaluated()` (C++20, `<type_traits>`) est une **fonction** rendant un
`bool`, à mettre dans un `if` ordinaire pour choisir un algorithme lent mais évaluable contre un
algorithme rapide mais interdit à la compilation. Mesuré, à `-O0` comme à `-O2`, sur un corps
rendant la racine entière dans la branche constante et `999` sinon : `constexpr int a = racine(50)`
vaut **7**, `racine(n)` vaut **999**, et `racine(50)` passé à un `printf` vaut **999** aussi. Cette
dernière valeur est la leçon : un littéral en argument ne crée aucune exigence.

`if consteval` (C++23 ; `__cpp_if_consteval` vaut 202106 ici, et `-std=c++20` l'accepte avec
`warning: consteval if is a C++23 extension`) est une **construction du langage**. Sa branche prise
est un contexte de fonction immédiate, ce qui y autorise des appels `consteval`. C'est la seule
différence qui compte, et elle se vérifie :

```cpp
consteval int lent(int n);
constexpr int refuse(int n) {
    if (std::is_constant_evaluated()) return lent(n);   // error: call to consteval function
    return 999;                                         // 'lent' is not a constant expression
}
constexpr int accepte(int n) {
    if consteval { return lent(n); }                    // ok : contexte de fonction immediate
    return 999;
}
```

### Le piège

`std::is_constant_evaluated()` dans un `if constexpr` est toujours vrai : la condition d'un
`if constexpr` est elle-même une expression constante, donc la question est posée depuis le monde
de la compilation, et la branche `else` n'est **jamais prise**. Attention à ne pas croire qu'elle
disparaît : dans une fonction ordinaire, elle est intégralement analysée, et une faute de frappe y
reste une erreur de compilation. C'est seulement dans un modèle que la branche écartée échappe à
l'instanciation.

```cpp
constexpr int f(int n) {
    if constexpr (std::is_constant_evaluated()) return n * 2;
    else return -1;   // code mort
}
int n = 5;  f(5), f(n);   // 10 et 10, mesure
```

Clang le dit, et c'est la seule protection : un `-Wconstant-evaluated` actif ici sous
`-Wall -Wextra`, disant que l'appel
`will always evaluate to 'true' in a manifestly constant-evaluated expression`. Même piège pour
`constexpr bool b = std::is_constant_evaluated();`.

## Ce que ça coûte

L'exemple mesuré est un crible d'Ératosthène rendant les `N` premiers nombres premiers dans un
`std::array`, écrit une fois et compilé de deux façons : la table en `constexpr auto`, ou la même
fonction non `constexpr` appelée au démarrage. Meilleur de trois passages, `-fsyntax-only`.

| `N` | table `constexpr` | table à l'exécution | surcoût |
|---|---|---|---|
| 1000 | 0.19 s | 0.14 s | 0.05 s |
| 2000 | 0.23 s | 0.14 s | 0.09 s |
| 4000 | 0.35 s | 0.14 s | 0.21 s |

Le même crible **lancé** pour `N = 4000` prend 0.078 ms à `-O2`, et 1.145 ms à `-O0` avec ASan et
UBSan, soit les conditions exactes des exercices. L'évaluateur constant est donc environ **2700
fois** plus lent que le code optimisé, et **180 fois** plus lent que le code instrumenté : le
calcul déplacé se paie une fois par fichier compilé. En échange le binaire change de forme, à
`N = 5000` :

| | `__TEXT` | `__DATA` | sections | initialisation |
|---|---|---|---|---|
| table `constexpr` | 32768 | 0 | `__const` | aucune |
| table à l'exécution | 16384 | 32768 | `__bss`, `__init_offsets` | `__GLOBAL__sub_I_...` |

La version `constexpr` n'a **aucun** initialisateur dynamique : `nm` n'y montre ni
`__GLOBAL__sub_I_...` ni `___cxx_global_var_init`, et les 20000 octets (5000 × 4) sont dans
`__TEXT,__const`.

### Les limites de l'évaluateur

La norme ne fixe aucun plafond obligatoire, mais son annexe en recommande, et clang reprend ces
valeurs au chiffre près. Trouvées par bissection sur cette machine :

| Limite | Défaut | Option | Dernier cas qui passe |
|---|---|---|---|
| pas d'évaluation | 1048576 (2^20) | `-fconstexpr-steps=` | boucle de 1048571 tours |
| profondeur d'appels | 512 | `-fconstexpr-depth=` | 511 appels imbriqués |

La troisième ligne du tableau ci-dessous n'est pas une limite de l'évaluateur mais une limite
d'**affichage** du diagnostic : `-fconstexpr-backtrace-limit=`, 10 entrées par défaut. Elle change
ce que tu lis, pas ce que le compilateur sait calculer.

Les deux vraies limites se signalent par `constexpr evaluation hit maximum step limit` et
`constexpr evaluation exceeded maximum depth of 512 calls`. Le crible bute sur la limite de pas
vers `N = 4550` ; avec `-fconstexpr-steps=1000000000`, `N = 200000` passe en 9.4 s. Relever
`-fconstexpr-depth` est plus dangereux : l'évaluateur récursif consomme la pile du compilateur, et
sur cette machine (`ulimit -s` à 8176 Kio) une récursion de 1490 appels avec
`-fconstexpr-depth=1500` ne donne pas une erreur mais un plantage,
`unable to execute command: Illegal instruction: 4` ; 1000 tient, 1500 non. Une récursion profonde
se réécrit en boucle, elle ne se débloque pas par une option.

## À retenir

1. `constexpr` sur une **fonction** est une permission. Mesuré : à `-O0` `carre(7)` produit un vrai
   `bl`, et la constante à `-O2` vient de l'optimiseur, pas du mot-clé.
2. `constexpr` sur une **variable** est une exigence, et le seul des deux qui garantisse. Avec
   `static_assert`, ce sont les deux façons de **prouver** l'évaluation à la compilation.
3. `const` porte sur l'écriture, `constexpr` sur le moment. `const int b = lu();` est légal et
   n'est pas une constante ; tout `constexpr` est `const`, l'inverse est faux.
4. `consteval` interdit l'appel à l'exécution : la fonction n'est pas dans le binaire. C'est
   l'outil quand un repli à l'exécution serait un bug, pas un compromis.
5. `constinit` n'implique ni `const` ni constance des lectures : il garantit l'initialisation
   **avant** `main`. Mesuré, sans lui, inverser deux fichiers sur la ligne de liaison fait passer
   une globale de 42 à 0, sans un seul diagnostic.
6. En C++20 allouer dans une évaluation constante est permis si tout est libéré avant la fin — d'où
   `constexpr std::string` limité à 22 caractères sur cette libc++ dès qu'il doit survivre. `throw`
   rattrapé à la compilation reste du C++26, absent ici.
7. `std::is_constant_evaluated()` ne va que dans un `if` ordinaire ; dans un `if constexpr` elle
   vaut toujours `true` et tue la branche `else`, avec pour seule alarme `-Wconstant-evaluated`.
   `if consteval` ouvre un contexte de fonction immédiate — le corps d'une fonction `consteval`
   en est un aussi.

**Exercices : `17_constexpr`.**
