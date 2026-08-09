# 11 — Le mode trace

La fonctionnalité qui transforme la calculatrice en outil pédagogique :

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

Trois colonnes : le jeton, ce qu'il a fait en français, l'état de la pile après.

## Ce qu'il faut enregistrer

Pour dire « 1 + 2 = 3 », il ne suffit pas de connaître la pile *après* : il faut
aussi savoir ce qui a été consommé. On garde donc les deux états.

```rust
#[derive(Debug, Clone, PartialEq)]
pub struct Step {
    pub token: String,
    pub before: Vec<f64>,
    pub after: Vec<f64>,
}
```

Les champs sont `pub` : c'est un simple sac de données destiné à l'affichage, il
n'a aucun invariant à protéger. Contrairement à `Engine`, où le champ `stack` est
privé exprès.

Pas de `Eq` cette fois : la struct contient des `f64`, et on a vu au chapitre 05
pourquoi les flottants ne peuvent pas être `Eq`. `PartialEq` suffit pour écrire
un `assert_eq!` dans un test.

`token: String` plutôt que `&str` : même raison qu'au chapitre 05, on évite les
durées de vie explicites. Un `Step` est produit une fois par jeton **en mode trace
seulement** — c'est-à-dire quand l'utilisateur a explicitement demandé à voir le
détail. Personne ne trace un million de lignes.

## Un seul évaluateur, deux entrées

La tentation serait d'écrire deux boucles : une pour évaluer, une pour tracer.
Mauvaise idée — elles divergeraient au premier changement.

On garde **une** boucle, avec un drapeau, et deux fonctions publiques qui
l'appellent :

```rust
pub fn eval_line(&mut self, line: &str) -> Result<(), EvalError> {
    self.run(line, false)?;
    Ok(())
}

pub fn eval_traced(&mut self, line: &str) -> Result<Vec<Step>, EvalError> {
    self.run(line, true)
}

fn run(&mut self, line: &str, trace: bool) -> Result<Vec<Step>, EvalError> {
    let backup = self.stack.clone();
    let mut steps = Vec::new();

    for token in line.split_whitespace() {
        let before = if trace {
            self.stack.clone()
        } else {
            Vec::new()
        };

        if let Err(error) = self.eval_token(token) {
            self.stack = backup;
            return Err(error);
        }

        if trace {
            steps.push(Step {
                token: token.to_string(),
                before,
                after: self.stack.clone(),
            });
        }
    }

    Ok(steps)
}
```

`run` est privé — pas de `pub` : c'est un détail d'implémentation. Les deux
fonctions publiques donnent chacune le type de retour qui a du sens pour elle.
`eval_line` jette le `Vec<Step>` (toujours vide) et rend `()`.

### « Mais ça alloue un `Vec` pour rien quand on ne trace pas ! »

Non, et c'est un point qui vaut la peine d'être connu : **`Vec::new()` n'alloue
pas.** Un `Vec` vide est juste un pointeur bidon, une longueur à 0 et une capacité
à 0. L'allocation n'a lieu qu'au premier `push`.

Donc dans le cas non tracé, `let mut steps = Vec::new()` et les
`else { Vec::new() }` sont littéralement gratuits. Le compilateur, voyant que
`trace` est une constante à l'endroit de l'appel, supprime même les branches
mortes.

C'est ce qui rend cette solution **à la fois** la plus simple et la plus rapide.
Ça n'arrive pas toujours ; quand ça arrive, ne cherche pas plus loin.

### Pourquoi un `bool` plutôt qu'une closure de rappel ?

Une autre solution consisterait à passer une closure appelée à chaque étape :

```rust
fn run(&mut self, line: &str, mut on_step: impl FnMut(&str, &[f64]))
```

C'est plus souple — l'appelant décide quoi faire de chaque étape, sans qu'on
construise de `Vec` du tout. C'est ce qu'on écrirait pour une bibliothèque.

Pour un programme fermé de moins de 600 lignes, avec un seul consommateur qui
veut justement la liste complète, le `bool` est plus simple à lire et à
expliquer. La
souplesse qu'on n'utilise pas est un coût, pas un gain.

## Afficher la trace

```rust
fn print_trace(steps: &[Step]) {
    for step in steps {
        let token = format!("{:<6}", step.token);
        let after = format!("→ [{}]", fmt_stack(&step.after));
        println!(
            "  {} {:<24} {}",
            token,
            explain(&step.token, &step.before, &step.after),
            after
        );
    }
}
```

`{:<6}` aligne à gauche sur au moins 6 caractères ; `{:>6}` alignerait à droite,
`{:^6}` centrerait. C'est ce qui donne les colonnes.

Pourquoi passer par la variable `token` au lieu de mettre `{:<6}` directement
dans le `println!` ? Ça n'a l'air de rien maintenant, mais au chapitre suivant on
va colorer ce jeton, et **il faudra le remplir avant de le colorer** : les codes
de couleur ANSI sont des caractères invisibles à l'écran mais bien réels pour
`{:<6}`, qui compterait alors une dizaine de caractères et n'ajouterait aucun
remplissage. Résultat : colonnes de travers.

C'est un piège classique de mise en forme de terminal. **Compte d'abord,
colore ensuite.**

## Expliquer un jeton en français

C'est la partie amusante. On ne veut pas maintenir une table de descriptions à
côté du dictionnaire d'opérateurs — elle se désynchroniserait. On **déduit** la
description en comparant les deux états de la pile.

