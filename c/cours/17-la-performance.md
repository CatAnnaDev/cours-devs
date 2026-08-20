# 17 — La performance

Tous les chiffres viennent de programmes compilés et lancés sur la machine de référence : Apple M4,
arm64, macOS 27, Apple clang 21, un seul thread, `-O2` sauf mention contraire. Aucun n'est cité de
mémoire, et aucun ne vaut pour ta machine tant que tu ne l'as pas relancé dessus.

## On ne devine pas, on mesure

Les trois règles générales — mesurer avant, mesurer la bonne chose, optimiser ce qui compte — sont
dans `notions/mesurer.md`, avec les pièges communs à tous les langages. Ce chapitre ajoute ce qui
est propre au C, et qui tient en une phrase. **Le code que tu as écrit n'est pas le code qui
s'exécute.** « Mesurer la bonne chose » veut donc dire, en plus, vérifier que ce que tu crois
mesurer existe encore dans le binaire.

Premier corollaire : un binaire de mise au point n'a rien à dire sur la performance. Même somme de
4 Mio d'`int` : 3,06 ms en `-O0`, 2,04 ms en `-O1`, 0,453 ms en `-O2`, 0,434 ms en `-O3`, 1,18 ms
en `-Os`. Avec ASan et UBSan par-dessus : 12,44 ms en `-O0`, 2,93 ms en `-O2`. Le lanceur des
exercices compile en `-O0` avec les deux sanitizers, soit **27 fois** plus lent que `-O2` :
excellent pour trouver un débordement, inutilisable pour chronométrer.

## Mesurer honnêtement en C

### L'horloge

La norme C17 ne fournit que `clock()`, qui compte du temps processeur en unités de
`CLOCKS_PER_SEC`, et `timespec_get(&t, TIME_UTC)`, qui donne l'heure murale et peut donc reculer.
Ni l'une ni l'autre ne convient. L'horloge monotone vient de POSIX. C23 ajoute un `TIME_MONOTONIC`
optionnel pour `timespec_get` ; il n'est **pas** défini dans la libc d'Apple, même en `-std=c23`.

```c
#include <time.h>

static double maintenant(void) {
    struct timespec t;
    clock_gettime(CLOCK_MONOTONIC, &t);          // POSIX, pas la norme C
    return (double)t.tv_sec + (double)t.tv_nsec * 1e-9;
}
```

Le grain est une propriété de l'implémentation, pas de la norme. Sur macOS,
`clock_getres(CLOCK_MONOTONIC)` rend 1000 ns et le plus petit écart réellement observé entre deux
lectures vaut 1000,0 ns : elle avance par pas d'une microseconde, quand `CLOCK_MONOTONIC_RAW` et
`CLOCK_UPTIME_RAW` rendent 42 ns, le tic de `mach_absolute_time` valant 125/3 ns. Un appel à
`maintenant()` coûte lui-même **28,8 ns**. Ne chronomètre jamais un bloc plus court qu'environ
mille fois le grain, ici une milliseconde.

### Répéter, et prendre la médiane

Vingt et une exécutions de la même boucle de 5 millions d'itérations donnent, en ms,
`7,43 4,89 5,20 5,24 5,30 ... 5,36` : min 4,89, médiane 5,30, moyenne 5,36, max 7,43. La première
est 1,40 fois plus lente que la médiane — caches froids, branches non prédites, pages pas encore
fautées. La moyenne l'absorbe et se déplace de 1 %, la médiane l'ignore. Publie **min, médiane et
max** : si max/min dépasse 1,5, la mesure ne vaut rien.

### Le piège : le compilateur supprime ton banc d'essai

```c
double jetee(unsigned n) {
    double t0 = maintenant();
    for (unsigned i = 1; i <= n; i++) melanger(i);   // résultat inutilisé
    return maintenant() - t0;
}
```

