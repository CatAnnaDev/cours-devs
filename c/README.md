# c — apprendre le C par la mémoire

Le C n'est pas un langage compliqué : il a trente-deux mots-clés et tient dans une brochure. Ce
qui est compliqué, c'est **ce qu'il ne fait pas pour toi**. Il ne vérifie pas tes indices, ne
compte pas tes octets, ne libère rien, et ne dit jamais non.

Ce cours part de là. Presque tous les exercices tournent autour d'une seule question : **où sont
les octets, à qui appartiennent-ils, et jusqu'à quand ?**

## Deux dossiers

```
cours/      les leçons, à lire dans l'ordre
clings/     les exercices, à réparer
```

Une leçon, puis sa section d'exercices. Puis la suivante.

## Démarrer

```bash
cd clings
make
./clings
```

`make` compile le programme (un seul fichier C, aucune dépendance). `./clings` s'arrête sur le
premier exercice non terminé, affiche la consigne, compile ton fichier, le lance, et te dit ce qui
casse. Tu corriges, tu sauvegardes, **il relance tout seul**.

Il te faut : un compilateur C (`clang` ou `gcc`) et `make`. C'est tout.

- **macOS** : `xcode-select --install`
- **Debian / Ubuntu** : `sudo apt install build-essential`
- **Fedora** : `sudo dnf install gcc make`
- **Windows** : WSL, ou MSYS2 avec le paquet `mingw-w64-x86_64-toolchain`

Le compilateur utilisé est `cc` par défaut. Pour en imposer un autre :
`make CC=gcc` et `CLINGS_CC=gcc ./clings`.

## Ton éditeur

Les exercices écrivent `#include "verif.h"`, et le fichier est à la racine de `clings/`. Le runner
compile avec `-I.`, donc **ne change jamais ce chemin** : sinon `./clings verify` casse.

En revanche ton éditeur, lui, ne connaît pas cette option. Deux fichiers sont là pour le lui dire :

| Fichier | Pour qui |
|---|---|
| `compile_flags.txt` | fourni, suffit à clangd, ccls, et la plupart des éditeurs |
| `compile_commands.json` | plus complet, à générer avec `make compile_commands.json` |

Si `nvim` se plaint encore de `verif.h` introuvable, c'est que clangd a démarré à la racine du
dépôt et non dans `clings/`. Deux remèdes :

```bash
cd clings && nvim exercices/00_intro/intro1.c
```

ou génère la base complète, qui contient des chemins absolus et fonctionne d'où que tu l'ouvres :

```bash
make compile_commands.json
```

`compile_commands.json` est ignoré par git : il contient des chemins absolus propres à ta machine.

## Ce qui rend ce cours différent

Chaque exercice est compilé avec **AddressSanitizer et UndefinedBehaviorSanitizer**. Ce ne sont pas
des options exotiques : ce sont deux détecteurs intégrés au compilateur, qui instrumentent ton
programme et l'arrêtent **à l'instruction exacte** où il fait n'importe quoi.

Concrètement, quand tu écris un octet de trop :

```
==12437==ERROR: AddressSanitizer: heap-buffer-overflow on address 0x602000000980
WRITE of size 4 at 0x602000000980 thread T0
    #0 0x000104c6c970 in main mem1.c:12

0x602000000980 is located 0 bytes after 16-byte region [0x602000000970,0x602000000980)
allocated by thread T0 here:
    #0 0x000105515214 in malloc
    #1 0x000104c6c854 in main mem1.c:7
```

Le fichier, la ligne, la nature de la faute, **et l'endroit où le bloc avait été alloué**. Sans
sanitizer, le même bug se serait manifesté trois fonctions plus loin, ou pas du tout, ou seulement
sur la machine du client.

**Apprendre à lire ces rapports est une compétence à part entière**, et c'est la première leçon du
cours.

## Les commandes

