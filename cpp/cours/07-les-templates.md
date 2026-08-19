# 07 — Les templates, les concepts, et ce que ça coûte

## Un template n'est pas du code : c'est une recette

```cpp
template <typename T>
T maximum(T a, T b) { return a < b ? b : a; }
```

Ce bloc ne produit **aucune instruction**. Tant que personne n'écrit `maximum(3, 4)`, il n'existe
pas de fonction : il existe une règle pour en fabriquer une. C'est l'appel qui déclenche
l'**instanciation**, et elle seule qui génère un vrai `int maximum(int, int)`. Le corps n'est
d'ailleurs complètement vérifié qu'à ce moment-là : **un template jamais instancié n'est jamais
vraiment vérifié**.

**Conséquence immédiate : la définition va dans l'en-tête.** Pour instancier, le compilateur a
besoin du corps, pas de la signature. Rangée dans un `.cpp`, elle est invisible aux autres unités
de traduction, qui n'instancient rien — et l'erreur ne tombe pas à la compilation mais à l'édition
de liens, `undefined symbol: int maximum<int>(int, int)`. Deux issues seulement : tout dans
l'en-tête, ou l'instanciation explicite des types voulus (voir « Ce que ça coûte »).

## Template de fonction et déduction

**Se déduit** : tout paramètre de template qui apparaît dans le type d'un argument. **Ne se déduit
pas** : le type de retour, et un paramètre non-type absent des arguments.

```cpp
template <typename Sortie, typename Entree>
Sortie convertir(Entree valeur) { return static_cast<Sortie>(valeur); }
auto x = convertir<double>(3);   // Sortie imposé, Entree déduit
auto y = convertir(3);           // erreur : Sortie introuvable
maximum(1, 2.0);                 // erreur : T = int d'un côté, T = double de l'autre
maximum<double>(1, 2.0);         // ok, on impose T
```

Les deux dernières lignes montrent la limite qui surprend le plus : **la déduction ne traverse
aucune conversion implicite**. Le compilateur ne cherche pas un type commun, il exige un accord
exact. Soit on annote, soit on prend deux paramètres de type distincts. Et comme les arguments
explicites se donnent de gauche à droite, **place en premier ce qui ne se déduit pas**. Dernier
détail : par valeur le type déduit **décroît** (`const` et références sautent, un tableau devient
un pointeur), alors que `const T &` le conserve exactement.

## Template de classe, paramètres non-type, CTAD

```cpp
template <typename T, std::size_t N>
struct Tampon {
    T donnees[N];
    constexpr std::size_t taille() const { return N; }
};
```

`N` est un **paramètre non-type** : une valeur connue à la compilation qui fait partie du type.
`Tampon<int, 4>` et `Tampon<int, 8>` sont deux types sans rapport : aucune conversion entre eux, et
**deux instanciations complètes** dans le binaire. C'est ce que fait `std::array<T, N>`, et
pourquoi sa taille est gratuite — elle n'est stockée nulle part, elle est dans le type.

Depuis C++17 les arguments du constructeur se déduisent aussi (CTAD), et un **guide de déduction**
comble les cas où il ne suffit pas :

```cpp
std::vector v{1, 2, 3};                             // std::vector<int>
std::pair p{1, 2.5};                                // std::pair<int, double>
template <typename T> struct Boite { T valeur; };
template <typename T> Boite(T) -> Boite<T>;         // le guide
Boite b{42};                                        // Boite<int>
```

CTAD sert à déclarer une variable, ou à écrire un `new Boite{42}` ; un paramètre de fonction, lui,
s'écrit toujours `Boite<int>`.

## `if constexpr` : la branche non prise n'est pas compilée

C'est **la** différence, et elle n'a rien à voir avec la vitesse.

```cpp
template <typename T>
void decrire(const T &valeur) {
    if (std::is_pointer_v<T>) { std::cout << *valeur; }   // erreur quand T = int
    else                      { std::cout << valeur; }
}

decrire(42);
```

