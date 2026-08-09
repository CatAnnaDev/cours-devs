# 07 — Le dictionnaire d'opérateurs

On passe de 4 opérateurs à une trentaine, sans que `ops.rs` devienne un
monstre. C'est le chapitre où le code devient *joli*.

## Le problème : 25 fois le même squelette

Écrit naïvement, chaque opérateur binaire donne ceci :

```rust
"+" => {
    let (a, b) = engine.pop2(token)?;
    engine.stack_mut().push(a + b);
    Ok(())
}
"-" => {
    let (a, b) = engine.pop2(token)?;
    engine.stack_mut().push(a - b);
    Ok(())
}
```

Quatre lignes de cérémonie pour un caractère de contenu, répétées vingt-cinq
fois. Ce n'est pas seulement laid : le jour où on change la façon de dépiler, il
faut modifier vingt-cinq endroits, et on en oubliera un.

Ce qui varie, c'est **uniquement le calcul**. Ce qui ne varie pas — dépiler,
vérifier, empiler — doit être écrit une seule fois.

## La solution : passer le calcul en paramètre

```rust
fn binary(
    engine: &mut Engine,
    op: &str,
    f: impl Fn(f64, f64) -> Result<f64, EvalError>,
) -> Result<(), EvalError> {
    let (a, b) = engine.pop2(op)?;
    let result = f(a, b)?;
    engine.stack_mut().push(result);
    Ok(())
}
```

Et l'appel devient :

```rust
"+" => binary(engine, token, |a, b| Ok(a + b)),
"-" => binary(engine, token, |a, b| Ok(a - b)),
"*" => binary(engine, token, |a, b| Ok(a * b)),
```

Une ligne par opérateur, et une seule copie de la mécanique.

### Les closures

`|a, b| Ok(a + b)` est une **closure** : une fonction anonyme écrite sur place.
Les paramètres vont entre barres verticales, le corps suit. Les types sont
inférés depuis le contexte, d'où l'absence d'annotation.

« Closure » (fermeture) parce qu'elle peut *capturer* des variables de son
environnement :

```rust
"sqrt" => unary(engine, token, |a| domain(token, a >= 0.0, a.sqrt())),
```

Ici la closure utilise `token`, qui n'est pas un de ses paramètres : elle l'a
capturé. Le compilateur s'assure que `token` vit assez longtemps.

### Pourquoi `impl Fn` et pas autre chose ?

Il y a quatre façons de recevoir « un bout de code » en Rust.

| Écriture | Ce que c'est | Ici |
|---|---|---|
| `fn(f64, f64) -> f64` | pointeur de fonction brut | ne peut **pas** capturer `token` |
| `impl Fn(...)` | générique : un type concret, choisi à la compilation | **notre choix** |
| `&dyn Fn(...)` / `Box<dyn Fn(...)>` | objet-trait : résolu à l'exécution | appel indirect, allocation pour la version `Box` |
| une `enum` d'opérations | possible | beaucoup de cérémonie pour rien |

`impl Fn` signifie « un type quelconque qui sait être appelé ainsi ». C'est du
**générique** : le compilateur génère une version spécialisée de `binary` pour
chaque closure. Résultat, `binary(engine, token, |a, b| Ok(a + b))` compile en
exactement le même code machine que la version écrite à la main — l'abstraction
est gratuite. C'est ce que Rust appelle *zero-cost abstraction*, et c'est vrai ici
au sens strict : il n'y a aucun appel indirect à l'exécution.

À l'inverse, `Box<dyn Fn>` alloue et passe par un pointeur de fonction à
l'exécution. C'est utile quand on veut *stocker* des closures de types différents
dans une même structure (une `HashMap<String, Box<dyn Fn…>>`, par exemple).
Ici on ne stocke rien : le `match` fait le tri à la compilation.

### `Fn`, `FnMut`, `FnOnce` ?

Trois traits, selon ce que la closure fait de son environnement :

- `Fn` — ne modifie rien, peut être appelée autant de fois qu'on veut.
- `FnMut` — modifie ce qu'elle a capturé.
- `FnOnce` — consomme ce qu'elle a capturé, donc appelable une seule fois.

Nos closures ne font que lire, donc `Fn`. En cas de doute, écris `Fn` et laisse
le compilateur te corriger — il te dira exactement lequel il faut.

## Et pour les fonctions à un argument

```rust
fn unary(
    engine: &mut Engine,
    op: &str,
    f: impl Fn(f64) -> Result<f64, EvalError>,
) -> Result<(), EvalError> {
    let a = engine.pop(op)?;
    let result = f(a)?;
    engine.stack_mut().push(result);
    Ok(())
}
```

