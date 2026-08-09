# 13 — Les tests

Rust intègre les tests au langage : pas de bibliothèque à choisir, pas de
configuration. `cargo test` et c'est parti.

## La forme

À la fin de `src/eval.rs` :

```rust
#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn empile_les_nombres() {
        let mut engine = Engine::new();
        engine.eval_line("1 2 3").unwrap();
        assert_eq!(engine.stack(), [1.0, 2.0, 3.0]);
    }
}
```

```bash
cargo test
```

Trois éléments à comprendre.

### `#[cfg(test)]`

**Compilation conditionnelle.** Ce module n'existe que lorsqu'on compile pour les
tests. Dans `cargo build --release`, il disparaît complètement : pas une
instruction, pas un octet dans le binaire livré.

C'est ce qui permet d'écrire ses tests **à côté du code testé** sans alourdir le
produit. La plupart des langages obligent à les mettre dans une arborescence
séparée ; ici, le test de `fmt_num` est vingt lignes sous `fmt_num`, et on le lit
en même temps.

### `use super::*;`

`super` est le module parent — ici `eval`. L'étoile importe tout ce qu'il
contient. Le module `tests` étant *à l'intérieur* de `eval`, il a accès même aux
éléments privés : c'est voulu, et c'est ce qui permet de tester des fonctions
internes sans les rendre publiques pour de mauvaises raisons.

C'est aussi pour ça qu'on peut écrire, dans les tests de `style.rs` :

```rust
let style = Style { color: true };
```

alors que le champ est privé. Le test est dans le module, il a le droit.

### `#[test]`

Marque une fonction comme test. Elle ne prend pas de paramètre et ne rend rien.
Elle **réussit si elle ne panique pas**.

`cargo test` les exécute **en parallèle**, sur plusieurs threads. Conséquence
pratique : chaque test doit être indépendant, et créer son propre `Engine`.

## Les macros d'assertion

```rust
assert!(condition);
assert!(condition, "message si ça rate : {valeur}");
assert_eq!(gauche, droite);
assert_ne!(gauche, droite);
```

Préfère toujours `assert_eq!` à `assert!(a == b)` : en cas d'échec, il affiche
les deux valeurs.

```
assertion `left == right` failed
  left: [7.0]
 right: [8.0]
```

Là où `assert!(a == b)` dirait seulement « assertion failed ». C'est aussi pour
ça qu'on met `#[derive(Debug)]` partout (chapitre 05) : sans `Debug`, `assert_eq!`
ne compile même pas.

## Réduire le bruit avec un helper

Écrire trois lignes de mise en place par test devient vite pénible :

```rust
fn eval(line: &str) -> Vec<f64> {
    let mut engine = Engine::new();
    engine.eval_line(line).expect("évaluation valide");
    engine.stack().to_vec()
}
```

Les tests deviennent alors des affirmations pures :

```rust
#[test]
fn respecte_l_ordre_des_operandes() {
    assert_eq!(eval("10 3 -"), [7.0]);
    assert_eq!(eval("10 4 /"), [2.5]);
}

#[test]
fn enchaine_les_operations() {
    assert_eq!(eval("5 1 2 + 4 * + 3 -"), [14.0]);
}
```

Une ligne, un cas. C'est le format qui permet d'en écrire cinquante sans fatigue.

### `expect` plutôt que `unwrap`

```rust
.expect("évaluation valide")
```

`expect` fait comme `unwrap` mais affiche ton message en plus de l'erreur. Dans
un test, ça transforme « called `Result::unwrap()` on an `Err` value » en quelque
chose qui dit *ce qu'on attendait*. Coût : zéro. Prends l'habitude.

### `to_vec()`

`engine.stack()` rend `&[f64]`, une vue empruntée à l'`Engine`, qui va disparaître
à la fin de la fonction. `to_vec()` en fait une copie possédée, qu'on peut
renvoyer. Le compilateur refuserait la version sans.

## Que tester ?

Pas « toutes les lignes ». Les endroits où l'on peut se tromper.

### Ce qui a un sens métier

```rust
#[test]
fn respecte_l_ordre_des_operandes() {
    assert_eq!(eval("10 3 -"), [7.0]);
    assert_eq!(eval("10 4 /"), [2.5]);
}
```

C'est le piège annoncé au chapitre 02 : intervertir `a` et `b` donnerait `-7` et
`0.4`. Ce test-là est le plus important de tout le projet, parce que c'est l'erreur
qu'on a le plus de chances de réintroduire un jour.

### Les erreurs, pas seulement les succès

```rust
#[test]
fn signale_le_manque_d_operandes() {
    let mut engine = Engine::new();
    assert_eq!(
        engine.eval_line("3 +"),
        Err(EvalError::NeedsOperands { op: String::from("+"), need: 2, got: 1 })
    );
}
```

Voilà le bénéfice concret de l'`enum` du chapitre 05 : on compare une **valeur**,
pas un message. On peut retraduire toute l'interface sans casser un seul test.

Un helper rend la série lisible :