`std::is_pointer_v<int>` vaut `false`, la branche est morte à l'exécution — et pourtant ça ne
compile pas. Un `if` ordinaire n'écarte rien : **les deux branches sont instanciées**, et `*valeur`
sur un `int` est une faute de type. Il suffit d'écrire `if constexpr (std::is_pointer_v<T>)` pour
que ça compile : la branche non prise devient une **instruction écartée**, qui n'est **pas
instanciée** pour ce `T`. Attention à la portée exacte de la promesse : ce qui, dans cette branche,
ne dépend pas de `T` est quand même analysé et diagnostiqué, et hors d'un template rien n'est
écarté du tout. `if constexpr` n'est pas un `#if` : il coupe l'instanciation, pas la compilation.
Au passage, ça remplace la plupart des cascades de surcharges et des acrobaties SFINAE : une
fonction, un symbole, une instanciation par type.

## Les concepts (C++20)

```cpp
template <std::integral T> T doubler(T valeur);                            // contrainte
template <typename T> requires std::floating_point<T> T doubler(T valeur); // clause requires
void trier(std::integral auto &a, std::integral auto &b);                  // Concept auto
```

Trois écritures, à une nuance près : la troisième déclare **deux** paramètres de template
indépendants, donc `a` et `b` peuvent y avoir des types différents. Et on écrit les siens :

```cpp
template <typename T>
concept Accumulable = requires (T a, T b) {
    { a + b } -> std::convertible_to<T>;
    { a += b } -> std::same_as<T &>;
};
```

Le bloc `requires` **n'exécute rien** : `a` et `b` sont fictifs, chaque ligne est une expression
dont on demande seulement qu'elle compile, et `{ expression } -> Concept` contraint **le type du
résultat** — `std::convertible_to<T>` est souple, `std::same_as<T &>` exact (ici pour interdire un
`+=` qui ne renverrait pas la référence). Une ligne `typename T::membre;` exigerait en plus un type
imbriqué, ce qui écarterait d'un coup `int` et `double` : à réserver aux concepts de conteneurs.

**Premier bénéfice : le message d'erreur.**

```cpp
template <typename T>
T somme(const std::vector<T> &v) { T total{}; for (auto &x : v) { total += x; } return total; }
somme(std::vector<Point>{});     // Point n'a pas de operator+=
```

Sans contrainte, l'erreur est **à l'intérieur** de la fonction, sur le `+=`, rapportée avec toute
la pile qui y mène — `In instantiation of ...`, `required from here`, puis tous les `operator+`
candidats de la bibliothèque standard : compte 60 à 100 lignes chez GCC pour ce cas minuscule. Avec
`template <Accumulable T> T somme(...)`, l'erreur tient en trois à dix lignes **au site d'appel** :
`Accumulable<Point>` n'est pas satisfait, parce que `a += b` est mal formée. Le concept déplace
l'erreur de « où ça a cassé » vers « ce que tu n'as pas fourni ».

**Second bénéfice : la sélection de surcharge.** Entre deux candidats viables, le plus contraint
gagne.

```cpp
template <typename T> concept Nombre = std::integral<T> || std::floating_point<T>;
template <typename T> concept Entier = std::integral<T>;
template <Nombre T> void traiter(T valeur);      // #1
template <Entier T> void traiter(T valeur);      // #2

traiter(3);      // #2 : Entier implique Nombre, donc #2 est plus contraint
traiter(3.0);    // #1 : seul candidat viable
```

Le compilateur ne compare pas des noms mais des contraintes atomiques : `Entier` **subsume**
`Nombre` parce que `std::integral<T>` implique `std::integral<T> || std::floating_point<T>`. Aucune
ambiguïté, aucune priorité à inventer. C'est ce qui remplace `std::enable_if_t`, où chaque
surcharge devait s'écrire en négatif, avec un recouvrement à corriger à chaque ajout.

## Variadiques et expressions de repli

