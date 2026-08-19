# 11 — Écrire son conteneur

## Pourquoi écrire son `vector` alors que celui de la bibliothèque est excellent

Il l'est, et le chapitre 05 disait de le prendre par défaut. Deux raisons de l'écrire quand même.
D'abord **comprendre ce qu'il fait** : `reserve`, `capacity`, l'invalidation, la copie surprise
quand un déplacement n'est pas `noexcept` — tout ce qui se lisait comme une règle à retenir devient
évident dès qu'on a écrit les vingt lignes qui la produisent. Ensuite parce que **les mêmes
questions reviennent au premier conteneur maison** — réserve d'entités, tampon circulaire, arbre,
petite table : aucun n'est un `vector`, tous doivent dire où vit la mémoire, quand les objets
naissent, ce que devient l'état si une construction lève, qui détruit quoi et dans quel ordre.

## Réserver n'est pas construire

C'est **tout** le sujet. Allouer de la mémoire et faire naître un objet sont deux opérations
distinctes, et un conteneur est l'objet qui les découple : il détient de la mémoire pour
`capacity()` éléments et n'y a fait naître que `size()` objets, le reste étant de l'espace brut où
il n'y a rien à lire ni à détruire. `new T(args)` fait les deux d'un coup — d'où quatre primitives.

| Étape | Outil | Ce que ça fait | Ce que ça ne fait pas |
|---|---|---|---|
| réserver | `::operator new(n)` | rend `n` octets bruts | ne construit aucun objet |
| construire | `new (p) T(args)` | fait naître un `T` en `p` | n'alloue rien |
| détruire | `p->~T()` | termine la vie de l'objet | ne rend pas la mémoire |
| rendre | `::operator delete(p)` | rend les octets | n'appelle aucun destructeur |

```cpp
void *brut = ::operator new(3 * sizeof(std::string));   // 72 octets sur libc++, 96 sur libstdc++
std::string *p = static_cast<std::string *>(brut);      // aucun objet ne vit encore ici
new (p + 0) std::string("un");                          // placement new : construit sur place
new (p + 1) std::string("deux");
p[1].~basic_string();                                   // destructeur explicite
p[0].~basic_string();                                   // la memoire, elle, est toujours a nous
::operator delete(brut);                                // et seulement maintenant, on la rend
```

`sizeof(std::string)` vaut 24 sur la libc++ d'Apple clang 21 et 32 sur la libstdc++ de GCC 16.1
(mesurés, arm64 macOS) : le nombre d'octets réservés dépend de l'implémentation, la séparation
n'en dépend pas. Deux détails. `::operator new(n)` ne garantit l'alignement que jusqu'à
`__STDCPP_DEFAULT_NEW_ALIGNMENT__`, **16** sur arm64 macOS comme sur x86-64 (mesuré) ; un `T`
sur-aligné exige la surcharge `std::align_val_t` de C++17, sans quoi l'adresse rendue n'est pas un
multiple de 64 (mesuré). Et la forme dimensionnée `::operator delete(p, n)`, C++14, aide
l'allocateur : `__cpp_sized_deallocation` n'est défini chez Apple clang 21 qu'avec
`-fsized-deallocation`, alors qu'il vaut `201309` chez GCC 16.1.

## Les outils qui écrivent ces boucles à ta place

| Outil | Norme | Rôle |
|---|---|---|
| `std::construct_at(p, args...)` | **C++20** | `new (p) T(args...)`, utilisable en `constexpr` |
| `std::destroy_at(p)` | **C++17** | `p->~T()` |
| `std::destroy_n(p, n)` | **C++17** | détruit `n` objets consécutifs |
| `std::uninitialized_copy_n(src, n, dst)` | **C++11** | copie-construit `n` objets dans du brut |
| `std::uninitialized_move_n(src, n, dst)` | **C++17** | déplace-construit `n` objets dans du brut |
| `std::allocator_traits<A>` | **C++11** | l'interface unique vers un allocateur |

