# Annexe A — Décoder les erreurs du compilateur

Les treize erreurs que tu vas rencontrer en écrivant ce projet, dans l'ordre où
elles arrivent en général. Pour chacune : à quoi elle ressemble vraiment, ce que
le compilateur essaie de te dire, et quoi faire.

## D'abord, savoir lire un message

Un message `rustc` est toujours construit pareil :

```
error[E0308]: mismatched types          ← le code et le résumé
 --> src/main.rs:1:26                   ← où
  |
1 | let x: f64 = 3;
  |        ---   ^ expected `f64`, found integer
  |        |
  |        expected due to this          ← pourquoi il attendait ça
  |
help: use a float literal                ← la correction
  |
1 | let x: f64 = 3.0;
  |               ++
```

Trois réflexes :

1. **Lis jusqu'en bas.** Le `help:` arrive en dernier et contient très souvent la
   réponse exacte, parfois même le code à écrire.
2. **Le premier message d'abord.** Une seule vraie erreur en produit dix par effet
   domino. Corrige la première, recompile, et la moitié disparaît.
3. **`rustc --explain E0308`** ouvre une page d'explication complète avec des
   exemples. Ça marche pour tous les codes.

---

## E0308 — mismatched types

```
error[E0308]: mismatched types
1 | let x: f64 = 3;
  |        ---   ^ expected `f64`, found integer
help: use a float literal
1 | let x: f64 = 3.0;
```

**Ce qu'il dit** : tu as promis un type, tu en as fourni un autre.

**Le cas typique dans ce projet** : oublier le `.0`. En Rust, `3` est un entier et
`3.0` un flottant, et il n'y a **aucune conversion implicite** — jamais. C'est
volontaire : les conversions silencieuses entre entiers et flottants sont une
source classique de perte de précision.

**Quoi faire** : écris `3.0`. Pour convertir explicitement, `x as f64` ou
`f64::from(x)`.

---

## E0384 — cannot assign twice to immutable variable

```
error[E0384]: cannot assign twice to immutable variable `x`
1 | let x = 1; x = 2;
  |     -      ^^^^^ cannot assign twice to immutable variable
help: consider making this binding mutable
1 | let mut x = 1;
```

**Ce qu'il dit** : en Rust une variable est immuable par défaut (chapitre 03).

**Quoi faire** : ajoute `mut`. Mais pose-toi d'abord la question : as-tu vraiment
besoin de modifier, ou peux-tu créer une nouvelle variable ? Un `let` qui masque
le précédent est souvent plus clair qu'une mutation.

---

## E0596 — cannot borrow as mutable

```
error[E0596]: cannot borrow `v` as mutable, as it is not declared as mutable
1 | let v: Vec<f64> = Vec::new(); v.push(1.0);
  |                               ^ cannot borrow as mutable
help: consider changing this to be mutable
1 | let mut v: Vec<f64> = Vec::new();
```

**Ce qu'il dit** : `push` a besoin d'un `&mut self`, et ta variable n'est pas
`mut`.

C'est le cousin de E0384 : celui-ci concerne l'appel d'une méthode qui modifie,
l'autre l'affectation directe.

**Quoi faire** : `let mut v = …`. Si `v` est un paramètre de fonction, c'est la
signature qu'il faut changer : `fn f(v: &mut Vec<f64>)`.

---

## E0432 / E0433 — unresolved import

```
error[E0432]: unresolved import `eval`
1 | use eval::Engine;
  |     ^^^^ use of unresolved module or unlinked crate `eval`
```

**Ce qu'il dit** : le module `eval` n'existe pas de son point de vue.

**Le cas typique dans ce projet, et de loin le plus fréquent** : tu as créé
`src/eval.rs` mais oublié `mod eval;` dans `src/main.rs`. Créer le fichier ne
suffit pas — sans la déclaration, Rust ne le compile même pas (chapitre 06).

**Quoi faire** : ajoute `mod eval;` en tête de `main.rs`. Vérifie aussi le chemin :
depuis un autre module, c'est `crate::eval::Engine`.

---

## E0599 — no method named …

```
error[E0599]: no method named `flush` found for struct `Stdout` in the current scope
  = help: items from traits can only be used if the trait is in scope
help: trait `Write` which provides `flush` is implemented but not in scope;
      perhaps you want to import it
    + use std::io::Write;
```

**Ce qu'il dit** : la méthode existe, mais elle vient d'un **trait** que tu n'as
pas importé.

C'est une règle qui surprend au début : en Rust, une méthode de trait n'est
utilisable que si le trait est dans la portée. C'est ce qui évite qu'importer une
bibliothèque fasse apparaître cinquante méthodes sur tes types sans prévenir.