Même schéma, un opérande au lieu de deux.

## Les garde-fous

Deux petites fonctions qui rendent tous les messages d'erreur cohérents :

```rust
fn nonzero(x: f64) -> Result<f64, EvalError> {
    if x == 0.0 {
        Err(EvalError::DivByZero)
    } else {
        Ok(x)
    }
}

fn domain(op: &str, ok: bool, value: f64) -> Result<f64, EvalError> {
    if ok {
        Ok(value)
    } else {
        Err(EvalError::Domain(op.to_string()))
    }
}
```

Ce qui donne des définitions qui se lisent comme la spécification :

```rust
"/" => binary(engine, token, |a, b| nonzero(b).map(|b| a / b)),
"sqrt" => unary(engine, token, |a| domain(token, a >= 0.0, a.sqrt())),
"ln" => unary(engine, token, |a| domain(token, a > 0.0, a.ln())),
```

`.map(|b| a / b)` sur un `Result` veut dire « si c'est `Ok`, applique ceci à la
valeur ; si c'est `Err`, ne fais rien et garde l'erreur ». C'est le même `map`
que sur les itérateurs, la même idée : transformer le contenu sans toucher au
contenant.

### Pourquoi ces garde-fous ?

Sans eux, `1 0 /` rendrait `inf` et `-1 sqrt` rendrait `NaN`. Les flottants IEEE
754 ne plantent pas : ils **propagent** silencieusement. Et `NaN` est contagieux —
toute opération avec un `NaN` donne un `NaN`. L'utilisateur verrait apparaître
`NaN` cinq calculs plus loin sans savoir d'où ça vient.

Rendre l'erreur **là où elle se produit**, c'est la seule façon de rendre un
message utile.

### `%` : `rem_euclid` plutôt que `%`

```rust
"%" | "mod" => binary(engine, token, |a, b| nonzero(b).map(|b| a.rem_euclid(b))),
```

En Rust comme en C, `-7.0 % 3.0` vaut `-1.0` : l'opérateur `%` garde le signe du
dividende. `(-7.0f64).rem_euclid(3.0)` vaut `2.0` : le résultat est toujours dans
`[0, |b|)`.

C'est presque toujours ce qu'on veut d'un modulo (indexer un tableau circulaire,
ramener un angle dans un intervalle), et c'est ce que fait Python. Choix assumé,
à documenter.

### `^` : `powf` et pas `^`

Attention au piège venu d'autres langages : en Rust, `^` est le **ou exclusif
binaire**, pas la puissance. La puissance sur les flottants, c'est `powf` :

```rust
"^" | "**" | "pow" => binary(engine, token, |a, b| Ok(a.powf(b))),
```

On accepte trois écritures pour le même opérateur. Le `|` dans un motif de
`match` veut dire « ou ». C'est gratuit et ça évite à l'utilisateur de deviner.

### La factorielle

```rust
fn factorial(op: &str, x: f64) -> Result<f64, EvalError> {
    if x < 0.0 || x.fract() != 0.0 || x > 170.0 {
        return Err(EvalError::Domain(op.to_string()));
    }
    Ok((1..=x as u64).map(|i| i as f64).product())
}
```

Trois refus, et le troisième mérite l'explication : **`171!` dépasse la capacité
d'un `f64`** (environ 1,8 × 10³⁰⁸). Au-delà, le résultat est `inf`. Plutôt que de
rendre un infini, on rend une erreur.

`(1..=n)` est un intervalle **inclusif** — `(1..n)` s'arrêterait à `n-1`. Et
`product()` est le pendant de `sum()` : il multiplie tous les éléments. Sur un
intervalle vide (cas `x = 0`), il rend `1.0`, ce qui donne `0! = 1` gratuitement,
sans cas particulier.

## Les constantes

```rust
"pi" => push(engine, std::f64::consts::PI),
"e" => push(engine, std::f64::consts::E),
"tau" => push(engine, std::f64::consts::TAU),
```

Ne réécris jamais `3.14159265358979` : `std::f64::consts` contient déjà toutes les
constantes usuelles à la précision maximale. `TAU` vaut `2π` — pratique pour la
trigo, puisqu'un tour complet vaut `tau` radians et pas `2 * pi`.

Le petit helper :

```rust
fn push(engine: &mut Engine, n: f64) -> Result<(), EvalError> {
    engine.stack_mut().push(n);
    Ok(())
}
```

