# 02 — La notation RPN

Avant d'écrire une ligne de code, il faut comprendre ce qu'on construit. Ce
chapitre ne contient pas de Rust : prends un papier.

## Le problème de la notation habituelle

Tu écris `2 + 3 * 4`. Combien ça fait ?

`14`, parce qu'on a appris que `*` est prioritaire sur `+`. Mais cette règle est
une **convention arbitraire** que l'ordinateur doit connaître. Et dès qu'on veut
l'autre résultat, il faut inventer un deuxième mécanisme : les parenthèses,
`(2 + 3) * 4`.

Un programme qui évalue `2 + 3 * 4` doit donc :

1. découper le texte en jetons,
2. construire un arbre en tenant compte des priorités,
3. gérer les parenthèses imbriquées,
4. gérer l'associativité (`2 ^ 3 ^ 2` se lit de droite à gauche, pas `-` ni `/`),
5. seulement ensuite, calculer.

C'est faisable — ça s'appelle un *parseur* — mais c'est déjà un vrai projet.

## La notation polonaise inverse

En RPN, on écrit les opérandes **avant** l'opérateur :

| Notation habituelle | RPN |
|---------------------|-----|
| `3 + 4` | `3 4 +` |
| `2 + 3 * 4` | `2 3 4 * +` |
| `(2 + 3) * 4` | `2 3 + 4 *` |
| `(5 + (1 + 2) * 4) - 3` | `5 1 2 + 4 * + 3 -` |

Regarde bien la troisième et la quatrième ligne : **il n'y a plus une seule
parenthèse**, et pourtant les deux expressions sont différentes et non ambiguës.
Il n'y a plus non plus de priorités : l'ordre d'écriture *est* l'ordre de calcul.

C'est le format des calculatrices HP depuis 1968, et du langage Forth. Ce n'est
pas une curiosité : c'est aussi ce que produisent les compilateurs en interne, et
c'est exactement le format d'exécution de la machine virtuelle Java ou du
bytecode Python.

## Comment on l'évalue : une pile

Une **pile** (*stack*) est une liste où l'on n'ajoute et ne retire qu'à un seul
bout, appelé le sommet. Deux opérations :

- `push` : poser une valeur sur le sommet,
- `pop` : retirer la valeur du sommet.

C'est du **LIFO** — *last in, first out*, le dernier arrivé est le premier servi.
Comme une pile d'assiettes.

L'algorithme d'évaluation RPN tient en trois lignes :

> Pour chaque jeton de gauche à droite :
> - si c'est un nombre, `push`
> - si c'est un opérateur, `pop` ce qu'il lui faut d'opérandes, calcule, `push` le résultat
>
> À la fin, le résultat est au sommet de la pile.

C'est tout. Pas d'arbre, pas de récursion, pas de priorités.

## Déroule-le à la main

Sur `5 1 2 + 4 * + 3 -`, la pile est écrite de gauche (fond) à droite (sommet) :

| Jeton | Action | Pile après |
|-------|--------|------------|
| `5` | push 5 | `[5]` |
| `1` | push 1 | `[5 1]` |
| `2` | push 2 | `[5 1 2]` |
| `+` | pop 2 et 1, push 1+2 | `[5 3]` |
| `4` | push 4 | `[5 3 4]` |
| `*` | pop 4 et 3, push 3×4 | `[5 12]` |
| `+` | pop 12 et 5, push 5+12 | `[17]` |
| `3` | push 3 | `[17 3]` |
| `-` | pop 3 et 17, push 17−3 | `[14]` |

Résultat : `14`.

## Le piège de l'ordre

Pour `+` et `*` l'ordre est sans importance. Pour `-` et `/`, il est capital.

Quand on dépile `10 3 -`, on récupère `3` **en premier** (c'est le sommet), puis
`10`. Mais le calcul attendu est `10 - 3`, pas `3 - 10`.

Donc : **le premier dépilé est l'opérande de droite.** On le nommera `b`, et le
second dépilé `a`, pour calculer `a - b`. Retiens-le, c'est la source d'erreur
numéro un dans une calculatrice RPN, et on écrira un test exprès pour ça.

## Les cas qui font mal

Un programme sérieux doit répondre proprement à ces situations :

| Entrée | Problème |
|--------|----------|
| `3 +` | il manque un opérande |
| `+` | il n'en manque même deux |
| `1 0 /` | division par zéro |
| `-1 sqrt` | hors du domaine de définition |
| `3 4 bidule` | jeton inconnu |
| `3 4 + oups *` | l'erreur arrive **au milieu** de la ligne |

Le dernier est le plus intéressant. À ce stade, `3 4 +` a déjà été exécuté et la
pile vaut `[7]` : l'utilisateur voit son calcul à moitié appliqué. On décidera au
chapitre 09 que c'est inacceptable, et on rendra chaque ligne **atomique** :
elle réussit entièrement, ou elle ne change rien du tout.

## Ce qu'on veut à l'arrivée

```bash
$ blap "3 4 + 2 *"
14
```

et un mode interactif où la pile survit d'une ligne à l'autre :

```
$ blap
rpn › 3 4
  = 4
[3 4] › +
  = 7
[7] › dup *
  = 49
[49] › q
```

Ce `[3 4]` affiché dans l'invite, c'est le grand luxe de la RPN : **on voit
l'état de la machine en permanence.**

---

**Chapitre suivant :** [03 — La pile en Rust](03-la-pile-en-rust.md)