```rust
fn error(line: &str) -> EvalError {
    let mut engine = Engine::new();
    engine.eval_line(line).expect_err("erreur attendue")
}

#[test]
fn garde_fous() {
    assert_eq!(error("1 0 /"), EvalError::DivByZero);
    assert_eq!(error("0 inv"), EvalError::DivByZero);
    assert_eq!(error("-1 sqrt"), EvalError::Domain(String::from("sqrt")));
    assert_eq!(error("171 fact"), EvalError::Domain(String::from("fact")));
    assert_eq!(error("bidule"), EvalError::Unknown(String::from("bidule")));
}
```

`expect_err` est le symétrique d'`expect` : il panique si le résultat est `Ok`.
Autrement dit, **le test échoue aussi si l'erreur n'arrive pas** — c'est le point
essentiel. Un test d'erreur qui passerait en cas de succès ne teste rien.

### Les invariants

```rust
#[test]
fn restaure_la_pile_apres_une_erreur() {
    let mut engine = Engine::new();
    engine.eval_line("1 2 3").unwrap();
    assert!(engine.eval_line("+ oups *").is_err());
    assert_eq!(engine.stack(), [1.0, 2.0, 3.0]);
}
```

C'est la propriété du chapitre 09, celle qu'on ne verrait jamais à l'œil nu et
qu'un remaniement futur pourrait casser en silence. **Un invariant non testé est
un invariant qui va disparaître.**

### Les cas limites

```rust
#[test]
fn refuse_les_valeurs_non_finies() {
    let mut engine = Engine::new();
    assert_eq!(engine.eval_line("inf"), Err(EvalError::Unknown(String::from("inf"))));
    assert_eq!(engine.eval_line("NaN"), Err(EvalError::Unknown(String::from("NaN"))));
}

#[test]
fn affiche_les_entiers_sans_partie_decimale() {
    assert_eq!(fmt_num(7.0), "7");
    assert_eq!(fmt_num(-0.0), "0");
    assert_eq!(fmt_num(2.5), "2.5");
}

#[test]
fn fonctions_unaires() {
    assert_eq!(eval("0 !"), [1.0]);
}
```

`inf`, `NaN`, `-0.0`, `0!` : les valeurs qu'on n'aurait pas pensé à essayer à la
main. Chacun de ces tests correspond à une décision explicite prise dans un
chapitre précédent ; le test est ce qui l'inscrit dans la durée.

### Ce qu'on ne teste pas

Le REPL et les couleurs. Tester une boucle interactive demanderait d'injecter une
fausse entrée et de capturer la sortie — ce qui est possible en rendant `repl`
générique sur `impl BufRead` et `impl Write`, mais qui complique le code pour
tester… une boucle de trente lignes sans logique métier.

L'arbitrage classique : **on teste ce qui décide, pas ce qui affiche.** Ici toute
la décision est dans `eval.rs` et `ops.rs`, et ces deux-là sont couverts.

## Comparer des flottants dans un test

Un piège général, qu'on esquive ici sans le vouloir :

Ce test-ci **échoue** :

```rust
assert_eq!(eval("0.1 0.2 +"), [0.3]);
```

`0.1 + 0.2` vaut `0.30000000000000004` en binaire. La bonne façon est de comparer
à une tolérance près :

```rust
let result = eval("0.1 0.2 +")[0];
assert!((result - 0.3).abs() < 1e-10);
```

Nos tests utilisent exprès des valeurs exactement représentables — entiers,
moitiés, quarts — pour rester lisibles. Quand ce n'est pas possible, passe par la
tolérance.

## Les commandes utiles

```bash
cargo test                        # tout
cargo test garde_fous             # les tests dont le nom contient "garde_fous"
cargo test -- --nocapture         # laisse passer les println! des tests
cargo test -- --test-threads=1    # en série, utile pour déboguer
```

Par défaut, `cargo test` **capture** la sortie des tests qui passent et ne montre
que celle des tests qui échouent. D'où `--nocapture` quand on veut voir ses traces
de débogage.

Le `--` sépare, comme toujours, les options de `cargo` de celles du binaire de
test.

## Où placer les tests

Deux emplacements, deux usages :

- **`#[cfg(test)] mod tests` dans le fichier** — tests unitaires. Accès aux
  éléments privés. C'est ce qu'on fait, et c'est le cas majoritaire.
- **`tests/` à la racine du projet** — tests d'intégration. Chaque fichier y est
  compilé comme un programme séparé qui utilise ton projet **de l'extérieur**,
  donc uniquement via ce qui est `pub`. Utile pour une bibliothèque, pour
  vérifier que l'API publique est utilisable.

Notre projet est un binaire sans bibliothèque : les tests d'intégration n'auraient
rien à importer. On reste sur le premier.

## Le résultat

```
running 17 tests
test eval::tests::empile_les_nombres ... ok
test eval::tests::respecte_l_ordre_des_operandes ... ok
...
test result: ok. 17 passed; 0 failed; 0 ignored
```

Dix-sept tests pour environ 580 lignes de code. Ce n'est pas énorme, et
c'est suffisant : chacun couvre une décision qu'on a prise en connaissance de
cause au fil des chapitres.

---

**Chapitre suivant :** [14 — Finitions](14-finitions.md)
