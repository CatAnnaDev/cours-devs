# 16 — Les ranges

Tout ce qui est dit « mesuré » ici a été compilé et lancé sur la machine de référence : arm64
macOS, Apple clang 21, libc++ `_LIBCPP_VERSION` 220106, `-std=c++23`. La norme décrit des concepts
et des complexités, jamais des tailles ni des temps ni un catalogue complet : ce qui vient de cette
libc++ est signalé comme tel.

## Le problème : deux itérateurs partout

Un algorithme de `<algorithm>` prend deux itérateurs, et les trois conséquences font mal. **On
réécrit toujours la même paire**, `v.begin(), v.end()`. **On ne compose pas** : « les pairs, au
carré, les trois premiers » n'est pas un enchaînement mais trois passes et deux vecteurs jetables.
**On peut se tromper de bornes** — ceci compile sans un diagnostic à `-Wall -Wextra`, et c'est UB :

```cpp
std::vector<int> v{5, 3, 1};
std::vector<int> w{9, 8, 7};
std::sort(v.begin(), w.end());   // compile ; zero avertissement ; UB
```

### Avant, après

```cpp
std::vector<int> nombres(1000);                  // 0, 1, 2, ... 999
std::iota(nombres.begin(), nombres.end(), 0);
constexpr auto est_pair = [](int x) { return x % 2 == 0; };
constexpr auto carre    = [](int x) { return x * x; };

// avant : trois passes, deux vecteurs intermediaires
std::vector<int> pairs, carres;
std::copy_if(nombres.begin(), nombres.end(), std::back_inserter(pairs), est_pair);
carres.reserve(pairs.size());
std::transform(pairs.begin(), pairs.end(), std::back_inserter(carres), carre);
std::vector<int> classique(carres.begin(), carres.begin() + 3);
```

```cpp
// apres : une expression, rien de materialise avant le dernier maillon
auto trois = nombres | std::views::filter(est_pair) | std::views::transform(carre)
                     | std::views::take(3) | std::ranges::to<std::vector>();
```

Les deux donnent `0 4 16`. Sur 1000 éléments, `operator new` compté : **12 allocations, 6104
octets** pour la version classique, **1 allocation, 12 octets** pour la version ranges — le
`take(3)` remonte jusqu'à la source et arrête le travail après trois éléments.

## Le vocabulaire

| Mot | Ce que c'est | Le concept qui le définit |
|---|---|---|
| **range** | ce sur quoi `ranges::begin` et `ranges::end` marchent | `std::ranges::range<R>` |
| **vue** | un range déplaçable en O(1), et copiable en O(1) s'il est copiable du tout | `std::ranges::view<V>` |
| **sentinelle** | l'objet de fin, pas forcément du type de l'itérateur | `std::sentinel_for<S, I>` |
| **adaptateur** | l'objet qui fabrique une vue, branchable avec `\|` | aucun : ce sont des objets |
| **projection** | l'invocable appliqué à l'élément avant le prédicat | `indirectly_regular_unary_invocable` |

Un conteneur est un range mais **pas** une vue : mesuré, `view<vector<int>>` est faux,
`view<ref_view<vector<int>>>`, `view<string_view>` et `view<span<int>>` vrais. La frontière est le
coût de la copie, pas la propriété : `owning_view<vector<int>>` est une vue de 24 octets qui
déplace le vecteur. Mesuré : `ref_view` 8 octets, `iota_view` 8 bornée ou non, `span<int>` 16.

### La sentinelle, concrètement

Avant C++20, la fin d'un range était du même type que le début ; une sentinelle est n'importe quoi
qui sait répondre `== iterateur`. Ce type parcourt une chaîne C **sans `strlen` préalable** :

```cpp
struct FinDeChaine { bool operator==(const char* p) const { return *p == '\0'; } };
auto s = std::ranges::subrange(texte, FinDeChaine{});
```

