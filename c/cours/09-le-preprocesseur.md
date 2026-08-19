# 09 — Le préprocesseur

## Une passe de texte, avant le compilateur

Avant que le compilateur ne voie une ligne de ton code, un autre programme passe dessus. Il ne fait
qu'une chose — **remplacer du texte par du texte** — sans rien comprendre : ni les types, ni les
portées, ni les blocs, ni les fonctions. Toute la difficulté du chapitre tient là : **une macro
n'obéit à aucune règle du C**, parce qu'elle s'applique avant que le C ne commence.

D'où l'outil numéro un, à sortir dès qu'une macro fait quelque chose d'inattendu :

```sh
cc -E fichier.c              # le texte tel que le compilateur va le lire
cc -E fichier.c | tail -40   # la fin du fichier, la partie qui t'intéresse
cc -dM -E - < /dev/null      # la liste de toutes les macros prédéfinies
```

La sortie est énorme, mais **ton** code est tout en bas, développé. Ne devine jamais : regarde.

## `#define` sans paramètres

```c
#define TAILLE 4
int tableau[TAILLE];         // int tableau[4];
#define LONGUEUR 4;          // le point-virgule fait partie du texte
int autre[LONGUEUR];         // int autre[4;];   -> erreur incompréhensible
int reste = LONGUEUR - 1;    // int reste = 4; - 1;
```

Le jeton devient du texte, point : pas une variable, pas de type, plus rien ensuite. **Une macro ne
se termine donc jamais par un point-virgule**, et l'erreur pointe la ligne d'utilisation.

```c
#define VIES_MAX 3           // aucun type, aucune portée, invisible au débogueur
#undef VIES_MAX              // les trois formes ci-dessous sont exclusives
enum { VIES_MAX = 3 };       // un vrai int, respecte les blocs, visible au débogueur
const int vies_max = 3;      // typé, adressable, mais pas une constante de compilation
```

La macro n'a **pas de portée** : elle vaut de sa ligne jusqu'au `#undef` ou à la fin du fichier, y
compris dans les fonctions et les en-têtes inclus ensuite — d'où les collisions et la convention
des MAJUSCULES. Elle est aussi **invisible au débogueur**. Le `enum` est le bon choix par défaut.

Le `const`, lui, a un piège propre au C : `const int taille = 4;` **n'est pas une constante de
compilation**, et `int tableau[taille];` donne un VLA. D'où le seul terrain où la macro garde un
avantage net, **la dimension d'un tableau** — plus `#if`, qui ne connaît ni `enum` ni `const`.

## Macros à paramètres : les deux règles de parenthèses

```c
#define DOUBLE(x) x * 2
DOUBLE(1 + 2)                // 1 + 2 * 2     -> 5, faux
#define SOMME(a, b) (a) + (b)
SOMME(1, 2) * 3              // (1) + (2) * 3 -> 7, faux
```

Le paramètre est substitué **tel quel**, la priorité s'appliquant après : d'où la **première règle,
chaque paramètre entre parenthèses**. Ça ne suffit pas — le second développement est juste, mais il
atterrit au milieu d'une expression qui le coupe. D'où la **seconde règle, l'expression entière**.

```c
#define DOUBLE(x)   ((x) * 2)
#define SOMME(a, b) ((a) + (b))
DOUBLE(1 + 2)                // ((1 + 2) * 2)   -> 6
SOMME(1, 2) * 3              // ((1) + (2)) * 3 -> 9
```

Les deux règles sont indépendantes, et il faut les deux. Toujours.

## La double évaluation

```c
#define MAX(a, b) ((a) > (b) ? (a) : (b))
int i = 5;
int m = MAX(i++, 3);         // ((i++) > (3) ? (i++) : (3))
```

Les parenthèses sont parfaites, et `i++` apparaît quand même **deux fois** : sur la branche prise
il s'exécute deux fois, `i` finit à 7 et `m` vaut 6. Même chose avec `MAX(calculer_score(), 3)` ou
avec `getchar()`, qui consomme deux caractères. **Aucun arrangement de parenthèses ne répare ça** :
le problème n'est pas la priorité, c'est que le texte de l'argument est écrit deux fois.

```c
static inline int max_entier(int a, int b) { return a > b ? a : b; }
```