```cpp
template <typename... Valeurs>
auto somme(Valeurs... valeurs) { return (valeurs + ... + 0); }
```

`Valeurs...` est un **pack** : zéro, un ou mille types, comptés par `sizeof...(valeurs)`. Une
expression de repli déroule un opérateur binaire sur tout le pack, sous quatre formes :

| Forme | Développement pour `a, b, c` |
|---|---|
| `(... op pack)` | unaire à gauche : `((a op b) op c)` |
| `(pack op ...)` | unaire à droite : `(a op (b op c))` |
| `(init op ... op pack)` | binaire à gauche : `(((init op a) op b) op c)` |
| `(pack op ... op init)` | binaire à droite : `(a op (b op (c op init)))` |

La forme binaire donne une valeur même sur un pack vide — c'est le rôle du `0` ci-dessus ; la forme
unaire n'accepte le pack vide que pour `&&` (`true`), `||` (`false`) et la virgule (`void`). Ainsi
`(std::cout << ... << valeurs);` affiche tout, `(... && predicat(valeurs));` teste tous les
éléments, `(traiter(valeurs), ...);` appelle dans l'ordre garanti. L'écriture historique passait,
elle, par une récursion :

```cpp
template <typename T> T somme(T valeur) { return valeur; }
template <typename T, typename... Reste>
auto somme(T valeur, Reste... reste) { return valeur + somme(reste...); }
```

Pour cinq arguments, le compilateur instancie **cinq fonctions** : celle à cinq paramètres, puis à
quatre, trois, deux, un — cinq résolutions de surcharge, cinq corps, cinq symboles ; sur un pack de
trente, trente instanciations. Le repli n'en produit **qu'une**, et donne le même code à
l'exécution : toute la différence est chez le compilateur.

## Le transfert parfait

```cpp
void prendre(std::string &&texte);        // rvalue reference : seulement des rvalues
template <typename T> void relayer(T &&valeur);   // référence universelle : tout
```

**`T&&` n'est une rvalue reference que si `T` n'est pas déduit ici.** Dans un template où `T` se
déduit à cet endroit précis, `T&&` est une **référence universelle** : elle accepte lvalues et
rvalues, `const` ou non, et `auto&&` aussi. En revanche `std::vector<T>&&` n'en est pas une, ni
`T&&` dans une méthode d'un template de classe. Le mécanisme est la déduction plus la **réduction
des références** :

| Argument | `T` déduit | `T&&` après réduction |
|---|---|---|
| lvalue `std::string` | `std::string&` | `std::string&` |
| rvalue `std::string` | `std::string` | `std::string&&` |

La règle de réduction tient en une ligne : seul `&& &&` donne `&&`, les trois autres combinaisons
donnent `&`. **Une référence lvalue l'emporte toujours** — c'est ce qui fait remonter « c'était une
lvalue » dans `T`. Le terme normatif est *forwarding reference* ; « référence universelle » vient
de Meyers et reste le plus répandu.

```cpp
template <typename T>
void ajouter_move(std::vector<verif::Sonde> &stock, T &&v) { stock.push_back(std::move(v)); }
template <typename T>
void ajouter_forward(std::vector<verif::Sonde> &stock, T &&v) { stock.push_back(std::forward<T>(v)); }
```

`std::move` casse **inconditionnellement** en rvalue ; `std::forward<T>` est un `static_cast<T&&>`
qui, par la réduction ci-dessus, ne casse que si `T` a été déduit sans référence. Après
`verif::Compteur::remettre_a_zero()` et un `stock.reserve(4)`, les compteurs le prouvent :

| Appel | copies | déplacements | état de `sonde` |
|---|---|---|---|
| `ajouter_forward(stock, sonde)` | 1 | 0 | intact |
| `ajouter_forward(stock, verif::Sonde{})` | 0 | 1 | — |
| `ajouter_move(stock, sonde)` | **0** | **1** | **vidé** |

