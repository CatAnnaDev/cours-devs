# 16 — Mesurer avant d'optimiser

> **Chapitre bonus.** Le tutoriel est fini au chapitre 15 : tu as un programme
> complet, testé, propre. Ce qui suit est une deuxième couche, facultative, pour
> comprendre comment on rend du code Rust *vraiment* rapide — et surtout comment
> on sait qu'on y est arrivé. Rien ici n'est nécessaire pour utiliser `blap`.

## La règle zéro

> On n'optimise pas ce qu'on n'a pas mesuré.

Ce n'est pas un slogan, c'est une observation. Demande à dix développeurs où leur
programme passe son temps : neuf se trompent. On imagine toujours que c'est dans
la boucle de calcul, alors que c'est presque toujours dans les allocations,
les entrées/sorties, ou une conversion de chaîne qu'on n'avait même pas
remarquée.

Ce chapitre construit l'instrument. Le chapitre 17 s'en sert.

## Les trois pièges de la mesure

### Piège 1 — mesurer en debug

```bash
cargo run --example bench            # faux
cargo run --release --example bench  # vrai
```

En debug, le compilateur n'inline rien, ne supprime aucune vérification
redondante, et laisse chaque petite fonction comme un vrai appel. Nos helpers
`binary`, `unary`, `pop2` deviennent des appels réels au lieu de disparaître.
Un banc d'essai en debug mesure surtout le mode debug.

C'est tellement fréquent que le banc d'essai le dit lui-même :

```rust
if cfg!(debug_assertions) {
    eprintln!("ATTENTION : compile en debug. Relance avec --release");
}
```

`cfg!(...)` est évalué **à la compilation** et se réduit à `true` ou `false` : en
release, cette branche disparaît entièrement du binaire.

### Piège 2 — le compilateur supprime ce que tu mesures

```rust
let start = Instant::now();
for _ in 0..1_000_000 {
    fmt_stack(&stack);
}
println!("{:?}", start.elapsed());
```

Ce code mesure zéro. Le résultat de `fmt_stack` n'est jamais utilisé, la fonction
n'a pas d'effet de bord observable, donc LLVM supprime la boucle entière. Tu
mesures le temps d'exécution de rien du tout.

La parade est dans la bibliothèque standard :

```rust
use std::hint::black_box;

black_box(fmt_stack(black_box(&stack)));
```

`black_box` est une **barrière d'optimisation** : elle dit au compilateur « fais
comme si quelqu'un, quelque part, regardait cette valeur ». Résultat, il ne peut
ni supprimer le calcul, ni le sortir de la boucle, ni le pré-calculer.

On la met des deux côtés :

- **autour de l'entrée**, pour empêcher la constante d'être repliée à la
  compilation (sinon `black_box("5 1 2 +")` serait évalué… pendant la compilation) ;
- **autour de la sortie**, pour empêcher le calcul d'être supprimé.

Un banc d'essai sans `black_box` donne systématiquement des gains
extraordinaires. C'est le signe qu'il ne mesure rien.

### Piège 3 — prendre la moyenne

Une machine moderne est bruyante : ordonnanceur, autres processus, changement de
fréquence, migration de cœur. Ce bruit n'ajoute **jamais** de la vitesse, il en
enlève. La distribution des mesures est donc asymétrique : un plancher net, et
une longue traîne vers le haut.

Conséquence :

- **la moyenne** mélange ton code et le bruit du système ;
- **le minimum** approche le mieux « ce que ferait la machine si elle n'avait que
  ça à faire ».

D'où :

```rust
fn measure(mut body: impl FnMut()) -> Duration {
    body();
    let mut best = Duration::MAX;
    for _ in 0..ROUNDS {
        let start = Instant::now();
        body();
        let elapsed = start.elapsed();
        if elapsed < best {
            best = elapsed;
        }
    }
    best
}
```

