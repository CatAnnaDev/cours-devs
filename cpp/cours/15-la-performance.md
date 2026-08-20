# 15 — La performance en C++

Machine de référence : Apple M4, Apple clang 21 (`clang-2100.3.30.1`), libc++, arm64 macOS. Sauf
mention contraire les mesures sont prises à `-O2` **sans** sanitizers : un binaire ASan/UBSan à
`-O0` ne dit rien sur la vitesse du code en production. Chaque chiffre ici a été lancé.

## Le coût est invisible à la lecture

C'est la thèse de tout le cours, et elle mérite enfin d'être écrite. En C, la ligne dit ce qu'elle
fait : `f(t)` passe un pointeur, `malloc` alloue, un appel est un appel. En C++, elle ne dit plus
rien. `f(t)` peut copier un mégaoctet, `a = b` peut allouer, `x.f()` peut être deux chargements
dépendants et un branchement indirect, `for (auto x : v)` peut copier chaque élément. Rien ne
distingue le cas gratuit du cas cher. Donc le travail n'est **pas** d'optimiser : optimiser suppose
qu'on sait où va le temps, et en C++ on ne le sait pas. Le travail est de **voir**.

## Les quatre coûts cachés

### 1. La copie implicite

Un seul caractère les sépare. Mille chaînes de trente-huit à quarante caractères, sommées, le
vecteur passé d'abord par valeur puis par référence constante :

| Signature | Allocations | Octets | Temps par appel |
|---|---|---|---|
| `std::vector<std::string>` | 1001 | 71 200 | 13 317 ns |
| `const std::vector<std::string> &` | 0 | 0 | 265 ns |

Cinquante fois. Les 1001 allocations sont le tampon du vecteur plus une par chaîne : sur libc++
`sizeof(std::string)` vaut 24 et sa capacité SSO 22 caractères, mesurés, donc au-delà de 22 chaque
chaîne alloue. Le `&` absent ne se voit pas à la relecture, et c'est le problème.

### 2. L'allocation

Un `::operator new(32)` suivi de son `delete` prend **6,9 ns**, la même paire sur 256 octets **8,8
ns**, et 32 octets pris sur la pile **0,25 ns** : facteur 28. Et l'allocation se cache dans la
croissance : 100 000 `push_back` sur un `std::vector<int>` vide font **18 allocations** et touchent
1 048 572 octets, contre **1 allocation** et 400 000 octets avec `reserve`. La suite des capacités
mesurée sur cette libc++ est `1 2 4 8 16 32 64 128 ...` : facteur 2. La norme n'impose aucun
facteur, seulement que `push_back` soit amorti constant.

### 3. L'indirection virtuelle

```
__Z11via_virtuelPK1Bi:        __Z6directi:
    ldr  x8, [x0]                 add  w8, w0, w0, lsl #1
    ldr  x2, [x8, #16]            add  w0, w8, #1
    br   x2                       ret
```

À gauche deux chargements **dépendants** — le vptr, puis la case de la vtable — et un branchement
indirect ; à droite deux instructions arithmétiques. Mais le vrai prix n'est pas là : sur 100 000
éléments, la version directe tient **0,063 ns par élément** et la virtuelle **0,765 ns**, douze
fois plus. Ce n'est pas le coût du saut, c'est que la boucle virtuelle ne se vectorise plus :
`-Rpass=loop-vectorize` signale la directe *vectorized loop (vectorization width: 4, interleaved
count: 4)* et ne dit rien de la virtuelle. `sizeof` de la dérivée vaut 8 : un pointeur de vtable.

### 4. L'effacement de type

| `sizeof(std::function<int(int)>)` | capture 16 o | capture 17 o | la boucle | inlinée |
|---|---|---|---|---|
| 32 | 0 allocation | 1 allocation | 0,757 ns/élt | 0,063 ns/élt |

Le seuil de 16 octets est celui de **cette** libc++, trouvé en bissectant ; la norme ne garantit
aucune optimisation de petit objet, seulement que la construction peut allouer. Et le chiffre qui
compte : `std::function` et l'appel virtuel coûtent **la même chose**, 0,76 ns — ce n'est pas une
alternative légère au polymorphisme, c'est le même mécanisme.

