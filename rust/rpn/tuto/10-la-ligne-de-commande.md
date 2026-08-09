# 10 — La ligne de commande

Le moteur est complet. Il lui manque une interface. On veut deux modes :

```bash
blap "3 4 + 2 *"     # one-shot : calcule, affiche, quitte
blap                 # interactif : la pile survit d'une ligne à l'autre
```

## D'abord : afficher un nombre correctement

Petit problème avant tout le reste :

```rust
println!("{}", 7.0_f64);
println!("{:?}", 7.0_f64);
```

affiche `7` puis `7.0`.

Bonne nouvelle : avec `{}`, Rust écrit déjà `7` et pas `7.0`. Mais on veut aussi
contrôler les gros nombres et le cas de `-0.0`, qui s'affiche `-0` alors que c'est
bien zéro. Une petite fonction dans `src/eval.rs` :

```rust
pub fn fmt_num(n: f64) -> String {
    if n == 0.0 {
        return String::from("0");
    }
    if n.fract() == 0.0 && n.abs() < 1e15 {
        return format!("{}", n as i64);
    }
    format!("{n}")
}
```

Ligne par ligne :

- **`n == 0.0`** attrape `0.0` **et** `-0.0`, parce qu'en IEEE 754 les deux zéros
  sont égaux même s'ils n'ont pas la même représentation binaire. Ça évite
  d'afficher `-0` après un `0 neg`.
- **`n.fract() == 0.0`** teste si la partie fractionnaire est nulle, donc si le
  nombre est entier. Dans ce cas on passe par `i64` pour obtenir `7` plutôt que
  `7` avec une éventuelle notation scientifique.
- **`n.abs() < 1e15`** est le garde-fou : au-delà, la conversion en `i64`
  perdrait de la précision, ou déborderait carrément. Un `f64` ne représente
  exactement les entiers que jusqu'à 2⁵³, environ 9 × 10¹⁵. On reste largement en
  dessous, et au-delà on laisse Rust choisir sa notation.

Et pour la pile entière :

```rust
pub fn fmt_stack(stack: &[f64]) -> String {
    let parts: Vec<String> = stack.iter().map(|&n| fmt_num(n)).collect();
    parts.join(" ")
}
```

`map` transforme chaque élément, `collect` rassemble le résultat dans la
collection indiquée par le type — ici `Vec<String>` —, `join` les colle avec un
séparateur. Trois maillons d'une chaîne d'itérateurs, chacun faisant une seule
chose.

C'est un peu gourmand : on alloue une `String` par nombre, plus le `Vec`, plus le
résultat. Pour une invite de commande affichée entre deux frappes au clavier,
c'est parfaitement indolore, et cette version se lit d'un coup d'œil. Le chapitre
15 montre la variante sans allocation, pour le jour où ça compterait.

## Lire les arguments

```rust
let mut args: Vec<String> = std::env::args().skip(1).collect();
```

`std::env::args()` donne un itérateur sur les arguments. Le **premier est le
chemin du programme lui-même**, d'où le `skip(1)` : c'est une convention héritée
d'Unix, valable dans tous les langages.

On demande des `String` et non des `&str` : les arguments sont fournis par le
système d'exploitation au démarrage, ils n'existent pas ailleurs en mémoire, il
faut donc les posséder.

```rust
let trace = matches!(args.first().map(String::as_str), Some("-t" | "--trace"));
if trace {
    args.remove(0);
}
```

`args.first()` rend `Option<&String>`, et `.map(String::as_str)` le transforme en
`Option<&str>` pour pouvoir le comparer à des littéraux. `String::as_str` est
passé comme une fonction — c'est plus court que `|s| s.as_str()` et strictement
équivalent.

Puis le dispatch :

```rust
match args.first().map(String::as_str) {
    Some("-h" | "--help") => print_help(),
    Some(_) => one_shot(&args.join(" ")),
    None => repl(),
}
```

- `Some("-h" | "--help")` — les motifs se combinent, y compris à l'intérieur d'un
  `Some`.