Mesuré : `ranges::distance(s)` vaut 7 pour `"bonjour"`, `sized_range<decltype(s)>` est **faux**, et
`sizeof(s)` vaut **8 octets** contre 16 pour un `subrange<const char*, const char*>`. Cas limite
standard : `std::unreachable_sentinel_t`, 1 octet, jamais égale à rien, c'est le `end()` de
`views::iota(1)` — le test de fin s'évapore.

## Les algorithmes de `std::ranges`

Quatre différences. **Ils prennent le range entier**, ou un couple itérateur/sentinelle. **Ils sont
contraints par des concepts**, donc l'erreur tombe au point d'appel : mesuré, `std::sort` sur une
`std::list` produit **114 lignes** de diagnostic dont la première pointe dans
`__algorithm/make_heap.h`, contre **22** pour `std::ranges::sort(l)`, première ligne sur le fichier
source. **Ils rendent plus** : `ranges::sort` rend l'itérateur de fin, `std::sort` rend `void`.
**Ils prennent une projection.**

### Les projections, le gain quotidien

Le dernier paramètre par défaut de presque tous ces algorithmes est une projection : un invocable
appliqué à l'élément **avant** le prédicat, et un pointeur sur membre en est un. Bloc lancé :

```cpp
struct Employe { std::string nom; int age; double paye; };
std::vector<Employe> eq{{"ana", 41, 3200}, {"bo", 29, 4100}, {"cy", 35, 2900}};
std::ranges::sort(eq, {}, &Employe::age);                       // tri par age croissant
auto vieux  = std::ranges::max_element(eq, {}, &Employe::age);  // "ana"
auto pauvre = std::ranges::min_element(eq, {}, &Employe::paye); // "cy"
auto n = std::ranges::count_if(eq, [](int a) { return a >= 35; }, &Employe::age); // 2
```

Le `{}` central est le comparateur par défaut, `std::ranges::less`. Sans projection, chaque lambda
reprendrait `const Employe&` pour chercher un champ : la projection sépare **quoi comparer** de
**comment comparer**, et les deux se réutilisent séparément. Côté manques, vérifié en compilant :
`ranges::accumulate`, `reduce`, `inner_product`, `partial_sum` et `adjacent_difference`
**n'existent pas**, la norme n'en prévoit pas ; le remplaçant C++23 `ranges::fold_left` est là,
mais `fold_left_first`, `fold_right` et `fold_right_last` sont **absents de cette libc++** et
`__cpp_lib_ranges_fold` n'y est jamais défini. `ranges::iota` existe, dans `<numeric>`. Enfin aucun
algorithme `std::ranges` n'accepte de politique d'exécution : `ranges::sort(execution::seq, v)` ne
compile pas, et ici `<execution>` réclame `-D_LIBCPP_ENABLE_EXPERIMENTAL` pour seulement exister.

## Les vues : paresseuses, sans propriété, composables

Une vue ne fait rien à la construction : elle range son prédicat, garde un `ref_view` de 8 octets
sur sa source, et attend qu'on l'itère. Deux compteurs globaux le prouvent, sur `v = 1..6` :

```cpp
auto vue = v | vw::filter   ([](int x) { ++n_pred;    return x % 2 == 0; })
             | vw::transform([](int x) { ++n_transfo; return x * 10;     });
```

| Étape | `n_pred` | `n_transfo` |
|---|---|---|
| après la construction de `vue` | 0 | 0 |
| après `vue.begin()` seul, puis après `*vue.begin()` | 2 puis 2 | 0 puis 1 |
| parcours complet, pipeline neuf | 6 | 3 |
| `... \| take(1)`, pipeline neuf | 4 | 1 |
| deux parcours du même pipeline | 10 | 6 |

Zéro à la construction : la paresse est réelle. Deux appels au prédicat pour un simple `begin()` :
`filter_view::begin()` doit **chercher** le premier élément retenu, rejette 1 puis accepte 2. Ce
n'est pas O(1), et la norme l'**oblige** à mémoriser le résultat : sans ce cache, `filter_view` ne
pourrait pas offrir le `begin()` en temps amorti constant qu'exige le concept `range`. D'où la
dernière ligne, où le second parcours ne coûte que 4 prédicats.