La troisième ligne est le bug : on voulait copier, on a pillé la variable de l'appelant, et rien ne
l'a signalé — le code compile, le vecteur contient la bonne valeur, seule la sonde d'origine est
devenue vide. **La règle** : `std::forward<T>` sur un `T&&` déduit, `std::move` seulement sur ce
qu'on possède, et une seule fois. `emplace_back` n'est rien d'autre : un pack transféré.

## Ce que ça coûte

**Le temps de compilation.** Chaque instanciation est une compilation de plus : substitution,
résolution de surcharge, génération de code, optimisation. `std::vector<int>` et
`std::vector<std::string>` sont deux vecteurs compilés intégralement, et un en-tête template inclus
dans cent fichiers est instancié cent fois, l'éditeur de liens jetant les quatre-vingt-dix-neuf
copies en trop. Un projet générique passe souvent plus de temps à instancier qu'à compiler son
propre code.

**Le gonflement du binaire, et le cache.** Chaque couple `(T, N)` distinct produit un corps
distinct : `Tampon<int, 4>` et `Tampon<int, 8>` ne partagent pas une instruction, et sur du code
très générique le segment de code peut doubler. Le vrai prix n'est pas l'espace disque, c'est le
**cache d'instructions** : un L1i fait typiquement 32 Kio, et une boucle chaude dont le code a
triplé rate là où elle tenait. **Les messages d'erreur**, eux, remontent toute la chaîne
d'instanciation.

**À l'exécution : rien.** Un template instancié produit **le même code** que la version écrite à la
main pour ce type : pas d'indirection, pas de table virtuelle, pas de test de type. C'est ce qui le
sépare des génériques effacés et de l'héritage virtuel, et pourquoi `std::sort` bat `qsort`. **Le
prix des templates est payé par le compilateur, jamais par le programme.** Restent les remèdes,
quand la compilation devient le goulot :

```cpp
template class Tampon<int, 4>;                  // instanciation explicite, dans un seul .cpp
extern template class Tampon<int, 4>;           // dans l'en-tête : interdit aux autres unités
void trier_impl(std::span<int> valeurs);        // le cœur, non générique, compilé une fois
template <typename Conteneur>
void trier(Conteneur &conteneur) { trier_impl(std::span<int>(conteneur)); }
```

L'instanciation explicite compile le corps **une fois**, dans une unité choisie ; `extern template`
interdit aux autres de l'instancier, elles émettent un appel externe. Le troisième remède est le
meilleur : **factoriser la partie non générique dans une fonction non template**, et ne laisser au
template qu'une coquille mince — une instanciation minuscule par conteneur, un seul algorithme.

| Construction | À la compilation | À l'exécution |
|---|---|---|
| template de fonction, une instanciation | une compilation de plus | identique au code écrit à la main |
| template de classe, N valeurs non-type | N corps distincts | rien |
| repli sur N arguments contre récursion | une instanciation contre N | identique après inlining |

## À retenir

1. Un template est une recette : rien n'existe avant l'instanciation, donc **la définition va dans
   l'en-tête** — sinon l'erreur tombe à l'édition de liens.
2. La déduction ne traverse aucune conversion et ne devine ni le type de retour ni un paramètre
   non-type : annote, et place en premier ce qui ne se déduit pas.
3. `if constexpr` n'est pas un `if` rapide : **la branche non prise n'est pas compilée**.
4. Un concept sert deux fois : une erreur courte au site d'appel, et une surcharge choisie parce
   qu'elle est **plus contrainte**.
5. Une expression de repli remplace une récursion variadique : **une** instanciation au lieu de N.
6. `T&&` déduit est une référence universelle : `std::forward<T>` pour la relayer, `std::move`
   seulement sur ce qu'on possède — sinon on vide la variable de l'appelant en silence.
7. Le prix se paie à la compilation et dans le cache d'instructions ; à l'exécution il est
   exactement nul. Les remèdes : instanciation explicite, `extern template`, cœur non générique.

**Exercices : `07_templates`.**