- `Some(_)` — un argument quelconque : c'est une expression à calculer. On
  recolle tous les arguments avec des espaces, ce qui fait que
  `blap 3 4 +` marche aussi bien que `blap "3 4 +"` (sauf que le shell mangerait
  le `*`, d'où l'habitude des guillemets).
- `None` — aucun argument, on lance le mode interactif.

### Pourquoi pas `clap` ?

`clap` est l'excellente bibliothèque d'analyse d'arguments de l'écosystème Rust.
Elle gère les sous-commandes, les valeurs par défaut, la génération de l'aide et
de la complétion shell.

On s'en passe pour deux raisons. La première est pédagogique : trois options ne
justifient pas d'apprendre une bibliothèque, et faire l'analyse à la main une
fois dans sa vie apprend ce que `clap` fait pour toi. La seconde est que `clap`
tire une quinzaine de paquets et allonge sérieusement la compilation, pour un
programme qui a trois options.

Le seuil est à peu près là : **au-delà de cinq options ou dès qu'il y a des
sous-commandes, prends `clap`.** En dessous, `std::env::args` suffit.

## Le mode one-shot

```rust
fn one_shot(expression: &str) {
    let mut engine = Engine::new();

    match engine.eval_line(expression) {
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

### `if let Some(&top)`

`last()` rend `Option<&f64>`. Le motif `Some(&top)` déstructure les deux couches
d'un coup : `top` est un `f64`, pas une référence. Même idée que le `|acc, &x|` du
chapitre 08.

Et si la pile est vide (l'utilisateur a tapé `"clear"`), on n'affiche rien du
tout. Pas de ligne vide, pas de `(vide)` : dans un tuyau shell, l'absence de
résultat doit être l'absence de sortie.

### Le code de sortie

```rust
std::process::exit(1);
```

Un programme Unix rend un entier en terminant : `0` veut dire succès, tout le
reste veut dire échec. C'est ce qui permet d'écrire :

```bash
blap "1 0 /" && echo "ça a marché"
```

sans que le `echo` s'exécute. Sans ce `exit(1)`, ton programme mentirait au shell,
et tous les scripts qui l'utilisent seraient faux.

Vérifie avec :

```bash
blap "1 0 /"; echo $?
```

## Le REPL

REPL = *Read, Eval, Print, Loop*. Le squelette :

```rust
fn repl() {
    println!("blap — calculatrice RPN. `?` pour l'aide, `q` pour quitter.");

    let mut engine = Engine::new();
    let mut line = String::new();

    loop {
        print_prompt(&engine);

        line.clear();
        match std::io::stdin().read_line(&mut line) {
            Ok(0) => {
                println!();
                break;
            }
            Ok(_) => {}
            Err(error) => {
                eprintln!("lecture impossible : {error}");
                break;
            }
        }

        match line.trim() {
            "" => continue,
            "q" | "quit" | "exit" => break,
            "?" | "help" => print_help(),
            input => match engine.eval_line(input) {
                Ok(()) => match engine.stack().last() {
                    Some(&top) => println!("  = {}", fmt_num(top)),
                    None => println!("  (pile vide)"),
                },
                Err(error) => eprintln!("  × {error}"),
            },
        }
    }

    println!("à bientôt");
}
```

Il y a beaucoup de choses importantes dans ces trente lignes.

### `loop` et pas `while true`

`loop` est la boucle infinie explicite de Rust. Elle a un avantage réel sur
`while true` : le compilateur *sait* qu'elle ne se termine que par un `break`, ce
qui lui permet de mieux raisonner sur ce qui suit, et sur les valeurs qu'elle
produit (une boucle `loop` peut renvoyer une valeur avec `break valeur`).

### `read_line` **ajoute** à la chaîne

C'est le piège classique. `read_line` n'écrase pas le contenu de la `String`
qu'on lui passe, il **concatène**. Sans le `line.clear()`, la deuxième ligne
contiendrait aussi la première, et ainsi de suite.

Pourquoi réutiliser la même `String` plutôt qu'en créer une neuve à chaque tour ?
Parce que `clear()` conserve la mémoire déjà allouée. Ici encore c'est
imperceptible, mais c'est un réflexe qui coûte une ligne et se généralise à des
situations où il compte vraiment.

### `Ok(0)` : la fin de l'entrée

`read_line` rend le **nombre d'octets lus**. Zéro octet signifie la fin du flux :
l'utilisateur a tapé `Ctrl-D`, ou l'entrée était un fichier redirigé qui vient de
se terminer.

Sans ce cas, la boucle tournerait à l'infini en lisant des lignes vides et
saturerait un cœur de processeur. C'est le bug numéro un des REPL faits maison.

Le `println!()` avant le `break` sert à passer une ligne : `Ctrl-D` ne provoque
pas de retour chariot dans le terminal, l'invite resterait collée au prompt du
shell.

### Les trois cas de `read_line`

- `Ok(0)` — fin de l'entrée, on sort proprement.
- `Ok(_)` — quelque chose a été lu, on continue. Le corps est vide, et c'est
  volontaire : la donnée est dans `line`, pas dans la valeur de retour.
- `Err(error)` — vraie erreur d'entrée / sortie. Rare, mais possible (entrée
  binaire non-UTF-8, par exemple). On le signale et on sort, plutôt que de boucler
  sur une erreur permanente.

### `line.trim()`

`read_line` conserve le `\n` final. Et sous Windows, c'est `\r\n`. `trim()`
enlève les espaces des deux côtés, donc les deux cas d'un coup, sans code
spécifique par plateforme.

## L'invite, et pourquoi il faut vider le tampon

```rust
use std::io::Write;

