# 05 — Des erreurs propres

On supprime tous les `unwrap()` et tous les `panic!`. À la fin de ce chapitre,
aucune entrée utilisateur, si tordue soit-elle, ne peut faire tomber le programme.

## Comment représenter une erreur ?

Trois options, par ordre de qualité croissante.

### 1. Une `String`

```rust
fn eval_line(...) -> Result<(), String> {
    Err(format!("jeton inconnu : {token}"))
}
```

Ça marche, et c'est ce que fait tout le monde au début. Les défauts :

- **On ne peut pas la tester correctement.** Un test devient une comparaison de
  texte : change une virgule dans le message et le test casse.
- **On ne peut pas réagir différemment selon l'erreur.** Distinguer « division
  par zéro » de « jeton inconnu » demande de fouiller dans la chaîne. Horrible.
- **Ça alloue à chaque erreur, même si personne ne lit le message.**
- **Traduire l'interface** oblige à retoucher chaque site d'erreur.

### 2. Un code entier

```rust
Err(3)
```

Non. C'est le style C, on ne sait plus ce que `3` veut dire trois mois plus tard,
et rien n'empêche de renvoyer `42`.

### 3. Une énumération

```rust
enum EvalError {
    NeedsOperands { op: String, need: usize, got: usize },
    Unknown(String),
    DivByZero,
    Domain(String),
}
```

C'est **la** façon Rust. Un `enum` Rust n'a rien à voir avec un `enum` C : chaque
variante peut **transporter des données**, et de formes différentes. Ici :

- `DivByZero` ne porte rien : le nom dit tout.
- `Unknown(String)` porte le jeton fautif, en variante tuple.
- `NeedsOperands { … }` porte trois champs nommés, en variante structurée. Trois
  valeurs anonymes seraient illisibles à la lecture ; nommées, elles se
  documentent seules.

L'appelant peut alors faire un `match` dessus et réagir précisément. Le message
humain, lui, sera calculé au dernier moment, uniquement au moment de l'affichage.

### Pourquoi `String` dans les variantes et pas `&str` ?

C'est le seul compromis du chapitre, et il est assumé. Un `&str` serait une vue
sur la ligne saisie ; il faudrait alors expliquer au compilateur que l'erreur ne
peut pas survivre à cette ligne, ce qui s'écrit avec une **durée de vie**
explicite (`EvalError<'a>`). C'est correct, plus rapide, et hors sujet pour un
premier projet : ça contaminerait une dizaine de signatures.

`String` alloue — mais **seulement sur le chemin d'erreur**, c'est-à-dire une fois
de temps en temps quand un humain se trompe de touche. Ça ne se mesure pas. Le
chapitre 15 montre comment passer à la version sans allocation, quand tu seras à
l'aise avec les durées de vie.

C'est un arbitrage qui revient souvent en Rust : **la version qui alloue est plus
simple, la version qui emprunte est plus rapide.** Choisis selon le chemin —
chaud ou froid — pas par principe.

## Les `derive`

```rust
#[derive(Debug, Clone, PartialEq, Eq)]
enum EvalError { ... }
```

`derive` demande au compilateur d'écrire une implémentation de trait à ta place :

- **`Debug`** — permet `{:?}`. À mettre sur tout, sans réfléchir : sans lui, la
  valeur n'apparaît même pas dans le message d'un test raté.
- **`Clone`** — permet de dupliquer explicitement avec `.clone()`.
- **`PartialEq`** — permet `==`. Indispensable pour écrire
  `assert_eq!(erreur, EvalError::DivByZero)` au chapitre 13.
- **`Eq`** — dit que l'égalité est une vraie relation d'équivalence. On peut le
  mettre ici parce que `EvalError` ne contient **aucun** `f64`. C'est important :
  les flottants ne peuvent pas être `Eq`, puisque `NaN != NaN`. C'est exactement
  pour ça que `PartialEq` et `Eq` sont deux traits distincts en Rust — le
  « partial » signifie « il peut exister des valeurs qui ne sont pas égales à
  elles-mêmes ».

## Écrire le message : le trait `Display`

```rust
use std::fmt;

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
```

Implémenter `Display`, c'est déclarer « voici comment on m'affiche à un humain ».
Une fois fait, `println!("{error}")` fonctionne. C'est ce trait qui manquait au
`Vec` du chapitre 03.

Points à noter :

- **`write!` et pas `println!`.** On n'imprime pas, on *écrit dans le formateur*
  qu'on nous prête. C'est ce qui permet de réutiliser cet affichage n'importe où :
  console, fichier, chaîne, message d'erreur d'un test.
- **`*need`** — dans le `match`, `need` est un `&usize` emprunté à `self`. Le `*`
  le déréférence pour le comparer à `1`. Le compilateur te le dira si tu l'oublies.
- **On sépare le calcul du message de son émission.** Toute la mise en forme est
  ici, et nulle part ailleurs. Traduire le programme, c'est éditer cette fonction.

Et on ajoute une ligne :

```rust
impl std::error::Error for EvalError {}
```

Le trait `Error` est la convention de l'écosystème pour « ceci est un type
d'erreur ». Il n'exige rien de plus que `Debug` + `Display`, d'où le corps vide.
Il rend `EvalError` compatible avec tout ce qui manipule des erreurs de façon
générique (`Box<dyn Error>`, `anyhow`, `?` vers une autre erreur). Une ligne
aujourd'hui, des ennuis en moins plus tard.