Chaque appartenance ci-dessus a été vérifiée au compilateur, en recompilant le même fichier sous
`-std=c++11`, `c++14`, `c++17`, `c++20` et `c++23` : `std::construct_at` est bien un échec de
compilation en C++17, `std::destroy_n` un échec en C++14. Ces algorithmes ne sont pas seulement
plus courts, **ils sont sûrs face aux exceptions** : si la construction du quatrième objet lève,
les trois déjà construits sont détruits avant que l'exception ne reparte (mesuré).

`std::allocator_traits` est l'autre pièce : un conteneur n'appelle jamais son allocateur
directement, il passe par `allocator_traits<A>::allocate`, `::construct`, `::destroy`,
`::deallocate`, et le trait fournit un défaut pour tout ce que l'allocateur n'a pas défini — un
allocateur utile tient donc en trois membres, et C++23 y ajoute `allocate_at_least(n)`. Sous
`[[no_unique_address]]`, un allocateur sans état ne coûte rien : `sizeof(Tableau<int>)` reste
**24** mesuré, un pointeur et deux `std::size_t`.

## La croissance

### Pourquoi multiplicative, et avec quel facteur

| Cent `push_back` dans un tableau vide | Allocations | Transferts d'éléments |
|---|---|---|
| croissance `capacite + 1` | 100 | **4 950** |
| croissance `capacite * 2` | 8 | **127** |

Agrandir de un remet en cause tout le contenu à chaque insertion : une somme en n², donc un
`push_back` en O(n) et un remplissage en O(n²). Multiplier par `k` espace les réallocations
géométriquement, et le total des transferts reste borné par k/(k−1) fois n — moins de 200 pour
n = 100 et k = 2, 127 en pratique. C'est le `push_back` **amorti constant** que réclame la norme —
elle réclame ce coût, et n'impose **aucun** facteur. Le facteur retenu, lui, varie :

| Implémentation | Facteur | Comment il est établi ici |
|---|---|---|
| libc++ (Apple clang 21, arm64 macOS) | **2** | mesuré : capacités 1, 2, 4, 8, 16, 32, 64, 128 |
| libstdc++ (GCC 16.1, arm64 macOS) | **2** | mesuré : les mêmes capacités exactement |
| Microsoft STL | 1,5 | documenté par Microsoft, **non mesuré** ici |

### Ce que 1,5 permet que 2 ne permet pas

Le facteur 2 a un défaut : **la mémoire déjà rendue ne peut jamais servir à la réallocation
suivante**. Avec des blocs 1, 2, 4, …, la somme de ce qui a été rendu — le bloc courant étant
encore vivant pendant la copie — reste toujours sous la demande : il manque 2, 3, 5, 9, 17, 33, 65
octets, soit plus de la moitié de ce qu'on réclame, à chaque étape, pour toujours, et le tas avance
en terrain neuf. Avec 1,5 — capacités entières 1, 2, 3, 4, 6, 9, 13 — la somme des blocs rendus
finit par rattraper la demande : à la cinquième allocation on demande 6 et on a rendu 6, à la
sixième on demande 9 et on a rendu 10. Le seuil exact est le nombre d'or : tout facteur
strictement inférieur à φ ≈ 1,618 finit par permettre la réutilisation, tout facteur supérieur
jamais — vérifié par calcul sur les facteurs 1,4 à 2,0. **Avec deux grosses réserves** : ce
raisonnement suppose un tas contigu, une fusion parfaite des blocs libres et aucun en-tête par
bloc, c'est un modèle et pas une mesure ; et libc++ comme libstdc++ ont tranché pour 2.

## `push_back` contre `emplace_back`

`push_back` prend un objet, en deux surcharges — `const T &` et `T &&`. `emplace_back` prend les
**arguments du constructeur**, dans un pack, et les relaie par transfert parfait (chapitre 07) :

```cpp
template <typename... Args>
T &emplace_back(Args &&...args) {                          // renvoie une reference depuis C++17
    if (taille_ == capacite_) { reserver(capacite_ == 0 ? 1 : capacite_ * 2); }
    std::construct_at(donnees_ + taille_, std::forward<Args>(args)...);
    return donnees_[taille_++];
}
void push_back(const T &valeur) { emplace_back(valeur); }
void push_back(T &&valeur)      { emplace_back(std::move(valeur)); }
```

