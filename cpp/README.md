# cpp — le C++ moderne, et ce que ça coûte

Le C ne fait rien pour toi et tu le sais. Le C++ fait **énormément** pour toi, et c'est le
problème : tout a l'air gratuit. Une ligne innocente peut copier un mégaoctet, allouer trois fois,
ou incrémenter un compteur atomique à chaque tour de boucle.

Ce cours part de là. Chaque construction est présentée avec **son prix**, et la plupart des
exercices se terminent par une vérification du genre :

```
ok    la somme est correcte
RATE  aucune copie
      attendu 0, obtenu 3
```

Ce n'est pas de la micro-optimisation : c'est apprendre à lire le C++ pour ce qu'il fait vraiment,
et pas pour ce qu'il a l'air de faire.

## Deux dossiers

```
cours/       les leçons, à lire dans l'ordre
cpplings/    les exercices, à réparer
```

## Démarrer

```bash
cd cpplings
make
./cpplings
```

Il te faut un compilateur C++ récent et `make`. Le runner lui-même est en **C++17** pour compiler
partout ; les exercices sont en **C++20** (et C++23 pour les derniers).

- **macOS** : `xcode-select --install`
- **Debian / Ubuntu** : `sudo apt install build-essential` (GCC 12 ou plus)
- **Fedora** : `sudo dnf install gcc-c++ make`
- **Windows** : WSL, ou MSYS2 avec `mingw-w64-x86_64-toolchain`

Pour imposer un compilateur : `make CXX=g++` et `CPPLINGS_CXX=g++ ./cpplings`.

## Ton éditeur

Les exercices écrivent `#include "verif.hpp"`, à la racine de `cpplings/`. Le runner compile avec
`-I.` — **ne change pas ce chemin**, sinon `./cpplings verify` casse.

Ton éditeur, lui, a besoin qu'on le lui dise :

| Fichier | Pour qui |
|---|---|
| `compile_flags.txt` | fourni, suffit à clangd et à la plupart des éditeurs |
| `compile_commands.json` | plus complet, chemins absolus — `make compile_commands.json` |

Si clangd se plaint quand même, c'est qu'il a démarré à la racine du dépôt :
`cd cpplings && nvim exercices/...`, ou génère la base complète.

## La Sonde

`verif.hpp` fournit un type qui compte tout ce qui lui arrive :

```cpp
verif::Compteur::remettre_a_zero();
// ... ton code ...
VERIFIE_ENTIER(verif::Compteur::copies, 0, "aucune copie");
```

`verif::Sonde` incrémente un compteur dans son constructeur, son constructeur de copie, son
constructeur de déplacement et son destructeur. C'est l'outil pédagogique central du cours : au
lieu de te dire « ça copie », il te le **prouve**, et tu vois le compteur tomber à zéro quand tu
corriges.

C'est aussi ce qui remplace LeakSanitizer, absent sur macOS ARM : quand
`constructions != destructions`, quelque chose a fui.

## Les sanitizers

Chaque exercice est compilé avec **AddressSanitizer et UndefinedBehaviorSanitizer**. En C++ ils
attrapent des fautes que le C ne peut même pas produire :

| Message | Ce que tu as fait |
|---|---|
| `heap-use-after-free` | gardé un pointeur à travers un `push_back` qui a réalloué |
| `container-overflow` | indexé au-delà de `size()` — libc++ annote ses conteneurs |
| `stack-use-after-return` | une lambda a capturé une locale **par référence** et lui a survécu |
| `double-free` | copie superficielle d'une classe qui possède un bloc brut |

Les quatre arrivent réellement dans les exercices, et c'est le but.

## Les commandes

```
./cpplings                  reprend au premier exercice non terminé
./cpplings list             où j'en suis
./cpplings run <id>         relancer un exercice précis
./cpplings hint <id>        un indice
./cpplings solution <id>    la correction
./cpplings reset <id>       remettre l'exercice dans son état d'origine
./cpplings verify           vérifier que toutes les solutions passent
./cpplings quiz             le questionnaire
./cpplings quiz list        combien de questions par section
```

Chaque exercice contient `const bool PAS_FINI = true;`. Elle marque ta progression et t'oblige à
relire ce que tu viens de corriger.

## Le programme

| Section | Leçon | Contenu |
|---|---|---|
| `00_intro` | `cours/00-avant-de-commencer.md` | l'outil, lire un mur d'erreurs de template, les sanitizers |
| `01_bases` | `cours/01-les-bases.md` | `auto`, références, `const`/`constexpr`, accolades, liaison structurée |
| `02_valeurs` | `cours/02-les-valeurs.md` | copie, référence, `std::move`, élision, paramètre puits |
| `03_raii` | `cours/03-raii.md` | destructeur, `unique_ptr`, `shared_ptr` et son compteur atomique, garde |
| `04_regle_zero` | `cours/04-la-regle-de-zero.md` | règles de 0, 3 et 5, déplacement à la main, `= delete` |
| `05_conteneurs` | `cours/05-les-conteneurs.md` | `vector`, invalidation, `emplace_back`, `string` et SSO, `map` |
| `06_algos` | `cours/06-les-algorithmes.md` | lambdas et captures, `<algorithm>`, ranges, `std::span` |
| `07_templates` | `cours/07-les-templates.md` | déduction, paramètres non-type, `if constexpr`, concepts, repli, `forward` |
| `08_erreurs` | `cours/08-les-erreurs.md` | le prix réel d'une exception, `noexcept`, `optional`, `expected` |
| `09_layout` | `cours/09-la-disposition-memoire.md` | alignement, ligne de cache, `[[no_unique_address]]`, faux partage, AoS/SoA |
| `10_polymorphisme` | `cours/10-le-polymorphisme.md` | virtuel et son coût, `override`, découpage, CRTP, effacement de type |
| `11_conteneur` | `cours/11-ecrire-son-conteneur.md` | stockage brut, placement `new`, croissance, garantie forte |
| `12_alloc` | `cours/12-les-allocateurs.md` | compter ce qui alloue, `pmr`, arène, propagation, `small_vector` |
| `13_threads` | `cours/13-les-threads.md` | course de données, `jthread`, `atomic`, modèle mémoire, contention |
| `14_coroutines` | `cours/14-les-coroutines.md` | `promise_type`, `co_yield`, le cadre, le piège des paramètres |

**83 exercices, 38 questions.**

## La suite

| Section | Contenu |
|---|---|
| `15_perf` | mesurer, lire l'assembleur, ce que le compilateur fait déjà pour toi |