Mesuré : **0,000 ms** pour 20 millions d'itérations. L'assembleur `-O2` explique tout — deux appels
à `_clock_gettime`, un `fsub`, un `ret`, aucune branche entre les deux : la boucle n'existe plus.
Deux parades : accumuler le résultat et s'en servir après la mesure donne 21,0 ms et 21 blocs de
base, boucle vectorisée ; encadrer chaque tour de la barrière opaque
`__asm__ volatile("" : : "r"(x) : "memory")` donne 45,1 ms et 4 blocs, boucle scalaire. Elle
marche, mais elle **change ce que tu mesures** : ici elle interdit la vectorisation et double le
temps.

Et il y a pire. Avec un noyau affine — huit tours de `x = 1664525 * x + 1013904223` — même la
version accumulée disparaît : `-O2` compose les huit étapes en une seule et remplace la boucle par
sa forme close. Sous la barrière, il ne reste qu'une addition par tour.

```
LBB2_2:
	add	w10, w10, w11       ; les huit multiplications ont disparu
	subs	w19, w19, #1
	b.ne	LBB2_2
```

La barrière garantit que la **valeur** est produite, pas qu'elle est calculée comme tu l'as écrite.
Le seul contrôle fiable est de lire l'assembleur : `cc -O2 -S -o - source.c`.

## Limité par le calcul ou par la mémoire

C'est le seul diagnostic qui oriente le travail, et il se tranche sur le papier, avec deux repères.

**Débit de calcul** : des chaînes de FMA `double` indépendantes en NEON, pour saturer les unités.
Avec 4 chaînes, 14,0 Gflop/s ; 8, 34,1 ; 16, 51,2 ; 24, **52,7** ; 32, 38,3 car on déborde les
registres. Le pic est vers **52 Gflop/s**, et avec quatre chaînes on en atteint le quart : sans
parallélisme d'instructions, c'est la latence des FMA qui commande, pas leur débit.

**Débit mémoire** : la triade `d[i] = a[i] + k * b[i]` sur trois tableaux de 256 Mio, donc 805 Mo
traversés, tient en 9,3 ms, soit **86 Go/s**.

L'**intensité arithmétique** d'un noyau est son nombre d'opérations flottantes divisé par le nombre
d'octets qu'il doit traverser. Le point de bascule est le rapport des deux repères : 52 / 86 =
**0,6 flop par octet**. En dessous on est limité par la mémoire, au-dessus par le calcul.

| noyau | intensité | plafond | mesuré `-O2` | mesuré `-ffast-math` |
|---|---|---|---|---|
| `s += a[i]` sur `double` | 1/8 = 0,125 | 86 Go/s | 15,3 Go/s | 61,8 Go/s |
| Horner degré 15, 15 FMA | 30/8 = 3,75 | 52 Gflop/s | 45,5 Gflop/s | 52,8 Gflop/s |

Lis bien la première ligne : à `-O2`, la somme est **cinq fois en dessous** de son propre plafond
mémoire. Elle n'est donc ni limitée par le calcul ni par la bande passante, mais par une troisième
chose — la chaîne de dépendances des additions flottantes, une par élément. L'intensité dit aussi
quand on n'est d'aucun des deux côtés, et qu'il faut chercher ailleurs. Le test empirique reste le
plus rapide : divise la taille des données par deux ; si le temps suit, tu traverses de la mémoire.

## Les gains qui rapportent, dans l'ordre

### 1. Un meilleur algorithme

Compter les paires de doublons parmi 50 000 `int` tirés dans `[0, 20000)` : **143,0 ms** en double
boucle O(n²), **2,71 ms** en triant puis balayant, O(n log n). **53 fois**, même réponse (62 344),
et l'écart croît avec n. Aucune micro-optimisation ne rattrape ça, et c'est le seul gain qui
s'améliore quand les données grossissent.

### 2. Une meilleure disposition mémoire

Matrice `float` 4096×4096, soit 64 Mio, sommée par lignes puis par colonnes : 8,49 ms contre
51,6 ms en `-O2`, et 1,11 ms contre 53,5 ms en `-O2 -ffast-math`, soit **48 fois**. Le parcours par
colonnes avance par pas de 16 Kio : il ne profite ni du préchargement ni des lignes de cache, et il
ne se vectorise pas. Même code, même nombre d'additions.