### L'ordre réel d'évaluation, qui surprend

Retournons le pipeline : transformer **puis** filtrer, six éléments en entrée.
```cpp
auto vue = v | vw::transform([](int x) { ++n_transfo; return x * 10;      })
             | vw::filter   ([](int x) { ++n_pred;    return x % 20 == 0; });
```

Mesuré : `n_pred` = 6, `n_transfo` = **9**. La transformation tourne neuf fois pour six éléments,
parce qu'un itérateur de `filter_view` évalue le prédicat sur `*it` — ce qui déclenche la
transformation — puis le corps de la boucle déréférence à nouveau et la transformation
**recommence** ; `transform_view` ne met rien en cache, sa fonction étant réputée pure. La règle :
**filtrer avant de transformer**, et aucun effet de bord dans un `transform` amont d'un `filter`.

## Le piège central : une vue ne possède rien

`v | views::filter(...)` sur une lvalue produit un `ref_view`, c'est-à-dire un pointeur nu : la vue
vaut ce que vaut sa source, et l'invalidation d'itérateurs la casse comme un itérateur.

```cpp
std::vector<int> source{1, 2, 3, 4, 5, 6};
auto pairs = source | vw::filter([](int x) { return x % 2 == 0; });
for (int x : pairs) somme += x;   // ok : met en cache le begin()
source.push_back(8);              // realloc : l'ancien tampon est libere
for (int x : pairs) somme += x;   // le begin() en cache pointe dans le vide
```

Lancé avec `-fsanitize=address`, le second parcours donne :

```
==89981==ERROR: AddressSanitizer: heap-use-after-free on address 0x603000000fa4
READ of size 4 at 0x603000000fa4 thread T0
    #0 0x0001003e5078 in main t5b.cpp:17
freed by thread T0 here:
    #10 ... std::vector<int>::push_back[abi:nqe220106](int&&) vector.h:466
    #11 0x0001003e4e8c in main t5b.cpp:15
```

ASan nomme les deux lignes, celle qui lit et le `push_back` qui a libéré. Variante encore plus
fréquente, une fonction qui rend une vue sur son paramètre `const std::vector<int>&` appelée avec
un temporaire : même faute, ASan répond `stack-use-after-scope` au lieu de `heap-use-after-free`.

### `borrowed_range` et `dangling`

Un range est **emprunté** quand ses itérateurs survivent à la mort de l'objet range. Mesuré : vrai
pour `vector<int>&`, `string_view`, `span<int>`, `subrange<It>` et `iota_view<int>` ; faux pour
`vector<int>` prvalue et pour `owning_view<vector<int>>`. La bibliothèque s'en sert pour refuser un
itérateur pendant : sur une fonction qui rend un `vector` par valeur, `ranges::find(fabrique(), 2)`
ne rend pas un itérateur mais `std::ranges::dangling`, type vide et inutilisable.

```
error: indirection requires pointer operand
       ('borrowed_iterator_t<std::vector<int>>' (aka 'std::ranges::dangling') invalid)
```

Quatre lignes de diagnostic à la compilation au lieu d'un use-after-free à l'exécution. Le même
mécanisme choisit la vue de base : `views::all_t<vector<int>&>` est `ref_view`, et sur un
temporaire `views::all_t<vector<int>>` est `owning_view` — vérifié. D'où le fait que
`fabrique() | views::filter(...)` soit sûr là où une vue sur un `const&` temporaire ne l'est pas.

## Le catalogue disponible ici

Chaque ligne a été compilée et lancée en `-std=c++23`, mais sur l'entrée qui convient à
l'adaptateur : `{1,2,3,4,5,6,7,8}` pour la plupart, une `string` pour `split`, une `map` pour
`keys` et `values`, deux séquences pour `zip`, et un vecteur de vecteurs pour `join`.