**Depuis C99, `inline` est standard.** Le compilateur intègre le corps sur place à `-O2` comme
l'aurait fait la macro — même code machine, zéro appel — mais l'argument est évalué **une seule
fois**, les types sont vérifiés, et la fonction a un nom pour le débogueur. Une `static inline`
dans un `.h` est aussi rapide qu'une macro et infiniment plus sûre : le remplaçant par défaut.

## `do { ... } while (0)`

Une macro multi-instructions casse un `if` sans accolades, et les accolades seules ne suffisent
pas.

```c
#define ECHANGER(a, b) int t = a; a = b; b = t;
if (desordre)                //  if (desordre)
    ECHANGER(x, y);          //      int t = x;    <- seule ligne soumise au if
                             //  x = y; y = t;     <- toujours exécutées

#define ECHANGER(a, b) { int t = a; a = b; b = t; }
if (desordre)                //  if (desordre)
    ECHANGER(x, y);          //      { int t = x; x = y; y = t; };
else                         //  else  <- error: 'else' without a previous 'if'
    signaler();
```

Le point-virgule que tu écris **après l'appel** devient une instruction vide qui termine le `if`,
et le `else` se retrouve orphelin. La solution est un idiome, et il n'y en a pas d'autre :

```c
#define ECHANGER(a, b) \
    do { int t = (a); (a) = (b); (b) = t; } while (0)
```

`do { ... } while (0)` est **une seule instruction** qui **exige** un point-virgule derrière. Celui
de l'appel devient celui de la boucle : plus rien ne traîne, le `else` retombe sur ses pieds, et la
macro s'utilise comme un appel de fonction, la boucle étant supprimée sans effort. Et **pas** de
point-virgule après le `while (0)` de la définition : c'est l'appel qui le fournit.

## `#` et `##`

`#` transforme un paramètre en chaîne littérale, `##` colle deux jetons pour n'en former qu'un.

```c
#define AFFICHER(expression) printf(#expression " = %d\n", (expression))
AFFICHER(2 + 3);             // printf("2 + 3" " = %d\n", (2 + 3));
```

Le piège classique : **`#` et `##` ne développent pas leur argument**. Le remède est un second
idiome, **la macro intermédiaire**, dont la traversée force le développement avant qu'ils le
voient.

```c
#define VERSION 17
#define CHAINE_BRUTE(x) #x
#define CHAINE(x) CHAINE_BRUTE(x)
CHAINE_BRUTE(VERSION)        // "VERSION"
CHAINE(VERSION)              // "17"
#define COLLER_BRUT(a, b) a##b
#define COLLER(a, b) COLLER_BRUT(a, b)
int COLLER_BRUT(temporaire_, __LINE__);   // int temporaire___LINE__;  -> faux
int COLLER(temporaire_, __LINE__);        // int temporaire_42;        -> juste
```

Retiens la forme : **une macro `_BRUT` qui fait le travail, une macro publique qui l'appelle**.

## Les gardes d'inclusion

`#include "vecteur.h"` copie le fichier à l'endroit de la directive, littéralement. Si `joueur.h`
et `monde.h` l'incluent tous les deux et qu'un `.c` inclut les deux, son contenu arrive **deux
fois**. Une déclaration de fonction supporte la répétition, et depuis C11 un `typedef` identique
aussi ; redéfinir une `struct` ou un `enum`, ou définir deux fois un objet, reste une erreur.

```c
#ifndef VECTEUR_H
#define VECTEUR_H
typedef struct { float x, y; } Vecteur;
Vecteur vecteur_ajouter(Vecteur a, Vecteur b);
#endif
```

Au premier passage `VECTEUR_H` n'existe pas : le contenu est gardé et la macro définie. Au second,
tout disparaît jusqu'au `#endif`. Le nom doit être **unique dans tout le projet**.

L'alternative, non standard mais comprise partout depuis vingt ans : `#pragma once`. Sa limite est
qu'il identifie les fichiers par leur emplacement réel — périphérique et inœud, ce qui traverse
correctement les liens symboliques, mais **pas les copies** : le même en-tête présent en deux
exemplaires, ou vu à travers certains montages réseau, compte pour deux. Les gardes classiques
portent sur un nom de macro et sont immunisées.

## Compilation conditionnelle