## Rendre le coût visible

### Deux compteurs, pas un chronomètre

`verif::Sonde` compte constructions, copies, déplacements et destructions dans `verif::Compteur`.
C'est l'instrument le plus utile du cours parce qu'il ne mesure pas du temps mais des
**événements**, reproductibles au chiffre près là où un temps varie d'un lancement à l'autre : un
`std::vector<Sonde>` rempli de quatre éléments sans `reserve` donne 4 constructions et **7
déplacements** : les quatre temporaires poussés, plus les trois éléments redéplacés par les deux
réallocations que traverse une capacité passant de 1 à 2 puis à 4. Et quelques lignes rendent
visible presque toute allocation du programme, bibliothèque standard comprise :

```cpp
namespace compte { inline std::size_t allocations = 0, octets = 0; }

void *operator new(std::size_t n) {
    compte::allocations++;  compte::octets += n;
    void *p = std::malloc(n ? n : 1);      // new(0) doit rendre un pointeur unique valide
    if (!p) { throw std::bad_alloc{}; }
    return p;
}
void operator delete(void *p) noexcept { std::free(p); }
void operator delete(void *p, std::size_t) noexcept { std::free(p); }   // delete dimensionne
```

Tous les chiffres d'allocation du chapitre viennent de là. Deux limites à connaître : les
compteurs ne sont touchés que par `operator new`, donc la forme *dimensionnée* de `delete` est un
confort et non une nécessité — son comportement par défaut appelle la forme ordinaire. Et surtout,
`operator new(size_t, align_val_t)` est une fonction **distincte** : une allocation sur-alignée,
un `new` sur un type `alignas(64)` par exemple, passe à côté du compteur sans être vue.

### Les outils réels

Distinguons ce qui existe **où**. `perf` est un outil Linux : `command -v perf` ne trouve rien ici,
vérifié. Sont présents sur cette machine, tous vérifiés : `xctrace` (version 27.0, gabarits *Time
Profiler*, *Allocations*, *Leaks*, *CPU Counters*), `sample`, `leaks`, `heap`, `malloc_history`,
`vmmap`, `dtrace` ; `instruments` n'existe plus, `xctrace` l'a remplacée.

Lancé ici, `MallocStackLogging=1 leaks --atExit -- ./gains2` rend *191 nodes malloced for 30 KB, 0
leaks for 0 total leaked bytes*. Pour lire ce que le compilateur produit, pas besoin de godbolt.org
: `c++ -O2 -S -o - f.cpp` écrit l'assembleur sur la sortie standard, et `-Rpass=loop-vectorize`
comme `-Rpass-missed=inline` disent ce qui fut vectorisé ou pas inliné. C'est godbolt en local,
avec *ton* compilateur — ce qui compte, puisque godbolt n'a pas Apple clang.

## Mesurer sans mentir

### Le compilateur supprime ce qui ne sert pas

Une somme jamais lue n'est pas calculée, un objet jamais lu n'est pas construit — et depuis C++14
la norme autorise explicitement d'élider une paire `new`/`delete`.

| Mesuré à `-O2` | résultat jeté | sous barrière |
|---|---|---|
| somme sur 1 000 000 `int` | 0 à 42 ns | 94 375 à 113 083 ns |
| `std::string(4096, 'a')` | 42 ns | 541 à 709 ns |

Un facteur 2 400 entre « ma boucle est instantanée » et la vérité. La parade, lancée ici :

```cpp
template <typename T>
inline void ne_pas_optimiser(const T &valeur) {
    asm volatile("" : : "r,m"(valeur) : "memory");   // la valeur est lue, la memoire est modifiee
}
```

Le compilateur ne voit pas dans un `asm volatile` : il doit matérialiser la valeur et considérer
toute la mémoire comme modifiée. Extension gcc/clang, non standard — mais c'est aussi ce que fait
`benchmark::DoNotOptimize` de Google Benchmark.

