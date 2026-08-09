# 06 — Des modules et une struct

`main.rs` fait déjà 80 lignes et mélange trois métiers : la définition des
erreurs, le calcul, l'affichage. On range, et on en profite pour transformer la
pile nue en un vrai objet.

## Découper en modules

Un **module** est une unité de nom et de visibilité. Un fichier `.rs` dans `src/`
devient un module dès qu'on le déclare.

Crée `src/eval.rs` et `src/ops.rs`, puis en tête de `src/main.rs` :

```rust
mod eval;
mod ops;
```

`mod eval;` se lit : « il existe un module `eval`, va chercher son contenu dans
`src/eval.rs` ». **Sans cette ligne, le fichier n'est pas compilé du tout** — il
est simplement ignoré. C'est l'erreur numéro un des débutants : « j'ai créé le
fichier mais Rust ne le voit pas ».

Contrairement à C, il n'y a **pas de `#include`, pas de fichiers d'en-tête, pas
d'ordre de compilation à gérer**. L'arbre des modules est déclaré, et le
compilateur se débrouille.

### Le découpage retenu

| Fichier | Responsabilité |
|---------|----------------|
| `src/eval.rs` | la pile, la boucle d'évaluation, les erreurs |
| `src/ops.rs` | le dictionnaire des opérateurs |
| `src/main.rs` | l'interface : arguments, affichage, REPL |

Le critère n'est pas « faire des fichiers de taille égale », c'est : **quand je
change une chose, combien de fichiers dois-je ouvrir ?** Ajouter un opérateur ne
touche que `ops.rs`. Changer la couleur d'un message ne touche que `main.rs`.
C'est le signe d'un bon découpage.

## La visibilité

En Rust, **tout est privé par défaut**, y compris entre modules d'un même projet.
C'est l'inverse de la plupart des langages, et c'est délibéré : ce qui est public
est un contrat qu'on s'engage à ne pas casser, donc ça doit être un choix.

Trois niveaux nous suffisent :

| Déclaration | Visible depuis |
|---|---|
| `fn parse_number(…)` | ce module seulement |
| `pub(crate) fn stack_mut(…)` | tout le projet, et rien d'autre |
| `pub fn eval_line(…)` | l'extérieur, si c'était une bibliothèque |

`pub(crate)` est le niveau le plus utile et le plus sous-employé. Il dit : « les
autres modules du projet en ont besoin, mais ce n'est pas de l'API publique ».
On s'en servira pour `stack_mut` : `ops.rs` doit pouvoir modifier la pile, sans
que ça devienne une promesse faite au monde entier.

## De la pile nue à une `struct`

Aujourd'hui on trimballe `stack: &mut Vec<f64>` de fonction en fonction. Trois
raisons d'en faire un type :

1. **Le nom porte du sens.** `Engine` dit ce que c'est ; `Vec<f64>` dit comment
   c'est fait. On pourra changer le comment sans toucher les appelants.
2. **On peut protéger l'invariant.** Le champ `stack` sera privé : personne ne
   peut le vider par accident depuis `main.rs`.
3. **On aura besoin d'ajouter des choses.** Un jour, des variables nommées, un
   historique. Avec une struct, ça ne change aucune signature.

```rust
#[derive(Debug, Default)]
pub struct Engine {
    stack: Vec<f64>,
}
```

`Default` fournit `Engine::default()`, qui construit chaque champ avec sa valeur
par défaut — pour un `Vec`, le vecteur vide. Il ne coûte rien et beaucoup de code
générique le réclame.

## Le bloc `impl`

```rust
impl Engine {
    pub fn new() -> Self {
        Engine { stack: Vec::new() }
    }

    pub fn stack(&self) -> &[f64] {
        &self.stack
    }

    pub(crate) fn stack_mut(&mut self) -> &mut Vec<f64> {
        &mut self.stack
    }
}
```

Rust sépare les **données** (`struct`) du **comportement** (`impl`). Il n'y a pas
de classe, pas d'héritage. On peut écrire plusieurs blocs `impl` pour le même
type, et même dans plusieurs fichiers.

### `new` n'est pas un mot-clé