fn print_prompt(engine: &Engine) {
    let stack = engine.stack();

    if stack.is_empty() {
        print!("rpn › ");
    } else {
        print!("[{}] › ", fmt_stack(stack));
    }

    let _ = std::io::stdout().flush();
}
```

Le point crucial est le `flush()`.

La sortie standard est **tamponnée par ligne** quand elle va vers un terminal :
Rust accumule le texte et ne l'envoie réellement qu'en voyant un `\n`. Or `print!`
(sans `ln`) n'en écrit pas. Sans `flush`, l'invite resterait invisible tant que
l'utilisateur n'a pas tapé sa ligne — et il attendrait devant un écran vide.

Ce comportement existe dans tous les langages. En Python c'est
`print(..., flush=True)`, en C c'est `fflush(stdout)`.

Deux détails :

- **`use std::io::Write;`** est obligatoire. `flush` vient du trait `Write`, et en
  Rust une méthode de trait n'est utilisable que si le trait est dans la portée.
  Si tu oublies cette ligne, l'erreur te dira exactement quel trait importer.
- **`let _ = …`** ignore explicitement le `Result` rendu par `flush`. Rust
  avertit quand on jette un `Result` en silence ; le `let _ =` dit « je sais, et
  c'est voulu ». Si vider le tampon de la sortie échoue, il n'y a rien
  d'intelligent à faire — on ne peut même pas afficher de message d'erreur.

## Le `match` sur la ligne

```rust
match line.trim() {
    "" => continue,
    "q" | "quit" | "exit" => break,
    "?" | "help" => print_help(),
    input => match engine.eval_line(input) { ... },
}
```

La dernière branche lie la valeur à `input` au lieu d'utiliser `_` : on veut à la
fois attraper tous les cas restants **et** récupérer la valeur.

Le `""` en premier évite d'afficher `(pile vide)` chaque fois que quelqu'un
appuie sur Entrée sans rien taper.

Trois façons de quitter, parce que l'utilisateur ne devrait pas avoir à deviner
laquelle tu as choisie.

## Vérifie

```bash
cargo run -- "3 4 + 2 *"
cargo run -- "1 0 /"; echo $?
cargo run
```

Dans le REPL, tape `3 4`, puis `+`, puis `dup *`, puis `Ctrl-D`. L'invite doit
afficher la pile à chaque tour.

---

**Chapitre suivant :** [11 — Le mode trace](11-le-mode-trace.md)
