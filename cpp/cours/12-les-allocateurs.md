# 12 — Les allocateurs, et le tas qu'on ne voit pas

Le chapitre 09 a listé ce qui alloue sans le dire ; le chapitre 07 du cours de C a montré l'arène.
Ce chapitre répond à la question qu'ils laissent ouverte : **brancher une arène sous un
`std::vector`**, et le vérifier. Tout ce qui est dit « mesuré » vient de la machine de référence,
arm64 macOS, Apple clang 21, libc++ ; quand une autre cible diffère, les deux chiffres sont donnés.
La norme dit peu sur les tailles : **presque tous ces nombres sont des faits d'implémentation**.

## Rendre visible l'invisible

`operator new` et `operator delete` sont **remplaçables** : une version globale du programme est
appelée partout, y compris depuis le code déjà compilé de la bibliothèque — vérifié, une allocation
faite dans `libc++.dylib` par `new_delete_resource()` est bien comptée. C'est le seul crochet
garanti par la norme, et il a plus de formes qu'on ne croit : depuis C++14 la version
**dimensionnée**, qui reçoit la taille du bloc, et depuis C++17 la version **alignée**, qui reçoit
un `std::align_val_t` dès que l'alignement dépasse `__STDCPP_DEFAULT_NEW_ALIGNMENT__`, **16 sur les
deux ABI 64 bits**. Laquelle est appelée n'est pas portable : **Apple clang 21 n'appelle jamais la
dimensionnée**, sauf avec `-fsized-deallocation`, quand **GCC 16 l'appelle toujours**.

### Le compteur

```cpp
namespace suivi {
inline std::size_t allocations = 0, liberations = 0, octets = 0;
inline void remettre_a_zero() { allocations = 0; liberations = 0; octets = 0; }
inline void *compter(void *b, std::size_t t) {
    if (b == nullptr) { throw std::bad_alloc(); } allocations++; octets += t; return b;
}
inline void *prendre(std::size_t t) { return compter(std::malloc(t == 0 ? 1 : t), t); }
inline void *prendre(std::size_t t, std::size_t a) {
    // macOS : aligned_alloc exige a >= sizeof(void *) ET une taille multiple de a, verifie
    if (a < sizeof(void *)) { a = sizeof(void *); }
    std::size_t utile = t == 0 ? 1 : t;
    return compter(std::aligned_alloc(a, (utile + a - 1) / a * a), t);
}
inline void rendre(void *b) { if (b) { liberations++; std::free(b); } }
}
void *operator new(std::size_t t) { return suivi::prendre(t); }
void *operator new[](std::size_t t) { return suivi::prendre(t); }
void *operator new(std::size_t t, std::align_val_t a) { return suivi::prendre(t, (std::size_t)a); }
void *operator new[](std::size_t t, std::align_val_t a) { return suivi::prendre(t, (std::size_t)a); }
void operator delete(void *b) noexcept { suivi::rendre(b); }
void operator delete[](void *b) noexcept { suivi::rendre(b); }
void operator delete(void *b, std::size_t) noexcept { suivi::rendre(b); }           // dimensionnee
void operator delete[](void *b, std::size_t) noexcept { suivi::rendre(b); }
void operator delete(void *b, std::align_val_t) noexcept { suivi::rendre(b); }      // alignee
void operator delete[](void *b, std::align_val_t) noexcept { suivi::rendre(b); }
void operator delete(void *b, std::size_t, std::align_val_t) noexcept { suivi::rendre(b); }
void operator delete[](void *b, std::size_t, std::align_val_t) noexcept { suivi::rendre(b); }
// + les huit nothrow : quatre new rendant nullptr, quatre delete pour constructeur qui leve
```

### Ce que ça attrape, ce que ça rate

| Compté | Pas compté |
|---|---|
| `new`, `new[]`, tous les conteneurs standard | `std::malloc` direct, et tout le C sous le C++ |
| `std::string`, `std::function`, `std::any` | une classe qui définit son `operator new` membre |
| le code compilé **dans** `libc++.dylib` | `throw 42` : l'objet d'exception évite `operator new` |
| toute ressource `pmr` remontant à son amont | le placement `new` ; la pile ; le statique ; `mmap` |