**Le cas typique dans ce projet** : `flush()` (chapitre 10) et `is_terminal()`
(chapitre 12).

**Quoi faire** : copie la ligne `use` proposée. Le compilateur te la donne toute
faite.

Quand le message ne propose rien, c'est que la méthode n'existe vraiment pas :
vérifie l'orthographe, et le type sur lequel tu l'appelles.

---

## E0277 — trait bound not satisfied

```
error[E0277]: `Vec<{float}>` doesn't implement `std::fmt::Display`
1 | println!("{}", v);
  |           --   ^ `Vec<{float}>` cannot be formatted with the default formatter
  = note: in format strings you may be able to use `{:?}` instead
```

**Ce qu'il dit** : tu demandes à un type quelque chose qu'il ne sait pas faire.

**Le cas typique** : afficher un `Vec` avec `{}`. Aucun `Vec` n'implémente
`Display`, parce qu'il n'existe pas de façon évidente de présenter une liste à un
humain (chapitre 03).

**Quoi faire** : `{:?}` pour du débogage, ou implémente `Display` toi-même si
c'est ton type (chapitre 05).

Le `{float}` entre accolades signifie « un type flottant que je n'ai pas encore
fixé ». C'est normal à ce stade de l'inférence.

---

## E0004 — non-exhaustive patterns

```
error[E0004]: non-exhaustive patterns: `E::B` not covered
2 | match e { E::A => {} }
  |       ^ pattern `E::B` not covered
help: ensure that all possible cases are being handled by adding a match arm
      with a wildcard pattern or an explicit pattern as shown
2 | match e { E::A => {}, E::B => todo!() }
```

**Ce qu'il dit** : ton `match` oublie un cas.

Ce n'est pas une contrainte, c'est un cadeau : le jour où tu ajoutes une variante
à `EvalError`, le compilateur te listera **tous** les `match` à mettre à jour.
Aucun cas ne peut être oublié par distraction.

**Quoi faire** : traite le cas manquant. Évite le `_ => {}` réflexe : il fait
taire l'erreur aujourd'hui *et* le rappel utile demain. Ne mets un `_` que quand
tu veux vraiment dire « tout le reste », comme dans `ops::apply`.

---

## E0382 — borrow of moved value

```
error[E0382]: borrow of moved value: `v`
2 | let v = vec![1.0]; take(v); println!("{}", v.len());
  |     -                   -                  ^ value borrowed here after move
  |     |                   value moved here
  |     move occurs because `v` has type `Vec<f64>`, which does not implement the `Copy` trait
note: consider changing this parameter type in function `take` to borrow instead
1 | fn take(v: Vec<f64>) -> usize
  |            ^^^^^^^^ this parameter takes ownership of the value
```

**Ce qu'il dit** : tu as **donné** la valeur, puis tu as voulu t'en resservir.

C'est la règle du propriétaire unique (chapitre 03). `take(v)` reçoit `v` en
possession ; à partir de là, `v` n'existe plus chez toi.

**Quoi faire**, dans l'ordre de préférence :

1. **Changer la signature en `&Vec<f64>` ou mieux `&[f64]`** si la fonction n'a
   besoin que de lire. C'est presque toujours la bonne réponse, et le compilateur
   la suggère lui-même.
2. `&mut` si elle doit modifier.
3. `.clone()` en dernier recours. Le message le propose (« consider cloning »),
   mais c'est la solution la plus coûteuse, et souvent le signe qu'on n'a pas
   posé la bonne signature.

Note la mention de `Copy` : avec un `f64`, ce problème n'existerait pas, puisque
les types `Copy` sont recopiés au lieu d'être déplacés.

---

## E0502 — cannot borrow as mutable because also borrowed as immutable

```
error[E0502]: cannot borrow `v` as mutable because it is also borrowed as immutable
1 | let first = &v[0]; v.push(2.0); println!("{first}");
  |              -     ^^^^^^^^^^^             ----- immutable borrow later used here
  |              |     mutable borrow occurs here
  |              immutable borrow occurs here
```

**Ce qu'il dit** : quelqu'un lit pendant que quelqu'un d'autre écrit.

Et ce n'est pas de la paranoïa : `push` peut réallouer le `Vec`, ce qui
déplacerait les données en mémoire et rendrait `first` pendante. Dans un langage
sans vérificateur d'emprunts, ce code compile et lit de la mémoire libérée.

**Le cas typique dans ce projet** : lire `engine.stack().len()` puis appeler
`engine.stack_mut()` dans la même expression (chapitre 08).

