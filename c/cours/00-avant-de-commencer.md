# 00 — Avant de commencer

## Ce que fait vraiment un compilateur C

Tu écris du texte. Le processeur, lui, n'exécute que des nombres. Entre les deux, quatre étapes —
et savoir lesquelles t'évitera la moitié des erreurs incompréhensibles.

**1. Le préprocesseur.** Il ne comprend pas le C. Il fait du copier-coller : il remplace chaque
`#include` par le contenu du fichier, chaque `#define` par sa valeur, et supprime les blocs
`#if` non retenus. Tu peux voir son résultat :

```bash
cc -E mon_fichier.c | less
```

Fais-le une fois sur un fichier qui inclut `<stdio.h>`. Tu obtiendras des milliers de lignes. Ça
explique pourquoi une erreur dans un `#include` peut te donner un message qui parle d'un fichier
que tu n'as jamais ouvert.

**2. La compilation.** Le texte devient des instructions machine, un fichier `.o` par fichier
source. C'est là que se trouvent les erreurs de syntaxe et de type.

**3. L'édition de liens.** Les `.o` sont assemblés, et chaque appel de fonction est relié à sa
définition. C'est là que se trouve l'erreur la plus déroutante du débutant :

```
Undefined symbols for architecture arm64:
  "_ma_fonction", referenced from: _main
```

Traduction : « tu as **déclaré** cette fonction, tu l'as **appelée**, mais je n'en trouve nulle
part la **définition** ». Le compilateur était content ; c'est le linker qui se plaint.

**4. Le chargement.** Le système met le programme en mémoire et saute dans `main`.

## Compiler à la main, une fois

Avant d'utiliser `clings`, fais-le à la main. Crée `essai.c` :

```c
#include <stdio.h>

int main(void) {
    printf("salut\n");
    return 0;
}
```

Puis :

```bash
cc essai.c -o essai
./essai
```

C'est tout. `cc` est le compilateur, `-o essai` donne le nom du programme produit, `./essai` le
lance. Les options qu'on ajoutera :

| Option | Effet |
|---|---|
| `-std=c17` | fixe la version du langage, pour ne pas dépendre du compilateur |
| `-Wall -Wextra` | active les avertissements utiles |
| `-g` | garde les numéros de ligne, pour le débogueur et les sanitizers |
| `-O0` / `-O2` | pas d'optimisation / optimisation normale |
| `-fsanitize=address,undefined` | les deux détecteurs |

**`-Wall` ne veut pas dire « tous les avertissements ».** C'est une blague récurrente du métier :
il en active une bonne partie, `-Wextra` en ajoute, et il en reste encore. Traite les
avertissements comme des erreurs : en C, un avertissement est presque toujours un bug.

## Lire une erreur de compilation

```
essai.c:4:19: error: expected ';' after expression
    4 |     printf("salut\n")
      |                      ^
      |                      ;
```

Quatre informations, dans l'ordre : **le fichier**, **la ligne**, **la colonne**, **la cause**. Le
compilateur te montre même où insérer le point-virgule.

Deux règles pour survivre :

**Lis la PREMIÈRE erreur.** Une erreur de syntaxe déboussole le compilateur, qui produit ensuite
dix erreurs fantômes. Corrige la première, recompile, recommence. Chercher dans la dernière est la
perte de temps la plus classique.

**Une erreur qui parle d'une ligne parfaitement correcte vient souvent de la ligne d'avant.** Un
point-virgule manquant en fin de ligne 12 fait pointer l'erreur en ligne 13.

## Les sanitizers

C'est ce qui rend ce cours possible.

Un programme C fautif ne prévient pas. Il écrase un octet qui ne lui appartient pas, continue,
et plante quarante fonctions plus loin — ou pas du tout, ou seulement chez quelqu'un d'autre.
C'est ce qui rend le débogage du C réputé pénible.

Les sanitizers renversent ça. Compilé avec `-fsanitize=address`, ton programme garde une carte de
la mémoire valide et **vérifie chaque accès**. À la première faute, il s'arrête et raconte tout.

Essaie, maintenant :

```c
#include <stdlib.h>

int main(void) {
    int *nombres = malloc(4 * sizeof(int));
    nombres[4] = 1;
    free(nombres);
    return 0;
}
```

```bash
cc -g -fsanitize=address bug.c -o bug && ./bug
```

```
==12437==ERROR: AddressSanitizer: heap-buffer-overflow on address 0x602000000980
WRITE of size 4 at 0x602000000980 thread T0
    #0 0x000104c6c970 in main bug.c:5

0x602000000980 is located 0 bytes after 16-byte region [0x602000000970,0x602000000980)
allocated by thread T0 here:
    #0 0x000105515214 in malloc
    #1 0x000104c6c854 in main bug.c:4
```

