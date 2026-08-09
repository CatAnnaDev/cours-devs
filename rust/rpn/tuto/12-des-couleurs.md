# 12 — Des couleurs

Le programme marche. On va le rendre agréable — et surtout, apprendre à le faire
**sans casser son utilisation dans un script**.

## Comment un terminal fait des couleurs

Il n'y a pas d'API. On envoie du texte, et le terminal interprète certaines
séquences de caractères comme des commandes de mise en forme. Ce sont les
**séquences d'échappement ANSI**, standardisées en 1979 et toujours d'actualité.

Une séquence commence par le caractère d'échappement `\x1b` (27 en décimal, aussi
noté `ESC` ou `\e`), suivi de `[`, d'un code, et de `m` :

```
\x1b[31m   passe en rouge
\x1b[0m    remet tout à zéro
```

Donc pour écrire « erreur » en rouge :

```rust
println!("\x1b[31merreur\x1b[0m");
```

Les codes utiles :

| Code | Effet |
|------|-------|
| `0` | remise à zéro |
| `1` | gras |
| `2` | atténué |
| `31` | rouge |
| `32` | vert |
| `33` | jaune |
| `36` | cyan |

Les couleurs de base vont de 30 à 37 pour le texte, 40 à 47 pour le fond. Il
existe aussi une palette de 256 couleurs et du vrai RGB, mais toutes les
configurations ne les gèrent pas ; les huit couleurs de base marchent partout, et
elles ont l'avantage de **respecter le thème choisi par l'utilisateur**.

**Toujours refermer avec `\x1b[0m`.** Sinon la couleur déborde sur le reste de la
session, y compris après la fin de ton programme.

## Le vrai sujet : savoir quand se taire

Le problème n'est pas de colorer, c'est de **ne pas colorer** au mauvais moment :

```bash
blap "3 4 +" > resultat.txt
```

Si on écrit les séquences ANSI sans réfléchir, le fichier contient
`\x1b[32m7\x1b[0m` au lieu de `7`. Tout script qui lit cette sortie est cassé.

Deux conditions à vérifier :

### 1. La sortie va-t-elle vers un terminal ?

```rust
use std::io::IsTerminal;

let is_terminal = std::io::stdout().is_terminal();
```

`is_terminal()` demande au système si la sortie standard est branchée sur un
terminal interactif, ou sur un fichier / un tuyau. C'est dans la bibliothèque
standard depuis Rust 1.70 ; avant, il fallait une bibliothèque externe.

C'est exactement ce que fait `ls` : coloré dans ton terminal, brut dans
`ls > liste.txt`.

### 2. L'utilisateur a-t-il demandé le silence ?

```rust
let asked_no_color = std::env::var_os("NO_COLOR").is_some();
```