| Adaptateur | Norme | Sortie mesurée |
|---|---|---|
| `filter(pair)` / `transform(carre)` | C++20 | `2 4 6 8` / `1 4 9 16 25 36 49 64` |
| `take(3)` / `take_while(<5)` | C++20 | `1 2 3` / `1 2 3 4` |
| `drop(5)` / `drop_while(<5)` | C++20 | `6 7 8` / `5 6 7 8` |
| `reverse` / `iota(3) \| take(4)` | C++20 | `8 7 6 5 4 3 2 1` / `3 4 5 6` |
| `join` / `split(',')` sur `"un,deux,,trois"` | C++20 | `1 2 3 4 5 6` / `[un] [deux] [] [trois]` |
| `keys` / `values` / `counted(v.begin()+2, 3)` | C++20 | `ana bo` / `41 29` / `3 4 5` |
| `zip(iota(0), lettres)` / `adjacent<2>` | C++23 | `0:a 1:b 2:c` / `12 23 34 45 56 67 78` |
| `join_with(0)` / `chunk_by(less_equal)` / `repeat(7) \| take(3)` | C++23 | `1 2 0 3 0 4 5 6` / `129 345 1` / `7 7 7` |

**Absents de cette libc++**, vérifié y compris avec `-D_LIBCPP_ENABLE_EXPERIMENTAL` et
`-std=c++2c` : `views::enumerate`, `chunk`, `stride`, `slide`, `cartesian_product`, `as_const`,
`concat`. Le manque qui gêne est `enumerate` ; le remplacement tient en une ligne, et marche :

```cpp
for (auto [i, m] : std::views::zip(std::views::iota(0), mots))
  std::printf(" %d=%s", i, m.c_str());   // 0=un 1=deux 2=trois
```

Deux propriétés à connaître avant de composer, mesurées : `filter_view` est **bidirectionnel, pas
à accès aléatoire, et pas `sized_range`** — d'où l'échec de `ranges::sort` sur un `filter`. Et
`filter_view` comme `drop_while_view` **ne s'itèrent pas en `const`**, ayant un cache à remplir,
alors que `transform`, `take` et `reverse`, si : un paramètre `const auto&` refuse un `filter`.

## Matérialiser

`std::ranges::to` est du **C++23** ; `__cpp_lib_ranges_to_container` vaut ici 202202, et en
`-std=c++20` le même code donne `no member named 'to' in namespace 'std::ranges'`. Il accepte le
type complet ou juste le modèle, et convertit au passage :

```cpp
auto a = pipe() | std::ranges::to<std::vector<int>>();
auto b = pipe() | std::ranges::to<std::vector>();          // CTAD : vector<int>
auto c = std::ranges::to<std::set<int>>(pipe());           // forme fonction
auto d = pipe() | std::ranges::to<std::vector<double>>();  // conversion elementaire
auto e = paires | std::ranges::to<std::map<int, int>>();
```

Sans lui — en C++20, ou sur une plateforme qui ne l'a pas — deux recours : le constructeur à deux
itérateurs, qui exige un `common_range`, et `ranges::copy` vers un `back_insert_iterator`, qui
n'exige rien.

```cpp
auto p = pipe(); std::vector<int> f;
std::vector<int> e(p.begin(), p.end());           // ok : filter sur vector est common
std::ranges::copy(pipe(), std::back_inserter(f)); // marche meme si non common
auto infini = std::views::iota(1) | std::views::take(4);
std::vector<int> g(infini.begin(), infini.end()); // NE COMPILE PAS : pas common_range
auto h = infini | std::views::common;             // le pont vers l'ancien monde
```

`ranges::to` donne en prime une capacité exacte : mesuré sur 1000 éléments, 1000 pour une source
`sized` et 500 pour un `filter` non `sized`, alors que `copy` vers un `back_inserter` finit à
**512** pour 500 éléments. Mais ce n'est pas un `reserve` gratuit — sur cette libc++, le
constructeur `from_range` appelle `ranges::distance` avant d'allouer, donc il **parcourt le
pipeline une fois de plus**. Mesuré sur `filter | transform | take(3)` : 7 appels au prédicat pour
un parcours nu, **13** avec `ranges::to`. La paresse ne survit pas à la matérialisation.

