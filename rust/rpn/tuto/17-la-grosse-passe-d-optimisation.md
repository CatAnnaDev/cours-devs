# 17 — La grosse passe d'optimisation

> **Chapitre bonus, suite du 16.** On reprend le programme du chapitre 15 et on
> le réécrit pour la vitesse, une optimisation à la fois, chacune mesurée. Le
> résultat complet est dans [`tuto/opti/`](opti/) : c'est un projet séparé,
> compilable, testé, qui se comporte exactement comme l'original.
>
> Le code de `src/` à la racine **ne change pas**. C'est volontaire : la version
> simple reste la référence du tutoriel, et tu peux comparer les deux fichier par
> fichier.

## L'objectif

Le chapitre 16 a montré où va le temps. On vise trois choses :

1. **Zéro allocation en régime établi**, sur tous les chemins, y compris la trace
   et l'affichage.
2. **Zéro allocation sur le chemin d'erreur** — parce qu'un chemin d'erreur qui
   alloue est un chemin d'erreur qui peut échouer à son tour.
3. **Ne pas rendre le code illisible.** Une optimisation qu'on ne peut plus
   maintenir est une régression déguisée.

Chaque section suit le même plan : le problème, le avant, le après, ce que ça
rapporte, ce que ça coûte.

---

## 1. Les erreurs qui empruntent au lieu de copier

### Le problème

```rust
pub enum EvalError {
    NeedsOperands { op: String, need: usize, got: usize },
    Unknown(String),
    DivByZero,
    Domain(String),
}
```

Chaque `String` est une allocation. Et `EvalError::Unknown(other.to_string())`
s'exécute **avant** que quiconque sache si le message sera lu : dans
`blap "1 2 + +" > /dev/null`, on alloue pour rien.