Le compteur mesure des **appels**, pas la mémoire vivante : pour le pic il faut soustraire à chaque
libération, donc connaître la taille — d'où l'intérêt de la forme dimensionnée. Et passer par
`std::malloc` **ne désarme pas** ASan, qui l'intercepte en dessous.

## Le catalogue de ce qui alloue sans le dire

| Construction | libc++ arm64 | libc++ x86-64 | libstdc++ (GCC 16) |
|---|---|---|---|
| `sizeof(std::string)`, seuil sans allocation | 24, **22** car. | 24, **22** car. | 32, **15** car. |
| `std::string(23, 'x')` | 1 alloc de 25 | 1 alloc de 26 | 1 alloc de 24 |
| `sizeof(std::function<void()>)`, capture tenue | 32, **16** oct. | 48, **24** oct. | 32, **16** oct. |
| `sizeof(std::any)`, objet tenu sur place | 32, **24** oct. | 32, **24** oct. | 16, **8** oct. |
| nœud de `std::list<int>` / `std::map<int, int>` | 24 / 40 oct. | 24 / 40 oct. | 24 / 40 oct. |
| `std::deque<int>` avec **un** élément | 2 : 8 + 4096 | idem | 2 : 64 + 512 |
| `shared_ptr<int> p(new int)` puis `make_shared` | 2 : 4 + 32, puis **1** | idem | 2 : 4 + 24, puis **1** |

**`std::string`** : le seuil dépend du chemin. Construite d'un coup à 23 caractères, la chaîne
demande la place utile — 25 octets sur arm64, 26 sur x86-64 ; atteinte par `push_back` depuis vide,
elle en demande 48, puis 96, 192, 384. Même contenu, deux tailles de bloc.

**`std::function` et `std::any` ne promettent rien sur la taille** : la norme n'exige **aucun**
tampon interne et n'en fixe aucun seuil. Ce qu'elle garantit pour `std::function`, c'est que la
construction ne **lève pas** depuis un pointeur de fonction ou un `reference_wrapper` — ce qui y
interdit de fait l'allocation. Les seuils, eux, diffèrent entre deux ABI du **même** libc++. Pour
`std::any`, en revanche, la condition est bel et bien normative : la petite optimisation n'est
permise que si `is_nothrow_move_constructible_v<T>` est vrai, donc un type d'**un** octet dont le
déplacement peut lever alloue quand même, sur libc++ comme sur libstdc++. **La capture de lambda,
elle, n'alloue rien** : `sizeof` vaut 1 sans capture, 24 pour `[a, b, c]` avec un `int`, un `long`
et un `double`, et l'objet vit sur la pile. Ce qui alloue, c'est **l'effacement de type** — la
ranger dans un `std::function<int()>` coûte 32 octets.

**`shared_ptr`** fait 16 octets, son bloc de contrôle 32 sur libc++ et 24 sur libstdc++ : deux
compteurs atomiques, le pointeur d'objet, la table du destructeur de type effacé. `make_shared` les
fusionne, d'où **une** allocation et **une** ligne de cache au lieu de deux ; un deleteur qui
capture est stocké *dans* le bloc, et une lambda de 24 octets le fait passer de 32 à 56.

## L'allocateur à l'ancienne

Chaque conteneur prend l'allocateur en **dernier** paramètre gabarit — deuxième pour `vector`,
quatrième pour `map` ; le contrat tient en quatre lignes, et
`std::allocator_traits` en déduit `rebind_alloc`, qui transforme `AllocArene<int>` en
`AllocArene<Noeud>` pour les nœuds de `std::list`.

```cpp
template <typename T>
struct AllocArene {
    using value_type = T;
    Arene *source;
    AllocArene(Arene *s) : source(s) {}
    template <typename U> AllocArene(const AllocArene<U> &autre) : source(autre.source) {}
    T *allocate(std::size_t n) { return (T *)source->allouer(n * sizeof(T), alignof(T)); }
    void deallocate(T *, std::size_t) {}
    bool operator==(const AllocArene &autre) const { return source == autre.source; }
};
```

