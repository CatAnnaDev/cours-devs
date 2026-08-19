# 10 — Les modules : compilation séparée

Le C n'a pas de modules, pas d'`import`, pas de paquets. Il a un compilateur qui traite **un
fichier à la fois**, sans jamais voir les autres, et un éditeur de liens qui recolle les morceaux à
la fin en ne regardant que des **noms**. Tout le chapitre découle de ces deux phrases.

## Ce que fait vraiment `cc fichier.c`

Quatre étapes à la file, et chacune s'arrête sur demande. Quatre programmes autrefois ; clang
fait aujourd'hui les trois premières dans un seul processus, et n'appelle `ld` que pour la
dernière.

```sh
cc -E salut.c -o salut.i    # 1. préprocesseur     : du texte vers du texte
cc -S salut.c -o salut.s    # 2. compilation       : du C vers de l'assembleur
cc -c salut.c -o salut.o    # 3. assemblage        : de l'assembleur vers un objet binaire
cc salut.o    -o salut      # 4. édition de liens  : des objets vers un exécutable
```

### Ce que contient un `.o`

Les tailles disent l'essentiel : sur trois lignes de C, le `.i` fait **570 lignes** avec
`#include <stdio.h>` contre 10 sans, le `.o` **1008 octets** dont 104 de code, l'exécutable
**50040**. Un objet n'est pas du code prêt à courir : c'est du code **à trous**, plus les trous.

```
$ nm salut.o
0000000000000064 D _compteur_global
0000000000000000 T _doubler
                 U _printf
```

Deux choses fournies, une réclamée. `U` veut dire *undefined* : le fichier appelle `printf` sans
savoir où il est, et a laissé là une **relocation**, une note qui dit « ici, il manquera une
adresse ». Autres lettres : `T` du code, `D` une donnée initialisée, `S` une donnée à zéro sur
Mach-O (`B` sous ELF, et `b` ici quand elle est `static`), `C` une définition provisoire (plus
bas). **Majuscule : visible par l'éditeur de liens. Minuscule : non.**

## Déclarer, définir

**Une déclaration décrit, une définition fabrique.** Le compilateur a besoin de la première pour
vérifier un appel, l'éditeur de liens de la seconde pour trouver le code.

```c
int doubler(int valeur);                        // déclaration : promesse, aucun octet produit
int doubler(int valeur) { return valeur * 2; }  // définition  : le corps, donc du code
extern int niveau;                              // déclaration d'une variable
int niveau = 2;                                 // définition
```

La règle tient en une ligne : **autant de déclarations qu'on veut, une seule définition**. Répéter
trois fois `int doubler(int);` passe sans un mot ; définir deux fois donne `error: redefinition of
'doubler'`, et c'est le compilateur qui parle, tout se passant dans un seul fichier.

### Ce que l'éditeur de liens ne sait pas

Il ne connaît **que des noms** : pas les types, pas les paramètres, pas les tailles. Un `.o` qui
réclame `_moyenne` sera raccordé à n'importe quel `.o` qui fournit `_moyenne`.

```c
double moyenne(double a, double b) { return (a + b) / 2.0; }   // dans def.c
int moyenne(int a, int b);                                     // déclaré dans use.c
printf("%d\n", moyenne(10, 20));                               // affiche 10, pas 15
```

Ça compile, ça lie, ça tourne, et ça ment : les entiers partent dans des registres généraux, la
fonction va les chercher dans des registres flottants. Même faute sur une variable — `char petit =
7;` ici, `extern long long petit;` là — et le lien passe encore, mais ASan crie
`global-buffer-overflow ... 0 bytes after global variable 'petit' ... of size 1`. **Le compilateur
aurait attrapé les deux fautes** s'il avait vu les deux faces : la discipline des en-têtes est là.

## Lire les deux erreurs de l'éditeur de liens

Elles n'ont **jamais** de numéro de ligne, et c'est ce qui déroute. Texte exact de `ld`, macOS :

```
Undefined symbols for architecture arm64:
  "_calculer", referenced from:
      _main in principal-841c33.o
ld: symbol(s) not found for architecture arm64
```

