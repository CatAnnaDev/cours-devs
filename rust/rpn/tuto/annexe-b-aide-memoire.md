# Annexe B — Aide-mémoire

Tout ce que le tutoriel utilise, sur une page. À garder ouvert pendant que tu
codes.

## Cargo

| Commande | Effet |
|---|---|
| `cargo new nom` | crée le projet, avec `git init` et `.gitignore` |
| `cargo check` | vérifie sans produire de binaire — le plus rapide |
| `cargo build` | binaire de debug dans `target/debug/` |
| `cargo build --release` | binaire optimisé dans `target/release/` |
| `cargo run -- args` | build puis lance ; le `--` sépare les options de cargo |
| `cargo test` | lance les tests |
| `cargo test nom` | lance les tests dont le nom contient `nom` |
| `cargo test -- --nocapture` | montre les `println!` des tests qui passent |
| `cargo fmt` | reformate tout au style officiel |
| `cargo fmt --check` | échoue si ce n'est pas formaté (pour la CI) |
| `cargo clippy --all-targets` | linter, tests inclus |
| `cargo doc --open` | génère et ouvre la doc du projet et de ses dépendances |
| `cargo add nom` | ajoute une dépendance |
| `cargo install --path .` | installe le binaire dans `~/.cargo/bin` |

## Types de base

| Type | Quoi |
|---|---|
| `i32`, `i64`, `u32`, `u64`, `usize` | entiers ; `usize` pour les tailles et index |
| `f32`, `f64` | flottants ; `f64` par défaut |
| `bool`, `char` | `char` est un caractère Unicode, 4 octets |
| `String` / `&str` | texte possédé / vue sur du texte |
| `Vec<T>` / `&[T]` | tableau possédé / vue sur une suite contiguë |
| `Option<T>` | `Some(v)` ou `None` |
| `Result<T, E>` | `Ok(v)` ou `Err(e)` |
| `Cow<'a, str>` | soit possédé, soit emprunté (chapitre 17) |

Règle de signature : **prends `&str` et `&[T]`, rends `String` et `Vec<T>`.**

## Possession et emprunt

| Écriture | Sens |
|---|---|
| `T` | je prends la possession, l'appelant perd la valeur |
| `&T` | je lis ; autant de lecteurs simultanés qu'on veut |
| `&mut T` | je modifie ; un seul à la fois, et aucun `&T` pendant ce temps |
| `mut x` | cette variable peut être réaffectée ou modifiée |

Les types `Copy` (nombres, `bool`, `char`, et les tuples de `Copy`) sont recopiés
au lieu d'être déplacés.

Un emprunt vit jusqu'à sa **dernière utilisation**, pas jusqu'à la fin du bloc.

## `Option` et `Result`

| Méthode | Sur | Effet |
|---|---|---|
| `.unwrap()` | les deux | sort la valeur, panique sinon |
| `.expect("msg")` | les deux | pareil, avec ton message |
| `.expect_err("msg")` | `Result` | sort l'erreur, panique si c'est `Ok` |
| `.ok()` | `Result` | `Result<T, E>` → `Option<T>` |
| `.is_ok()`, `.is_err()` | `Result` | test booléen |
| `.is_some()`, `.is_none()` | `Option` | test booléen |
| `.map(f)` | les deux | transforme la valeur, laisse l'échec tel quel |
| `.filter(f)` | `Option` | garde `Some` seulement si `f` est vrai |
| `.copied()` | `Option<&T>` | → `Option<T>` quand `T: Copy` |
| `.ok_or(e)` | `Option` | → `Result`, avec cette erreur si `None` |
| `?` | les deux | sort la valeur, ou quitte la fonction avec l'échec |

## `Vec` et tranches

| Méthode | Effet |
|---|---|
| `push(x)` / `pop()` | ajoute au bout / retire le bout (rend `Option<T>`) |
| `last()` / `first()` | regarde sans retirer (rend `Option<&T>`) |
| `len()` / `is_empty()` | taille |
| `clear()` | vide, **garde la capacité allouée** |
| `truncate(n)` | ne garde que les `n` premiers |
| `swap(i, j)` | échange deux éléments sur place |
| `reserve(n)` | prépare la place pour `n` de plus |
| `extend_from_slice(&s)` | ajoute tout le contenu d'une tranche |
| `drain(..)` | parcourt en vidant |
| `to_vec()` | copie possédée d'une tranche |
| `rotate_left(n)` | décale circulairement |
| `&v[a..b]` | sous-tranche |

## Itérateurs

| Méthode | Effet |
|---|---|
| `.iter()` | itère sur des `&T` |
| `.enumerate()` | ajoute l'index : `(i, x)` |
| `.map(f)` | transforme chaque élément |
| `.filter(f)` | garde ceux qui passent |
| `.fold(init, f)` | réduit à une seule valeur |
| `.sum()` / `.product()` | somme / produit |
| `.min()` / `.max()` / `.count()` | agrégats |
| `.collect::<Vec<_>>()` | rassemble dans une collection |
| `.any(f)` / `.all(f)` | tests |
| `(1..=n)` / `(0..n)` | intervalle inclusif / exclusif |

## `str` et `String`

| Méthode | Effet |
|---|---|
| `split_whitespace()` | découpe en mots, tous séparateurs confondus |
| `trim()` | enlève les espaces aux deux bouts |
| `parse::<T>()` | analyse en `T`, rend un `Result` |
| `chars()` / `bytes()` | itère par caractère / par octet |
| `len()` | **en octets**, pas en caractères |
| `chars().count()` | en caractères |
| `push(c)` / `push_str(s)` | ajoute à une `String` |
| `to_string()` / `String::from(s)` | crée une `String` |
| `format!("…")` | construit une `String` |

## `f64`