Tableau de structures contre structure de tableaux, 4 Mi particules de 64 octets dont on ne lit que
`x`, `y`, `z` : l'AoS traverse 256 Mio en **4,23 ms**, soit 64 Go/s. Le SoA ne traverse que 48 Mio,
et met **2,32 ms** à `-O2` : le volume a été divisé par 5,3, le temps seulement par 1,8, parce que
le débit s'effondre à 21 Go/s — trop peu de travail par ligne chargée pour tenir le préchargeur
occupé. Il faut `-ffast-math` pour descendre à 0,80 ms et retrouver le plein débit. C'est la
section « Le budget » en pratique.

### 3. Moins d'allocations

Deux millions de nœuds de 16 octets, construits puis parcourus (chapitre 12) : **20,8 ms** avec un
`malloc` par nœud, **1,20 ms** avec une arène et un seul `malloc` (17x), **0,44 ms** avec un
tableau contigu sans liste (47x). Coûts bruts sur cette libc : `malloc(16)` **3,8 ns**, `free`
**5,5 ns**, soit 9,3 ns par nœud et 18,6 ms pour deux millions : presque tout l'écart.

### 4. Moins d'appels indirects

Même boucle, même fonction, un seul mot-clé de différence :

```c
static int (*p)(int) = double_de;   // 0,461 ms
       int (*q)(int) = double_de;   // 6,235 ms
```

**13,5 fois.** Sur le pointeur `static`, le compilateur prouve qu'aucune autre unité ne peut le
changer : il remplace l'appel par le corps, puis vectorise. Sur le global, il doit charger le
pointeur et sauter. Le surcoût direct d'un appel indirect est d'environ 0,7 ns ; son vrai coût est
d'interdire l'inlining et la vectorisation autour. Cas réel : trier 4 Mi `int` avec `qsort` et son
comparateur par pointeur coûte **339 ms**, contre **208 ms** pour un tri spécialisé — 1,6 fois.

## Ce que `-O2` fait déjà pour toi

Trois « optimisations manuelles », et l'assembleur comparé instruction par instruction avec celui
de la version naïve :

```c
unsigned a_mul(unsigned x) { return x * 8u; }    // lsl w0, w0, #3
unsigned a_dec(unsigned x) { return x << 3; }    // lsl w0, w0, #3    identique
double d_pow(double x) { return pow(x, 2.0); }   // fmul d0, d0, d0
double d_mul(double x) { return x * x; }         // fmul d0, d0, d0   identique
for (size_t i = 0; i < n; i++) s += v[i] * (k * k + 1);   // invariant recalculé
const int c = k * k + 1;                                  // sorti à la main
for (size_t i = 0; i < n; i++) s += v[i] * c;             // 84 instructions, identiques
```

Réduction de force, repli de `pow` en multiplication, sortie d'invariant, élimination de
sous-expression commune, déroulage et vectorisation — la somme d'`int` traite seize entiers par
tour. Les écrire à la main ne produit rien de plus, seulement du code moins lisible.

Voici la quatrième, celle qui produit du code **faux** :

```c
int e_div(int x) { return x / 8; }   // add w8,w0,#7 / cmp w0,#0 / csel / asr w0,w8,#3
int e_dec(int x) { return x >> 3; }  // asr w0, w0, #3
```

Le compilateur n'a pas oublié de simplifier : il ne peut pas. La norme impose depuis C99 que la
division entière tronque **vers zéro**, alors que le décalage arrondit **vers moins l'infini**. Les
trois instructions en plus corrigent exactement ça pour les négatifs : `-9 / 8` vaut `-1` quand
`-9 >> 3` vaut `-2`, et `-3 / 8` vaut `0` quand `-3 >> 3` vaut `-1`. Au passage, le décalage à
droite d'un `int` négatif est **défini par l'implémentation** en C17 comme en C23 — arithmétique
sur celle-ci, mais rien ne l'impose. C23 a rendu obligatoire le complément à deux pour la
*représentation*, pas la sémantique de `>>` ; et `<<` sur un signé négatif reste un comportement
indéfini. De toute façon ça ne change rien ici : ce sont deux opérations différentes.