Les deux `push_back` se réduisent à un `emplace_back` : `std::forward<Args>` fait tout le travail,
en conservant la catégorie de valeur de chaque argument jusqu'au constructeur. Compteurs mesurés
sur `std::vector<Sonde>` après `reserve(16)`, donc sans réallocation :

| Appel | constructions | copies | déplacements |
|---|---|---|---|
| `push_back(s)` | 0 | 1 | 0 |
| `emplace_back(s)` | 0 | 1 | 0 |
| `push_back(Sonde{2})` | 1 | 0 | 1 |
| `emplace_back(2)` | **1** | 0 | **0** |
| `push_back(std::move(s))`, `emplace_back(std::move(s))` | 0 | 0 | 1 |

Une seule ligne diffère vraiment : `emplace_back(2)` construit l'unique `Sonde` directement dans le
tableau, là où `push_back(Sonde{2})` en construit une temporaire puis la déplace. **Le gain est là
et nulle part ailleurs : quand on a des arguments et pas encore d'objet.** Dès que l'objet existe,
les deux font la même chose — copie pour une lvalue, déplacement pour une rvalue — et
`emplace_back(std::move(s))` ne gagne rien. Une nuance à connaître, sur l'autre forme : c'est
`emplace_back(2)` qui contourne `explicit`, en construisant directement là où `push_back(2)` refuse
de compiler (chapitre 05).

## La garantie forte, appliquée à la croissance

Le chapitre 08 posait les quatre garanties ; la croissance est l'endroit où la **forte** — tout ou
rien — se gagne ou se perd, et l'ordre des opérations seul la décide.

```cpp
void reserver(std::size_t demande) {
    if (demande <= capacite_) { return; }
    Tableau neuf;                                     // 1. un conteneur temporaire, vide
    neuf.donnees_ = allouer(demande);                 // 2. allouer : si ca leve, rien n'a bouge
    neuf.capacite_ = demande;
    for (std::size_t i = 0; i < taille_; i++) {       // 3. transferer un a un
        std::construct_at(neuf.donnees_ + i, std::move_if_noexcept(donnees_[i]));
        neuf.taille_++;                               //    si ca leve, ~neuf detruit le deja fait
    }
    echanger(neuf);                                   // 4. echange noexcept : le point de bascule
}                                                     // 5. ~neuf detruit et libere l'ancien bloc
```

1. **L'allocation est en premier**, et c'est la seule étape qui échoue en pratique : tant qu'elle
   n'a pas réussi, l'objet n'a pas été touché.
2. **L'ancien bloc reste intact pendant tout le transfert.** Si la construction du 501ᵉ élément
   lève, on abandonne le bloc neuf — son destructeur détruit les 500 déjà construits — et l'objet
   reste exactement dans l'état d'avant.
3. **La bascule est un `swap` `noexcept`** : le seul instant où l'objet change d'état ne peut pas
   échouer. Forme générale de la garantie forte : préparer à côté, valider par l'infaillible.

Mesuré : la copie lève au 3ᵉ élément, `taille()`, `capacite()` et le pointeur restent inchangés.

### Pourquoi `std::move_if_noexcept` existe

Le point 2 tient à un fil : **on ne revient en arrière que si l'ancien bloc n'a pas été vidé**, et
déplacer les éléments les vide. Si le déplacement de `T` lève au milieu, les 500 premiers sont déjà
des coquilles et l'état d'avant est irrécupérable. `std::move_if_noexcept(x)` tranche ce dilemme :
il rend une rvalue si le constructeur de déplacement de `T` est `noexcept` **ou** si `T` n'est pas
copiable, une lvalue sinon — il déplace quand c'est sûr, copie quand le déplacement casserait la
garantie, déplace quand même faute d'alternative. Vérifié sur trois types, avec `std::vector` et
cent `push_back`, sur libc++ comme sur libstdc++ — 127 transferts dans les trois cas :

| Type | déplacement `noexcept` | copiable | Résultat mesuré |
|---|---|---|---|
| `Solide` | oui | oui | 127 déplacements, 0 copie |
| `Fragile` | non | oui | 0 déplacement, **127 copies** |
| `SansCopie` | non | **non** | 127 déplacements, garantie forte abandonnée |

