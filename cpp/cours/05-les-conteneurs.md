# 05 — Les conteneurs

## `std::vector` d'abord, et presque toujours

C'est le conteneur par défaut, et pas par facilité : ses éléments sont **contigus en mémoire**, ce
qui le rend beaucoup plus rapide que les structures chaînées sur du matériel moderne, même pour des
opérations où la théorie dit l'inverse.

Un parcours de `std::list` fait un saut de cache par élément. Un parcours de `vector` charge une
ligne de cache et sert huit à seize éléments. Sur des données réelles, l'écart est d'un facteur dix
ou plus, et il écrase les avantages algorithmiques des listes.

**La règle** : `vector` par défaut. Choisis autre chose quand tu peux **expliquer** pourquoi.

## Taille et capacité

```cpp
std::vector<int> v;
v.size();       // nombre d'éléments
v.capacity();   // nombre de places réservées
v.reserve(100); // réserve sans construire
```

Quand `size()` atteint `capacity()`, `push_back` :

1. alloue un nouveau bloc, plus grand (typiquement le double) ;
2. **déplace ou copie** tous les éléments existants ;
3. détruit les anciens ;
4. libère l'ancien bloc.

Sur cent `emplace_back` sans `reserve`, ça arrive sept fois, et cent quatre-vingt-dix-neuf
déplacements ont lieu au total. Avec `reserve(100)` : zéro.

**`reserve` dès que tu connais l'ordre de grandeur.** C'est la seule optimisation de conteneur qui
soit à la fois triviale et systématiquement payante.

Attention : `reserve` change la capacité, pas la taille. `resize` construit réellement des
éléments. Confondre les deux donne un vecteur de zéros là où on voulait un vecteur vide.

## L'invalidation

C'est **la** faute numéro un du C++ moderne.

```cpp
std::vector<int> nombres = {1, 2, 3};
int *premier = &nombres[0];
nombres.push_back(4);        // réallocation : le bloc a bougé
*premier;                    // heap-use-after-free
```

Après une réallocation, tout pointeur, référence ou itérateur vers l'ancien bloc est pendant.

Ce qui rend la faute vicieuse : **elle ne se produit que parfois**. Si la capacité suffisait, rien
ne bouge et le code marche. Il casse le jour où le vecteur grandit d'un élément de plus.

Le tableau des invalidations, à connaître :

| Conteneur | Ce qui invalide |
|---|---|
| `vector` | toute réallocation ; `erase` invalide à partir du point d'effacement |
| `deque` | insertion au milieu ; les références survivent aux insertions aux extrémités |
| `list` | rien, sauf l'élément effacé |
| `map` / `set` | rien, sauf l'élément effacé |
| `unordered_map` | le **rehash** invalide les itérateurs, pas les références |

Les remèdes, par ordre de robustesse :

1. **Ne garde pas de pointeur à travers une modification.** Ré-indexe après coup.
2. **Stocke des indices plutôt que des pointeurs.** Un indice survit à la réallocation.
3. **`reserve` d'avance**, quand la taille finale est connue.

## `push_back` contre `emplace_back`

```cpp
sondes.push_back(Sonde(3));   // construit une temporaire, puis la déplace
sondes.emplace_back(3);       // construit directement dans le conteneur
```

`emplace_back` transmet ses arguments au constructeur, sur place. Une construction, zéro copie,
zéro déplacement.

Deux nuances, pour ne pas en faire une religion :

**Avec un objet déjà construit, il n'y a pas de différence.** `push_back(std::move(x))` et
`emplace_back(std::move(x))` font tous deux un déplacement.

**`emplace_back` contourne les constructeurs `explicit`.** Il construit directement, donc il
accepte des conversions que `push_back` refuserait. C'est parfois pratique et parfois un bug.

## `std::string` et l'optimisation des petites chaînes

Une `std::string` courte — jusqu'à une quinzaine de caractères sur la plupart des implémentations —
range ses octets **dans l'objet lui-même**, sans aucune allocation.

C'est ce qui explique qu'un `std::string` fasse 24 ou 32 octets alors qu'un pointeur plus une taille
n'en demanderaient que 16 : le reste est le tampon interne.

Conséquence pratique : manipuler des chaînes courtes est bien moins coûteux qu'on ne le croit, et
tenter de les éviter à tout prix est souvent une optimisation prématurée. Ce qui coûte, c'est la
chaîne longue copiée en boucle.

Et son complément, `std::string_view` : un pointeur plus une longueur, **aucune allocation, aucune
copie**, qui accepte une `string`, un littéral, une sous-chaîne.

```cpp
void afficher(std::string_view texte);
```

Avec la précaution habituelle : **une vue ne possède rien**. La faire survivre à sa source est un
`heap-use-after-free`.

## `map` contre `unordered_map`

| | `std::map` | `std::unordered_map` |
|---|---|---|
| structure | arbre équilibré | table de hachage |
| recherche | log(n) | temps constant moyen |
| parcours | **dans l'ordre des clés** | ordre imprévisible |
| bornes (`lower_bound`) | oui | non |
| stabilité des références | totale | survit au rehash |
| mémoire par élément | élevée | élevée |

`unordered_map` est plus rapide en recherche, `map` garantit l'ordre. Choisis selon ce dont tu as
**besoin**, pas selon lequel est « plus rapide ».

Et un troisième larron qu'on oublie : pour de petites collections — jusqu'à quelques dizaines
d'éléments — un `std::vector<std::pair<K, V>>` parcouru linéairement bat souvent les deux, parce
qu'il est contigu. C++23 ajoute `std::flat_map`, qui est exactement ça, avec l'interface d'une map.

## Effacer

```cpp
for (std::size_t i = 0; i < v.size(); i++) {
    if (predicat(v[i])) {
        v.erase(v.begin() + i);     // saute l'élément suivant
    }
}
```

Effacer décale tout ce qui suit ; l'indice `i` désigne alors l'élément d'après, qui n'est jamais
testé. Deux éléments consécutifs à supprimer, et le second survit.

La bonne façon, depuis C++20 :

```cpp
std::erase_if(v, predicat);
```

Une seule passe, correcte, et l'intention est lisible. Avant C++20, c'était l'idiome
*erase-remove* :

```cpp
v.erase(std::remove_if(v.begin(), v.end(), predicat), v.end());
```

`std::remove_if` ne supprime rien — il déplace les survivants au début et renvoie la nouvelle fin.
C'est le `erase` qui tronque. Ce nom trompeur est la raison d'être de `std::erase_if`.

## À retenir

1. `vector` par défaut : la contiguïté bat la théorie sur du matériel réel.
2. `reserve` dès que l'ordre de grandeur est connu.
3. Toute réallocation invalide pointeurs, références et itérateurs. Stocke des indices.
4. `emplace_back` construit sur place ; avec un objet déjà fait, ça ne change rien.
5. Une `string` courte n'alloue pas ; `string_view` n'alloue jamais et ne possède rien.
6. `map` pour l'ordre, `unordered_map` pour la vitesse, un `vector` trié pour les petites tailles.
7. `std::erase_if`, jamais une boucle indexée qui efface.

**Exercices : `05_conteneurs`.**