## Ce que `-O2` ne peut pas faire

**Il ne voit pas au-delà de l'unité de traduction.** Une fonction de deux lignes appelée 16 Mi fois
et définie dans un autre `.c` : **12,86 ms** en `-O2`, **1,09 ms** en `-O2 -flto`, et **1,09 ms**
aussi si on fusionne les deux fichiers. **11,8 fois**, et LTO retrouve exactement le chiffre du
fichier unique. C'est le meilleur rapport gain sur effort de tout le chapitre : un drapeau.

**Il ne sait rien d'un objet à portée externe.** C'est le `static` du pointeur plus haut : 13,5
fois, pour un mot-clé. Tout ce qui n'a pas besoin d'être visible dehors doit être `static`
(chapitre 10) : ce n'est pas de l'hygiène, mais de l'information donnée au compilateur.

**Il doit supposer que deux pointeurs se recouvrent**, et c'est ce que `restrict` promet. Mais
attention au folklore : sur clang 21, la mesure ne le confirme pas. Une boucle à six pointeurs
donne 1,022 ms sans `restrict` et 1,016 ms avec, soit rien. La raison est dans l'assembleur : la
version sans en contient **cinq contrôles de recouvrement** et 114 instructions, celle avec en
contient zéro et 98. Le compilateur ne renonce pas, il **duplique la boucle** et choisit à
l'exécution — seize instructions une fois par appel, invisibles sur 4 Mi éléments. `restrict` reste
utile — il documente un contrat, il aide quand la duplication est impossible — mais ce n'est pas un
accélérateur, et c'est une **promesse** : la violer est un comportement indéfini (chapitre 11).

## La vectorisation

Pour savoir si elle a eu lieu, on demande au compilateur. Il faut `-O2` au moins :

```
cc -O2 -Rpass=loop-vectorize -Rpass-missed=loop-vectorize -Rpass-analysis=loop-vectorize -c f.c
```

Sortie anticipée, indexation non affine, dépendance portée par la boucle, appel non inlinable : les
quatre familles de blocage, avec les messages réellement obtenus.

| boucle | diagnostic de clang 21 |
|---|---|
| `if (v[i] == c) return i;` | `Cannot vectorize potentially faulting early exit loop` |
| `d[i] = a[idx[i]];` | `cannot identify array bounds` |
| `d[i] = d[i-1] * 0.5f + d[i];` | `unsafe dependent memory operations in loop` |
| appel à une fonction externe | `call instruction cannot be vectorized` |

Reste le contre-exemple des flottants, et il est retors. `float s = 0; s += v[i];` est signalée
**vectorisée**, largeur 4, entrelacement 4 — et pourtant sept fois plus lente que la même boucle
en `-ffast-math`. L'assembleur tranche :

```
	ldp	q1, q2, [x9, #-32]   ; les chargements sont bien vectoriels
	fadd	s0, s0, s1           ; mais l'accumulation reste scalaire :
	fadd	s0, s0, s5           ; seize additions en chaîne par tour
	...                          ; (la version int fait add.4s, sur quatre voies)
```

C'est une réduction **ordonnée** : le compilateur refuse de regrouper les additions parce que
l'addition flottante n'est pas associative. Sur 16 Mi `float` valant 1,0 précédés d'un `2^24`, la
somme de gauche à droite donne **16 777 216** en 9,14 ms, celle relâchée sur seize accumulateurs —
quatre vectoriels de quatre voies, ce que veut dire « largeur 4, entrelacement 4 » — donne **32 505
856** en 1,29 ms, et l'exact est **33 554 431**. Avec quatre accumulateurs scalaires on obtiendrait
29 360 128 : le chiffre dépend du nombre de voies, et c'est bien le problème. La version « sûre »
est donc 7,1 fois plus lente **et** plus fausse, chaque `+ 1,0f` étant absorbé par un `2^24` déjà
accumulé : le compilateur ne protège pas l'exactitude, il protège la reproductibilité.

