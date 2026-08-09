# 08 — Manipuler la pile

Jusqu'ici tous nos opérateurs font du calcul. Une vraie calculatrice RPN a une
seconde famille : les mots qui **réorganisent la pile** sans rien calculer.

C'est ce qui transforme la RPN en petit langage. Sans eux, impossible de réutiliser
une valeur : `dup *` élève au carré sans retaper le nombre.

## Le vocabulaire, hérité de Forth

Notation : la pile est écrite du fond vers le sommet, le sommet est à droite.

| Mot | Avant | Après | Rôle |
|-----|-------|-------|------|
| `dup` | `[… a]` | `[… a a]` | duplique le sommet |
| `drop` | `[… a]` | `[…]` | jette le sommet |
| `swap` | `[… a b]` | `[… b a]` | échange les deux du sommet |
| `over` | `[… a b]` | `[… a b a]` | recopie l'avant-dernier au sommet |
| `rot` | `[… a b c]` | `[… b c a]` | fait remonter le troisième |
| `clear` | n'importe quoi | `[]` | vide tout |

Ces six mots suffisent à réarranger n'importe quoi. `dup`, `swap` et `drop` sont
ceux qu'on utilise vraiment ; `over` et `rot` servent pour les formules à trois
termes.

Exemple concret — la moyenne de trois nombres :

```
3 4 5 sum 3 /
```

Et le carré de l'hypoténuse, sans variables :

```
3 dup * 4 dup * +
```

## `dup`

```rust
"dup" => {
    let x = engine.pop(token)?;
    let stack = engine.stack_mut();
    stack.push(x);
    stack.push(x);
    Ok(())
}
```

On dépile puis on empile deux fois. On pourrait faire plus court avec
`last().copied()`, mais dépiler donne gratuitement le bon message d'erreur sur
pile vide, via `pop`.

Note le `let stack = engine.stack_mut();` : on prend l'emprunt mutable **une
fois**, puis on s'en sert deux fois. Écrire `engine.stack_mut().push(x)` deux
fois compilerait aussi, mais nommer l'emprunt est plus lisible et évite de
répéter la chaîne d'appels.

Et note surtout que `x` est utilisé deux fois après avoir été poussé une première
fois : c'est possible parce que `f64` est `Copy` (chapitre 03). Avec une `String`,
il aurait fallu `.clone()`.

## `drop`

```rust
"drop" => {
    engine.pop(token)?;
    Ok(())
}
```

On dépile et on jette. Le `?` sert quand même : sur pile vide, `drop` doit
signaler une erreur, pas faire semblant d'avoir travaillé.

Le résultat de `pop` n'est pas affecté à une variable. Rust avertit quand on
ignore un `Result`, mais ici le `?` l'a déjà consommé : ce qui reste est un `f64`
inutilisé, ce qui est parfaitement légal et silencieux.

## `swap`

```rust
"swap" => {
    let len = engine.stack().len();
    if len < 2 {
        return Err(EvalError::NeedsOperands {
            op: token.to_string(),
            need: 2,
            got: len,
        });
    }
    engine.stack_mut().swap(len - 1, len - 2);
    Ok(())
}
```

Ici on **ne dépile pas**. `swap` sur une tranche échange deux éléments sur place :
aucune valeur ne bouge en dehors du tableau, aucun redimensionnement.

Attention à la séquence : on lit la longueur avec `engine.stack()` (emprunt
partagé), et on ferme cet emprunt avant de demander `engine.stack_mut()` (emprunt
exclusif). Les deux ne peuvent pas coexister — c'est la règle du chapitre 03. Si
tu écrivais :

```rust
let stack = engine.stack_mut();
let len = engine.stack().len();
```

le compilateur refuserait, avec un message qui dit précisément ça : on ne peut
pas emprunter en lecture pendant qu'un emprunt en écriture est vivant.

En pratique, le compilateur est plus malin qu'il n'y paraît : un emprunt se
termine à sa **dernière utilisation**, pas à la fin du bloc. C'est ce qui fait que
notre version passe.

## `over`

```rust
"over" => {
    let (a, b) = engine.pop2(token)?;
    let stack = engine.stack_mut();
    stack.push(a);
    stack.push(b);
    stack.push(a);
    Ok(())
}
```

On réutilise `pop2` — donc gratuitement le bon message si la pile a moins de deux
éléments. On repose `a`, `b`, puis une copie de `a`.

## `rot`