### L'élision fausse le compteur de copies

Une Sonde compte ce que le compilateur a laissé, pas ce que le code demande.

| Fonction | Constructions | Copies | Déplacements |
|---|---|---|---|
| `return Sonde(3);` (prvalue) | 1 | 0 | 0 |
| `Sonde s(3); return s;` (NRVO) | 1 | 0 | 0 |
| `return b ? a : c;` | 2 | 1 | 0 |

Les deux premières lignes se ressemblent et ne relèvent pas du même mécanisme. Pour la prvalue,
**la norme depuis C++17 interdit la copie** : il n'y a jamais eu d'objet à copier. Pour la NRVO
elle *autorise* seulement l'élision, et clang la fait. La preuve tient dans un drapeau : avec
`-fno-elide-constructors`, le prvalue reste à **0 copie, 0 déplacement** — la norme gagne — tandis
que la NRVO passe à **1 déplacement** ; le troisième cas n'est élidable d'aucun côté. Si le
compteur t'étonne, ce drapeau sépare le garanti de l'offert.

### `constexpr` a déjà tout calculé

```cpp
constexpr long long somme(int n) { long long s = 0; for (int i = 0; i < n; ++i) s += i; return s; }
constexpr long long r = somme(200000);   // evalue par le compilateur, absent du binaire
```

Chronométrer `r` mesure zéro, à `-O2` comme à `-O0` : la valeur est dans le binaire. Le même appel
sur un argument venu de `argc` prend 41 ns à `-O2` et 103 250 ns à `-O0`. Un banc d'essai
`constexpr` mesure la vitesse de `printf` ; parade, faire venir l'entrée de l'exécution — `argc`,
un fichier, un générateur semé au lancement. Au passage, une limite de **cette** implémentation,
bissectée : l'évaluateur `constexpr` de clang plafonne par défaut à 2^20 = 1 048 576 pas.
`somme(1000000)` passe, `somme(1048576)` donne *constexpr evaluation hit maximum step limit*, et
`-fconstexpr-steps=100000000` la fait passer. La norme ne fixe aucune limite chiffrée : elle
autorise seulement l'implémentation à en avoir une.

## Ce que les abstractions coûtent vraiment

Même calcul pour tous — la somme de `x * 3 + 1` sur 100 000 `int`, 50 répétitions, `-O2`. Les
variantes passant par un objet sont choisies via `argc`, pour empêcher la dévirtualisation.

| Forme | ns/élément | Rapport | Vectorisée |
|---|---|---|---|
| boucle indexée à la main | 0,060 | x1,0 | oui, largeur 4 x 4 |
| `std::views::transform` | 0,060 | x1,0 | oui, largeur 4 x 4 |
| lambda passée à un template | 0,062 | x1,0 | oui, largeur 4 x 4 |
| `std::accumulate` + lambda | 0,111 | x1,8 | oui, largeur 4 x 4 |
| `std::function` | 0,764 | x12,7 | non |
| méthode virtuelle | 0,764 | x12,7 | non |

Trois lancements donnent les mêmes chiffres à 0,02 ns près, et ce tableau est le résumé du
chapitre. Les abstractions **statiques** — ranges, lambdas, templates — sont gratuites au chiffre
près : le compilateur les efface, et une vue de ranges vectorise aussi bien qu'une boucle à la
main. Les **dynamiques** coûtent un facteur 12, identiques entre elles, et la raison n'est pas le
saut mais la vectorisation perdue. `std::accumulate` est vectorisé lui aussi et reste 1,8 fois plus
lent : cela vient de l'ordonnancement produit ; on le mesure, on ne l'explique pas.

## Le coût de compilation

Un coût de performance, mais celui de la développeuse. Compilation d'un fichier contenant seulement
`#include <X>` et un `main` vide, avec le nombre de lignes prétraitées :