`new` est une simple fonction, nommée par convention. Ce qui la distingue, c'est
qu'elle n'a **pas** de paramètre `self` : c'est une *fonction associée*, appelée
`Engine::new()` et non `engine.new()`. C'est l'équivalent d'une méthode statique.

`Self` (majuscule) désigne le type dans lequel on est. Écrire `-> Self` plutôt
que `-> Engine` évite d'avoir à renommer si le type change de nom.

### Les trois formes de `self`

| Receveur | Sens |
|---|---|
| `fn stack(&self)` | je lis |
| `fn stack_mut(&mut self)` | je modifie |
| `fn into_stack(self)` | je consomme : l'objet n'existe plus après |

Ce sont exactement les trois formes d'emprunt du chapitre 03, appliquées au
receveur. On n'utilisera que les deux premières.

### Pourquoi `stack()` rend `&[f64]` et pas `&Vec<f64>` ?

C'est un réflexe à prendre.

```rust
pub fn stack(&self) -> &[f64] {
    &self.stack
}
```

`&[f64]` est une **tranche** : un pointeur plus une longueur, une vue en lecture
seule sur une suite contiguë de `f64`. `&Vec<f64>` révèle en plus que c'est un
`Vec` — donc que ça a une capacité, que ça peut grandir — alors que l'appelant
n'a aucun droit là-dessus de toute façon.

Trois avantages :

- **Moins de promesses.** On pourrait un jour stocker la pile dans un tableau
  fixe sans casser un seul appelant.
- **Plus d'appelants possibles.** Une fonction qui prend `&[f64]` accepte un
  `Vec`, un tableau `[f64; 3]`, ou une sous-partie d'un autre tableau. Une
  fonction qui prend `&Vec<f64>` n'accepte qu'un `Vec`.
- **C'est la convention.** Idem pour `&str` plutôt que `&String`.

Le `&self.stack` se convertit tout seul : c'est une *coercion de déréférencement*,
Rust sait passer de `&Vec<T>` à `&[T]` sans qu'on écrive quoi que ce soit.

## `src/eval.rs` à la fin du chapitre

```rust
use std::fmt;

use crate::ops;

#[derive(Debug, Default)]
pub struct Engine {
    stack: Vec<f64>,
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum EvalError {
    NeedsOperands { op: String, need: usize, got: usize },
    Unknown(String),
    DivByZero,
    Domain(String),
}

impl Engine {
    pub fn new() -> Self {
        Engine { stack: Vec::new() }
    }

    pub fn stack(&self) -> &[f64] {
        &self.stack
    }

    pub(crate) fn stack_mut(&mut self) -> &mut Vec<f64> {
        &mut self.stack
    }

    pub fn eval_line(&mut self, line: &str) -> Result<(), EvalError> {
        for token in line.split_whitespace() {
            self.eval_token(token)?;
        }
        Ok(())
    }

    fn eval_token(&mut self, token: &str) -> Result<(), EvalError> {
        match parse_number(token) {
            Some(number) => {
                self.stack.push(number);
                Ok(())
            }
            None => ops::apply(self, token),
        }
    }

    pub(crate) fn pop(&mut self, op: &str) -> Result<f64, EvalError> {
        match self.stack.pop() {
            Some(x) => Ok(x),
            None => Err(EvalError::NeedsOperands {
                op: op.to_string(),
                need: 1,
                got: 0,
            }),
        }
    }

    pub(crate) fn pop2(&mut self, op: &str) -> Result<(f64, f64), EvalError> {
        let len = self.stack.len();
        if len < 2 {
            return Err(EvalError::NeedsOperands {
                op: op.to_string(),
                need: 2,
                got: len,
            });
        }
        let a = self.stack[len - 2];
        let b = self.stack[len - 1];
        self.stack.truncate(len - 2);
        Ok((a, b))
    }
}

fn parse_number(token: &str) -> Option<f64> {
    match token.parse::<f64>() {
        Ok(number) if number.is_finite() => Some(number),
        _ => None,
    }
}

impl fmt::Display for EvalError {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        match self {
            EvalError::NeedsOperands { op, need, got } => {
                let s = if *need > 1 { "s" } else { "" };
                write!(f, "`{op}` attend {need} opérande{s} mais n'en a que {got}")
            }
            EvalError::Unknown(token) => write!(f, "jeton inconnu : `{token}`"),
            EvalError::DivByZero => write!(f, "division par zéro"),
            EvalError::Domain(op) => write!(f, "`{op}` : opération hors domaine"),
        }
    }
}

impl std::error::Error for EvalError {}
```