**Quoi faire** : sépare dans le temps. Un emprunt meurt à sa **dernière
utilisation**, pas à la fin du bloc — donc il suffit souvent de réordonner :

```rust
let len = engine.stack().len();
engine.stack_mut().swap(len - 1, len - 2);
```

Si tu ne peux pas séparer, copie la valeur dont tu as besoin avant (facile avec un
`f64`, qui est `Copy`).

---

## E0499 — cannot borrow as mutable more than once

```
error[E0499]: cannot borrow `v` as mutable more than once at a time
1 | let a = &mut v; let b = &mut v; a.push(1.0);
  |         ------          ^^^^^^  - first borrow later used here
  |         |               second mutable borrow occurs here
  |         first mutable borrow occurs here
```

**Ce qu'il dit** : deux écrivains en même temps sur la même donnée.

C'est la règle qui rend impossibles, à la compilation, les courses de données
entre threads — et les invalidations d'itérateur en mono-thread.

**Quoi faire** : n'en garde qu'un. Nomme l'emprunt mutable une seule fois et
sers-t'en plusieurs fois, comme dans `dup` au chapitre 08 :

```rust
let stack = engine.stack_mut();
stack.push(x);
stack.push(x);
```

---

## E0507 — cannot move out of index

```
error[E0507]: cannot move out of index of `Vec<String>`
1 | let s: String = v[0];
  |                 ^^^^ move occurs because value has type `String`,
  |                      which does not implement the `Copy` trait
help: consider borrowing here
1 | let s: String = &v[0];
```

**Ce qu'il dit** : tu essaies de sortir une valeur d'une collection sans la
retirer, alors qu'elle n'est pas `Copy`.

Autoriser ça laisserait un trou dans le `Vec` : un emplacement qui existe encore
mais dont la valeur a été emportée.

**Quoi faire** : emprunte (`&v[0]`), clone (`v[0].clone()`), ou retire pour de bon
(`v.remove(0)`, `v.pop()`).

**Pourquoi tu ne le verras pas dans ce projet** : notre pile contient des `f64`,
qui sont `Copy`. C'est précisément pour ça que `let a = self.stack[len - 2];`
fonctionne dans `pop2` sans la moindre cérémonie.

---

## E0106 — missing lifetime specifier

```
error[E0106]: missing lifetime specifier
1 | struct S { t: &str }
  |               ^ expected named lifetime parameter
help: consider introducing a named lifetime parameter
1 | struct S<'a> { t: &'a str }
```

**Ce qu'il dit** : tu ranges une référence dans une structure, et le compilateur
a besoin de savoir combien de temps la chose pointée est censée vivre.

**Quoi faire**, et la réponse est un choix de conception :

- **Suivre le `help:`** et ajouter `<'a>`. Rapide, sans allocation, mais la durée
  de vie se propage à tout ce qui touche ce type. C'est ce que fait la version
  optimisée (chapitre 17).
- **Passer à une `String`** possédée. Une allocation, et plus aucune contrainte.
  C'est ce que fait le tutoriel principal (chapitre 05).

Aucune des deux n'est « la bonne » dans l'absolu. Alloue quand c'est froid,
emprunte quand c'est chaud.

---

## E0597 — does not live long enough

```
error[E0597]: `s` does not live long enough
1 | let r; { let s = String::from("x"); r = &s; } println!("{r}");
  |              -                          ^^  -            - borrow later used here
  |              |                          |   `s` dropped here while still borrowed
  |              |                          borrowed value does not live long enough
```

**Ce qu'il dit** : tu gardes une référence vers quelque chose qui vient d'être
détruit.

C'est le bug qui, en C, s'appelle *use after free* et donne des plantages
aléatoires trois heures plus tard. Ici, il ne compile pas.

**Quoi faire** : fais vivre la valeur au moins aussi longtemps que la référence —
en général, sors-la du bloc :

```rust
let s = String::from("x");
let r = &s;
println!("{r}");
```

---

## Quand rien ne marche

1. **`cargo clean` puis `cargo check`.** Rare, mais un cache abîmé existe.
2. **Commente la moitié du code.** Bissection : la moitié qui reproduit l'erreur
   contient le problème.
3. **Simplifie jusqu'à l'absurde.** Réduis à dix lignes qui échouent encore. Neuf
   fois sur dix, tu vois le problème pendant la réduction.
4. **Écris les types explicitement.** Quand l'inférence part de travers, l'erreur
   apparaît souvent loin de la cause ; annoter recentre le message.
5. **Demande.** Le forum <https://users.rust-lang.org> répond vite et bien, à
   condition de donner un exemple minimal qui reproduit — voir le point 3.

---

**Retour au [sommaire](README.md).**