```rust
fn explain(token: &str, before: &[f64], after: &[f64]) -> String {
    if token.parse::<f64>().is_ok() {
        return format!("empile {token}");
    }

    match token {
        "dup" => String::from("duplique le sommet"),
        "drop" => String::from("retire le sommet"),
        "swap" => String::from("échange les deux du sommet"),
        "over" => String::from("copie l'avant-dernier au sommet"),
        "rot" => String::from("fait tourner les trois du sommet"),
        "clear" | "cls" => String::from("vide la pile"),
        "sum" => format!("somme de la pile = {}", fmt_num(top(after))),
        "prod" => format!("produit de la pile = {}", fmt_num(top(after))),

        _ if before.len() == after.len() + 1 && before.len() >= 2 => {
            let a = fmt_num(before[before.len() - 2]);
            let b = fmt_num(before[before.len() - 1]);
            let result = fmt_num(top(after));
            match infix_symbol(token) {
                Some(symbol) => format!("{a} {symbol} {b} = {result}"),
                None => format!("{token}({a}, {b}) = {result}"),
            }
        }
        _ if before.len() == after.len() && !before.is_empty() => {
            format!("{token}({}) = {}", fmt_num(top(before)), fmt_num(top(after)))
        }
        _ if after.len() == before.len() + 1 => {
            format!("empile {token} = {}", fmt_num(top(after)))
        }
        _ => token.to_string(),
    }
}
```

Les trois branches `_ if …` sont des **gardes** (on en a vu une au chapitre 06).
Elles se lisent : « n'importe quel jeton, *à condition que* … ». Elles sont
testées dans l'ordre, la première qui passe gagne.

Et la logique est purement arithmétique :

| Variation de taille | Interprétation | Exemple affiché |
|---|---|---|
| `-1`, avec au moins 2 avant | opérateur binaire | `1 + 2 = 3` |
| `0`, pile non vide | fonction unaire | `sqrt(9) = 3` |
| `+1` | constante empilée | `empile pi = 3.14159…` |

L'énorme avantage : **ajouter un opérateur binaire au chapitre 07 ne demande
aucune modification ici.** `explain` le décrira correctement tout seul. Seuls les
mots de manipulation de pile, qui n'ont pas de forme mathématique, ont besoin
d'une phrase écrite à la main.

Pour la présentation des opérateurs courants, une petite table de symboles :

```rust
fn infix_symbol(token: &str) -> Option<&str> {
    match token {
        "+" => Some("+"),
        "-" => Some("−"),
        "*" => Some("×"),
        "/" => Some("÷"),
        "^" | "**" | "pow" => Some("^"),
        "%" | "mod" => Some("mod"),
        _ => None,
    }
}
```

Les vrais signes typographiques : `−` (moins mathématique, U+2212) et non le
trait d'union, `×` et non l'astérisque, `÷` et non la barre oblique. Ça ne change
rien au calcul, ça change beaucoup à la lecture.

Quand `infix_symbol` rend `None` — c'est le cas de `min`, `max` —, on retombe sur
la notation fonctionnelle `min(3, 9) = 3`, qui est juste et lisible.

Et le dernier utilitaire :

```rust
fn top(stack: &[f64]) -> f64 {
    match stack.last() {
        Some(&n) => n,
        None => 0.0,
    }
}
```

Une valeur de repli pour ne pas avoir à gérer `Option` dans chaque `format!`. Dans
`explain`, les gardes garantissent déjà que la pile n'est pas vide quand on
appelle `top`.

## Brancher le mode

Dans `main`, l'option `-t` était déjà extraite au chapitre 10 ; il reste à la
transmettre :

```rust
fn one_shot(expression: &str, trace: bool) {
    let mut engine = Engine::new();

    let result = if trace {
        engine.eval_traced(expression).map(|steps| print_trace(&steps))
    } else {
        engine.eval_line(expression)
    };

    match result {
        Ok(()) => {
            if let Some(&top) = engine.stack().last() {
                println!("{}", fmt_num(top));
            }
        }
        Err(error) => {
            eprintln!("erreur : {error}");
            std::process::exit(1);
        }
    }
}
```

Le `.map(|steps| print_trace(&steps))` mérite un mot. Les deux branches du `if`
doivent avoir le **même type**. Or `eval_traced` rend `Result<Vec<Step>, _>` et
`eval_line` rend `Result<(), _>`. Le `map` applique `print_trace` à la valeur en
cas de succès, et comme `print_trace` ne rend rien, le résultat devient
`Result<(), _>` : les deux types coïncident.

C'est une façon élégante de dire « fais ça avec le succès, et laisse l'erreur
tranquille ».

## La commande `trace` du REPL

Dans le REPL on veut pouvoir basculer à la volée. C'est pour ça que le paramètre
est déclaré `mut` :

```rust
fn repl(mut trace: bool) {
```

Un paramètre `mut` ne rend pas l'appelant modifiable — la valeur a été copiée à
l'entrée. Ça dit simplement « je me réserve le droit de changer ma copie ».

```rust
"trace" => {
    trace = !trace;
    let state = if trace { "activée" } else { "coupée" };
    println!("  trace {state}");
}
```

Et la branche d'évaluation devient la même que dans `one_shot` :

```rust
input => {
    let result = if trace {
        engine.eval_traced(input).map(|steps| print_trace(&steps))
    } else {
        engine.eval_line(input)
    };

    match result {
        Ok(()) => match engine.stack().last() {
            Some(&top) => println!("  = {}", fmt_num(top)),
            None => println!("  (pile vide)"),
        },
        Err(error) => eprintln!("  × {error}"),
    }
}
```

## Vérifie

```bash
cargo run -- -t "5 1 2 + 4 * + 3 -"
cargo run -- -t "2 dup * 3 dup * + sqrt"
```

Puis dans le REPL : tape `trace`, puis `3 4 +`, puis `trace` à nouveau.

---

**Chapitre suivant :** [12 — Des couleurs](12-des-couleurs.md)
