# 04 — Lire une ligne et la découper

On passe de « `3 4 +` codé en dur » à « une chaîne quelconque est évaluée ».

## Découper en jetons

```rust
fn main() {
    let line = "3 4 + 2 *";

    for token in line.split_whitespace() {
        println!("[{token}]");
    }
}
```

```
[3]
[4]
[+]
[2]
[*]
```

### Pourquoi `split_whitespace` et pas `split(' ')` ?

Essaie `line.split(' ')` avec l'entrée `"3   4 +"` (trois espaces) : tu obtiens
`["3", "", "", "4", "+"]`. Deux jetons vides à traiter.

`split_whitespace` :

- traite toute suite d'espaces comme **un seul** séparateur,
- accepte aussi les tabulations et les retours à la ligne,
- ignore les espaces en début et en fin de chaîne,
- connaît les espaces Unicode, pas seulement l'espace ASCII.

Bref, il fait exactement ce qu'un humain entend par « découper en mots ». Le
choix est gratuit et il supprime toute une classe de bugs.

Bonus : il ne renvoie pas de nouvelles chaînes. Chaque jeton est une **tranche**
qui pointe *dans* la chaîne d'origine — aucune copie, aucune allocation.

### `String` et `&str`

Deux types de texte en Rust, et c'est déroutant au début.

| | `String` | `&str` |
|---|---|---|
| Possède ses données | oui | non, c'est une vue |
| Redimensionnable | oui | non |
| Allouée sur le tas | oui | pointe où on lui dit |
| Analogie | `Vec<T>` | `&[T]` |

Un littéral `"3 4 +"` est un `&str` : il vit dans le binaire lui-même. Une chaîne
lue au clavier est une `String` : sa taille n'est pas connue à la compilation.

Règle de pouce, et elle vaut pour tout le projet :

> **Une fonction qui lit du texte prend `&str`. Une fonction qui doit garder le
> texte renvoie `String`.**

Prendre `&str` en paramètre accepte les deux formes (une `String` se convertit
automatiquement), donc c'est toujours le bon choix.

## Reconnaître un nombre

```rust
let n: f64 = "3.5".parse().unwrap();
let n = "3.5".parse::<f64>().unwrap();
```

Les deux lignes font la même chose. `parse` est **générique** : c'est le type
attendu qui décide de l'analyse. Comme on est parfois dans un contexte où Rust ne
peut pas le deviner, la syntaxe `::<f64>` (surnommée le *turbofish*) permet de le
préciser sur place.

`parse` ne rend pas un `f64` mais un **`Result<f64, ParseFloatError>`** : le texte
peut ne pas être un nombre.

```rust
enum Result<T, E> {
    Ok(T),
    Err(E),
}
```

Même idée qu'`Option`, mais l'échec porte une explication. En Rust il n'y a pas
d'exceptions : **une fonction qui peut échouer le déclare dans son type de
retour**, et l'appelant ne peut pas l'ignorer par accident. C'est plus verbeux
qu'un `try/catch`, et infiniment plus fiable : il n'existe pas de chemin d'échec
invisible.

Et c'est ce qui nous donne notre test « est-ce un nombre ? » gratuitement :

```rust
match token.parse::<f64>() {
    Ok(number) => stack.push(number),
    Err(_) => {}
}
```

La branche `Err` est le cas « ce n'est pas un nombre », donc : un opérateur.

Pas besoin d'écrire un analyseur : `parse::<f64>` gère déjà les décimaux, le
signe, et la notation scientifique (`-1.5e3`). Le `_` dans `Err(_)` veut dire
« il y a bien une erreur ici mais je ne m'en sers pas ».

## L'évaluateur, première version

```rust
fn main() {
    let line = "3 4 + 2 *";
    let mut stack: Vec<f64> = Vec::new();

    for token in line.split_whitespace() {
        match token.parse::<f64>() {
            Ok(number) => stack.push(number),
            Err(_) => {
                let b = stack.pop().unwrap();
                let a = stack.pop().unwrap();
                let result = match token {
                    "+" => a + b,
                    "-" => a - b,
                    "*" => a * b,
                    "/" => a / b,
                    _ => panic!("jeton inconnu : {token}"),
                };
                stack.push(result);
            }
        }
    }

    println!("{:?}", stack.last());
}
```

```bash
cargo run
```

Sortie : `Some(14.0)`. On a une calculatrice RPN fonctionnelle en vingt lignes.

Deux détails de syntaxe :

- **`match` sur des chaînes.** On peut faire correspondre directement des
  littéraux `&str`. Plus tard on écrira `"+" | "plus" =>` pour accepter plusieurs
  écritures d'une même opération.
- **`match` est une expression.** Il *rend* une valeur, donc on peut l'affecter à
  `result`. C'est le cas de presque tout en Rust : `if`, `match`, un bloc `{}`
  produisent une valeur. D'où l'absence de `return` en fin de fonction : la
  dernière expression sans point-virgule *est* la valeur de retour.

## Ce qui cloche déjà

Cette version marche sur les entrées correctes. Sur les autres :

```rust
let line = "3 +";
```

```
thread 'main' panicked at src/main.rs:10:32:
called `Option::unwrap()` on a `None` value
```

Et avec `let line = "3 4 bidule";` :

```
thread 'main' panicked at src/main.rs:16:26:
jeton inconnu : bidule
```

Dans les deux cas le programme **meurt**. Pour un outil interactif, c'est
rédhibitoire : une faute de frappe ne doit pas faire perdre la pile de
l'utilisateur.

### `panic!` : quand est-ce légitime ?

`panic!` arrête le programme (ou le thread) en déroulant la pile d'appels. La
règle communément admise :

> **`panic!` pour un bug du programmeur. `Result` pour une erreur de
> l'utilisateur ou du monde extérieur.**

Un index hors bornes est un bug : `panic!`. Un fichier absent, une entrée mal
tapée, une connexion coupée sont des événements normaux : `Result`.

Ici, « jeton inconnu » vient de l'utilisateur. C'est un `Result`. On corrige au
chapitre suivant.

## Un mot sur `stack.last()`

```rust
println!("{:?}", stack.last());
```

`last()` rend un `Option<&f64>` — une *référence* au dernier élément, sans le
retirer, et `None` si la pile est vide. C'est ce qu'on veut pour afficher : le
résultat doit rester sur la pile pour le calcul suivant.

Ne confonds pas :

- `pop()` — retire et rend la valeur,
- `last()` — regarde sans toucher.

---

**Chapitre suivant :** [05 — Des erreurs propres](05-des-erreurs-propres.md)