Il ne peut jamais échouer, mais il rend un `Result` : c'est ce qui permet à
**toutes** les branches du `match` d'avoir le même type. Sans lui, il faudrait
écrire un bloc `{ … ; Ok(()) }` sur trois lignes pour chaque constante.

## `src/ops.rs`, la partie calcul

```rust
use crate::eval::{Engine, EvalError};

pub fn apply(engine: &mut Engine, token: &str) -> Result<(), EvalError> {
    match token {
        "pi" => push(engine, std::f64::consts::PI),
        "e" => push(engine, std::f64::consts::E),
        "tau" => push(engine, std::f64::consts::TAU),

        "+" => binary(engine, token, |a, b| Ok(a + b)),
        "-" => binary(engine, token, |a, b| Ok(a - b)),
        "*" => binary(engine, token, |a, b| Ok(a * b)),
        "/" => binary(engine, token, |a, b| nonzero(b).map(|b| a / b)),
        "%" | "mod" => binary(engine, token, |a, b| nonzero(b).map(|b| a.rem_euclid(b))),
        "^" | "**" | "pow" => binary(engine, token, |a, b| Ok(a.powf(b))),
        "min" => binary(engine, token, |a, b| Ok(a.min(b))),
        "max" => binary(engine, token, |a, b| Ok(a.max(b))),

        "neg" => unary(engine, token, |a| Ok(-a)),
        "abs" => unary(engine, token, |a| Ok(a.abs())),
        "inv" => unary(engine, token, |a| nonzero(a).map(|a| 1.0 / a)),
        "sqrt" => unary(engine, token, |a| domain(token, a >= 0.0, a.sqrt())),
        "exp" => unary(engine, token, |a| Ok(a.exp())),
        "ln" => unary(engine, token, |a| domain(token, a > 0.0, a.ln())),
        "log" | "log10" => unary(engine, token, |a| domain(token, a > 0.0, a.log10())),
        "log2" => unary(engine, token, |a| domain(token, a > 0.0, a.log2())),
        "sin" => unary(engine, token, |a| Ok(a.sin())),
        "cos" => unary(engine, token, |a| Ok(a.cos())),
        "tan" => unary(engine, token, |a| Ok(a.tan())),
        "floor" => unary(engine, token, |a| Ok(a.floor())),
        "ceil" => unary(engine, token, |a| Ok(a.ceil())),
        "round" => unary(engine, token, |a| Ok(a.round())),
        "!" | "fact" => unary(engine, token, |a| factorial(token, a)),

        other => Err(EvalError::Unknown(other.to_string())),
    }
}
```

Relis ce bloc : **une ligne par opérateur, et chaque ligne dit exactement ce que
fait l'opérateur.** Plus de duplication à maintenir, plus de liste écrite deux
fois comme au chapitre 05. Ajouter `asin` est une ligne, et c'est tout.

C'est ça, le bénéfice concret des closures en paramètre.

## Pourquoi un `match` et pas une `HashMap` ?

La tentation est grande de construire une table `HashMap<&str, Box<dyn Fn…>>`.
C'est ce qu'on ferait en Python. En Rust, le `match` est meilleur ici :

- **Il est résolu à la compilation.** Le compilateur transforme un `match` sur des
  chaînes en un arbre de décision optimisé (souvent : test de longueur, puis
  comparaison directe). Aucune fonction de hachage n'est appelée.
- **Il ne coûte aucune initialisation.** Pas de table à construire au démarrage,
  donc pas besoin d'une variable globale — donc pas de `OnceLock`, pas de
  `lazy_static`, pas de toute cette machinerie.
- **Il ne peut pas contenir de doublon silencieux.** Un motif déjà couvert
  déclenche un avertissement `unreachable pattern`. Deux `insert` avec la même
  clé dans une `HashMap` écrasent le premier sans rien dire.

La `HashMap` redeviendrait le bon outil si les opérateurs étaient **définis à
l'exécution** — par exemple si l'utilisateur pouvait déclarer ses propres
fonctions. Ce serait un excellent exercice, et c'est dans le chapitre 15.

## Vérifie

Adapte temporairement `main` pour essayer :

```rust
fn main() {
    let mut engine = Engine::new();
    match engine.eval_line("2 10 pow sqrt") {
        Ok(()) => println!("{:?}", engine.stack().last()),
        Err(error) => eprintln!("erreur : {error}"),
    }
}
```

`2 10 pow` donne 1024, puis `sqrt` donne 32. Essaie aussi `pi 2 / sin` (1),
`5 fact` (120), `171 fact` (erreur de domaine), `0 ln` (erreur de domaine).

---

**Chapitre suivant :** [08 — Manipuler la pile](08-manipuler-la-pile.md)