```c
#if defined(__APPLE__)
#  include <mach/mach_time.h>
#elif defined(__linux__) || defined(__unix__)
#  include <time.h>
#else
#  error "plateforme non prise en charge"
#endif
```

`#ifdef X` abrège `#if defined(X)` et `#ifndef X` abrège `#if !defined(X)` ; seul `defined` sait
combiner plusieurs conditions, d'où sa préférence dès qu'il y a un `&&`. `#error` arrête la
compilation plutôt que de laisser passer un cas non prévu. Piège : dans un `#if`, **tout
identifiant inconnu vaut 0** — une faute de frappe désactive la branche en silence, d'où `-Wundef`.

| Nom | Ce que c'est |
|---|---|
| `__FILE__` | le nom du fichier source, sous forme de chaîne |
| `__LINE__` | le numéro de ligne, sous forme d'entier |
| `__STDC_VERSION__` | `201710L` en C17, `201112L` en C11 |
| `NDEBUG` | non définie par le langage : c'est **toi** qui la définis |

`__func__` figure dans la liste habituelle mais **n'est pas une macro** : c'est un identifiant
prédéfini, un tableau `static const char[]` déclaré au début de chaque fonction. Conséquence :
`printf("dans " __FILE__ "\n")` compile, `printf("dans " __func__ "\n")` non — il faut `%s`.

`assert` est l'utilisateur le plus connu de tout ça : si `NDEBUG` est définie **avant** l'inclusion
de `<assert.h>`, il devient une macro vide et la vérification disparaît, ce que fait `-DNDEBUG` en
production. Donc jamais d'effet de bord dedans, ni de validation d'entrée utilisateur.

## Quand ne pas écrire une macro

C'est la question à se poser en premier, et la réponse est presque toujours « oui, ne l'écris pas
».

| Ce que tu veux | Ce qu'il faut écrire |
|---|---|
| un petit calcul rapide | `static inline` dans le `.h` |
| une constante entière | `enum { VIES_MAX = 3 };` |
| une constante typée, adressable | `static const float GRAVITE = 9.81f;` |
| choisir selon le type de l'argument | `_Generic` |

`_Generic` (C11) fait la sélection sur type : la macro reste, mais c'est le langage qui choisit :

```c
#define valeur_absolue(x) _Generic((x), float: fabsf, double: fabs, default: abs)(x)
```

Restent **deux choses que seule une macro sait faire**. La première est de **capturer le contexte
de l'appelant** : `__FILE__` et `__LINE__` développés dans une fonction donnent la ligne de cette
fonction, pas de l'appel. C'est ce que fait le `verif.h` de ce cours, et il n'y a pas d'autre voie
:

```c
#define VERIFIE(condition, message) \
    verif_resultat((condition) ? 1 : 0, message, __FILE__, __LINE__)
```

La seconde est de **générer du code** : une fonction ne peut ni déclarer un `enum` ni construire un
tableau de noms. Une macro, si — c'est l'idiome de la « X-macro » :

```c
#define LISTE_COULEURS(X)  X(ROUGE) X(VERT) X(BLEU)
#define DECLARER(nom) nom,
typedef enum { LISTE_COULEURS(DECLARER) COULEUR_NOMBRE } Couleur;
#undef DECLARER
#define NOMMER(nom) #nom,
static const char *const couleur_noms[] = { LISTE_COULEURS(NOMMER) };
#undef NOMMER
```

Ajouter une couleur, c'est ajouter **un jeton**, et tout reste synchronisé. Note les `#undef` :
une macro d'aide qui a fini son travail se range.

## À retenir

1. Le préprocesseur substitue du texte, sans connaître les types ni les portées ; `cc -E` montre le
   résultat exact, il n'y a rien à deviner.
2. Une macro ne finit jamais par un point-virgule ; pour une constante, préfère `enum` ou `const`.
3. Deux jeux de parenthèses : autour de chaque paramètre, et autour de l'expression entière.
4. La double évaluation ne se répare pas avec des parenthèses : écris une `static inline`.
5. Une macro multi-instructions s'enveloppe dans `do { ... } while (0)`, sans point-virgule final.
6. `#` et `##` ne développent pas leur argument : passe par une macro intermédiaire.
7. Tout en-tête porte une garde d'inclusion, `#ifndef`/`#define`/`#endif` ou `#pragma once`.

**Exercices : `09_preprocesseur`.**