La deuxième ligne est le piège chiffré du chapitre 08, ici dans son mécanisme : un `noexcept`
oublié, et toute croissance devient une avalanche de copies. La troisième montre que la garantie
forte n'est pas gratuite : quand elle est impossible, la norme la rétrograde en garantie de base —
c'est ce que spécifie `vector::push_back`, forte sauf si `T` n'est ni copiable ni
*nothrow-move-constructible*.

## Les détails qu'on oublie, et qui font les vrais bugs

### L'auto-affectation

L'affectation naïve — libérer l'ancien, puis copier le nouveau — marche parfaitement jusqu'au jour
où la source et la destination sont le même objet.

```cpp
Tableau &operator=(const Tableau &autre) {
    std::destroy_n(donnees_, taille_);
    ::operator delete(donnees_);                                     // le bloc de `autre` aussi
    T *neuf = allouer(autre.taille_);
    std::uninitialized_copy_n(autre.donnees_, autre.taille_, neuf);  // lecture d'un bloc libere
    ...
}
Tableau &operator=(Tableau autre) {   // copie et echange : le parametre vient de l'appelant
    echanger(autre);                  // echange noexcept, puis ~autre nettoie l'ancien contenu
    return *this;
}
```

Sous `-fsanitize=address`, la première version donne sur `t = alias;` un `heap-use-after-free`,
`READ of size 4`, pointant sur `uninitialized_copy_n`. Ça n'arrive jamais avec `t = t` écrit tel
quel — le compilateur le signale — mais tout le temps avec une référence, un `v[i] = v[j]`, un
alias derrière deux paramètres. La seconde règle le cas sans un seul test, son paramètre étant
construit avant l'entrée dans la fonction.

Variante que `std::vector` gère et qu'un conteneur maison rate presque toujours :
`v.push_back(v[0])`, légal et exigé par la norme — `std::vector` rend bien la bonne valeur (mesuré)
là où un `emplace_back` naïf, qui réalloue **avant** de construire, lit son `Args &&args` dans
l'ancien bloc déjà rendu : `heap-use-after-free` dans `construct_at`.

### L'invalidation, vue de l'intérieur

Le chapitre 05 énonçait la règle, le `reserver` ci-dessus l'explique : toute réallocation change
l'adresse de **tous** les éléments, donc pointeurs, références et itérateurs pris avant pointent
dans un bloc rendu à l'allocateur. Garder `int &premier = v[0];` à travers un `v.push_back(4);` qui
réalloue donne, mesuré, un `heap-use-after-free`, `READ of size 4`. Et la raison pour laquelle la
faute est vicieuse est visible dans le code : le `if (taille_ == capacite_)` ne déclenche rien tant
que la capacité suffit — le même code marche mille fois et casse la fois où le tableau grandit.

### `size` et `capacity`, et l'ordre du destructeur

`taille_` compte les objets **construits**, `capacite_` les places **payées**. Détruire `capacite_`
éléments appelle des destructeurs sur de la mémoire brute où aucun objet n'a jamais vécu ; lire
au-delà de `taille_` renvoie du bruit. C'est aussi la différence entre `reserve`, qui n'appelle
**aucun** constructeur, et `resize`, qui en appelle : mesuré, `reserver(5)` puis cinq
`emplace_back` donnent 5 constructions, 0 copie, 0 déplacement. Le destructeur fait deux choses
**dans cet ordre** :

```cpp
~Tableau() {
    std::destroy_n(donnees_, taille_);                    // 1. les objets
    ::operator delete(donnees_, capacite_ * sizeof(T),    // 2. puis la memoire
                      std::align_val_t{alignof(T)});
}
```

L'ordre inverse est indolore pour un `Tableau<int>` et catastrophique pour un
`Tableau<std::string>` : le destructeur de chaque chaîne lit son pointeur interne pour libérer son
tampon, et ce pointeur est dans le bloc déjà rendu. Mesuré : `heap-use-after-free`, `READ of
size 1`, dans `basic_string::__is_long`. Sauter la destruction ne déclenche rien du tout — chaque
chaîne fuit son tampon en silence, et sur macOS le détecteur de fuites d'AddressSanitizer n'est
**pas** disponible pour le dire (`detect_leaks is not supported on this platform`).