```
./clings                  reprend au premier exercice non terminé
./clings list             où j'en suis
./clings run <id>         relancer un exercice précis
./clings hint <id>        un indice
./clings solution <id>    la correction
./clings reset <id>       remettre l'exercice dans son état d'origine
./clings verify           vérifier que toutes les solutions passent
./clings quiz             le questionnaire
./clings quiz list        combien de questions par section
```

Chaque exercice contient une ligne `const int PAS_FINI = 1;`. Elle a deux rôles : elle marque ta
progression, et elle t'oblige à **relire** ce que tu viens de corriger avant de passer à la suite.
Le programme te dit quand tout passe ; c'est toi qui décides que c'est fini.

## Le programme

| Section | Leçon | Contenu |
|---|---|---|
| `00_intro` | `cours/00-avant-de-commencer.md` | l'outil, lire une erreur du compilateur, lire un rapport de sanitizer |
| `01_types` | `cours/01-les-types.md` | tailles, débordement, signé et non signé, flottants, conversions |
| `02_controle` | `cours/02-le-controle.md` | if, boucles, switch et la chute, opérateurs bit à bit |
| `03_fonctions` | `cours/03-les-fonctions.md` | prototypes, passage par valeur, sortie par pointeur, pointeurs de fonction |
| `04_pointeurs` | `cours/04-les-pointeurs.md` | adresse, déréférencement, arithmétique, `const`, pointeur pendouillant |
| `05_tableaux` | `cours/05-les-tableaux.md` | indices, hors bornes, tableau contre pointeur, deux dimensions |
| `06_chaines` | `cours/06-les-chaines.md` | le zéro terminal, `strlen`, `strcpy` et ses pièges, `snprintf` |
| `07_memoire` | `cours/07-la-memoire.md` | `malloc`/`free`, fuite, double free, use-after-free, `realloc`, vecteur |

**46 exercices, 21 questions.** La suite du programme est en bas de ce fichier.

## Le questionnaire

Parce que le code peut marcher sans qu'on ait compris — c'est exactement pour ça que `PAS_FINI`
existe.

```
./clings quiz              toutes les questions
./clings quiz 07_memoire   seulement une section
```

Chaque réponse est suivie d'une explication, **que tu aies juste ou faux** : tomber juste par
élimination n'apprend rien.

## Une note sur les fuites de mémoire

LeakSanitizer, qui détecte les blocs jamais libérés, **n'existe pas sur macOS ARM**. Les
exercices de la section `07_memoire` comptent donc les blocs à la main, avec `suivi_malloc` et
`suivi_free` fournis par `verif.h`, et `VERIFIE_PAS_DE_FUITE()`.

Ce n'est pas un pis-aller : compter ses allocations est ce que font les vrais moteurs de jeu et
les vrais serveurs, précisément parce qu'on ne peut pas dépendre d'un outil qui n'existe pas
partout. Sur Linux, `valgrind --leak-check=full ./ton_programme` reste l'outil de référence.

## La suite

| Section | Contenu |
|---|---|
| `08_structs` | struct, padding, alignement, union, champs de bits, tableau de structs |
| `09_preprocesseur` | `#define`, macros et parenthèses, gardes d'inclusion, compilation conditionnelle |
| `10_modules` | compilation séparée, `extern`, `static`, écrire un Makefile |
| `11_ub` | les comportements indéfinis classiques, prouvés par UBSan |
| `12_allocateurs` | écrire son allocateur : bump, arène, liste libre, pool |
| `13_structures` | vecteur générique, table de hachage à adressage ouvert, liste chaînée |
| `14_fichiers` | `fopen`, lecture tamponnée, binaire contre texte, écriture atomique |
| `15_processus` | `fork`, `exec`, tubes, un mini shell |
| `16_reseau` | sockets TCP, un serveur qui tient la charge avec `kqueue` / `epoll` |
| `17_perf` | mesurer, cache, alignement, dérouler, ce que le compilateur fait déjà |