Le constructeur gabarit n'est pas décoratif : c'est lui que `rebind` utilise, et sans lui rien de
ce qui a des nœuds ne compile. Voilà maintenant le problème :

```cpp
void prend(const std::vector<int> &);
std::vector<int, AllocArene<int>> v{AllocArene<int>(&arene)};
prend(v);   // no known conversion from 'vector<[...], AllocArene<int>>' to
            // 'const vector<[...], (default) std::allocator<int>>'
```

**Deux `vector<int>` avec deux allocateurs différents sont deux types sans rapport.** Pas de
conversion, pas d'interface commune : toute fonction acceptant les deux doit devenir un gabarit,
donc migrer dans un en-tête, donc être recompilée par tous ses appelants. Et `sizeof(vector<int>)`
passe de 24 avec `std::allocator`, vide, à **32** avec un allocateur tenant un pointeur.

## `std::pmr`, la réponse de C++17

L'idée tient en une phrase : **remplacer un paramètre gabarit par un pointeur**.
`<memory_resource>` définit une base abstraite dont les trois opérations sont virtuelles, donc
choisies à l'exécution : `do_allocate(taille, alignement)`, `do_deallocate(bloc, taille,
alignement)` et `do_is_equal`, appelées par les publiques `allocate`, `deallocate` et `is_equal`.

Par-dessus, `std::pmr::polymorphic_allocator<T>` est un allocateur conforme ne contenant qu'un
`memory_resource *` — `sizeof` vaut 8 — et `std::pmr::vector<T>` n'est qu'un alias pour
`std::vector<T, std::pmr::polymorphic_allocator<T>>`. Donc **le type ne change plus** : tous les
`std::pmr::vector<int>` sont le même type quelle que soit la ressource derrière. Le prix est de 8
octets par conteneur — 32 au lieu de 24 — plus un appel virtuel par allocation.

```cpp
std::vector<int> v;                                   // 11 allocations, 8188 octets
std::byte tampon[8192];
std::pmr::monotonic_buffer_resource arene(tampon, sizeof(tampon),
                                          std::pmr::null_memory_resource());