**Il manque une définition.** Trois causes : le `.c` n'a pas été passé au lien, le nom est mal
orthographié, ou la fonction est `static` dans son fichier.

```
duplicate symbol '_calculer' in:
    /var/folders/.../a-3bdd03.o
    /var/folders/.../b-bb372b.o
ld: 1 duplicate symbols
```

**Il y a deux définitions.** Presque toujours : une définition écrite dans un `.h` inclus par deux
`.c`. Les chemins affichés sont temporaires si l'on compile d'un coup, mais désignent les fautifs.

## Les trois liaisons

La **liaison** répond à une question : deux occurrences d'un nom, en deux endroits, sont-elles la
même chose ?

| Liaison | Ce que ça vaut | Comment on l'obtient |
|---|---|---|
| **externe** | visible de tout le programme | le défaut, pour un objet ou une fonction en portée fichier |
| **interne** | visible de ce fichier seul | `static` en portée fichier |
| **aucune** | ce nom-ci, ici | une locale sans `extern`, un paramètre, et tout ce qui n'est ni objet ni fonction : `typedef`, étiquette de `struct`, constante d'`enum` |

`static` a deux sens sans rapport : sur une variable **locale**, « qui survit à la sortie de la
fonction » ; en **portée fichier**, sur une fonction ou une variable, « privé à ce fichier ».

```c
static int aide(int v) { return v + 1; }    // dans s1.c
static int aide(int v) { return v * 100; }  // dans s2.c : aucun conflit
```

Le programme lie sans broncher : `nm` montre `t _aide` en minuscule, le nom reste pour le débogueur
mais l'éditeur de liens ne le voit pas, et appeler ce `aide` d'ailleurs donne `Undefined symbols`.

**`static` est le bon défaut.** Ce qui n'est pas destiné à l'extérieur doit le porter : le nom
cesse d'entrer en collision, le compilateur peut intégrer ou supprimer la fonction, et ce qui reste
est l'interface. Un avertissement hors `-Wall -Wextra` traque les oublis :

```
$ cc -Wmissing-prototypes -c fichier.c
warning: no previous prototype for function 'aide_publique' [-Wmissing-prototypes]
note: declare 'static' if the function is not intended to be used outside of this translation unit
```

## `extern` : une variable définie ailleurs

Une variable globale partagée s'écrit en deux morceaux, et un seul est une définition.

```c
/* config.h */                      /* config.c */
#ifndef CONFIG_H                    #include "config.h"
#define CONFIG_H                    int niveau_journal = 2;
extern int niveau_journal;
#endif
```

`extern` dit « ce nom existe, de ce type, ailleurs » : aucun octet réservé. Chaque `.c` qui inclut
`config.h` obtient la déclaration, un seul porte la définition. Sur une **fonction** c'est
redondant, une déclaration de fonction étant déjà externe.

Une définition posée dans l'en-tête casse dès la deuxième inclusion, et la garde n'y peut rien :
elle protège de la double inclusion **dans un même fichier**, pas de deux fichiers différents. Deux
`.c`, deux définitions, `duplicate symbol`. Le cas vicieux est la même faute **sans initialiseur**,
`int niveau;` dans un `.h` : c'est une **définition provisoire** (*tentative definition*). Le
piège est qu'elle a l'air inoffensive : chaque unité qui la porte se retrouve en fait avec une
définition externe à part entière, la norme en exige **exactement une** dans tout le programme, et
le programme est donc indéfini sans qu'aucun diagnostic soit exigé. Que l'éditeur de liens les
fusionne n'est pas du C : c'est une extension répandue, listée à l'annexe J.5. Sur Apple clang 21
ça lie **sans un mot** — `-fcommon` y est encore le défaut, `nm` affiche le symbole en `C` — là où
`-fno-common` donne `duplicate symbol '_niveau'`. Écris `extern`, et ne parie sur aucun des deux.

## Ce qu'on met dans un `.h`

Un en-tête est **l'interface publique** d'un `.c` : ce qui peut être répété y a sa place, rien de
plus.

| Dans le `.h` | Dans le `.c` |
|---|---|
| déclarations des fonctions publiques | **définitions** de ces fonctions |
| `struct`, `union`, `enum`, `typedef` | les fonctions `static` d'aide |
| `#define` et macros de l'interface | les variables `static` de fichier |
| `extern` sur les variables partagées | **la** définition de ces variables |
| `static inline` courtes | tout le reste |

