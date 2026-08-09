# 01 — Avant de commencer

## Installer Rust

Une seule commande, sur macOS et Linux :

```bash
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
```

Sur Windows, télécharge `rustup-init.exe` depuis <https://rustup.rs>.

### Pourquoi `rustup` et pas le paquet de ma distribution ?

Tu pourrais faire `brew install rust` ou `apt install rustc`. Évite.

- Rust sort une nouvelle version **toutes les six semaines**. Les paquets système
  ont souvent un an de retard, et beaucoup de code moderne ne compilera pas.
- `rustup` installe la *chaîne d'outils* complète : le compilateur `rustc`, le
  gestionnaire de projet `cargo`, le formateur `rustfmt`, le linter `clippy`, la
  doc hors-ligne. Le paquet système en oublie souvent la moitié.
- `rustup` permet d'avoir plusieurs versions côte à côte et de basculer par
  projet. Un jour tu en auras besoin.

Vérifie que tout est là :

```bash
rustc --version
cargo --version
```

## Créer le projet

```bash
cargo new blap
cd blap
```

`cargo new` fabrique ceci :

```
blap/
├── Cargo.toml
├── .gitignore
└── src/
    └── main.rs
```

Trois remarques :

- **`cargo` a déjà fait `git init`** et écrit un `.gitignore` qui ignore `/target`.
  C'est exactement ce qu'on veut : `target/` contient les fichiers compilés, ça
  se régénère, ça n'a rien à faire dans un dépôt.
- **`src/` est obligatoire.** Cargo ne cherche le code nulle part ailleurs.
- **`src/main.rs` est le point d'entrée** d'un programme exécutable. Si le fichier
  s'appelait `src/lib.rs`, Cargo construirait une *bibliothèque* — du code destiné
  à être utilisé par d'autres programmes, sans `fn main`. Nous, on veut un binaire.

## Lire `Cargo.toml`

```toml
[package]
name = "blap"
version = "0.1.0"
edition = "2024"

[dependencies]
```

- `name` : le nom du paquet, et donc du binaire produit.
- `version` : au format *semver* (`majeur.mineur.correctif`).
- `edition` : **ce n'est pas une version du langage.** Rust ne casse jamais le
  code existant ; quand une évolution serait incompatible, elle est mise derrière
  une édition. Un projet en édition 2015 compile encore aujourd'hui avec le
  compilateur le plus récent. Les éditions peuvent se mélanger dans un même
  projet. Prends toujours la plus récente pour un nouveau projet.
- `[dependencies]` : vide, et **il va rester vide**. Tout ce projet tient avec la
  bibliothèque standard. C'est délibéré : on va comprendre ce qu'on écrit.

## Le premier programme

`src/main.rs` contient déjà :

```rust
fn main() {
    println!("Hello, world!");
}
```

```bash
cargo run
```

Décortiquons :

- `fn main()` — la fonction appelée au démarrage. Un binaire Rust en a exactement une.
- `println!` — le `!` signale une **macro**, pas une fonction. Une macro est du
  code qui écrit du code au moment de la compilation. `println!` doit être une
  macro parce qu'elle vérifie ton format *à la compilation* : si tu écris
  `println!("{} {}", x)` avec un seul argument, ça ne compile pas. En C, le même
  bug donne un plantage à l'exécution.

## Les quatre commandes que tu vas taper tous les jours

```bash
cargo check     # compile juste assez pour vérifier les erreurs — le plus rapide
cargo build     # produit le binaire de debug dans target/debug/
cargo run       # build + lance
cargo test      # lance les tests
```

`cargo check` est ton réflexe pendant que tu écris : il fait toute l'analyse (types,
emprunts, exhaustivité des `match`) sans générer le code machine, donc il est
plusieurs fois plus rapide que `build`.

### Debug et release

```bash
cargo build             # target/debug/blap   — compile vite, tourne lentement
cargo build --release   # target/release/blap — compile lentement, tourne vite
```

En mode debug, le compilateur n'optimise rien et garde les informations de
débogage. En mode release, il optimise à fond. L'écart peut être d'un facteur 10
à 100 sur du code de calcul. Règle simple : **debug pendant le développement,
release pour mesurer une performance ou distribuer le binaire.**

Un piège classique : « Rust est lent chez moi » veut dire neuf fois sur dix
« j'ai mesuré en debug ».

## Passer des arguments

```bash
cargo run -- "salut"
```

Le `--` sépare les options de `cargo` de celles de *ton* programme. Sans lui,
`cargo` croirait que `--trace` s'adresse à lui.

---

**Chapitre suivant :** [02 — La notation RPN](02-la-notation-rpn.md)