| aucun | `<cstdio>` | `<memory>` | `<string>` | `<vector>` | `<ranges>` | `<chrono>` |
|---|---|---|---|---|---|---|
| 8 lignes | 711 | 33 483 | 51 996 | 65 766 | 75 680 | 89 759 |
| 0,01 s | 0,02 s | 0,12 s | 0,17 s | 0,22 s | 0,25 s | 0,32 s |

Un `#include <chrono>` coûte trente fois un `main` vide. Sur une vraie unité du cours,
`solutions/06_algos/algo3.cpp` : 56 952 lignes prétraitées, 0,25 s ; l'arbre des 83 solutions
compile en 16,47 s en série. `-ftime-trace` dit où part ce temps :

```
Total ExecuteCompiler  238,7 ms   Total Source          205,6 ms   Total Backend     16,7 ms
Total Frontend         219,6 ms   Total InstantiateFunc  27,1 ms   Total Optimizer   14,0 ms
                                  Total InstantiateClass  21,7 ms
```

**92 % du temps est dans le frontend**, l'essentiel dans *Source*, la lecture des en-têtes ;
l'optimiseur pèse 6 %. Vérifié : la même unité prend 0,20 s à `-O0`, 0,21 s à `-O2`, 0,24 s avec
toutes les options du runner, sanitizers compris. **Passer de `-O0` à `-O2` ne coûte quasiment
rien** ; ce qui coûte, c'est ce qu'on inclut. Les templates, eux, coûtent à l'instanciation et pas
linéairement — une classe template instanciée à N types distincts, avec un `vector<string>` :

| Instanciations distinctes | 0 | 100 | 500 | 2000 |
|---|---|---|---|---|
| temps | 0,21 s | 0,34 s | 0,84 s | 5,32 s |
| coût marginal | — | 1,3 ms | 1,3 ms | 2,6 ms |

Ce qui réduit vraiment la facture, mesuré : un **en-tête précompilé**. Une unité incluant `<vector>
<string> <algorithm> <ranges> <memory>` passe de **0,28 s à 0,05 s**, 5,6 fois moins, pour un
`.pch` construit une fois en 0,33 s. Les remèdes folkloriques échouent : déclarer `class Truc;`
n'aide pas pour un template, qu'il faut définir pour instancier, et `-O0` non plus.

## Les optimisations qui rapportent en C++

Classées par gain **mesuré sur cette machine**. L'ordre surprend : c'est pour ça qu'on mesure.

**1. Déplacer plutôt que copier — facteur 78 517.** Un `std::vector<std::string>` de 10 000
éléments de 64 octets : la copie prend 128 127 ns et **10 001 allocations**, le déplacement **1,6
ns et zéro allocation** — un vol de trois pointeurs, quelle que soit la taille.

**2. `string_view` et `span` en paramètre — facteur 41.** Prendre `const std::string &` et appeler
avec un `const char *` construit une chaîne temporaire à chaque appel : 10,26 ns et **une
allocation par appel**, contre 0,25 ns et **zéro allocation** pour `std::string_view`. `sizeof`
vaut 16 pour `string_view` comme pour `span<const int>` : deux mots en registres, et un `span<const
int>` accepte un `vector<int>` comme un `int[8]`. Vues **non propriétaires** : une sur un
temporaire est un use-after-free.

**3. `reserve` — de 21 allocations à 1, et parfois bien plus.** Le gain en temps est modeste :
0,817 contre 0,750 ns par `push_back` sur `vector<int>` (8 %), 11,12 contre 10,54 ns sur
`vector<string>` (5 %), la réallocation étant amortie. Le vrai gain est ailleurs, propre au C++ :

| Croissance jusqu'à 100 000 éléments | Copies | Déplacements |
|---|---|---|
| déplacement `noexcept` | 0 | 131 071 |
| déplacement **non** `noexcept` | **131 071** | 0 |
| déplacement non `noexcept`, avec `reserve` | 0 | 0 |

`std::vector` ne déplace à la réallocation que si le déplacement est `noexcept` ; sinon il ne peut
garantir la forte sécurité face aux exceptions, et il **copie**. Un `noexcept` oublié transforme
131 071 déplacements en 131 071 copies, en silence ; `is_nothrow_move_constructible_v` répond avant
la mesure, et `reserve` supprime la question.

