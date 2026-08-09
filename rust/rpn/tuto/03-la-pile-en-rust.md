# 03 — La pile en Rust

Objectif du chapitre : évaluer `3 4 +` en dur, sans lire quoi que ce soit. Tout
le reste du projet est une extension de ces vingt lignes.

## Quel type pour les nombres ?

Rust a une dizaine de types numériques. Pour une calculatrice, le choix se joue
entre trois candidats.

**`i32` (entier signé 32 bits)** — non. `10 3 /` donnerait `3`. Une calculatrice
qui ne sait pas faire `2.5` n'est pas une calculatrice.

**`f32` (flottant 32 bits)** — non. Environ 7 chiffres significatifs. `f32` ne
sert que quand la mémoire ou la bande passante compte vraiment (graphismes,
réseaux de neurones, gros tableaux). Ici on manipule quelques valeurs : autant
prendre la précision.

**`f64` (flottant 64 bits)** — oui. Environ 15 à 17 chiffres significatifs. C'est
le type des flottants en JavaScript, en Python, et le type par défaut des
littéraux décimaux en Rust. Toutes les fonctions mathématiques de la
bibliothèque standard (`sqrt`, `ln`, `sin`…) sont directement disponibles dessus.

Petit avertissement qui vaut pour tous les langages : les flottants sont des
**approximations binaires**. `0.1 + 0.2` ne fait pas exactement `0.3`, ni en Rust
ni ailleurs. On vivra avec — et on soignera l'affichage au chapitre 10 pour que
`7.0` s'écrive `7` et pas `7.0000000000000001`.

## Quel type pour la pile ?

**Un tableau `[f64; 64]`** — la taille est fixée à la compilation. Il faudrait
gérer un compteur de remplissage à la main et décider d'une limite arbitraire.
On saura le faire plus tard ; ce n'est pas le bon outil ici.

**Une `VecDeque`** — une file à deux bouts. On n'a besoin que d'un seul bout.
Payer pour le second serait absurde.

**Une `LinkedList`** — jamais. Une liste chaînée disperse ses éléments dans la
mémoire, ce qui ruine le cache du processeur. En pratique elle est plus lente
qu'un `Vec` sur à peu près tout, y compris là où la théorie dit le contraire.

**Un `Vec<f64>`** — oui. Tableau redimensionnable, contigu en mémoire, avec
exactement les deux méthodes qu'il nous faut :

```rust
stack.push(3.0);
let x = stack.pop();
```

`push` ajoute au bout, `pop` retire le bout.

Le bout d'un `Vec`, c'est exactement le sommet d'une pile. `Vec` **est** une pile
en Rust ; il n'existe pas de type `Stack` séparé, et il n'en faut pas.

## `pop` ne rend pas un nombre

Écris ceci dans `src/main.rs` :

```rust
fn main() {
    let mut stack: Vec<f64> = Vec::new();

    stack.push(3.0);
    stack.push(4.0);

    let b = stack.pop();
    let a = stack.pop();

    println!("{a:?} {b:?}");
}
```

```bash
cargo run
```

Sortie : `Some(3.0) Some(4.0)`.

`pop` ne rend pas un `f64` mais un **`Option<f64>`**, parce que la pile peut être
vide. `Option<T>` est une énumération de la bibliothèque standard :

```rust
enum Option<T> {
    Some(T),
    None,
}
```

C'est **la** réponse de Rust au problème du `null`. Dans la plupart des langages,
n'importe quelle référence peut valoir `null` et exploser à l'exécution ; en
Rust, une valeur qui peut être absente le dit **dans son type**, et le
compilateur t'oblige à traiter le cas. Le milliard de dollars d'erreurs qu'a
coûté le `null` s'arrête ici.

Pour sortir la valeur d'un `Option`, la façon la plus explicite est le `match` :

```rust
match stack.pop() {
    Some(value) => println!("j'ai {value}"),
    None => println!("pile vide"),
}
```

Le `match` de Rust est **exhaustif** : si tu oublies une branche, ça ne compile
pas. C'est ce qui fait qu'on ne peut pas ignorer un cas par distraction.

### Et `unwrap()` ?

```rust
let b = stack.pop().unwrap();
```

`unwrap()` dit : « donne-moi la valeur, et si c'est `None`, plante ». C'est
pratique pour bricoler, et **inacceptable dans un programme livré** : ton
utilisateur tape `+` sur une pile vide et le programme s'arrête sur un message
incompréhensible.