## L'opérateur `?`

Un helper qui dépile deux valeurs :

```rust
fn pop2(stack: &mut Vec<f64>, op: &str) -> Result<(f64, f64), EvalError> {
    let len = stack.len();
    if len < 2 {
        return Err(EvalError::NeedsOperands {
            op: op.to_string(),
            need: 2,
            got: len,
        });
    }
    let a = stack[len - 2];
    let b = stack[len - 1];
    stack.truncate(len - 2);
    Ok((a, b))
}
```

Remarque le détail : on **vérifie la longueur d'abord**, puis on lit par index et
on tronque. Deux `pop().unwrap()` auraient marché, mais on veut zéro `unwrap`
dans le code final, et `truncate` ajuste la longueur une seule fois au lieu de
deux. Le message d'erreur, lui, sait dire combien d'opérandes il y avait
réellement — c'est bien plus utile qu'un « pile vide ».

À l'appel :

```rust
let (a, b) = pop2(stack, token)?;
```

Le `?` se lit : « si c'est `Ok`, sors la valeur et continue ; si c'est `Err`,
arrête cette fonction tout de suite et renvoie l'erreur ». Il remplace :

```rust
let (a, b) = match pop2(stack, token) {
    Ok(values) => values,
    Err(error) => return Err(error),
};
```

C'est le seul sucre syntaxique important du langage, et c'est ce qui rend la
gestion d'erreurs explicite de Rust supportable. Deux règles :

- `?` ne s'utilise que dans une fonction qui renvoie `Result` (ou `Option`).
- Il convertit l'erreur au type de retour si une conversion est déclarée. Ici les
  types sont identiques, donc rien à faire.

## Le code du chapitre

`src/main.rs` en entier :

```rust
use std::fmt;

#[derive(Debug, Clone, PartialEq, Eq)]
enum EvalError {
    NeedsOperands { op: String, need: usize, got: usize },
    Unknown(String),
    DivByZero,
    Domain(String),
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

fn pop2(stack: &mut Vec<f64>, op: &str) -> Result<(f64, f64), EvalError> {
    let len = stack.len();
    if len < 2 {
        return Err(EvalError::NeedsOperands {
            op: op.to_string(),
            need: 2,
            got: len,
        });
    }
    let a = stack[len - 2];
    let b = stack[len - 1];
    stack.truncate(len - 2);
    Ok((a, b))
}

fn apply(stack: &mut Vec<f64>, token: &str) -> Result<(), EvalError> {
    if !matches!(token, "+" | "-" | "*" | "/") {
        return Err(EvalError::Unknown(token.to_string()));
    }

    let (a, b) = pop2(stack, token)?;

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

    stack.push(result);
    Ok(())
}

fn eval_line(stack: &mut Vec<f64>, line: &str) -> Result<(), EvalError> {
    for token in line.split_whitespace() {
        match token.parse::<f64>() {
            Ok(number) => stack.push(number),
            Err(_) => apply(stack, token)?,
        }
    }
    Ok(())
}

fn main() {
    let mut stack: Vec<f64> = Vec::new();

    match eval_line(&mut stack, "3 4 + 2 *") {
        Ok(()) => println!("{:?}", stack.last()),
        Err(error) => eprintln!("erreur : {error}"),
    }
}
```

Essaie successivement `"3 4 + 2 *"`, `"3 +"`, `"1 0 /"`, `"3 4 bidule"`. Plus
aucun plantage : à chaque fois un message clair, et le programme se termine
normalement.

Le compilateur t'avertit que `Domain` n'est jamais construite. C'est normal :
cette variante servira au chapitre 07 pour `sqrt` d'un négatif. On la garde,
l'avertissement disparaîtra tout seul. Un avertissement n'est pas une erreur —
mais ne prends jamais l'habitude d'en laisser traîner : ils finissent par cacher
les vrais.

### `matches!`

```rust
if !matches!(token, "+" | "-" | "*" | "/") {
```

`matches!` est une macro qui rend `true` si la valeur correspond au motif. C'est
l'écriture courte de :

```rust
match token {
    "+" | "-" | "*" | "/" => true,
    _ => false,
}
```

### `eprintln!` et pas `println!`

Un programme en ligne de commande écrit sur **deux** flux :

- `stdout` — le résultat, ce qu'on peut rediriger dans un fichier ou un tuyau.
- `stderr` — les messages destinés à l'humain : erreurs, avertissements.

Séparer les deux permet à `blap "3 4 +" > resultat.txt` de ne mettre que `7` dans
le fichier, tout en laissant les erreurs visibles à l'écran. C'est une convention
universelle sous Unix ; ne la casse jamais.

## Ce qui reste bancal

Deux choses, notées pour plus tard :

1. **La liste des opérateurs est écrite deux fois** — dans le `matches!` et dans
   le `match`. Ajouter `%` obligerait à penser aux deux endroits, et oublier l'un
   des deux donnerait un bug silencieux. Le chapitre 07 supprime la duplication.
2. **`3 4 + oups` laisse la pile à `[7]`** alors que la ligne a échoué. C'est le
   chapitre 09.

---

**Chapitre suivant :** [06 — Des modules et une struct](06-des-modules-et-une-struct.md)