## Ce que ça coûte

### À la compilation, et dans les erreurs

Temps d'un `-c` seul, meilleur de neuf mesures : 15 ms pour `int main() {}`, **176 ms** pour
`<vector>` + `<algorithm>`, **212 ms** en ajoutant `<ranges>`, **187 ms** pour une boucle manuelle
`if` + carré, **281 ms** pour le pipeline `filter | transform` équivalent. L'en-tête seul coûte
36 ms, la ligne en pipeline 94 de plus que la boucle : rien sur un fichier, une minute sur mille.

Contre-intuitif, mais mesuré : les concepts **raccourcissent** les erreurs de mauvais usage, 114
lignes pour `std::sort` sur une `std::list` contre 22 pour `std::ranges::sort` sur la même. En
revanche les erreurs de **branchement** restent opaques : un prédicat à deux paramètres passé à
`views::filter` donne 25 lignes ouvertes par `invalid operands to binary expression`, avec pour
second opérande `__pipeable<std::__bind_back_t<...>>`. Court, mais illisible.

### À l'exécution

20 millions d'entiers, meilleur de sept passages :

| Code | `-O2` | `-O0` + ASan/UBSan |
|---|---|---|
| boucle `s += x*x` / `transform(x*x)` | 1.24 / 1.28 ms | 229 / 415 ms |
| `drop(1) \| take(n) \| transform` | 1.27 ms | 417 ms |
| boucle `if (x%2==0) s += x*x` / `filter \| transform` | 3.18 / **54.9 ms** | 346 / 2032 ms |
| boucle à six étapes / pipeline à six étages | 80.6 / 94.3 ms | 457 / 5529 ms |

À `-O2`, `transform`, `take` et `drop` sont **exactement gratuits** : ils gardent l'accès aléatoire
et le compilateur vectorise comme sur la boucle nue. `filter` ne l'est pas — il détruit l'accès
aléatoire, empêche la vectorisation, et coûte **17 fois** la boucle équivalente. Empilé six fois,
le pipeline reste à 1.17 fois la boucle, mais la référence était déjà scalaire. La taille suit :
`sizeof` de `filter | transform` vaut 24 octets, celui de `filter | transform | take | reverse` en
vaut **104**. Et le cadre des exercices est `-O0` avec ASan et UBSan, où rien n'est inliné : le
pipeline à six étages y est **12 fois** plus lent que la boucle. « Une vue est gratuite » est vrai
pour `transform` à `-O2`, faux pour `filter`, faux partout à `-O0`.

## À retenir

1. Une vue est **paresseuse** : zéro appel de prédicat à la construction, mesuré. Le travail arrive
   au parcours, et `take(n)` remonte jusqu'à la source pour l'arrêter.
2. Une vue **ne possède rien** : sur une lvalue c'est un `ref_view` de 8 octets, qui meurt avec sa
   source et se casse à la moindre réallocation — `heap-use-after-free` sous ASan.
3. `borrowed_range` garantit que les itérateurs survivent au range ; un `vector` prvalue ne l'est
   pas, d'où `std::ranges::dangling` rendu par `ranges::find`, refusé à la compilation.
4. La **projection** est le gain quotidien : `sort(eq, {}, &T::champ)` sépare quoi comparer de
   comment comparer, sans écrire une lambda de plus.
5. **Filtrer avant de transformer** : dans `transform | filter`, la transformation a tourné 9 fois
   pour 6 éléments, le prédicat déréférençant avant que la boucle ne redéréférence.
6. `filter_view` n'est ni `sized_range`, ni à accès aléatoire, ni itérable en `const` : `sort`
   dessus ne compile pas, et un paramètre `const auto&` non plus.
7. Sur cette libc++ : `ranges::to` est là (C++23), `enumerate`, `chunk`, `stride`, `slide`,
   `cartesian_product`, `as_const` et `concat` non — et `filter` coûte 17 fois la boucle à `-O2`.

**Exercices : `16_ranges`.**