Le cas `static inline` porte un piège : écrire `static` dans un en-tête ne partage rien, **chaque
fichier qui l'inclut reçoit sa propre copie**, avec ses propres variables.

```c
/* compteur.h */
static int total = 0;
static inline void ajouter(int v) { total += v; }
static inline int lire_total(void) { return total; }
```

`f1.c` appelle `ajouter(10)`, `f2.c` appelle `ajouter(1)`, et le programme affiche `f1 : 10, f2 :
1` : il n'y a pas un `total` mais deux, un par unité de traduction. Parfait pour une fonction pure
de trois lignes, bug silencieux pour un état partagé.

**C'est le fonctionnement exact du `verif.h` de ce cours** : tout y est `static`, donc chaque
fichier qui l'inclut a **ses propres** compteurs. D'où la règle du runner — seul le fichier
principal inclut `verif.h` et appelle les macros ; un fichier annexe qui les appellerait
incrémenterait des compteurs que personne ne lit. Note au passage ce que `verif.h` ne fait **pas**
: ses fonctions sont `static` toutes nues, et une `static` non `inline` inutilisée déclenche
`warning: unused function` partout où l'on ne s'en sert pas. Le runner l'éteint avec
`-Wno-unused-function` ; dans un en-tête ordinaire, la bonne réponse est `static inline`, que le
compilateur ne signale jamais.

## Chaque `.c` inclut son propre `.h`

La première ligne de chaque `.c` est la plus rentable du chapitre : `#include "pile.h"` en tête de
`pile.c`. Sans elle, déclaration et définition vivent dans deux fichiers que rien ne confronte :
elles dérivent, et l'éditeur de liens aveugle recolle. Avec elle, le compilateur voit les deux dans
la **même unité de traduction**, et la moindre divergence devient `error: conflicting types for
'moyenne'`. Ça prouve aussi l'en-tête **autosuffisant** : s'il oublie un `#include <stddef.h>`, le
premier averti est son propre `.c`, pas la victime qui l'inclura dans six mois.

## Le type opaque

Déclarer une structure **sans la définir** est légal : l'en-tête annonce le nom, le `.c` seul
connaît les champs. C'est l'encapsulation du C, et la seule dont il dispose.

```c
/* pile.h */
#include <stddef.h>                   /* pour size_t : un .h se suffit a lui-meme */

typedef struct Pile Pile;             /* type incomplet : le nom, rien de plus */
Pile *pile_creer(size_t capacite);
void pile_detruire(Pile *pile);
int pile_empiler(Pile *pile, int valeur);
size_t pile_taille(const Pile *pile);

/* pile.c */
struct Pile { int *donnees; size_t taille; size_t capacite; };
```

Un pointeur vers un type incomplet reste utilisable ; c'est la taille qui devient inaccessible.

| Ce qu'on tente | Message de clang |
|---|---|
| `Pile p;` | `tentative definition has type 'Pile' ... that is never completed` |
| `sizeof(Pile)` | `invalid application of 'sizeof' to an incomplete type 'Pile'` |
| `pile->taille` | `incomplete definition of type 'Pile'` |

**Ce que ça coûte** : la structure ne vit plus sur la pile de l'appelant ni dans un tableau, donc
`pile_creer` doit allouer et chaque champ passe par une fonction. **Ce que ça apporte** : les
champs changent sans recompiler un client, les invariants ne sont plus violables du dehors, et
l'en-tête devient une vraie interface. `sqlite3` et `png_struct` sont exactement ça. `FILE` vise le
même but par une autre voie : la norme n'en fixe que le contrat, on ne touche jamais ses champs,
mais le type, lui, est complet — `sizeof(FILE)` compile et vaut 152 sur macOS.

## Le Makefile

`make` ne connaît qu'une idée : **si la cible est plus ancienne qu'un de ses prérequis, la commande
tourne**, sinon rien. Le reste est de la notation. La commande est précédée d'une **tabulation**.