std::pmr::vector<int> w(&arene);                      // 0 allocation
for (int i = 0; i < 1000; i++) { v.push_back(i); w.push_back(i); }
```

**Onze allocations contre zéro**, compteur à l'appui, sur un tampon qui ne survit pas à la
fonction. Le total de 8188 octets n'est pas un hasard : une ressource monotone **ne réutilise
jamais** ce qu'on lui rend, donc le tampon doit contenir la **somme** de tout ce qui a été demandé,
capacités intermédiaires comprises, et non le pic — ici 4 + 8 + … + 4096. Sans le troisième
argument un débordement partirait sur le tas en silence ; avec lui, il lève `std::bad_alloc`.

## Les ressources fournies

| Ressource | Ce qu'elle fait | `deallocate` | Quand la prendre |
|---|---|---|---|
| `monotonic_buffer_resource` | curseur qui avance, rend tout à sa destruction | ne fait **rien** | une image, une requête, un `parse` |
| `unsynchronized_pool_resource` | listes de blocs par classe de taille | recycle | beaucoup de petits objets, **un fil** |
| `synchronized_pool_resource` | idem, protégée par un verrou | recycle | idem, plusieurs fils |
| `new_delete_resource()` | appelle `::operator new` / `delete` | libère | l'amont par défaut |
| `null_memory_resource()` | lève `std::bad_alloc` à toute demande | ne fait rien | **garde-fou** : prouver qu'on n'alloue pas |

Les deux dernières sont rendues par une fonction, jamais construites, et `get_default_resource()`
vaut `new_delete_resource()` au démarrage. Mesures sur `std::pmr::list<int>` : à gauche dix mille
ajouts, à droite dix tours de mille suivis d'un `clear`.

| Ressource | 10 000 ajouts | 10 × (1000 ajouts + `clear`) |
|---|---|---|
| aucune (`std::list<int>`) | 10 000 allocs, 240 000 oct. | 10 000 allocs |
| `monotonic_buffer_resource` | **7** allocs, 260 320 oct. | 7 allocs, **260 320** oct. |
| `unsynchronized_pool_resource` | 21 allocs, 342 048 oct. | 11 allocs, **34 032** oct. |

La monotone demande 2080 octets puis double : sept blocs suffisent. Mais à droite elle a payé les
dix mille nœuds, n'en réutilisant aucun, quand le bassin n'a payé que le millier vivant. **Monotone
quand la mémoire meurt d'un coup, bassin quand elle tourne.**

## La propagation, le piège numéro un

```cpp
std::pmr::vector<std::string> v(&arene);
for (int i = 0; i < 20; i++) { v.emplace_back("une chaine de 45 caracteres, bien trop longue"); }
```

L'arène reçoit le tableau de vingt `std::string`. Les vingt **tampons de caractères**, eux, partent
sur le tas : 20 allocations, 960 octets, mesuré — libc++ arrondit à 48 octets pour une chaîne de
44 à 47 caractères. Un `std::string` ordinaire a son allocateur figé
dans son type et ignore celui de son conteneur. **Un conteneur `pmr` ne propage sa ressource qu'aux
éléments qui savent la recevoir** — d'où `std::pmr::string`.

| Élément | Construction | Allocations sur le tas |
|---|---|---|
| `std::string` | `push_back` ou `emplace_back` | **20** |
| `std::pmr::string` | `push_back(Chaine(...))` | **20** |
| `std::pmr::string` | `emplace_back(...)` | **0** |

La ligne du milieu est le piège dans le piège : `push_back` construit d'abord un `std::pmr::string`
**temporaire**, qui prend la ressource **par défaut**, donc le tas ; le déplacer dans le vecteur ne
sauve rien, deux ressources différentes obligeant à recopier les octets.

Deux règles de plus : pour `polymorphic_allocator`, les traits `propagate_on_container_*` et
`is_always_equal` valent **tous `false`**. D'où un piège : **copier** un `pmr::vector` donne un
vecteur sur la ressource **par défaut**, le **déplacer** la conserve. Pour copier dans l'arène :
`std::pmr::vector<int> copie(source, &arene);`.

## Écrire sa propre `memory_resource`

```cpp
class ArenePile : public std::pmr::memory_resource {
public:
    ArenePile(std::byte *debut, std::size_t taille, std::pmr::memory_resource *amont)
        : curseur_(debut), restant_(taille), amont_(amont) {}
private:
    void *do_allocate(std::size_t taille, std::size_t alignement) override {
        void *debut = curseur_; std::size_t place = restant_;
        // std::align avance `debut` et diminue `place` jusqu'a l'alignement voulu, ou rend
        // nullptr s'il n'y a plus la place : c'est le seul calcul correct.
        if (!std::align(alignement, taille, debut, place)) { return amont_->allocate(taille, alignement); }
        curseur_ = static_cast<std::byte *>(debut) + taille;
        restant_ = place - taille;
        return debut;
    }
    void do_deallocate(void *, std::size_t, std::size_t) override {}
    bool do_is_equal(const std::pmr::memory_resource &a) const noexcept override { return this == &a; }
    std::byte *curseur_; std::size_t restant_; std::pmr::memory_resource *amont_;
};   // et le vector, la map, la list de pmr s'en servent sans rien savoir
```

**`do_is_equal` n'est pas décoratif** : deux ressources sont égales si la mémoire de l'une peut
être rendue à l'autre — jamais pour une arène à curseur, et c'est ce `this == &a` qui force
l'affectation par déplacement à recopier élément par élément au lieu de voler le bloc. Attention à
ce qu'il ne fait **pas** : échanger deux conteneurs adossés à des ressources différentes n'est pas
refusé, c'est un comportement indéfini, et il est silencieux. `do_allocate`, lui, doit **lever**,
jamais rendre `nullptr`.

**Le piège de l'alignement.** La version naïve — `curseur_ += taille` — marche jusqu'au jour où un
`char` est suivi d'un `double` : le bloc rendu est impair, l'écriture est un comportement indéfini,
et sur arm64 elle ne plante même pas. UBSan l'attrape immédiatement :

> runtime error: assumption of 8 byte alignment for pointer of type 'void *' failed
> address is 1 aligned, misalignment offset is 1 bytes

## Le small buffer

Le motif inverse de l'arène : au lieu de sortir la mémoire du conteneur, on la met **dedans**.

```cpp
template <typename T, std::size_t N>
class TableauCourt {
    alignas(T) std::byte interne_[N * sizeof(T)];
    T *donnees_;                 // pointe sur interne_ tant qu'on tient dedans
    std::size_t taille_, capacite_;
};
```

Mesuré : `TableauCourt<int, 8>` fait 56 octets, `<int, 16>` 88, `<int, 64>` **280**, contre 24 pour
un `std::vector<int>`. Huit `push_back` : **zéro** allocation, contre quatre pour le `vector` qui
double ; le neuvième bascule sur le tas sans rien casser. Le compromis a trois faces. La **taille**
se paie partout, dans chaque structure qui en contient un et chaque ligne de cache. Le
**déplacement** cesse d'être trivial : plus de pointeur à voler, il faut déplacer les éléments un
par un dans le tampon. Et l'**adresse des éléments** change au basculement. `std::string` réussit
pourtant sans grossir, ses 22 caractères et son triplet pointeur/taille/capacité étant dans une
**union** — mais il faut écrire les deux représentations. Sinon, un `std::byte tampon[N]` et une
`monotonic_buffer_resource` donnent le même « rien sur le tas » en trois lignes.

## Ce que ça coûte

L'indirection de `pmr` est réelle. Même arène, même code, même boucle, cinquante millions
d'allocations de 32 octets : **2,0 ns** par appel avec un type concret, **5,5 ns** à travers un
`memory_resource *`. Trois nanosecondes et demie de plus : le prix de l'appel indirect et d'une
fonction non inlinable. Mais une paire `::operator new` / `::operator delete` coûte **8,5 ns** —
**l'arène virtuelle reste plus rapide que l'allocateur du système**. Sur `std::list<int>`, le nœud
coûte **3 ns** avec l'allocateur gabarit, **6 ns** avec `pmr`, **11,6 ns** avec `new`/`delete`.

| Opération, 200 000 `int`, 2000 tours | `std::vector<int>` | `std::pmr::vector<int>` |
|---|---|---|
| `resize` puis écriture indexée | 39,5 ms | 39,3 ms |
| `reserve` puis `push_back` | 117 ms | 353 ms |

La première ligne est la règle générale : **une fois le bloc obtenu, `pmr` ne coûte rien**, c'est
de la mémoire ordinaire. La seconde mérite d'être dite : il n'y a qu'**une** allocation par tour,
l'écart ne vient pas de l'appel virtuel, mais de la boucle `push_back`, que le compilateur optimise
moins bien quand le tampon sort d'un appel opaque — il ne peut plus prouver que les écritures ne
touchent pas les champs du vecteur. Le coût peut donc se propager dans le code qui suit.

Le rappel qui prime sur tout : **la première optimisation reste de ne pas allouer.** `reserve` fait
passer un `vector` de 5 allocations à 1 ; `std::string_view` en supprime des milliers ;
`make_shared` une sur deux. Une arène rend l'allocation quatre fois moins chère ; ne pas la faire
la rend gratuite, et supprime la ligne de cache, la fragmentation et la question de qui libère.

## À retenir

1. Remplace `operator new` pour compter — toutes les surcharges, sinon la moitié t'échappe.
2. Le compteur voit `new`, pas `malloc`, pas l'`operator new` membre d'une classe, pas la pile.
3. Les seuils de `string` (22), `function` (16) et `any` (24) ne sont normés nulle part : mesure.
4. L'allocateur gabarit fait partie du type : deux `vector<int>` différents ne se parlent pas.
5. `pmr` déplace le choix dans un pointeur : même type partout, 8 octets et un appel virtuel.
6. Monotone si la mémoire meurt d'un coup, bassin si elle tourne, `null` pour l'interdire.
7. Un conteneur `pmr` ne propage sa ressource qu'aux éléments `pmr` — et un temporaire construit
   avant l'appel a déjà alloué ailleurs, `emplace` ou pas.

**Exercices : `12_allocateurs`.**