On va s'en servir dans ce chapitre et le suivant, puis on le supprimera
entièrement au chapitre 05. Repère-le comme une dette qu'on va rembourser.

## Le premier calcul

```rust
fn main() {
    let mut stack: Vec<f64> = Vec::new();

    stack.push(3.0);
    stack.push(4.0);

    let b = stack.pop().unwrap();
    let a = stack.pop().unwrap();
    stack.push(a + b);

    println!("{stack:?}");
}
```

Sortie : `[7.0]`.

Note l'ordre : `b` d'abord, `a` ensuite — c'est le piège du chapitre 02. Ici avec
`+` ça ne se voit pas ; remplace par `a - b` et essaie de mélanger, tu obtiendras
`1.0` au lieu de `-1.0`.

## Trois mots de Rust au passage

### `mut`

```rust
let stack = Vec::new();
let mut stack = Vec::new();
```

En Rust une variable est **immuable par défaut**. Pour la modifier il faut le
demander avec `mut`. C'est l'inverse de presque tous les langages, et c'est
volontaire : la mutation est ce qui rend un programme dur à suivre, donc elle
doit être visible. Enlève le `mut` et lis l'erreur : le compilateur te propose
littéralement la correction.

### `{stack:?}` et le trait `Debug`

La première de ces deux lignes ne compile pas, la seconde affiche `[7.0]` :

```rust
println!("{stack}");
println!("{stack:?}");
```

Rust a deux façons d'afficher une valeur :

- `{}` utilise le trait `Display` — l'affichage *pour un humain*. Un `Vec` n'en a
  pas : personne ne sait comment tu veux présenter une liste à l'utilisateur.
- `{:?}` utilise le trait `Debug` — l'affichage *pour un développeur*, avec les
  crochets et les guillemets. C'est fait pour être lu dans une trace, pas dans
  une interface.

Un **trait**, c'est ce que d'autres langages appellent une interface : un
ensemble de méthodes qu'un type peut fournir. On en implémentera un nous-mêmes au
chapitre 05.

### Le typage : quand faut-il l'écrire ?

```rust
let mut stack: Vec<f64> = Vec::new();
let mut stack = Vec::new();
```

Rust **infère** le type dans l'immense majorité des cas : si tu fais un
`push(3.0)` juste après, il déduit tout seul `Vec<f64>`. Ici on l'écrit quand
même, parce que sans le `push` qui suit il ne pourrait pas deviner, et surtout
parce que sur la signature d'une fonction, le type est une documentation.

Règle courante : **on annote les signatures de fonctions (c'est obligatoire), on
laisse inférer à l'intérieur du corps** sauf quand ça aide à la lecture.

## La possession, en deux minutes

C'est l'idée centrale de Rust. Trois règles :

1. Chaque valeur a **un seul** propriétaire.
2. Quand le propriétaire sort de sa portée, la valeur est libérée.
3. On peut **emprunter** une valeur : `&T` en lecture (autant qu'on veut, en même
   temps), `&mut T` en écriture (un seul à la fois, et aucun `&T` pendant ce temps).

Conséquence : pas de ramasse-miettes, et pas de fuite ni de double libération
non plus. Le compilateur place les libérations pour toi, et refuse le code où
deux endroits pourraient modifier la même chose en même temps.

Regarde la différence :

```rust
fn taille(stack: &Vec<f64>) -> usize {
    stack.len()
}

fn vide(stack: &mut Vec<f64>) {
    stack.clear();
}
```

- `&Vec<f64>` : « prête-le-moi, je regarde seulement ».
- `&mut Vec<f64>` : « prête-le-moi, je vais le modifier ».
- `Vec<f64>` tout court : « donne-le-moi », l'appelant ne peut plus s'en servir.

Presque tout le projet utilise `&mut Vec<f64>` ou `&[f64]`, jamais la troisième
forme : on ne veut pas déplacer la pile, on veut travailler dessus.

`f64`, lui, est un cas à part : il implémente `Copy`. Les types petits et
simples (nombres, `bool`, `char`) sont recopiés au lieu d'être déplacés, parce
qu'une copie de 8 octets ne coûte rien. C'est pour ça qu'on écrit `stack.push(a)`
puis qu'on peut encore utiliser `a` juste après.

---

**Chapitre suivant :** [04 — Lire une ligne et la découper](04-lire-une-ligne-et-la-decouper.md)