La parade est chirurgicale, jamais globale : `#pragma clang loop vectorize(enable)` sur la boucle,
ou `#pragma float_control(precise, off)` sur la fonction. Les deux produisent bien les `fadd.4s`
attendus. Attention à ne pas croire qu'on y gagne en sûreté : `float_control(precise, off)` pose
exactement les mêmes hypothèses que `-ffast-math`, absence de NaN et d'infini comprise — seule la
**portée** est réduite. Le seul des deux qui n'autorise que la réassociation est
`#pragma clang loop vectorize(enable)`.

## Le budget

Raisonner en opérations est un réflexe hérité d'une époque où elles étaient chères : ici, un accès
manqué en cache coûte cent fois un accès en L1. Le bon compte est en octets traversés, et l'unité
de traversée n'est pas l'octet mais la **ligne de cache**.

```
lignes touchées = octets TRAVERSÉS par élément x nombre d'éléments / taille de ligne
temps plancher  = lignes touchées x taille de ligne / débit du niveau atteint
```

`sysctl hw.cachelinesize` annonce **128 octets** ici, et un balayage à pas croissant sur 256 Mio le
confirme : 1,12 ns par accès à un pas de 64 octets, puis un palier plat à 3,0-3,4 ns dès le pas de
128. Le niveau atteint dépend, lui, du jeu de données ; latence d'un accès dépendant :

| jeu | 16 Kio | 64 Kio | 256 Kio | 1 Mio | 4 Mio | 16 Mio | 64 Mio | 256 Mio |
|---|---|---|---|---|---|---|---|---|
| latence | 1,30 ns | 1,00 ns | 4,72 ns | 5,34 ns | 6,65 ns | 19,7 ns | 91,8 ns | 100,5 ns |

Les paliers correspondent aux 128 Kio de L1 de données et aux 16 Mio de L2 annoncés par `sysctl`.
Du meilleur au pire cas : cent fois. Reprenons les particules avec la formule, sans rien lancer :

```
AoS : 64 octets x 4 194 304 = 268 435 456 octets = 2 097 152 lignes de 128
SoA : 12 octets x 4 194 304 =  50 331 648 octets =   393 216 lignes de 128
plancher a 64 Go/s : 268435456 / 64e9 = 4,19 ms      50331648 / 64e9 = 0,79 ms
```

Mesuré : **4,23 ms**, et **0,80 ms** pour le second — mais seulement à `-ffast-math` ; à `-O2` il
met 2,32 ms, le débit n'étant pas atteint. Le budget donne le **plancher**, pas la promesse. La
prédiction a coûté deux divisions, aucun profileur. Le budget se calcule avant d'écrire le code, il
dit ce qu'aucune micro-optimisation ne franchira, et quand la mesure s'en écarte beaucoup, c'est
qu'autre chose commande — une chaîne de dépendances, un appel non inliné, une vectorisation
manquée.

## À retenir

1. Un binaire `-O0` avec sanitizers est 27 fois plus lent qu'un `-O2` : il ne mesure rien.
2. Sur macOS, `CLOCK_MONOTONIC` a un grain d'une microseconde : chronomètre au moins une
   milliseconde, répète, prends la médiane, publie min et max.
3. Vérifie dans l'assembleur que la boucle mesurée existe encore : accumuler le résultat ne suffit
   pas toujours, `-O2` sait résoudre une récurrence en forme close.
4. Calcule l'intensité arithmétique avant de toucher au code : sous 0,6 flop par octet sur cette
   machine, le nombre d'opérations ne décide de rien.
5. L'ordre des gains est algorithme (53x), disposition mémoire (5 à 48x), allocations (17 à 47x),
   appels indirects (13x) — et `-flto` seul en vaut 11,8.
6. `-O2` fait déjà réduction de force, sortie d'invariant, CSE, déroulage et vectorisation ;
   remplacer `x / 8` par `x >> 3` sur un `int` signé ne l'aide pas, ça le casse.
7. `-Rpass=loop-vectorize` peut annoncer « vectorisé » pour une réduction flottante restée
   scalaire : l'addition n'est pas associative, et seul l'assembleur dit la vérité.

**Exercices : `17_perf`.**
