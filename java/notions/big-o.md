# La « Big O sheet » — comprendre la complexité

Neniri, voici sans doute la notion qui te fera le plus progresser. Pas de panique : il n'y a
pas de maths compliquées, juste une façon de répondre à **une seule question** :

> « Si je donne 10× plus de données à mon code, est-ce qu'il devient 10× plus lent... ou
> 100× plus lent... ou pas plus lent du tout ? »

C'est ça, la **complexité**. On la note avec un grand **O** (« Big O »).

---

## L'idée de base

Quand tu écris du code, ce qui compte sur le long terme, ce n'est pas qu'il soit rapide sur
3 éléments — c'est qu'il **tienne le coup quand les données grossissent** (1, puis 1 000,
puis 1 000 000 d'éléments).

Le Big O décrit **comment le nombre d'opérations grandit en fonction de la taille des
données** (qu'on appelle `n`). On **ignore les détails** (les constantes, les petits +1) :
on ne garde que la *forme* de la croissance. Exemple : 3·n + 7 opérations, on dit juste
**O(n)** (« ça grandit proportionnellement à n »).

> Pourquoi ignorer les constantes ? Parce que quand `n` devient énorme, c'est la *forme* qui
> décide tout. Un O(n) battra toujours un O(n²) pour `n` assez grand, peu importe les détails.

---

## Les classes à connaître (de la meilleure à la pire)

| Notation | Nom | Ça veut dire | Exemple typique |
|---|---|---|---|
| **O(1)** | constant | le temps **ne dépend pas** de `n` | lire `tab[i]`, `HashMap.get(clé)` |
| **O(log n)** | logarithmique | double les données ≈ +1 étape | recherche dichotomique, arbre trié |
| **O(n)** | linéaire | 10× données = 10× temps | parcourir une liste une fois |
| **O(n log n)** | quasi-linéaire | un peu plus que linéaire | un bon tri (`sort`) |
| **O(n²)** | quadratique | 10× données = **100×** temps | deux boucles imbriquées sur `n` |
| **O(2ⁿ)**, **O(n!)** | exponentiel / factoriel | **explose** très vite | tester toutes les combinaisons |

---

## Le tableau qui fait « tilt »

Combien d'opérations environ, selon `n` et la complexité :

| `n` | O(1) | O(log n) | O(n) | O(n log n) | O(n²) |
|---:|---:|---:|---:|---:|---:|
| 10 | 1 | ~3 | 10 | ~33 | 100 |
| 100 | 1 | ~7 | 100 | ~664 | 10 000 |
| 1 000 | 1 | ~10 | 1 000 | ~10 000 | 1 000 000 |
| 1 000 000 | 1 | ~20 | 1 000 000 | ~20 000 000 | **1 000 000 000 000** |

Regarde la dernière colonne : en O(n²), un million d'éléments = **mille milliards**
d'opérations (= ton programme est figé). En O(n), c'est un million (instantané). **Même
machine, même données : c'est l'algorithme qui change tout.**

---

## Comment lire la complexité de TON code

Quelques réflexes simples :

- **Une boucle** sur les `n` éléments → **O(n)**.
- **Une boucle DANS une boucle**, les deux sur `n` → **O(n²)**. (Trois imbriquées → O(n³).)
- **Couper le problème en deux à chaque étape** (dichotomie) → **O(log n)**.
- **Des opérations à la suite** : on garde la plus grosse. O(n) puis O(n²) → **O(n²)**.
- **Accès direct** (par index ou par clé de hachage) → **O(1)**.

Exemple à repérer (le piège n°1, on en reparle dans `collections.md`) :

```text
pour chaque élément a de la liste:        // n tours
    pour chaque élément b de la liste:    // n tours -> n × n
        comparer a et b
```
→ **O(n²)**. Si tu vois deux boucles imbriquées sur les mêmes données, une alarme doit sonner.

---

## Le temps... mais aussi la mémoire

Le Big O sert aussi à décrire la **place mémoire** utilisée (la « complexité spatiale »).
Exemple : copier toute une liste dans une nouvelle = **O(n)** en mémoire. Parfois on accepte
d'utiliser plus de mémoire pour gagner en temps (par ex. un `HashSet` pour aller plus vite) :
c'est un **compromis** très courant.

---

## Essaie de tes propres yeux

Le même travail, en O(n²) puis en O(n). Lance-le et regarde la différence quand tu augmentes
la taille.

**En Rust** (mets-le dans un `fn main`) :

```rust
use std::collections::HashSet;
use std::time::Instant;

fn main() {
    let donnees: Vec<i32> = (0..20_000).collect();

    // O(n²) : pour chaque élément, on RE-cherche dans tout le Vec
    let t = Instant::now();
    let mut doublons_lents = 0;
    for &x in &donnees {
        if donnees.iter().filter(|&&y| y == x).count() > 1 { doublons_lents += 1; }
    }
    println!("O(n²) : {doublons_lents} en {:?}", t.elapsed());

    // O(n) : on met tout dans un HashSet (recherche en O(1))
    let t = Instant::now();
    let set: HashSet<i32> = donnees.iter().copied().collect();
    let present = set.contains(&12345);
    println!("O(n) : présent={present} en {:?}", t.elapsed());
}
```

**En Java** (dans une méthode `main`) :

```java
int[] donnees = new int[20_000];
for (int i = 0; i < donnees.length; i++) donnees[i] = i;

// O(n²)
long t = System.nanoTime();
long compte = 0;
for (int x : donnees)
    for (int y : donnees)
        if (x == y) compte++;
System.out.println("O(n^2) en " + (System.nanoTime() - t) / 1_000_000 + " ms");

// O(n) : HashSet, contains en O(1)
t = System.nanoTime();
java.util.HashSet<Integer> set = new java.util.HashSet<>();
for (int x : donnees) set.add(x);
boolean present = set.contains(12345);
System.out.println("O(n) en " + (System.nanoTime() - t) / 1_000_000 + " ms, présent=" + present);
```

Augmente `20_000` à `40_000` : la version O(n²) devient ~4× plus lente, la version O(n) à
peine plus. **C'est ça, sentir la complexité.**

---

## À retenir

1. Le Big O dit **comment ton code ralentit quand les données grossissent**.
2. Du meilleur au pire : **O(1) < O(log n) < O(n) < O(n log n) < O(n²) < O(2ⁿ)**.
3. **Deux boucles imbriquées sur les mêmes données = O(n²)** → cherche presque toujours mieux.
4. Améliorer la **complexité** (mieux choisir l'algorithme/la structure) rapporte **bien plus**
   que n'importe quelle micro-astuce. La suite : `collections.md`.
