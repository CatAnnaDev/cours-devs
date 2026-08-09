# Les collections (et le `Vec`) — laquelle choisir, et à quel prix

Tu as déjà vu les bases : les tableaux, tuples et `Vec` en Rust (`src/lecons/collections.rs`)
et les tableaux + `ArrayList` en Java (`java/src/lecons/Lecon05TableauxListes.java`). Ici on
va plus loin : **quelle collection prendre selon ce que tu veux faire**, et **combien coûte
chaque opération** (en Big O — relis `big-o.md` si besoin).

---

## Les grandes familles (Rust ↔ Java)

| Tu veux... | Rust | Java | Idée |
|---|---|---|---|
| une liste ordonnée, indexable | `Vec<T>` | `ArrayList<T>` | la plus courante |
| associer une **clé** à une **valeur** | `HashMap<K,V>` | `HashMap<K,V>` | « annuaire » : retrouver vite par clé |
| un **ensemble** de valeurs uniques | `HashSet<T>` | `HashSet<T>` | « est-ce que X est dedans ? » très vite |
| un truc trié en permanence | `BTreeMap`/`BTreeSet` | `TreeMap`/`TreeSet` | trié, accès en O(log n) |
| une file (ajout d'un côté, retrait de l'autre) | `VecDeque` | `ArrayDeque` | file d'attente |

> 90 % du temps tu utiliseras **`Vec`/`ArrayList`**, **`HashMap`** ou **`HashSet`**. Maîtrise
> bien ces trois-là.

---

## Le coût de chaque opération (la vraie « sheet »)

| Opération | `Vec` / `ArrayList` | `HashMap` | `HashSet` | `BTreeMap`/`TreeMap` |
|---|---|---|---|---|
| Accès par **index** | **O(1)** | — | — | — |
| Lire/écrire par **clé** | — | **O(1)** moy. | — | O(log n) |
| **Ajouter à la fin** (`push`/`add`) | **O(1)** amorti | — | — | — |
| Insérer/supprimer **au milieu** | **O(n)** | — | — | — |
| `insert`/`remove` par clé | — | **O(1)** moy. | **O(1)** moy. | O(log n) |
| **Chercher une valeur** (`contains`) | **O(n)** | O(1) (par clé) | **O(1)** | O(log n) |
| Parcourir tout | O(n) | O(n) | O(n) | O(n) (en ordre trié) |

Les deux lignes à graver :
- Dans un **`Vec`/`ArrayList`**, chercher une valeur, c'est **O(n)** (il faut tout parcourir).
- Dans un **`HashSet`/`HashMap`**, vérifier la présence d'une clé, c'est **O(1)**.

---

## Le piège n°1 : chercher dans une liste, dans une boucle

C'est l'erreur la plus fréquente, et elle transforme un O(n) en **O(n²)** :

```text
pour chaque commande c:                 // n
    si c.joueur EST DANS listeVIP:      // O(n) car listeVIP est une liste !
        ...
```
→ n × O(n) = **O(n²)**. Avec 10 000 éléments, ça rame déjà.

**La solution :** mets `listeVIP` dans un **`HashSet`**. Le test « est dans » passe à O(1),
et le tout devient **O(n)** :

```rust
// Rust
use std::collections::HashSet;
let vip: HashSet<&str> = liste_vip.iter().copied().collect();
for c in &commandes {
    if vip.contains(c.joueur) { /* ... */ }   // O(1)
}
```

```java
// Java
Set<String> vip = new HashSet<>(listeVip);
for (Commande c : commandes) {
    if (vip.contains(c.joueur)) { /* ... */ }  // O(1)
}
```

Même réflexe quand tu veux **retrouver un objet par son identifiant** : un
`HashMap<Id, Objet>` (get en O(1)) plutôt que parcourir une liste à chaque fois (O(n)).

---

## Le `Vec` en détail : `len` vs `capacity`

Un `Vec` (et un `ArrayList`) garde ses éléments dans un bloc mémoire continu. Deux nombres :

- **`len`** : combien d'éléments il contient *réellement*.
- **`capacity`** : combien il peut en contenir *avant de devoir s'agrandir*.

Quand tu `push` et que c'est plein, il **alloue un plus grand bloc et recopie tout**. Cette
recopie est en O(n)... mais elle arrive de plus en plus rarement (la capacité double à chaque
fois). En moyenne, un `push` coûte donc **O(1) « amorti »** — d'où le mot « amorti » dans le
tableau.

**Optimisation gratuite :** si tu sais combien d'éléments tu vas ajouter, **réserve la place
d'avance** pour éviter les recopies :

```rust
let mut v = Vec::with_capacity(10_000); // Rust
```
```java
List<Integer> v = new ArrayList<>(10_000); // Java
```

---

## Petit arbre de décision

- Je veux accéder **par position** (1er, 2e, i-ème) → **`Vec`/`ArrayList`**.
- Je veux retrouver **par clé/identifiant** → **`HashMap`**.
- Je veux juste savoir **« est-ce que X existe ? »** sans doublons → **`HashSet`**.
- J'ai besoin que ce soit **toujours trié** → `BTreeMap`/`TreeMap`.
- J'ajoute/retire surtout **aux extrémités** (file) → `VecDeque`/`ArrayDeque`.

---

## Côté Hytale (et tout jeu/serveur)

Le réflexe `HashMap` est partout dans les mods : par exemple, stocker les données des joueuses
dans `HashMap<UUID, DonneesJoueuse>` pour les retrouver en **O(1)** à chaque connexion ou à
chaque commande — au lieu de parcourir une liste. Dès qu'un code tourne souvent (à chaque
tick, à chaque event), une recherche en O(n) cachée dans une boucle peut faire ramer le
serveur. La bonne structure de données, c'est ta première optimisation.

---

## À retenir

1. **`Vec`/`ArrayList`** : top pour l'accès par index et l'ajout en fin ; mais chercher dedans
   est **O(n)**.
2. **`HashMap`/`HashSet`** : recherche/accès par clé en **O(1)** — ton arme contre les O(n²).
3. **Le piège** : chercher dans une liste à l'intérieur d'une boucle → passe par un `HashSet`/`HashMap`.
4. **`with_capacity` / `new ArrayList<>(n)`** : réserve la place si tu connais la taille.

Suite : `optimisations.md` — les petites optimisations qui marchent partout.