Lis-le ligne par ligne, c'est toujours la même structure :

| Ligne | Ce qu'elle dit |
|---|---|
| `ERROR: ... heap-buffer-overflow` | **la nature** de la faute |
| `WRITE of size 4` | tu **écrivais** 4 octets (une lecture dirait `READ`) |
| `#0 ... in main bug.c:5` | **où** : fichier et ligne |
| `0 bytes after 16-byte region` | **de combien** tu dépasses, et la taille du bloc |
| `allocated by ... bug.c:4` | **où le bloc avait été alloué** |

Ce dernier point est ce qui change tout : tu ne sais pas seulement où ça casse, tu sais **de quel
bloc il s'agit**.

### Le vocabulaire d'AddressSanitizer

Tu vas croiser ces sept messages tout au long du cours. Les reconnaître, c'est déjà savoir quoi
chercher.

| Message | Ce que tu as fait |
|---|---|
| `heap-buffer-overflow` | tu dépasses les bornes d'un bloc `malloc` |
| `stack-buffer-overflow` | tu dépasses les bornes d'un tableau local |
| `global-buffer-overflow` | idem sur une variable globale |
| `heap-use-after-free` | tu utilises un bloc déjà rendu par `free` |
| `double-free` | tu rends deux fois le même bloc |
| `stack-use-after-return` | tu utilises l'adresse d'une variable locale d'une fonction terminée |
| `SEGV on unknown address 0x000...` | tu as déréférencé un pointeur nul |

### Et UndefinedBehaviorSanitizer

L'autre détecteur, `-fsanitize=undefined`, attrape ce qui n'est pas une faute de mémoire mais
reste indéfini : débordement d'entier signé, décalage trop grand, division par zéro, indice
manifestement hors bornes, déréférencement mal aligné.

Ses messages sont plus courts :

```
types2.c:6:14: runtime error: signed integer overflow: 100000 * 100000
cannot be represented in type 'int'
```

`clings` active les deux, et avec `-fno-sanitize-recover=undefined` : le programme **s'arrête**
au lieu de continuer, pour que la faute soit impossible à rater.

## Ce que les sanitizers ne voient pas

Il faut le dire tout de suite, sinon on leur fait trop confiance :

- **Ils ne voient que ce que tu exécutes.** Un chemin non parcouru n'est pas vérifié. Ce n'est pas
  une analyse du code, c'est une surveillance de l'exécution.
- **Ils ralentissent**, d'un facteur deux à trois, et consomment beaucoup plus de mémoire. On les
  active en développement, pas en production.
- **LeakSanitizer n'existe pas sur macOS ARM.** Les fuites de la section `07_memoire` sont donc
  comptées à la main. Sur Linux, `valgrind` reste la référence.
- **Ils ne rendent pas ton programme correct.** Un programme qui passe ASan peut être faux du
  début à la fin.

## Le comportement indéfini, et pourquoi c'est si grave

C'est le concept le plus important du C, et le plus mal compris.

Quand la norme dit qu'une construction a un **comportement indéfini**, elle ne dit pas « le
résultat est imprévisible ». Elle dit : **le compilateur a le droit de supposer que ça n'arrive
jamais.**

La différence est énorme. Si tu écris :

```c
int somme = a + b;
if (somme < a) {
    gerer_le_debordement();
}
```

Le compilateur raisonne ainsi : « le débordement signé est indéfini, donc il n'arrive pas, donc
`a + b` est toujours supérieur ou égal à `a`, donc ce `if` est toujours faux, donc je supprime le
bloc. » Ton code de gestion d'erreur **disparaît du programme compilé**. Ce n'est pas un bug du
compilateur : c'est ce que la norme l'autorise à faire.

C'est pour ça qu'en C, un bug n'est pas seulement « une valeur fausse » : c'est parfois du code
qui n'existe plus, ou une boucle infinie, ou un programme qui marche en `-O0` et casse en `-O2`.

**Retiens-en une chose** : quand un sanitizer signale un comportement indéfini, ce n'est jamais
« pas grave » et jamais « ça marche quand même ». Ça marche jusqu'à ce que tu changes de niveau
d'optimisation.

## En route

```bash
cd clings
make
./clings
```

Le premier exercice ne demande rien : ouvre le fichier, passe `PAS_FINI` à 0, sauvegarde. C'est la
boucle que tu répéteras quatre-vingt-quatorze fois.

Le deuxième ne compile pas, exprès. Le troisième explose, exprès. Tu viens de lire ce qu'il faut
pour les deux.