Le compteur du chapitre 16 le confirme : quatre allocations pour deux lignes en
erreur, soit deux par erreur (une pour l'opérateur, une pour la mise en forme).

### Le après

```rust
pub enum EvalError<'a> {
    NeedsOperands { op: &'a str, need: usize, got: usize },
    Unknown(&'a str),
    DivByZero,
    Domain(&'a str),
}
```

Le `'a` est une **durée de vie**. Il ne fait rien à l'exécution : c'est une
annotation qui permet au compilateur de vérifier que le `&str` rangé dans
l'erreur pointe dans quelque chose de toujours vivant.

Elle se propage à toutes les signatures qui produisent une erreur :

```rust
pub fn eval_line<'a>(&mut self, line: &'a str) -> Result<(), EvalError<'a>> {
```

Ce qui se lit : « l'erreur que je peux renvoyer emprunte à la ligne que tu m'as
donnée, donc elle ne peut pas te survivre ». Le compilateur refusera de stocker
cette erreur dans une structure qui vivrait plus longtemps que la ligne — et
c'est exactement la garantie qu'on veut, puisque le message pointe dedans.

Dans `ops.rs`, tout devient gratuit :

```rust
other => Err(EvalError::Unknown(other)),
```

Plus de `.to_string()`. Le jeton fautif est déjà dans la ligne saisie ; on en note
juste l'adresse.

### Le gain

| | naïf | optimisé |
|---|---|---|
| ligne qui échoue | 52 ns | 29 ns |
| allocations (2 erreurs) | 4 | **0** |

Presque deux fois plus rapide, et surtout : **plus une seule allocation quand ça
casse**.

### Le prix

Une dizaine de signatures plus denses, et un concept que tu dois avoir en tête en
lisant le code. C'est exactement pour ça qu'on ne l'a pas fait dans le tutoriel
principal : c'est le meilleur rapport gain/complexité des optimisations de ce
chapitre, mais c'est aussi la plus intimidante à lire pour quelqu'un qui débute.

### La subtilité qui piège

```rust
fn nonzero<'a>(x: f64) -> Result<f64, EvalError<'a>> {
```

`'a` n'apparaît dans **aucun** paramètre. C'est légal : la fonction ne construit
que des variantes sans référence (`DivByZero`), donc l'appelant choisit `'a`
librement. Même chose pour `push`, qui renvoie `Result<(), EvalError<'static>>` :
il ne peut jamais échouer, donc son erreur peut prétendre vivre éternellement.

---

## 2. Le tampon de secours réutilisé

### Le problème

Chapitre 09 :

```rust
let backup = self.stack.clone();
```

Une allocation par ligne évaluée, systématiquement, même quand la ligne réussit —
c'est-à-dire toujours.

### Le après

On garde le tampon dans le moteur :

```rust
pub struct Engine {
    stack: Vec<f64>,
    scratch: Vec<f64>,
}
```

```rust
self.scratch.clear();
self.scratch.extend_from_slice(&self.stack);

for token in line.split_whitespace() {
    if let Err(error) = self.eval_token(token) {
        std::mem::swap(&mut self.stack, &mut self.scratch);
        return Err(error);
    }
}
```

Deux idées :

**`clear()` + `extend_from_slice()` ne réalloue pas.** `clear()` remet la longueur
à zéro sans rendre la mémoire ; `extend_from_slice` recopie les octets dans la
capacité déjà là. Après quelques lignes, la capacité est suffisante pour
toujours, et le nombre d'allocations tombe à zéro.

**`std::mem::swap` échange les deux `Vec`.** Un `Vec` est trois mots machine —
pointeur, longueur, capacité. `swap` en échange six au total. C'est instantané, et
ça évite d'avoir à convaincre le vérificateur d'emprunts qu'on a le droit
d'écraser `self.stack` avec quelque chose qui vient aussi de `self`.

Après le `swap`, `scratch` contient la pile abîmée. Aucune importance : elle est
écrasée au début de la ligne suivante.

### Le gain

| | naïf | optimisé |
|---|---|---|
| allocations (6 lignes) | 6 | **0** |
| évaluation d'une ligne | 110 ns | 95 ns |

### Le prix

Un champ de plus, et une invariante à ne pas casser : `scratch` doit être rempli
avant la boucle, sinon la restauration remet n'importe quoi. C'est le genre de
chose qu'on protège par un test — et c'est déjà fait, le test
`restaure_la_pile_apres_une_erreur` du chapitre 13 le couvre à l'identique.

---

## 3. La trace : d'un tableau d'objets à trois tampons plats

C'est l'optimisation la plus payante du chapitre, et de loin.

### Le problème

```rust
pub struct Step {
    pub token: String,
    pub before: Vec<f64>,
    pub after: Vec<f64>,
}

pub fn eval_traced(&mut self, line: &str) -> Result<Vec<Step>, EvalError>
```

Compte les allocations pour **un seul jeton** :

1. `token.to_string()` — une.
2. `before` : `self.stack.clone()` — une.
3. `after` : `self.stack.clone()` — une.
4. le `Vec<Step>` qui grandit — de temps en temps une de plus.

Trois allocations par jeton, plus la croissance du vecteur. Sur nos six lignes
d'essai : **367 allocations**, jetées et refaites à chaque ligne.

Et il y a pire que le nombre : `before` du pas *n* est exactement `after` du pas
*n−1*. On stocke deux fois la même chose.

### Le après

On renverse la structure. Au lieu d'un tableau d'objets qui possèdent chacun leurs
données, trois tampons parallèles qu'on remplit bout à bout :

```rust
#[derive(Debug, Default)]
pub struct Trace {
    tokens: Vec<(u32, u32)>,
    offsets: Vec<u32>,
    values: Vec<f64>,
}
```

- `values` — **tous** les états de la pile, mis à la queue leu leu.
- `offsets` — où commence chaque état dans `values`.
- `tokens` — la position de chaque jeton dans la ligne (section 4).

Il y a `n + 1` états pour `n` jetons : l'état initial, puis un après chaque jeton.
Le pas *i* va donc de l'état *i* à l'état *i + 1* — et le doublon `before`/`after`
disparaît de lui-même.

```rust
fn push_state(&mut self, state: &[f64]) {
    self.offsets.push(self.values.len() as u32);
    self.values.extend_from_slice(state);
}

fn seal(&mut self) {
    self.offsets.push(self.values.len() as u32);
}

fn span(&self, state: usize) -> Range<usize> {
    self.offsets[state] as usize..self.offsets[state + 1] as usize
}
```

Le `seal()` final pousse une borne sentinelle, ce qui permet à `span` de lire
`offsets[state + 1]` sans cas particulier pour le dernier état. C'est un grand
classique : **une sentinelle vaut mieux qu'un `if`.**

L'appelant fournit le tampon, donc il peut le réutiliser :

```rust
pub fn eval_traced<'a>(
    &mut self,
    line: &'a str,
    trace: &mut Trace,
) -> Result<(), EvalError<'a>>
```

Et on relit ça comme si c'était toujours un tableau de `Step` :

```rust
pub struct Step<'a> {
    pub token: &'a str,
    pub before: &'a [f64],
    pub after: &'a [f64],
}

pub fn iter<'a>(&'a self, line: &'a str) -> impl Iterator<Item = Step<'a>> {
    self.tokens.iter().enumerate().map(|(i, &(start, end))| Step {
        token: &line[start as usize..end as usize],
        before: &self.values[self.span(i)],
        after: &self.values[self.span(i + 1)],
    })
}
```

Le `Step` existe toujours, mais il ne **possède** plus rien : c'est une vue,
fabriquée à la volée, qui tient dans quelques registres. Le code d'affichage du
chapitre 11 fonctionne quasiment sans modification.

### Le nom de la technique

C'est le passage d'un *array of structs* à un *struct of arrays*, et de la
possession à l'**arène**. On le retrouve partout où la performance compte :
moteurs de jeu, compilateurs, bases de données, systèmes à entités.

Deux raisons pour lesquelles c'est plus rapide, et la seconde est souvent la plus
importante :

- **Une allocation au lieu de N.** Après quelques lignes, les trois `Vec` ont
  atteint leur taille de croisière et n'allouent plus jamais.
- **Les données sont contiguës.** Tous les états de la pile se suivent en
  mémoire, donc le processeur les lit en quelques lignes de cache. La version
  naïve les éparpille dans le tas : chaque `Vec<f64>` est ailleurs, et chaque
  accès risque un défaut de cache.

### Le gain

| | naïf | optimisé |
|---|---|---|
| trace + rendu d'une ligne | 1150 ns | 356 ns |
| allocations (6 lignes) | 367 | **0** |

Trois fois plus rapide, et le compteur d'allocations tombe à zéro.

### Le prix

C'est la partie la plus dense du projet. `offsets` a une longueur de
`tokens.len() + 2`, ce qui n'est évident pour personne, et une erreur d'indice ici
donne un panic ou pire, des données silencieusement décalées.

C'est exactement le genre de code qu'on ne s'autorise **que** parce qu'il est
couvert par des tests. Il y en a quatre rien que pour la trace, dont un qui
vérifie explicitement l'absence de réallocation :

```rust
#[test]
fn la_trace_est_reutilisable_sans_realloc() {
    let mut engine = Engine::new();
    let mut trace = Trace::new();
    engine.eval_traced("1 2 3 4 5 + + + +", &mut trace).unwrap();
    let capacity = trace.values.capacity();

    engine.eval_traced("1 1 +", &mut trace).unwrap();
    assert_eq!(trace.values.capacity(), capacity);
}
```

**Une optimisation non testée n'est pas une optimisation, c'est un pari.** Ce test
échouera le jour où quelqu'un remettra un `Vec::new()` quelque part.

---

## 4. Stocker des index plutôt que des références

Celle-là, c'est le compilateur qui l'a imposée — et il avait raison.

### Le problème

Première tentative, naturelle :

```rust
pub struct Trace<'a> {
    tokens: Vec<&'a str>,
    ...
}
```

Ça compile. Mais dans le REPL :

```rust
let mut trace = Trace::new();

loop {
    line.clear();
    std::io::stdin().read_line(&mut line);
    engine.eval_traced(&line, &mut trace)?;
}
```

```
error[E0502]: cannot borrow `line` as mutable because it is also borrowed as immutable
```

Le message est juste, et le raisonnement derrière est important : `trace` contient
des `&str` qui pointent **dans** `line`. Tant que `trace` existe, `line` est
emprunté. Or on veut l'effacer à chaque tour. Le vérificateur d'emprunts refuse,
et il a raison : sans lui, on lirait de la mémoire réécrite.

Réponse tentante : recréer le `Trace` à chaque tour de boucle. Mais on perd
exactement ce qu'on venait de gagner en section 3.

### Le après

On range la **position** du jeton, pas son adresse :

```rust
tokens: Vec<(u32, u32)>,
```

```rust
let base = line.as_ptr() as usize;

for token in line.split_whitespace() {
    ...
    let start = (token.as_ptr() as usize - base) as u32;
    trace.tokens.push((start, start + token.len() as u32));
}
```

`split_whitespace` renvoie des tranches **de la ligne elle-même**, pas des copies.
Leur adresse moins l'adresse du début de la ligne donne leur position. Aucun
`unsafe` : convertir un pointeur en entier et soustraire est une opération sûre.

Et la relecture prend la ligne en paramètre :

```rust
pub fn iter<'a>(&'a self, line: &'a str) -> impl Iterator<Item = Step<'a>>
```

`Trace` n'a plus de durée de vie du tout. Il peut vivre aussi longtemps qu'on veut,
être réutilisé pour mille lignes différentes, et le REPL compile.

### Ce que ça apprend

Ce n'est pas un contournement du vérificateur d'emprunts, c'est un **changement de
modèle** : un index est une référence sans les contraintes, au prix de devoir
fournir la base au moment de la lecture. On retrouve ce compromis dans toutes les
structures de données Rust un peu sérieuses — arbres, graphes, arènes de nœuds.

La règle à retenir : **quand une durée de vie contamine tout ton programme, c'est
souvent qu'il fallait un index.**

`u32` plutôt que `usize` : deux fois moins de mémoire, et une ligne saisie de plus
de 4 gigaoctets n'existe pas.

Un test spécifique verrouille le calcul de position, y compris avec des espaces
multiples :

```rust
#[test]
fn les_jetons_de_la_trace_pointent_au_bon_endroit() {
    let line = "  12   dup   *  ";
    engine.eval_traced(line, &mut trace).unwrap();
    let tokens: Vec<&str> = trace.iter(line).map(|step| step.token).collect();
    assert_eq!(tokens, ["12", "dup", "*"]);
}
```

---

## 5. Un booléen résolu à la compilation

### Le problème

Chapitre 11 :

```rust
fn run(&mut self, line: &str, trace: bool) -> Result<Vec<Step>, EvalError> {
    for token in line.split_whitespace() {
        ...
        if trace { ... }
    }
}
```

Le `if trace` est testé à **chaque jeton**, alors que sa valeur ne change jamais
pendant l'appel. Un test bien prédit coûte presque rien, mais il empêche surtout
le compilateur de simplifier la boucle.

### Le après

```rust
fn run<'a, const TRACE: bool>(
    &mut self,
    line: &'a str,
    trace: &mut Trace,
) -> Result<(), EvalError<'a>> {
    ...
    if TRACE {
        trace.tokens.push(...);
        trace.push_state(&self.stack);
    }
}

pub fn eval_line<'a>(&mut self, line: &'a str) -> Result<(), EvalError<'a>> {
    let mut sink = Trace::default();
    self.run::<false>(line, &mut sink)
}

pub fn eval_traced<'a>(&mut self, line: &'a str, trace: &mut Trace)
    -> Result<(), EvalError<'a>> {
    self.run::<true>(line, trace)
}
```

`const TRACE: bool` est un **paramètre générique de valeur**. Le compilateur
génère deux fonctions distinctes : dans `run::<false>`, tous les `if TRACE` sont
`if false` et disparaissent au premier passage d'optimisation, avec le code
qu'ils contiennent.

Autrement dit, on écrit **une** boucle et on en obtient **deux**, chacune parfaite
pour son cas. Sans duplication de code source à maintenir.

Et `Trace::default()` dans `eval_line` ne coûte rien : trois `Vec` vides, et un
`Vec` vide n'alloue pas (chapitre 11). Sur `run::<false>`, il n'est même jamais
touché.

### Le prix

Le binaire contient deux copies de la boucle. Ici elle fait vingt lignes, personne
ne le remarquera. Sur une fonction énorme instanciée avec dix combinaisons de
paramètres, c'est une explosion de code qui remplit le cache d'instructions et
ralentit tout. **Les génériques ne sont gratuits qu'à petite dose.**

---

## 6. `Display` plutôt qu'une fonction qui rend une `String`

### Le problème

```rust
pub fn fmt_num(n: f64) -> String
pub fn fmt_stack(stack: &[f64]) -> String
```

`fmt_stack` sur une pile de huit nombres, c'est : huit `String` (une par nombre),
plus le `Vec<String>` qui les tient, plus la `String` finale du `join`. Dix
allocations pour afficher huit nombres, immédiatement jetées.

### Le après

Un **newtype** qui sait s'afficher :

```rust
#[derive(Debug, Clone, Copy, PartialEq)]
pub struct Num(pub f64);

impl fmt::Display for Num {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        let n = self.0;
        if n == 0.0 {
            return f.write_str("0");
        }
        if n.fract() == 0.0 && n.abs() < 1e15 {
            return write!(f, "{}", n as i64);
        }
        write!(f, "{n}")
    }
}
```

`Num(x)` est une struct tuple à un champ : elle occupe **exactement** la place
d'un `f64`, et disparaît à la compilation. Elle ne sert qu'à accrocher un
comportement à un type qui ne t'appartient pas — on ne peut pas implémenter
`Display` directement sur `f64`, parce que ni le trait ni le type ne sont à nous.

Et surtout, `Display` écrit **dans le formateur qu'on lui prête**, pas dans une
`String` neuve. `println!("{}", Num(top))` va directement dans le tampon de sortie.

Pour la pile entière :

```rust
pub fn write_stack(out: &mut String, stack: &[f64]) {
    use fmt::Write;

    for (index, &n) in stack.iter().enumerate() {
        if index > 0 {
            out.push(' ');
        }
        let _ = write!(out, "{}", Num(n));
    }
}
```

La fonction ne rend rien : elle **ajoute** dans un tampon fourni par l'appelant,
qui le vide avec `clear()` entre deux usages et le réutilise pour toujours.

Le `use fmt::Write` est nécessaire : `write!` sur une `String` passe par
`std::fmt::Write`, et non par `std::io::Write` qui sert pour les fichiers et les
sorties. C'est le seul endroit du projet où les deux traits homonymes se croisent.

### Le gain

| | naïf | optimisé |
|---|---|---|
| formatage d'une pile de 8 | 217 ns | 106 ns |
| allocations (10 formatages) | 100 | **0** |

### Le prix

L'appelant doit fournir et gérer le tampon. C'est une API un peu moins agréable —
`let s = fmt_stack(&stack)` était plus court que trois lignes. C'est le compromis
habituel : **rendre une `String` est pratique pour l'appelant, écrire dans un
tampon est rapide.**

---

## 7. `Cow` : ne pas allouer quand la couleur est coupée

### Le problème

```rust
fn paint(self, code: &str, text: &str) -> String {
    if self.color {
        format!("\x1b[{code}m{text}\x1b[0m")
    } else {
        text.to_string()
    }
}
```

Regarde la branche `else` : les couleurs sont **désactivées**, on ne fait
strictement rien au texte… et on en alloue quand même une copie, uniquement pour
que les deux branches aient le même type.

C'est le cas de `blap "3 4 +" | grep 7`, de `NO_COLOR=1`, et de toute exécution
depuis un script.

### Le après

```rust
use std::borrow::Cow;

fn paint<'a>(self, code: &str, text: &'a str) -> Cow<'a, str> {
    if self.color {
        Cow::Owned(format!("\x1b[{code}m{text}\x1b[0m"))
    } else {
        Cow::Borrowed(text)
    }
}
```

`Cow` — *clone on write* — est une énumération à deux variantes : soit une valeur
possédée, soit un emprunt. Elle implémente `Display` et `Deref<Target = str>` dans
les deux cas, donc **aucun appelant ne change** :

```rust
eprintln!("{} {error}", style.red("erreur :"));
```

Quand les couleurs sont coupées, `Cow::Borrowed` ne fait que recopier un pointeur
et une longueur. Zéro allocation.

C'est le motif type de `Cow` : « la plupart du temps je n'ai rien à modifier,
mais parfois si ». On le croise partout en Rust, notamment dans les fonctions de
remplacement de texte qui rendent l'entrée telle quelle s'il n'y avait rien à
remplacer.

### Le gain

| | naïf | optimisé |
|---|---|---|
| invite complète (pile + couleur) | 355 ns | 101 ns |

Ce chiffre cumule `Cow`, `write_stack` et le tampon réutilisé — c'est l'ensemble
du chemin d'affichage de l'invite. **Trois fois et demie plus rapide.**

Un test verrouille explicitement l'absence de copie :

```rust
#[test]
fn sans_couleur_le_texte_n_est_pas_realloue() {
    let style = Style { color: false };
    assert!(matches!(style.red("bonjour"), Cow::Borrowed("bonjour")));
}
```

Note la formulation : on ne teste pas seulement que le texte est correct, on teste
**qu'il n'a pas été copié**. C'est la propriété qu'on vient d'acheter, donc c'est
elle qu'il faut protéger.

---

## 8. Verrouiller la sortie une seule fois

### Le problème

```rust
println!("...");
println!("...");
```

Chaque `println!` prend le verrou de `stdout`, écrit, et le relâche. Trente
`println!` dans l'aide, c'est trente verrouillages.

Rust verrouille parce que `stdout` est partagé entre threads : sans ça, deux
threads qui écrivent en même temps entrelaceraient leurs caractères.

### Le après

```rust
let mut out = BufWriter::new(std::io::stdout().lock());
let _ = writeln!(out, "...");
let _ = writeln!(out, "...");
let _ = out.flush();
```

Deux choses distinctes :

**`.lock()`** prend le verrou une fois pour toutes. Le programme est mono-thread,
donc la contention est nulle, mais on économise trente paires
verrouiller/déverrouiller.

**`BufWriter`** change la stratégie de tamponnage. Par défaut, `stdout` vers un
terminal est tamponné **par ligne** : chaque `\n` déclenche un appel système
`write`. Un `BufWriter` accumule jusqu'à 8 kio et n'écrit qu'une fois. Trente
appels système deviennent un.

Attention au piège : **il faut vider explicitement.** `BufWriter` le fait bien à sa
destruction, mais en ignorant silencieusement une éventuelle erreur, et surtout
`std::process::exit` ne détruit rien du tout. D'où le `out.flush()` avant chaque
`exit`, et avant chaque `eprintln!` — sinon l'erreur apparaîtrait dans le terminal
avant la sortie qui la précède.

Et **on ne met pas de `BufWriter` dans le REPL** : on veut l'invite à l'écran
immédiatement, pas dans huit kilo-octets. Le REPL garde le `lock()` seul, avec un
`flush()` explicite après l'invite. C'est un cas où la bonne réponse dépend de
l'usage, pas d'une règle générale.

---

## 9. Le petit outillage

Quatre changements sans histoire, mais qui se cumulent.

### `#[inline]` sur les fonctions minuscules

```rust
#[inline]
fn nonzero<'a>(x: f64) -> Result<f64, EvalError<'a>> { ... }
```

`#[inline]` est un **conseil** au compilateur, pas un ordre. Il compte surtout
entre *crates* : sans lui, une fonction d'une bibliothèque n'est en général pas
inlinable dans le code qui l'utilise, sauf en compilation LTO. Comme la version
optimisée est une bibliothèque appelée par un binaire et par les exemples, c'est
justement le cas.

Ne le mets que sur des fonctions de quelques lignes. Sur une grosse fonction, il
gonfle le code et ralentit.

### `impl FnOnce` plutôt que `impl Fn`

```rust
fn binary<'a>(
    engine: &mut Engine,
    op: &'a str,
    f: impl FnOnce(f64, f64) -> Result<f64, EvalError<'a>>,
) -> Result<(), EvalError<'a>>
```

On n'appelle la closure qu'une fois. Demander `FnOnce` — la contrainte la plus
faible — accepte davantage de closures, y compris celles qui consomment ce
qu'elles ont capturé. Aucun gain de vitesse, mais c'est la bonne signature :
**demande le minimum dont tu as besoin.**

### `reserve` avant plusieurs `push`

```rust
"over" => {
    let (a, b) = engine.pop2(token)?;
    let stack = engine.stack_mut();
    stack.reserve(3);
    stack.push(a);
    stack.push(b);
    stack.push(a);
    Ok(())
}
```

Sans `reserve`, chaque `push` teste la capacité et peut déclencher une
réallocation. Avec, un seul test, une seule réallocation possible.

### `drain` plutôt que `iter` + `clear`

```rust
let total = stack.drain(..).fold(init, f);
stack.push(total);
```

`drain(..)` parcourt **en consommant** : à la fin le `Vec` est vide, sans passe
supplémentaire. La version naïve faisait `iter().fold()` puis `clear()` — deux
parcours logiques au lieu d'un.

### Compter les caractères, pas les octets

```rust
fn pad(out: &mut String, text: &str, width: usize) {
    out.push_str(text);
    for _ in text.chars().count()..width {
        out.push(' ');
    }
}
```

Celle-ci n'est pas une optimisation mais une **correction** trouvée en chemin.
`{:<6}` compte les caractères Unicode ; si un jour un opérateur s'appelle `≤` ou
`√`, `text.len()` (des octets) donnerait un remplissage faux. Écrire le
remplissage à la main rend la règle explicite.

Chercher la vitesse fait relire du code ligne à ligne, et on y trouve des bugs.
C'est un effet de bord fréquent et bienvenu.

---

## 10. Le profil de compilation

Rien à changer dans le code, tout dans `Cargo.toml` :

```toml
[profile.release]
opt-level = 3
lto = "fat"
codegen-units = 1
strip = true
```

| Réglage | Effet | Coût |
|---|---|---|
| `opt-level = 3` | optimisations maximales (défaut en release) | — |
| `lto = "fat"` | optimisation entre unités de compilation : le code de la bibliothèque peut être inliné dans le binaire | compilation nettement plus lente |
| `codegen-units = 1` | une seule unité, donc vision globale du programme | plus de compilation en série, moins de parallélisme |
| `strip = true` | enlève les symboles de débogage | traces d'exécution illisibles |

`lto` et `codegen-units = 1` vont ensemble : ils donnent typiquement 5 à 20 % sur
du code comme le nôtre, contre plusieurs fois le temps de compilation. C'est
acceptable pour un binaire qu'on publie, pénible pendant le développement — d'où
le fait que ça ne s'applique qu'au profil `release`.

Un cinquième réglage existe, qu'on **n'active pas** :

```toml
panic = "abort"
```

Il supprime le code de déroulement de pile : binaire plus petit, un peu plus
rapide. Mais il casse `cargo test --release`, puisque le harnais de test attrape
les paniques pour rapporter les échecs. À n'activer que sur un profil dédié à la
distribution.

---

## 11. Ce qu'on n'a pas fait, et pourquoi

Aussi important que la liste précédente.

**Remplacer le `match` par une table de hachage.** Testé mentalement au chapitre
07, et la mesure ne le dément pas : le `match` sur `&str` compile en un arbre de
décision (longueur, puis comparaison) plus rapide que n'importe quel hachage, et
sans initialisation au démarrage.

**Écrire un analyseur de nombres maison.** `str::parse::<f64>` de la bibliothèque
standard implémente l'algorithme d'Eisel-Lemire, correctement arrondi et
redoutablement rapide. Toute version maison serait plus lente **et** fausse sur
les cas limites.

**Du SIMD.** Notre pile fait cinq éléments et les opérations sont séquentielles
par nature — chaque jeton dépend du précédent. Il n'y a rien à vectoriser.

**Du `unsafe` dans le moteur.** J'ai envisagé `get_unchecked` pour éviter la
vérification de bornes dans `pop2`. Gain non mesurable, et on échange une garantie
du compilateur contre une promesse humaine. Le seul `unsafe` du projet est dans le
compteur d'allocations du chapitre 16, où il est inévitable.

**Une arène globale ou un allocateur maison.** On est déjà à zéro allocation en
régime établi. Il n'y a plus rien à allouer plus vite.

Le point commun : **chacune de ces idées est raisonnable a priori, et la mesure
les élimine.** C'est exactement pourquoi le chapitre 16 vient avant celui-ci.

---

## 12. Le bilan

```bash
cd tuto/opti
cargo test
cargo run --release --example bench
cargo run --release --example allocs
```

| Mesure | naïf | optimisé | gain |
|---|---:|---:|---:|
| évaluation d'une ligne | 110 ns | 95 ns | ×1,2 |
| ligne qui échoue | 52 ns | 29 ns | ×1,8 |
| trace + rendu d'une ligne | 1150 ns | 356 ns | ×3,2 |
| formatage d'une pile de 8 | 217 ns | 106 ns | ×2,0 |
| invite complète | 355 ns | 101 ns | ×3,5 |

Allocations en régime établi :

| Chemin | naïf | optimisé |
|---|---:|---:|
| 6 lignes évaluées | 6 | **0** |
| 2 lignes en erreur | 4 | **0** |
| 6 lignes tracées et rendues | 367 | **0** |
| 10 formatages d'une pile | 100 | **0** |

Le programme optimisé, une fois lancé et chauffé, **n'appelle plus jamais
l'allocateur**. Quoi qu'on lui donne à calculer.

### Les trois leçons

**1. Le gain n'était pas là où on l'attendait.** L'évaluation, le cœur supposé du
programme, ne gagne que 20 %. L'affichage et la trace gagnent 200 à 250 %. Sans
mesure préalable, on aurait passé la journée à micro-optimiser `eval_token` pour
rien.

**2. Presque tout le gain vient des allocations.** Pas des instructions, pas des
branchements : du fait de ne plus demander de mémoire au système. Sur du code
Rust idiomatique, c'est de très loin la première source d'accélération —
chercher les `String`, `Vec`, `to_string`, `clone`, `collect` sur les chemins
chauds paie plus que toutes les astuces.

**3. Ça a coûté cher en lisibilité.** Compare :

```bash
wc -l src/*.rs           # version du chapitre 15
wc -l tuto/opti/src/*.rs # version optimisée
```

Le code optimisé est plus long, contient des durées de vie, un paramètre
générique de valeur, une arène indexée, et deux traits `Write` homonymes. Pour un
outil qu'on lance à la main dix fois par jour, **le programme du chapitre 15 est
le bon**. Cette version-ci ne se justifierait que si `blap` devenait la
bibliothèque d'évaluation d'un tableur, ou lisait des fichiers d'un million de
lignes.

C'est ça, savoir optimiser : pas connaître les astuces, mais savoir **quand elles
ne valent pas leur prix**.

---

## Pour continuer

- Reprends l'exercice 14 du chapitre 15 (le mode infixe) et écris-le directement
  dans la version optimisée. Tu verras si tu as compris les durées de vie.
- Ajoute un mode `-f fichier.rpn` qui évalue un million de lignes, et compare les
  deux versions pour de vrai, sur une charge réelle. C'est là que le facteur
  compte.
- Installe `samply` (`cargo install samply`) et profile
  `samply record ./target/release/blap_opti -t "…"`. Voir la flamme de son propre
  programme est un moment qu'on n'oublie pas.

---

**Retour au [sommaire](README.md).**