`NO_COLOR` est une convention inter-langages (voir <https://no-color.org>) : si la
variable d'environnement existe, **quelle que soit sa valeur**, le programme ne
doit pas colorer. C'est trois lignes à implémenter et ça rend ton outil poli.

`var_os` plutôt que `var` : `var` rend une erreur si la valeur n'est pas de
l'UTF-8 valide, ce qui n'a aucun intérêt ici puisqu'on ne regarde que l'existence.
`var_os` ne peut pas échouer pour cette raison.

## Où ranger cette décision ?

C'est la question intéressante du chapitre, et elle a plusieurs réponses.

### Ce qu'on ne va **pas** faire

**Une variable globale mutable.** En Rust, `static mut` demande du `unsafe` à
chaque accès, parce que rien ne garantit qu'un autre thread ne lit pas pendant
qu'on écrit. Le langage rend la chose pénible exprès : c'est un signal.

**Un `OnceLock` ou un `lazy_static`.** `OnceLock` est un emplacement global
qu'on n'initialise qu'une fois, de façon sûre même entre threads. C'est la bonne
réponse pour une bibliothèque publiée, et c'est ce qu'on écrirait dans du code
professionnel. Mais ça introduit la notion de synchronisation, de `static`, et
d'initialisation paresseuse, pour un programme mono-thread de quelques centaines
de lignes. On s'en passe.

**Recalculer à chaque appel.** `is_terminal()` est un appel système. Le faire
quatre fois par ligne affichée est absurde.

### Ce qu'on fait

Une petite struct, calculée une fois dans `main`, passée en paramètre.

`src/style.rs` :

```rust
use std::io::IsTerminal;

#[derive(Debug, Clone, Copy)]
pub struct Style {
    color: bool,
}

impl Style {
    pub fn detect() -> Style {
        let asked_no_color = std::env::var_os("NO_COLOR").is_some();
        let is_terminal = std::io::stdout().is_terminal();
        Style {
            color: !asked_no_color && is_terminal,
        }
    }

    fn paint(self, code: &str, text: &str) -> String {
        if self.color {
            format!("\x1b[{code}m{text}\x1b[0m")
        } else {
            text.to_string()
        }
    }

    pub fn bold(self, text: &str) -> String {
        self.paint("1", text)
    }

    pub fn dim(self, text: &str) -> String {
        self.paint("2", text)
    }

    pub fn red(self, text: &str) -> String {
        self.paint("31", text)
    }

    pub fn green(self, text: &str) -> String {
        self.paint("32", text)
    }

    pub fn yellow(self, text: &str) -> String {
        self.paint("33", text)
    }

    pub fn cyan(self, text: &str) -> String {
        self.paint("36", text)
    }
}
```

Sans oublier la déclaration dans `main.rs` :

```rust
mod style;

use style::Style;
```

### Pourquoi c'est bien

**`Copy`.** La struct ne contient qu'un `bool` : un octet. En la marquant `Copy`,
on peut l'écrire `style: Style` en paramètre au lieu de `&Style`, sans que
l'appelant perde la sienne. Moins de bruit syntaxique, et c'est plus rapide qu'une
référence.

**`fn paint(self, …)` et pas `&self`.** Même raison : pour un type `Copy` d'un
octet, prendre `self` par valeur est plus simple et au moins aussi efficace.

**Le champ `color` est privé.** Personne à l'extérieur ne peut le trafiquer ; on
passe forcément par `detect()`.

**La décision est prise une fois.** Un appel système au démarrage, et plus jamais.

**Passer explicitement le style est un choix, pas une contrainte.** Ça rend
visible dans chaque signature le fait que la fonction affiche quelque chose.
`fn print_help(style: Style)` dit qu'elle écrit à l'écran ; `fn factorial(…)` dit
qu'elle ne le fait pas. Une globale effacerait cette information.

### `paint` alloue une `String` à chaque appel

Oui. Pour un affichage de quelques dizaines de chaînes entre deux frappes au
clavier, c'est sans conséquence.

Si ce code était dans une boucle serrée, on renverrait un `Cow<str>` — un type
qui contient *soit* une chaîne empruntée (aucune allocation quand la couleur est
coupée), *soit* une chaîne possédée. Voir le chapitre 15.

## Brancher le style

Dans `main` :

```rust
let style = Style::detect();

match args.first().map(String::as_str) {
    Some("-h" | "--help") => print_help(style),
    Some(_) => one_shot(&args.join(" "), trace, style),
    None => repl(trace, style),
}
```

Et les affichages deviennent :

```rust
eprintln!("{} {error}", style.red("erreur :"));
println!("  {} {}", style.dim("="), style.green(&fmt_num(top)));
print!("{} ", style.cyan("rpn ›"));
```

Un code couleur par rôle, et pas plus :

| Rôle | Couleur |
|------|---------|
| invite, jetons de la trace | cyan |
| résultat | vert |
| erreur | rouge |
| titres de l'aide | jaune |
| état de la pile, notes | atténué |

Le gris atténué fait tout le travail : il **recule** ce qui est secondaire, ce qui
met en valeur le reste sans rien ajouter de criard.

## Le piège du remplissage, pour de bon

On l'avait annoncé au chapitre 11 :

```rust
let token = format!("{:<6}", step.token);
println!("  {} {:<24} …", style.cyan(&token), explain(…));
```

Le remplissage est appliqué **avant** la coloration. Dans l'autre ordre,
`{:<6}` compterait les dix caractères de `\x1b[36m+\x1b[0m` et n'ajouterait
aucun espace : les colonnes partiraient de travers dès la première ligne colorée.

La règle : **une chaîne colorée ne doit plus jamais être mesurée, alignée ou
tronquée.** Fais toute la mise en forme d'abord.

## Vérifie

```bash
cargo run -- "3 4 +"                 # coloré
cargo run -- "3 4 +" | cat           # brut : la sortie est un tuyau
NO_COLOR=1 cargo run -- "1 0 /"      # brut, malgré le terminal
cargo run -- "3 4 +" > out.txt       # out.txt ne contient que 7
```

Regarde le fichier avec `cat -v out.txt` : `-v` rend visibles les caractères de
contrôle. S'il n'y a que `7`, c'est gagné.

---

**Chapitre suivant :** [13 — Les tests](13-les-tests.md)
