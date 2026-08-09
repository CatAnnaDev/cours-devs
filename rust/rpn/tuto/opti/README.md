# blap_opti — la version optimisée

Le programme du tutoriel, réécrit pour la vitesse. Même comportement, mêmes
commandes, mêmes messages : seule l'implémentation change.

Ce dossier est un projet Cargo **indépendant** de celui de la racine. Il n'est pas
compilé quand tu construis `blap`, et le code de `../../src` ne dépend pas de lui.

Les deux chapitres qui expliquent tout ce qui est ici :

- [16 — Mesurer avant d'optimiser](../16-mesurer-avant-d-optimiser.md)
- [17 — La grosse passe d'optimisation](../17-la-grosse-passe-d-optimisation.md)

## Lancer

```bash
cd tuto/opti

cargo test                              # 20 tests
cargo run --release -- "3 4 + 2 *"      # comme blap
cargo run --release -- -t "3 4 + 2 *"

cargo run --release --example bench     # temps, naif contre optimise
cargo run --release --example allocs    # allocations, naif contre optimise
```

Les deux exemples doivent tourner **en release**. En debug les chiffres ne
veulent rien dire, et le programme te le dit.

## Ce qu'il y a dedans

| Fichier | Rôle |
|---|---|
| `src/lib.rs` | la bibliothèque, pour que les exemples puissent l'importer |
| `src/eval.rs` | moteur, erreurs empruntées, `Trace` en arène indexée, `Num` |
| `src/ops.rs` | dictionnaire des opérateurs, identique au fond |
| `src/style.rs` | couleurs rendues en `Cow<str>` |
| `src/main.rs` | interface, sortie verrouillée et tamponnée |
| `src/naive.rs` | **copie** de la version du chapitre 15, uniquement pour la comparaison |
| `examples/bench.rs` | banc d'essai en temps |
| `examples/allocs.rs` | compteur d'allocations via un allocateur global |

`src/naive.rs` n'est utilisé par aucun chemin du programme : il n'existe que pour
que les deux implémentations soient mesurées dans le même binaire, sur la même
machine, au même instant. C'est la seule façon honnête de comparer.

## Résultats

Machine : Apple Silicon, `rustc` 1.99, profil release (`lto = "fat"`,
`codegen-units = 1`).

| Mesure | naïf | optimisé | gain |
|---|---:|---:|---:|
| évaluation d'une ligne | 110 ns | 95 ns | ×1,2 |
| ligne qui échoue | 52 ns | 29 ns | ×1,8 |
| trace + rendu d'une ligne | 1150 ns | 356 ns | ×3,2 |
| formatage d'une pile de 8 | 217 ns | 106 ns | ×2,0 |
| invite complète | 355 ns | 101 ns | ×3,5 |

| Allocations en régime établi | naïf | optimisé |
|---|---:|---:|
| 6 lignes évaluées | 6 | 0 |
| 2 lignes en erreur | 4 | 0 |
| 6 lignes tracées et rendues | 367 | 0 |
| 10 formatages d'une pile | 100 | 0 |

Tes chiffres seront différents. Ce sont les **rapports** qui comptent, pas les
valeurs absolues.
