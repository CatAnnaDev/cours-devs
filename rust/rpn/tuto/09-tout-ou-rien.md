# 09 — Tout ou rien

Un chapitre court, sur un détail que presque personne ne traite, et qui fait
toute la différence entre un jouet et un outil.

## Le bug

Tape ceci mentalement dans le REPL qu'on écrira au chapitre suivant :

```
[3 4] › + oups *
```

Que se passe-t-il avec le code actuel ?

1. `+` est exécuté : la pile devient `[7]`.
2. `oups` échoue : on renvoie `EvalError::Unknown`.
3. `*` n'est jamais atteint.

L'utilisateur voit un message d'erreur… et sa pile a changé quand même. Elle vaut
`[7]`, plus `[3 4]`. Il a perdu ses données à cause d'une faute de frappe, et rien
ne le lui dit.

C'est **exactement** le problème qu'une base de données appelle une transaction
non atomique. La règle qu'on veut est simple :

> Une ligne réussit entièrement, ou elle ne change rien.

## Pourquoi la ligne et pas le jeton ?

On pourrait imaginer restaurer après chaque jeton. Ça n'a pas de sens : chaque
jeton *doit* modifier la pile, c'est son travail. L'unité qui compte pour
l'utilisateur, c'est ce qu'il a tapé avant d'appuyer sur Entrée. Donc la ligne.

## La solution

Sauvegarder la pile avant, restaurer en cas d'erreur.

```rust
pub fn eval_line(&mut self, line: &str) -> Result<(), EvalError> {
    let backup = self.stack.clone();

    for token in line.split_whitespace() {
        if let Err(error) = self.eval_token(token) {
            self.stack = backup;
            return Err(error);
        }
    }

    Ok(())
}
```

C'est tout. Quatre lignes.

### `if let`

```rust
if let Err(error) = self.eval_token(token) {
    ...
}
```

`if let` est un `match` à une seule branche qui vaut la peine d'être écrite. Ici
il dit : « si le résultat correspond au motif `Err(error)`, exécute ce bloc ;
sinon passe ». C'est plus court que :

```rust
match self.eval_token(token) {
    Ok(()) => {}
    Err(error) => { ... }
}
```

Pourquoi pas simplement `self.eval_token(token)?` ? Parce que le `?` renverrait
l'erreur immédiatement, sans nous laisser restaurer la pile. On a besoin de faire
quelque chose *avant* de partir.

### `self.stack = backup;`

Une affectation, pas une copie. `backup` est **déplacé** dans `self.stack`, et
l'ancienne valeur de `self.stack` (la pile abîmée) est libérée au passage. Pas
d'allocation supplémentaire, pas de fuite : le compilateur a inséré la libération
tout seul.

Après cette ligne, `backup` n'existe plus, et Rust t'empêcherait de l'utiliser.
C'est la règle du propriétaire unique, chapitre 03.

## Ça coûte quoi ?

Un `clone()` du `Vec` à chaque ligne évaluée, c'est-à-dire **une allocation par
appui sur Entrée**. Une pile de dix éléments fait 80 octets. Le temps de frappe
de l'utilisateur est cent millions de fois plus long.

Le prix est nul, et le code reste lisible par quiconque. C'est le bon arbitrage.

Si un jour on évaluait un million de lignes dans une boucle serrée — un fichier
de script, par exemple —, cette allocation deviendrait mesurable. On la
supprimerait alors avec un tampon réutilisé, gardé dans l'`Engine` :

```rust
pub struct Engine {
    stack: Vec<f64>,
    scratch: Vec<f64>,
}
```

`scratch.clear()` puis `scratch.extend_from_slice(&self.stack)` réutilise la
mémoire déjà allouée au lieu d'en demander de la neuve. Le chapitre 15 détaille.

Mais **on ne le fait pas maintenant**, et c'est le vrai enseignement de ce
chapitre : optimiser un chemin qu'on ne parcourt qu'une fois par seconde, c'est
compliquer son code pour rien. Optimise ce qui est chaud, mesure avant, garde le
reste simple.

## Vérifie

```rust
fn main() {
    let mut engine = Engine::new();

    engine.eval_line("1 2 3").unwrap();
    println!("avant : {:?}", engine.stack());

    let result = engine.eval_line("+ oups *");
    println!("résultat : {result:?}");
    println!("après : {:?}", engine.stack());
}
```

```
avant : [1.0, 2.0, 3.0]
résultat : Err(Unknown("oups"))
après : [1.0, 2.0, 3.0]
```

La pile est intacte. C'est le comportement qu'on gardera jusqu'à la fin, et il
sera couvert par un test au chapitre 13.

## Un mot sur `unwrap()` dans `main`

Tu as sans doute remarqué le `.unwrap()` ci-dessus. Dans du code de test ou une
petite démonstration jetable, c'est acceptable : si ça échoue, c'est un bug du
programmeur, et un plantage immédiat avec la ligne exacte est la meilleure
réaction possible (relis la règle du chapitre 04).

Ce qui n'est pas acceptable, c'est `unwrap()` sur une donnée venue de
l'utilisateur ou du système de fichiers. Le `main` définitif du chapitre 10 n'en
contient aucun.

---

**Chapitre suivant :** [10 — La ligne de commande](10-la-ligne-de-commande.md)
