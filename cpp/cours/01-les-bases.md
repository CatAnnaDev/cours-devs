# 01 — Les bases

## `auto` déduit, et il jette

```cpp
const std::string &reference = obtenir();
auto copie = reference;        // std::string — une COPIE
```

`auto` applique les règles de déduction des templates : il **enlève** les références et le `const`
de premier niveau. Écrire `auto` là où on voulait une référence produit une copie silencieuse.

La conséquence pratique la plus coûteuse est la boucle :

```cpp
for (auto element : conteneur)         // copie à chaque tour
for (const auto &element : conteneur)  // aucune copie
for (auto &element : conteneur)        // aucune copie, modifiable
```

Sur un `std::vector<std::string>` de mille éléments, la première ligne fait mille allocations par
parcours. Sur un `vector<int>`, elle ne coûte rien.

**La règle** : `const auto &` par défaut. Passe à `auto` seulement pour les types minuscules
(entiers, pointeurs, itérateurs), et à `auto &` quand tu modifies.

Une exception à connaître : `for (auto element : vue)` sur une vue de ranges est correct — une vue
est un objet minuscule, et la copier ne copie pas les données.

## Références

```cpp
int valeur = 21;
int &lien = valeur;
lien *= 2;                     // valeur vaut 42
```

Une référence est **un autre nom** pour le même objet. Ce n'est pas un pointeur déguisé du point de
vue du langage :

| | Pointeur | Référence |
|---|---|---|
| peut être nul | oui | non |
| peut changer de cible | oui | **non** |
| doit être initialisé | non | **oui** |
| syntaxe d'accès | `*p` | `r` |

Ce qui donne la règle de choix : **une référence quand l'objet existe forcément, un pointeur quand
l'absence est possible**. Un paramètre `const T&` dit « tu dois me donner quelque chose » ; un
`const T*` dit « tu peux me donner nullptr ».

Et le piège, qui revient au chapitre 02 : une référence ne prolonge pas la vie de ce qu'elle
désigne. Renvoyer une référence vers une variable locale est aussi faux qu'en C.

## `const` et `constexpr`

```cpp
const int taille = 10;              // ne change pas après initialisation
constexpr int cote = 10;            // connu à la COMPILATION
constexpr int carre(int n) { return n * n; }

static_assert(carre(5) == 25);      // calculé par le compilateur
int resultat = carre(cote);         // ou à l'exécution, au choix
```

`const` est une **promesse** : je ne modifierai pas. `constexpr` est une **capacité** : ceci peut
être calculé pendant la compilation.

Une fonction `constexpr` n'est pas obligée de l'être : appelée avec des valeurs connues à la
compilation, elle est évaluée là ; sinon elle est compilée normalement. C'est donc gratuit à
ajouter, et ça permet des `static_assert` et des tailles de tableaux.

**Mets `const` partout où c'est vrai.** Sur les paramètres, sur les variables locales, sur les
méthodes qui ne modifient pas l'objet. Ça ne coûte rien, ça documente, et le compilateur le
vérifie.

## Le piège des accolades

```cpp
std::vector<int> a(3, 5);     // TROIS cases valant 5
std::vector<int> b{3, 5};     // DEUX cases : 3 et 5
```

L'initialisation par accolades préfère **toujours** le constructeur prenant une
`std::initializer_list`, quand il en existe un. Il gagne même contre un constructeur qui
correspondrait mieux.

C'est déroutant, parce que le conseil général est d'utiliser les accolades — et il est bon : elles
interdisent les conversions qui perdent de l'information.

```cpp
int a{3.5};    // erreur de compilation : conversion rétrécissante
int b(3.5);    // 3, silencieusement
```

**La règle pratique** : accolades par défaut, **parenthèses pour les conteneurs quand on donne une
taille**.

Et son corollaire, `{}` initialise à zéro :

```cpp
int compteur{};       // 0, garanti
int nonInitialise;    // indéterminé, comme en C
```

## La liaison structurée

```cpp
for (const auto &[nom, score] : scores) {
    total += score;
}

auto [quotient, reste] = diviser(17, 5);
```

Décompose une paire, un tuple ou une structure en variables nommées. Ça remplace `.first` et
`.second`, qui ne disent rien de ce qu'ils contiennent.

Le `const auto &` compte ici aussi : sans lui, chaque entrée de la map est copiée.

## `nullptr`

```cpp
void appeler(int);
void appeler(const char *);

appeler(0);         // appelle la version int
appeler(NULL);      // appelle la version int, sur la plupart des implémentations
appeler(nullptr);   // appelle la version pointeur
```

`NULL` est un `0` déguisé, donc un entier, donc il choisit la mauvaise surcharge. `nullptr` a son
propre type, `std::nullptr_t`, qui ne se convertit qu'en pointeur.

Utilise `nullptr` partout. `NULL` et `0` pour un pointeur n'ont plus aucune raison d'exister en
C++ moderne.

## Ce qui a changé, par version

Utile pour lire du code trouvé en ligne : la version explique souvent le style.

| Version | Ce qu'elle apporte, en pratique |
|---|---|
| C++11 | `auto`, lambdas, `nullptr`, sémantique de déplacement, `unique_ptr` |
| C++14 | `make_unique`, lambdas génériques |
| C++17 | élision de copie **garantie**, liaison structurée, `optional`, `string_view`, `if` avec initialisation |
| C++20 | concepts, ranges, `span`, `<format>`, `constexpr` presque partout |
| C++23 | `expected`, `print`, `mdspan`, `flat_map` |

Deux repères : **C++11 a changé le langage**, et **C++17 est le minimum raisonnable** aujourd'hui
— surtout pour l'élision de copie garantie, qui rend le retour par valeur enfin gratuit.

## À retenir

1. `auto` jette les références et le `const` : `const auto &` par défaut dans les boucles.
2. Une référence ne peut être ni nulle ni recibler ; un pointeur, si.
3. `const` est une promesse, `constexpr` une capacité — et `constexpr` est gratuit.
4. Les accolades préfèrent `initializer_list` : parenthèses pour donner une taille.
5. `{}` initialise à zéro ; sans initialisateur, c'est indéterminé.
6. `nullptr`, jamais `NULL`.

**Exercices : `01_bases`.**