```make
CC       ?= cc
CFLAGS   ?= -std=c17 -O2 -g -Wall -Wextra
CPPFLAGS ?= -Isrc
LDLIBS   ?=
CIBLE    := pile
SOURCES  := $(wildcard src/*.c)
OBJETS   := $(SOURCES:src/%.c=build/%.o)
DEPS     := $(OBJETS:.o=.d)
$(CIBLE): $(OBJETS)
	$(CC) $^ $(LDLIBS) -o $@
build/%.o: src/%.c | build
	$(CC) $(CPPFLAGS) $(CFLAGS) -MMD -MP -c $< -o $@
build:
	mkdir -p $@
clean:
	rm -rf build $(CIBLE)
.PHONY: clean
-include $(DEPS)
```

Les automatiques : `$@` la cible, `$<` le premier prérequis, `$^` tous les prérequis, `%` le joker
de la règle de motif. `?=` n'affecte que si la variable n'est pas déjà définie, ce qui laisse
passer un `CC` venu de l'environnement ; `make CC=gcc` l'emporte de toute façon, parce qu'une
affectation en ligne de commande bat n'importe quelle affectation du fichier. Le `| build` est un
prérequis **d'ordre seulement** : le dossier doit exister avant, mais sa date ne déclenche rien —
sans la barre, chaque fichier déposé dedans périmerait tous les objets. `.PHONY` dit que `clean`
est un verbe, pas un fichier.

### Pourquoi le suivi des en-têtes est indispensable

Une règle qui ne liste que le `.c` ignore les en-têtes. Change `enum { TAILLE = 4 };` en `8` dans
`compte.h`, lance `make`, réponse : `` make: `prog' is up to date. `` Rien n'est recompilé. Pire :
le jour où **un seul** des deux fichiers finit par être reconstruit, les deux moitiés du programme
ne s'accordent plus sur la taille du tableau, et ça donne `AddressSanitizer: stack-buffer-overflow`
entre `somme` et `main` — un bug sans faute dans le code, deux sources justes compilées à deux
époques. Le remède est `-MMD -MP`, qui fait écrire au compilateur, en même temps que le `.o`, un
fichier `.d` portant les prérequis réels :

```make
build/principal.o: src/principal.c src/pile.h src/journal.h
src/pile.h:
src/journal.h:
```

Ce bloc est simplifié : Apple clang ajoute aussi le `SDKSettings.json` du SDK en prérequis, et
coupe la règle sur plusieurs lignes par des contre-obliques. Le fond ne change pas.

La première règle est celle que `-include $(DEPS)` réinjecte dans `make` : toucher `pile.h`
recompile désormais tout ce qui l'inclut, et **rien d'autre**. Les suivantes, sans commande,
viennent de `-MP` ; ce sont des cibles factices, et sans elles renommer un en-tête casse tout, le
vieux `.d` réclamant `` make: *** No rule to make target `vieux.h', needed by `a.o'.  Stop. ``

Enfin `-MMD` plutôt que `-MD` : le second liste aussi les en-têtes système, et le `.d` d'un fichier
incluant `<stdio.h>` et `<string.h>` passe ici de **4 lignes à 96**. Le `make` livré avec macOS est
**GNU Make 3.81** : `$(wildcard)`, `-include`, `%` et les prérequis d'ordre y fonctionnent tous.

## À retenir

1. `cc` enchaîne quatre étapes ; `-E`, `-S` et `-c` s'arrêtent après chacune des trois premières.
2. Une déclaration décrit, une définition fabrique : une seule définition dans tout le programme.
3. L'éditeur de liens ne connaît que des noms : `Undefined symbols` s'il manque une définition,
   `duplicate symbol` s'il y en a deux, et jamais de numéro de ligne.
4. `static` en portée fichier veut dire « privé à ce fichier » : le défaut, hors interface.
5. Une variable partagée s'écrit `extern` dans le `.h` et se définit dans un seul `.c`, jamais
   dans un en-tête, même sans initialiseur.
6. Ce qui est `static` dans un en-tête est copié par unité de traduction, variables comprises.
7. Chaque `.c` inclut son propre `.h`, et le Makefile suit les en-têtes avec `-MMD -MP`.

**Exercices : `10_modules`.**