### `parse_number` et la garde de `match`

```rust
match token.parse::<f64>() {
    Ok(number) if number.is_finite() => Some(number),
    _ => None,
}
```

Le `if number.is_finite()` est une **garde de motif** : la branche n'est prise que
si la condition est vraie ; sinon on continue vers les branches suivantes.

Pourquoi c'est nécessaire ? Parce que `"inf".parse::<f64>()` **réussit** et rend
l'infini. `"NaN"` aussi. Sans la garde, `inf 1 +` donnerait `inf` et `NaN NaN +`
donnerait `NaN`, et l'utilisateur se retrouverait avec une pile empoisonnée sans
comprendre d'où ça vient. Avec la garde, `parse_number` rend `None`, donc `inf`
part chez `ops::apply` qui répondra « jeton inconnu ». Message clair, pile propre.

C'est le genre de détail à trois caractères qui distingue un programme qui marche
d'un programme fiable.

### Pourquoi `Option` et pas directement le `Result` de `parse` ?

Parce qu'ici, « ce n'est pas un nombre » **n'est pas une erreur** : c'est le cas
normal d'un opérateur. `Option` dit « il y a une valeur ou il n'y en a pas »,
`Result` dit « ça a marché ou ça a échoué ». Choisir le bon des deux, c'est
choisir ce que le lecteur va comprendre.

## `src/ops.rs` de transition

```rust
use crate::eval::{Engine, EvalError};

pub fn apply(engine: &mut Engine, token: &str) -> Result<(), EvalError> {
    match token {
        "+" | "-" | "*" | "/" => {
            let (a, b) = engine.pop2(token)?;
            let result = match token {
                "+" => a + b,
                "-" => a - b,
                "*" => a * b,
                _ => {
                    if b == 0.0 {
                        return Err(EvalError::DivByZero);
                    }
                    a / b
                }
            };
            engine.stack_mut().push(result);
            Ok(())
        }
        other => Err(EvalError::Unknown(other.to_string())),
    }
}
```

Le double `match` est toujours là — c'est le sujet du chapitre 07.

À ce stade le compilateur signale que `pop` et la variante `Domain` ne servent à
rien. C'est normal : ils seront utilisés au chapitre 07. Les avertissements
disparaîtront d'eux-mêmes.

## `src/main.rs`

```rust
mod eval;
mod ops;

use eval::Engine;

fn main() {
    let mut engine = Engine::new();

    match engine.eval_line("3 4 + 2 *") {
        Ok(()) => println!("{:?}", engine.stack().last()),
        Err(error) => eprintln!("erreur : {error}"),
    }
}
```

## `use`, `crate::`, et les chemins

```rust
use crate::eval::{Engine, EvalError};
```

`use` n'importe rien au sens de C : **il crée juste un raccourci de nom**. Sans
lui, on écrirait `crate::eval::Engine` à chaque fois, ce qui est parfaitement
valide, juste bavard.

Les préfixes de chemin :

- `crate::` — depuis la racine de *ce* projet.
- `super::` — le module parent.
- `self::` — le module courant.
- rien du tout — un paquet externe, ou la bibliothèque standard (`std::fmt`).

Deux modules peuvent se référencer mutuellement (`eval` utilise `ops`, `ops`
utilise `eval`) sans aucune déclaration anticipée. Rust n'a pas ce problème.

## Les dépendances circulaires ne sont pas un défaut ?

Ici, non : `Engine` et `apply` forment un couple cohérent. Mais remarque comment
on a organisé le flux — `ops.rs` ne touche jamais au champ `stack` directement,
il passe par `pop`, `pop2` et `stack_mut`. C'est cette petite discipline qui fait
que `Engine` reste maître de ses données.

---

**Chapitre suivant :** [07 — Le dictionnaire d'opérateurs](07-le-dictionnaire-d-operateurs.md)
