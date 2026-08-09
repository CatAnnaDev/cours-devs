# De la source au programme

Ce qui se passe entre ton fichier texte et un processus qui tourne. Valable pour C, C++, Rust,
Go ; les langages à machine virtuelle ajoutent une étape.

## Les quatre étapes

**1. Le préprocesseur** (C, C++). Il ne comprend pas le langage : il fait du copier-coller. Chaque
`#include` est remplacé par le contenu du fichier, chaque `#define` par sa valeur, chaque bloc
`#if` non retenu disparaît.

```bash
cc -E fichier.c | wc -l      # souvent 30 000 lignes pour un fichier de 20
```

Ça explique les erreurs qui parlent d'un fichier que tu n'as jamais ouvert.

**2. La compilation.** Le texte devient des instructions machine, un fichier objet par fichier
source. C'est ici que vivent les erreurs de syntaxe et de type. Chaque fichier est compilé
**indépendamment** : le compilateur ne sait rien des autres.

**3. L'édition de liens.** Les objets sont assemblés, et chaque symbole utilisé est relié à sa
définition. C'est ici qu'apparaît :

```
Undefined symbols: "_ma_fonction", referenced from: _main
```

qui veut dire : « tu l'as déclarée, tu l'as appelée, je n'en trouve pas la définition ». Le
compilateur était content ; c'est le linker qui se plaint.

Et son symétrique, `duplicate symbol`, quand deux fichiers définissent le même nom — souvent une
fonction non `static` dans un `.h`.

**4. Le chargement.** Le système met le programme en mémoire, résout les bibliothèques dynamiques,
et saute dans `main`.

## Déclaration et définition

C'est la distinction qui explique les fichiers d'en-tête.

```c
int carre(int n);            // déclaration : « ça existe, voilà sa forme »
int carre(int n) { ... }     // définition : « voilà ce que ça fait »
```

Le compilateur a besoin de la **déclaration** pour vérifier les appels. Le linker a besoin de la
**définition**, une seule fois dans tout le programme.

D'où l'organisation :

| Fichier | Contient |
|---|---|
| `.h` / `.hpp` | déclarations, types, macros, fonctions `inline` |
| `.c` / `.cpp` | définitions |

Et la garde d'inclusion, sans laquelle un en-tête inclus deux fois redéfinit ses types :

```c
#ifndef MON_ENTETE_H
#define MON_ENTETE_H
...
#endif
```

`#pragma once` fait la même chose en une ligne, et marche sur tous les compilateurs modernes.

## Ce que fait l'optimiseur

Bien plus que ce qu'on imagine, et c'est pourquoi optimiser à la main est souvent inutile.

| Optimisation | Ce qu'elle fait |
|---|---|
| propagation de constantes | `int x = 2 * 3;` devient `6` |
| élimination de code mort | ce qui n'est jamais utilisé disparaît |
| inlining | le corps d'une petite fonction remplace l'appel |
| déroulage de boucle | quatre itérations écrites au lieu d'une boucle |
| vectorisation | quatre additions en une instruction SIMD |
| hoisting | un calcul invariant sorti de la boucle |
| réordonnancement | les instructions déplacées pour occuper le processeur |

Conséquences pratiques :

**Ne fais pas ces optimisations à la main.** Écrire `x >> 1` au lieu de `x / 2` n'apporte rien et
se lit moins bien. Le compilateur le fait, et mieux que toi sur l'architecture cible.

**`-O0` et `-O2` sont deux programmes différents.** Un bug qui n'apparaît qu'en `-O2` est presque
toujours un comportement indéfini — l'optimiseur a le droit de supposer qu'il n'arrive pas, et il
en profite.

**Ton benchmark peut être supprimé.** Voir `mesurer.md`.

Regarder ce que le compilateur produit est plus instructif qu'on ne croit : `cc -S -O2` donne
l'assembleur, et Compiler Explorer le montre côte à côte avec la source.

## Les niveaux d'optimisation

| Niveau | Usage |
|---|---|
| `-O0` | développement, débogage — le code correspond à la source |
| `-O1`, `-O2` | production. `-O2` est le défaut raisonnable |
| `-O3` | plus agressif, parfois plus lent (code plus gros, cache d'instructions) |
| `-Os` | optimise la taille — souvent bon pour l'embarqué |
| `-Ofast` | autorise à casser la norme sur les flottants. **À éviter** sauf raison précise |

Et deux options qui changent la donne :

**`-march=native`** compile pour **ta** machine : le binaire est plus rapide et ne tournera pas
ailleurs.

**LTO** (`-flto`) laisse l'optimiseur travailler à travers les fichiers, ce qui rétablit l'inlining
entre unités de compilation. Compilation plus lente, binaire plus rapide.

## Statique ou dynamique

| | Statique (`.a`, `.lib`) | Dynamique (`.so`, `.dll`, `.dylib`) |
|---|---|---|
| le code est | copié dans ton binaire | chargé au lancement |
| taille du binaire | grosse | petite |
| mise à jour de la bibliothèque | recompiler | remplacer le fichier |
| démarrage | rapide | résolution des symboles |
| déploiement | un seul fichier | il faut livrer les dépendances |

L'ennui du dynamique est bien connu : la bibliothèque doit être présente, dans la bonne version, à
un endroit trouvable. C'est pour ça que les jeux et les outils livrent de plus en plus en
statique.

## Les langages à machine virtuelle

Java et C# ajoutent une étape : la source devient du **bytecode**, portable, et c'est un compilateur
**JIT** qui le traduit en instructions machine à l'exécution.

Trois conséquences :

**Le code démarre lentement puis accélère.** Le JIT compile ce qui est chaud, après quelques
milliers d'exécutions. D'où l'échauffement obligatoire dans les benchmarks.

**Il peut optimiser mieux qu'un compilateur statique**, parce qu'il connaît le comportement réel :
quelles branches sont prises, quels types passent vraiment.

**Il peut se dé-optimiser.** Une supposition invalidée fait revenir à l'interprétation, puis
recompiler. Ça produit des variations de performance déroutantes.

Rust, Go, C et C++ compilent directement en natif : pas d'échauffement, pas de JIT, pas de
surprise — et pas d'optimisation adaptative non plus.

## À retenir

1. Préprocesseur, compilation, liens, chargement. Chaque erreur a son étape.
2. `undefined symbol` = déclaré et appelé, jamais défini.
3. Déclarations dans le `.h`, définitions dans le `.c`, gardes d'inclusion partout.
4. L'optimiseur fait déjà les astuces que tu voulais écrire à la main.
5. Un bug qui n'apparaît qu'en `-O2` est presque toujours un comportement indéfini.
6. `-O2` par défaut, `-O0` pour déboguer, `-Ofast` jamais sans raison.
7. Sur machine virtuelle, le code accélère après échauffement.