Le `body()` avant la boucle est le **préchauffage** : il paie la première
allocation des tampons, le premier défaut de page, le remplissage des caches. Ce
sont des coûts uniques qui n'ont rien à faire dans une mesure de régime établi.

Et `body` fait un **lot** de milliers d'itérations, pas une seule. `Instant::now()`
coûte lui-même quelques dizaines de nanosecondes ; mesurer une opération qui en
prend cent donnerait n'importe quoi. On mesure 20 000 opérations d'un coup, puis
on divise.

## Le banc d'essai

`tuto/opti/examples/bench.rs`, en entier dans le dépôt. La charpente :

```rust
const LINES: [&str; 6] = [
    "5 1 2 + 4 * + 3 -",
    "3 dup * 4 dup * + sqrt",
    "1 2 3 4 5 sum",
    "2 10 pow 2 log2",
    "pi 2 / sin 1 +",
    "10 3 % 7 max 2 min",
];

const ROUNDS: u32 = 7;
const BATCH: u32 = 20_000;
```

Six lignes représentatives : de l'arithmétique, de la manipulation de pile, des
fonctions transcendantes, une réduction. Pas une seule ligne artificielle — on
mesure ce que les gens tapent.

Et un point de détail qui compte :

```rust
for line in LINES {
    engine.eval_line(black_box(line)).unwrap();
}
engine.eval_line("clear").unwrap();
```

Sans le `clear`, chaque ligne laisserait son résultat sur la pile, qui grandirait
sans fin. Le coût de sauvegarde de la pile (chapitre 09) grandirait avec elle, et
on mesurerait une fuite plutôt qu'un programme. **Un banc d'essai doit tourner en
régime établi.**

### Un exemple, pas un test

Le fichier est dans `examples/`, pas dans `benches/`. Pourquoi ? Parce que le
`#[bench]` natif de Rust n'est toujours pas stabilisé, et que `benches/` sans lui
implique la bibliothèque `criterion` — excellente, mais c'est une dépendance et
une trentaine de paquets transitifs.

`cargo run --release --example bench` fait le travail :

- un exemple est un vrai binaire, compilé avec le même profil que le reste ;
- il utilise l'API publique de la bibliothèque, donc il valide aussi l'API ;
- zéro dépendance.

C'est aussi ce qui a poussé le projet optimisé à devenir une **bibliothèque plus
un binaire** (`src/lib.rs` + `src/main.rs`), l'exercice 9 du chapitre 15 : un
exemple ne peut importer que du code de bibliothèque.

## Compter les allocations

Les nanosecondes se discutent. Les allocations, non : soit ton code appelle
`malloc`, soit il ne l'appelle pas. C'est la mesure la plus stable et la plus
parlante qui soit.

Rust permet de remplacer l'allocateur global du programme. On l'enveloppe pour
compter, sans changer son comportement :

```rust
use std::alloc::{GlobalAlloc, Layout, System};
use std::sync::atomic::{AtomicUsize, Ordering};

static ALLOCATIONS: AtomicUsize = AtomicUsize::new(0);

struct Counting;

unsafe impl GlobalAlloc for Counting {
    unsafe fn alloc(&self, layout: Layout) -> *mut u8 {
        ALLOCATIONS.fetch_add(1, Ordering::Relaxed);
        unsafe { System.alloc(layout) }
    }

    unsafe fn dealloc(&self, ptr: *mut u8, layout: Layout) {
        unsafe { System.dealloc(ptr, layout) }
    }

    unsafe fn realloc(&self, ptr: *mut u8, layout: Layout, new_size: usize) -> *mut u8 {
        ALLOCATIONS.fetch_add(1, Ordering::Relaxed);
        unsafe { System.realloc(ptr, layout, new_size) }
    }
}

#[global_allocator]
static ALLOCATOR: Counting = Counting;
```

Quelques mots, parce que c'est le seul `unsafe` de tout le tutoriel :