## Ce que la vraie `std::vector` fait de plus

**Les allocateurs.** Un vrai `vector` est paramétré par `A`, passe par `allocator_traits`, gère la
`rebind` vers un autre type et respecte les traits `propagate_on_container_copy_assignment`,
`_move_assignment` et `_swap`, qui disent si l'allocateur suit son conteneur lors d'une affectation
ou d'un échange : c'est ce qui fait marcher `std::pmr::vector`.

**Les itérateurs complets** : `iterator`, `const_iterator`, leurs versions inverses, `insert` et
`erase` à une position, les constructeurs par plage, les guides CTAD. Le `Tableau` s'en tire à bon
compte parce que `T *` est déjà un itérateur contigu — `begin()` et `end()` suffisent pour que
`std::ranges::sort` et le `for` marchent, et `contiguous_range<Tableau<int>>` est satisfait
(vérifié par `static_assert`). **Et les garanties d'exception partout**, pas seulement sur la
croissance : `insert` au milieu, `resize`, `assign` ont chacun la leur, spécifiée.

**Les optimisations pour les types triviaux**, et c'est là que l'écart se creuse. Pour un type
trivialement copiable, transférer N éléments est un `memcpy` : aucun constructeur, aucun
destructeur. libc++ va plus loin que la norme et marque `std::string`, `std::vector` et
`std::unique_ptr` comme **trivialement relogeables** — mesuré, son trait interne répond `true` pour
les trois —, si bien que déplacer puis détruire l'original y devient un `memcpy`. Vérifié dans
l'assembleur à `-O2` : faire grandir un `std::vector<std::string>` compile vers un unique `memcpy`
**sur libc++**, quand le `Tableau` appelle N fois `construct_at`. libstdc++, lui, déroule une
boucle de déplacements par élément — sa `std::string` se pointe elle-même en petite optimisation,
donc elle n'est pas trivialement relogeable.

**Et `std::vector<bool>`**, le seul conteneur standard que personne ne défend : une spécialisation
qui range un bit par élément — mesuré, `std::vector<bool>(1000).capacity()` vaut 1024 **bits** —
donc pas un conteneur d'objets `bool`. Conséquences mesurées : `v.data()` n'existe pas, `bool &r =
v[0];` ne compile pas, `v[0]` rend un mandataire de **16 octets** sur libc++, et `auto element =
v[0]; element = true;` modifie le vecteur. **Un `operator[]` qui rend un mandataire casse les
attentes de tout le monde**, `auto` en tête.

## À retenir

1. Réserver n'est pas construire : `::operator new` pour les octets, placement `new` pour l'objet,
   `p->~T()` pour la fin de vie, `::operator delete` pour rendre. Quatre opérations, jamais trois.
2. N'écris pas ces boucles : `construct_at` (C++20), `destroy_at`, `destroy_n` et
   `uninitialized_move_n` (C++17) sont déjà corrects face aux exceptions.
3. La croissance est multiplicative ou elle est quadratique : 127 transferts contre 4 950 sur cent
   `push_back`. Facteur 2 mesuré sur libc++ et sur libstdc++ ; la norme n'en impose aucun.
4. `emplace_back` ne gagne que sur des arguments sans objet : sur un objet déjà construit, il fait
   exactement ce que fait `push_back`.
5. Garantie forte : allouer d'abord, garder l'ancien bloc intact, basculer par un `swap`
   `noexcept`. Et `move_if_noexcept`, parce qu'un déplacement qui lève interdit le retour.
6. Copie et échange règle l'auto-affectation sans un seul test ; `v.push_back(v[0])`, lui, se règle
   en construisant avant de basculer.
7. Un destructeur de conteneur détruit `taille_` objets, **puis** libère `capacite_` places.
   L'ordre inverse est un `heap-use-after-free` sur tout type qui possède quelque chose.

**Exercices : `11_conteneur`.**