**4. `emplace_back` — un déplacement par élément, pas plus.** `reserve` fait, sur quatre `Sonde` :

| Appel | Constructions | Copies | Déplacements |
|---|---|---|---|
| `v.push_back(Sonde(i))` | 4 | 0 | 4 |
| `v.emplace_back(i)` | 4 | 0 | **0** |
| `v.push_back(s)` (lvalue) | 1 | **4** | 0 |

`emplace_back` construit **dans** le tampon, `push_back` construit un temporaire puis le déplace.
Sur `std::string` la différence mesurée est de 0,1 ns sur 10,5 : à faire par réflexe parce que
c'est gratuit et plus court, jamais à présenter comme une optimisation.

**5. Éviter `shared_ptr` par défaut.** Il n'est pas cher, il est *plus* cher que l'alternative :

| | `sizeof` | via `make_...` | via `ptr(new int(1))` | copie du pointeur |
|---|---|---|---|---|
| `unique_ptr<int>` | 8 | 1 alloc, 4 o | 1 alloc | non copiable |
| `shared_ptr<int>` | 16 | 1 alloc, **32 o** | **2 allocs**, 36 o | **3,39 ns** |

Les 32 octets de `make_shared` sont l'objet plus le bloc de contrôle en une allocation ; écrit
`std::shared_ptr<int> p(new int(1))`, on en paie **deux**. La dernière colonne n'a pas de sens pour
`unique_ptr` : son constructeur de copie est supprimé, c'est tout l'intérêt du type. Les 3,39 ns
sont donc le prix d'une copie de `shared_ptr` et de sa destruction — un incrément atomique puis un
décrément, tous deux émis en ligne, mais qui sérialisent les caches entre cœurs dès qu'il y a
foule. `shared_ptr`
répond à une question précise — qui, parmi plusieurs, libère en dernier ? — et si elle est tranchée
à l'écriture, `unique_ptr` fait le travail pour deux fois moins de taille.

## À retenir

1. En C++ le coût n'est pas dans la syntaxe : un `&` manquant vaut ici un facteur 50 et 1001
   allocations. Le travail n'est pas d'optimiser, c'est de **voir** d'abord.
2. Les quatre coûts cachés : copie implicite (x50), allocation (6,9 ns contre 0,25 ns sur la pile),
   appel virtuel (x12), effacement de type (x12, plus une allocation au-delà de 16 o de capture).
3. Deux instruments suffisent, et aucun n'est un chronomètre : `verif::Sonde` compte les copies,
   une surcharge de `::operator new` les allocations — des **événements**, reproductibles au
   chiffre près. `perf` n'existe pas ici ; `xctrace`, `leaks` et `c++ -S` oui.
4. Un chronomètre en C++ ment de trois façons : le compilateur supprime l'objet inutilisé (facteur
   2 400 mesuré), l'élision efface des copies demandées, `constexpr` a tout calculé avant le
   lancement. Parades : barrière `asm volatile`, `-fno-elide-constructors`, entrée d'exécution.
5. Les abstractions statiques sont gratuites au chiffre près : ranges, lambdas et templates
   tiennent 0,060 ns/élément comme la boucle à la main et vectorisent pareil. Les dynamiques
   coûtent x12, et pas pour le saut : pour la vectorisation perdue.
6. Le temps de compilation est à 92 % dans le frontend, presque tout en lecture d'en-têtes : `-O0`
   ne rattrape rien (0,20 s contre 0,21 s), un en-tête précompilé oui (0,28 s à 0,05 s), et les
   instanciations de templates coûtent de façon superlinéaire.
7. Classées par gain mesuré : déplacer plutôt que copier (x78 517), `string_view` et `span` en
   paramètre (x41), `reserve` — surtout parce qu'un déplacement non `noexcept` fait copier 131 071
   fois —, `emplace_back` par réflexe, et `unique_ptr` par défaut.

**Exercices : `15_perf`.**