- **`unsafe impl`** — implémenter `GlobalAlloc` est un contrat que le compilateur
  ne peut pas vérifier : rendre un pointeur valide, aligné, de la bonne taille.
  Ici on délègue tout à `System`, donc le contrat est tenu par construction.
- **`AtomicUsize`** — l'allocateur peut être appelé depuis n'importe quel thread.
  Un `usize` ordinaire serait une course de données. `Ordering::Relaxed` suffit :
  on veut un compteur juste, pas un ordre entre événements.
- **`realloc` compte aussi**, parce qu'un `Vec` qui grandit fait exactement ça, et
  c'est précisément ce qu'on cherche à supprimer.

La mesure elle-même :

```rust
fn count(mut body: impl FnMut()) -> usize {
    body();
    body();
    let before = ALLOCATIONS.load(Ordering::Relaxed);
    body();
    ALLOCATIONS.load(Ordering::Relaxed) - before
}
```

**Deux passes de préchauffage**, pas une. La première fait grandir les tampons, la
seconde vérifie qu'ils ont atteint leur taille de croisière. La troisième est la
seule comptée : c'est le **régime établi**, le nombre d'allocations que ton
programme fait *pour toujours*, une fois lancé.

## La ligne de base

```bash
cd tuto/opti
cargo run --release --example bench
cargo run --release --example allocs
```

Voici la colonne « naïf » — le code du chapitre 15, celui de `src/` à la racine.
Machine : Apple Silicon, `rustc` 1.99, profil release avec `lto = "fat"`.

| Mesure | Temps | Allocations (6 lignes) |
|---|---|---|
| évaluation d'une ligne | 110 ns | 6 |
| ligne qui échoue | 52 ns | 4 (pour 2 lignes) |
| trace + rendu d'une ligne | 1150 ns | 367 |
| formatage d'une pile de 8 | 217 ns | 100 (pour 10 formatages) |
| invite complète (pile + couleur) | 355 ns | — |

Lis ce tableau avant de toucher au code, et regarde ce qu'il dit :

1. **L'évaluation pure est déjà correcte.** 110 ns pour analyser et exécuter une
   ligne de neuf jetons, c'est une douzaine de nanosecondes par jeton. Il n'y a
   pas grand-chose à gagner là.
2. **Le mode trace coûte dix fois le calcul.** 1150 ns contre 110. Et 367
   allocations pour six lignes, soit une soixantaine par ligne. C'est là qu'est
   l'argent.
3. **L'affichage coûte plus cher que le calcul.** Formater une pile de huit
   nombres (217 ns) prend deux fois le temps d'évaluer une expression complète.
4. **Une allocation par ligne évaluée** : c'est le `self.stack.clone()` du
   chapitre 09, exactement comme annoncé.

Autrement dit : **le point chaud n'est pas là où l'intuition le place.** Personne
n'aurait deviné que l'affichage domine le calcul dans une calculatrice. C'est
tout l'intérêt de mesurer d'abord.

## Ce qu'on ne fait pas ici

**Un profileur.** Pour un programme de six cents lignes dont on connaît déjà les
quatre chemins, le banc d'essai suffit. Sur un vrai projet, l'outil suivant est
un profileur d'échantillonnage : `samply` (multiplateforme, s'installe avec
`cargo install samply`), `perf` sous Linux, Instruments sous macOS. Ils te disent
*quelle ligne* brûle le temps, sans que tu aies à deviner quoi mesurer.

**`criterion`.** Dès que tu veux des intervalles de confiance, de la détection de
régression entre deux exécutions et des graphiques, prends-la. Notre `measure`
maison est honnête mais rustique.

**Optimiser tout de suite.** C'est le chapitre suivant, et maintenant on sait
quoi viser.

---

**Chapitre suivant :** [17 — La grosse passe d'optimisation](17-la-grosse-passe-d-optimisation.md)
