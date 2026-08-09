# 14 — Finitions

Le programme est complet. Reste ce qui sépare « ça marche chez moi » de « c'est
livrable ».

## L'aide

Un outil en ligne de commande sans `--help` est un outil qu'on n'utilise pas.

```rust
fn print_help(style: Style) {
    println!();
    println!("{}", style.bold("blap — notation polonaise inverse (RPN)"));
    println!();
    println!("{}", style.yellow("Saisie"));
    println!("  Empile les nombres puis applique l'opérateur : `3 4 + 2 *` donne 14");
    println!("  Décimaux, négatifs et notation scientifique acceptés : `-1.5e3`");
    println!();
    println!("{}", style.yellow("Opérateurs"));
    println!("  +  -  *  /          arithmétique");
    println!("  %  mod              modulo             ^  **  pow        puissance");
    println!();
}
```

(La version complète fait une trentaine de lignes : voir [`src/main.rs`](../src/main.rs).)

Une suite de `println!` plutôt qu'une seule grande chaîne : c'est plus long, mais
on peut insérer une couleur ligne par ligne, et surtout **on voit l'alignement des
colonnes directement dans le code source**. Une chaîne multiligne avec des `\n\` en
fin de ligne serait illisible à l'édition.

Accessible de deux façons — `blap --help` en argument, `?` ou `help` dans le
REPL —, parce que l'utilisateur n'a pas à deviner laquelle tu as prévue.

Trois règles pour une aide utile :

1. **Un exemple concret dans les trois premières lignes.** Personne ne lit une
   liste de règles ; tout le monde lit `3 4 + 2 *` donne `14`.
2. **Groupée par thème**, pas par ordre alphabétique.
3. **Les synonymes visibles.** `^ ** pow` sur la même ligne évite l'aller-retour
   « est-ce que ça marche aussi avec… ».

## `cargo fmt`

```bash
cargo fmt
```

Reformate tout le projet selon le style officiel. **Aucune option à configurer,
aucun débat.** C'est le grand mérite de `rustfmt` : la question de la mise en
forme est réglée une fois pour toutes à l'échelle de l'écosystème, donc tout code
Rust que tu liras ressemblera au tien.

```bash
cargo fmt --check
```

Ne modifie rien mais échoue si quelque chose n'est pas formaté. C'est la
commande à mettre dans une intégration continue.

Prends l'habitude de lancer `cargo fmt` avant chaque commit. Tu peux même le
brancher sur la sauvegarde dans ton éditeur.

## `cargo clippy`

```bash
cargo clippy --all-targets
```

`clippy` est un linter, c'est-à-dire un compilateur supplémentaire qui connaît
plusieurs centaines de motifs de code perfectible. Il ne signale pas des erreurs
mais des maladresses :

```
warning: this expression creates a reference which is immediately dereferenced
warning: length comparison to zero — consider using `is_empty()`
warning: use of `unwrap` on an `Option` value
```

Ce qui le rend précieux quand on débute : **presque chaque avertissement est une
leçon d'idiome**. Il ne dit pas seulement « c'est mal », il propose l'écriture
attendue, souvent avec un lien vers l'explication.

`--all-targets` inclut les tests, qu'il ignorerait sinon.

Vise le zéro avertissement. Quand tu es certain qu'un cas est justifié, tu peux
faire taire une règle précise :

```rust
#[allow(clippy::nom_de_la_regle)]
```

mais fais-le rarement, et jamais sans savoir ce que la règle voulait dire.

## Compiler pour de vrai

```bash
cargo build --release
./target/release/blap "3 4 + 2 *"
```

Le binaire est autonome : aucune dépendance à installer, aucun runtime. Tu peux
le copier dans `~/.local/bin` ou `/usr/local/bin` et l'appeler `blap` depuis
n'importe où.

```bash
cargo install --path .
```

fait la même chose proprement, en installant dans `~/.cargo/bin` (qui est déjà
dans ton `PATH` si tu as suivi le chapitre 01).

Rappel du chapitre 01 : **ne juge jamais la vitesse d'un programme Rust compilé
en debug.** L'écart avec `--release` va de 10 à 100.

## Le `.gitignore`

`cargo new` l'a déjà écrit :

```
/target
```

C'est presque tout ce qu'il faut. `target/` peut peser plusieurs centaines de
mégaoctets et se régénère intégralement.

Deux questions récurrentes :

**`Cargo.lock`, on le commit ou pas ?** Il fige les versions exactes de toutes les
dépendances. La règle officielle : **oui pour un binaire** (tu veux que tout le
monde compile exactement la même chose), **non pour une bibliothèque** (tu veux
que tes utilisateurs choisissent leurs versions). Ici c'est un binaire, donc on le
commit. Cela dit, avec zéro dépendance, il ne contient que ton propre paquet.

**Et les fichiers de mon éditeur ?** `.idea/`, `.vscode/` : plutôt dans ton
`.gitignore` global (`~/.config/git/ignore`), pour ne pas imposer ton outil aux
autres contributeurs.

## Le README

Le fichier que les gens lisent en premier — souvent le seul. L'ordre qui marche :

1. **Une phrase** qui dit ce que c'est.
2. **Comment l'installer et le lancer**, en commandes copiables.
3. **Un exemple qui fonctionne**, avec sa sortie réelle.
4. **Le tableau des fonctionnalités.**
5. **Les détails qui comptent** — ce que ton programme fait de particulier.
6. **Comment lancer les tests**, comment le projet est organisé.

Ce qui compte : **la sortie montrée doit être la vraie.** Rien ne détruit plus
vite la confiance qu'un exemple de README qui ne produit pas ce qui est écrit.
Copie-colle depuis ton terminal, ne réécris pas de mémoire.

Regarde le [README de ce projet](../README.md) : il suit exactement cette
structure.

## Un dernier tour

```bash
cargo fmt
cargo clippy --all-targets
cargo test
cargo build --release
```

Quatre commandes, dans cet ordre, avant chaque publication. Si les quatre passent
en silence, c'est bon.

## Et git

```bash
git add -A
git commit -m "calculatrice RPN"
```

`cargo new` a déjà fait `git init`. Si tu veux le pousser quelque part, ajoute une
origine et pousse — mais le premier commit, c'est maintenant, pas quand ce sera
« propre ». Un dépôt avec l'historique de sa construction vaut mieux qu'un dépôt
parfait avec un seul commit.

---

**Chapitre suivant :** [15 — Pour aller plus loin](15-pour-aller-plus-loin.md)