`abs` `sqrt` `powf` `powi` `exp` `ln` `log10` `log2` `sin` `cos` `tan` `asin`
`acos` `atan` `floor` `ceil` `round` `trunc` `fract` `min` `max` `rem_euclid`
`is_finite` `is_nan` `to_degrees` `to_radians`

Constantes : `std::f64::consts::{PI, E, TAU, SQRT_2, LN_2}`.

Pièges : `^` est le ou-exclusif, pas la puissance. `%` garde le signe du
dividende, `rem_euclid` non. `0.1 + 0.2 != 0.3`.

## Affichage

| Écriture | Effet |
|---|---|
| `{}` | trait `Display`, pour un humain |
| `{:?}` / `{:#?}` | trait `Debug` / version indentée |
| `{nom}` | interpole la variable `nom` |
| `{:<6}` `{:>6}` `{:^6}` | aligné à gauche / droite / centré sur 6 |
| `{:.2}` | deux décimales |
| `{:e}` | notation scientifique |
| `println!` / `print!` | vers `stdout`, avec / sans saut de ligne |
| `eprintln!` | vers `stderr` |
| `write!` / `writeln!` | dans un `Formatter`, une `String`, un fichier |
| `format!` | rend une `String` |

`print!` sans `\n` demande un `flush()` explicite pour être visible.

## Structures de contrôle

```rust
match valeur {
    Motif1 => …,
    Motif2 | Motif3 => …,
    x if condition => …,
    autre => …,
    _ => …,
}

if let Some(x) = option { … }
while let Some(x) = iterateur.next() { … }
loop { … break; }
for x in collection { … }
```

`match`, `if`, et les blocs `{}` sont des **expressions** : ils rendent une valeur.
La dernière expression sans point-virgule est la valeur de retour.

`matches!(v, Motif)` rend un `bool`.

## Structures, énumérations, traits

```rust
#[derive(Debug, Clone, Copy, PartialEq, Eq, Default)]
pub struct S { champ: T }

pub enum E {
    Vide,
    Tuple(T),
    Nommee { a: T, b: U },
}

impl S {
    pub fn new() -> Self { … }        // fonction associée : S::new()
    pub fn lire(&self) -> &T { … }    // méthode : s.lire()
    pub fn ecrire(&mut self) { … }
    pub fn consommer(self) { … }
}

impl fmt::Display for S {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        write!(f, "…")
    }
}
```

| `derive` | Donne |
|---|---|
| `Debug` | `{:?}` — à mettre partout |
| `Clone` | `.clone()` |
| `Copy` | copie implicite au lieu du déplacement (petits types seulement) |
| `PartialEq` | `==` — nécessaire pour `assert_eq!` |
| `Eq` | égalité totale — impossible avec des `f64` |
| `Default` | `T::default()` |

## Modules et visibilité

```rust
mod eval;              // charge src/eval.rs — sans ça, le fichier est ignoré
use crate::eval::Engine;
use std::io::Write;
```

| Préfixe | Sens |
|---|---|
| `crate::` | racine du projet |
| `super::` | module parent |
| `self::` | module courant |
| rien | `std` ou un paquet externe |

| Visibilité | Portée |
|---|---|
| rien | ce module seulement |
| `pub(crate)` | tout le projet |
| `pub` | l'extérieur, si c'est une bibliothèque |

## Tests

```rust
#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn nom_explicite() {
        assert_eq!(gauche, droite);
        assert_ne!(a, b);
        assert!(condition, "message {valeur}");
    }
}
```

`#[cfg(test)]` : compilé uniquement pour les tests. `use super::*` donne accès aux
éléments privés du module parent. Les tests tournent **en parallèle**.

## Entrées / sorties et environnement

```rust
std::env::args()                    // arguments, le premier est le programme
std::env::var_os("NO_COLOR")        // variable d'environnement
std::io::stdin().read_line(&mut s)  // ajoute à s, rend le nombre d'octets lus
std::io::stdout().flush()           // vide le tampon (trait Write)
std::io::stdout().is_terminal()     // trait IsTerminal
std::process::exit(1)               // code de sortie : 0 = succès
std::fs::read_to_string(chemin)     // lit un fichier entier
```

`read_line` rend `Ok(0)` en fin d'entrée (Ctrl-D) et **concatène** au lieu
d'écraser.

## Couleurs ANSI

`\x1b[` + code + `m`, refermé par `\x1b[0m`.

| Code | Effet | | Code | Effet |
|---|---|---|---|---|
| `0` | réinitialise | | `31` | rouge |
| `1` | gras | | `32` | vert |
| `2` | atténué | | `33` | jaune |
| `4` | souligné | | `34` | bleu |
| `7` | inversé | | `36` | cyan |

Ne colore que si `stdout().is_terminal()` **et** que `NO_COLOR` n'existe pas. Ne
mesure jamais une chaîne déjà colorée : aligne d'abord, colore ensuite.

## Performance (chapitres 16 et 17)

| Outil | Usage |
|---|---|
| `std::hint::black_box(x)` | empêche le compilateur de supprimer une mesure |
| `std::time::Instant::now()` | chronomètre ; prendre le **minimum**, pas la moyenne |
| `cfg!(debug_assertions)` | savoir si on est en debug |
| `#[global_allocator]` | compter les allocations |
| `#[inline]` | conseil d'inlining, sur les petites fonctions |
| `std::mem::swap(&mut a, &mut b)` | échange sans copier le contenu |
| `Vec::new()` | **n'alloue pas** tant qu'on ne pousse rien |
| `clear()` | vide sans rendre la mémoire |
| `const N: bool` en générique | branche résolue à la compilation |

Dans `Cargo.toml` : `lto = "fat"`, `codegen-units = 1`, `strip = true`.

---

**Retour au [sommaire](README.md).**
