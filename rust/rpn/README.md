# blap

Une calculatrice **RPN** (notation polonaise inverse) en ligne de commande, écrite en Rust,
sans aucune dépendance.

En RPN on tape les nombres *avant* l'opérateur : `3 4 +` donne `7`. Pas de parenthèses,
pas de priorités à retenir — une pile fait tout le travail.

## Lancer

```bash
cargo run -- "3 4 + 2 *"     # one-shot : affiche 14 et quitte
cargo run                    # mode interactif (REPL)
cargo build --release        # binaire dans target/release/blap
```

## Deux modes

**One-shot** — une expression, un résultat :

```bash
$ blap "10 2 / 3 +"
8
```

**Interactif** — la pile survit d'une ligne à l'autre, comme sur une vraie HP :

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

## Mode trace

Pour voir *ce qui se passe* à chaque jeton :

```bash
$ blap -t "5 1 2 + 4 * + 3 -"
  5      empile 5                 → [5]
  1      empile 1                 → [5 1]
  2      empile 2                 → [5 1 2]
  +      1 + 2 = 3                → [5 3]
  4      empile 4                 → [5 3 4]
  *      3 × 4 = 12               → [5 12]
  +      5 + 12 = 17              → [17]
  3      empile 3                 → [17 3]
  -      17 − 3 = 14              → [14]
14
```

Dans le REPL, tape `trace` pour l'activer ou la couper à la volée.

## Ce qu'elle sait faire

| Catégorie | Jetons |
|-----------|--------|
| Arithmétique | `+` `-` `*` `/` `%` (`mod`) `^` `**` `pow` |
| Fonctions | `neg` `abs` `inv` `sqrt` `exp` `ln` `log` `log2` `sin` `cos` `tan` `floor` `ceil` `round` `fact` (`!`) |
| Extrema | `min` `max` |
| Pile | `dup` `drop` `swap` `over` `rot` `clear` |
| Réductions | `sum` `prod` (sur toute la pile) |
| Constantes | `pi` `e` `tau` |
| Commandes | `?`/`help`, `q`/`quit`/`exit`, `trace` |

Les nombres acceptent décimaux, négatifs et notation scientifique (`-1.5e3`).
La trigo est en radians.

## Détails qui comptent

- **Évaluation tout ou rien** : si un jeton échoue au milieu d'une ligne, la pile
  est remise dans son état d'avant — pas de demi-calcul fantôme.
- **Garde-fous** : division et modulo par zéro, `sqrt`/`ln` hors domaine, `fact`
  au-delà de 170 (débordement `f64`) donnent un message clair, jamais un `NaN`.
- **Couleurs** coupées automatiquement si la sortie n'est pas un terminal, ou si
  la variable d'environnement `NO_COLOR` est définie.

## Tests

```bash
cargo test
```

## Architecture

- `src/eval.rs` — le moteur : la pile, l'évaluateur, les erreurs typées, l'affichage des nombres.
- `src/ops.rs` — le dictionnaire des opérateurs.
- `src/style.rs` — les couleurs du terminal.
- `src/main.rs` — l'interface : arguments, REPL, trace, aide.

## Tutoriel

Ce dépôt contient un tutoriel complet qui reconstruit ce projet depuis zéro,
étape par étape, pour quelqu'un qui débute en Rust : [`tuto/`](tuto/).