```rust
"rot" => {
    let stack = engine.stack_mut();
    let len = stack.len();
    if len < 3 {
        return Err(EvalError::NeedsOperands {
            op: token.to_string(),
            need: 3,
            got: len,
        });
    }
    stack[len - 3..].rotate_left(1);
    Ok(())
}
```

`stack[len - 3..]` est une **tranche** des trois derniers éléments. La syntaxe
`a..b` désigne un intervalle ; en omettant la borne de fin, on va jusqu'au bout.

`rotate_left(1)` décale tout d'un cran vers la gauche, le premier élément
revenant à la fin : `[a, b, c]` devient `[b, c, a]`. C'est exactement la
définition de `rot`.

Trois `pop` suivis de trois `push` auraient donné le même résultat en six
opérations et deux ajustements de longueur. `rotate_left` fait le travail sur
place, en une passe. Quand la bibliothèque standard a déjà le mot exact pour ce
que tu veux dire, utilise-le : c'est plus court, plus rapide, et surtout ça se lit.

## `clear`

```rust
"clear" | "cls" => {
    engine.stack_mut().clear();
    Ok(())
}
```

`clear()` remet la longueur à zéro **sans libérer la mémoire** : la capacité du
`Vec` est conservée, donc les prochains `push` ne réallouent pas. C'est
exactement ce qu'on veut pour une structure réutilisée en boucle.

`clear` ne peut pas échouer, même sur une pile vide. Vider ce qui est déjà vide
n'est pas une erreur.

## Les réductions : `sum` et `prod`

Celles-là consomment **toute** la pile et laissent un seul nombre.

```rust
fn reduce(
    engine: &mut Engine,
    op: &str,
    init: f64,
    f: impl Fn(f64, f64) -> f64,
) -> Result<(), EvalError> {
    let stack = engine.stack_mut();
    if stack.is_empty() {
        return Err(EvalError::NeedsOperands {
            op: op.to_string(),
            need: 1,
            got: 0,
        });
    }
    let total = stack.iter().fold(init, |acc, &x| f(acc, x));
    stack.clear();
    stack.push(total);
    Ok(())
}
```

```rust
"sum" => reduce(engine, token, 0.0, |acc, x| acc + x),
"prod" => reduce(engine, token, 1.0, |acc, x| acc * x),
```

Même technique qu'au chapitre 07 : ce qui varie (l'opération, la valeur initiale)
devient un paramètre.

### `fold`

```rust
stack.iter().fold(init, |acc, &x| f(acc, x))
```

`fold` (« plier ») parcourt les éléments en maintenant un **accumulateur**. Il
part de `init`, et pour chaque élément remplace l'accumulateur par
`f(accumulateur, élément)`. À la fin il rend l'accumulateur.

L'équivalent impératif :

```rust
let mut total = init;
for &x in stack.iter() {
    total = f(total, x);
}
```

Les deux sont corrects et compilent en un code identique. `fold` a l'avantage de
dire par sa seule présence « je réduis une collection à une valeur », et de ne
pas laisser traîner une variable mutable.

Pourquoi ne pas utiliser `sum()` de la bibliothèque standard ? Parce qu'il faut
aussi `prod`, et que `reduce` couvre les deux — et n'importe quelle réduction
future — avec le même code.

### `|acc, &x|` : le motif dans le paramètre

`stack.iter()` produit des `&f64`, pas des `f64`. Le motif `&x` dans les
paramètres de la closure **déstructure** la référence : `x` est directement un
`f64`. C'est plus agréable que d'écrire `*x` dans le corps.

C'est un petit exemple d'une idée générale de Rust : partout où on peut écrire un
nom de variable, on peut écrire un motif.

### Le choix des valeurs initiales

`0.0` pour la somme, `1.0` pour le produit : ce sont les **éléments neutres**.
Conséquence agréable : `reduce` marche sans cas particulier quel que soit le
nombre d'éléments. C'est aussi ce qui donnait `0! = 1` gratuitement au chapitre 07.

## Vérifie

```rust
fn main() {
    let mut engine = Engine::new();
    engine.eval_line("1 2 3 rot").unwrap();
    println!("{:?}", engine.stack());
}
```

Attendu : `[2.0, 3.0, 1.0]`.

Puis essaie `"3 dup *"` (9), `"1 2 over"` (`[1, 2, 1]`), `"1 2 3 4 sum"` (10),
`"1 2 3 4 prod"` (24), et `"swap"` sur une pile vide (erreur propre).

---

**Chapitre suivant :** [09 — Tout ou rien](09-tout-ou-rien.md)
