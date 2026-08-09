# 06 — Les algorithmes, les lambdas et les vues

## Écrire l'intention, pas la boucle

```cpp
auto trouve = std::find_if(v.begin(), v.end(), [](int n) { return n > 10; });
const int total = std::accumulate(v.begin(), v.end(), 0);
std::sort(v.begin(), v.end(), [](auto &a, auto &b) { return a.score > b.score; });
```

Trois raisons de préférer ça à une boucle écrite à la main, dans l'ordre d'importance :

1. **Ça dit ce que ça fait.** `std::find_if` se lit en une seconde ; une boucle avec un `break` et
   un drapeau demande une relecture.
2. **Les erreurs de bornes disparaissent.** Pas d'indice, pas de `<=` à la place de `<`.
3. **C'est au moins aussi rapide.** Les implémentations sont spécialisées et vectorisées.

Le prix : des messages d'erreur pénibles quand on se trompe de type — c'est le chapitre 00.

## Les itérateurs et `end()`

```cpp
auto trouve = std::find_if(v.begin(), v.end(), predicat);
if (trouve != v.end()) {
    utiliser(*trouve);
}
```

Un algorithme qui cherche renvoie `end()` quand il ne trouve pas. **`end()` ne désigne pas un
élément** : c'est la position juste après le dernier. Le déréférencer est un comportement
indéfini.

Comparer à `nullptr` ne veut rien dire : un itérateur n'est pas un pointeur, même si sur un
`vector` il en est souvent un.

## Les lambdas

```cpp
auto ajouter = [](int a, int b) { return a + b; };
```

Une lambda est un **objet** d'un type unique, généré par le compilateur, avec un `operator()`. Elle
n'est pas un pointeur de fonction, ce qui a deux conséquences : elle peut capturer un état, et elle
est **inlinable**, donc plus rapide qu'un pointeur de fonction passé à `qsort`.

### Les captures, et la seule qui soit dangereuse

```cpp
[]        // ne capture rien
[x]       // copie x
[&x]      // référence sur x
[=]       // copie tout ce qui est utilisé
[&]       // référence sur tout ce qui est utilisé
[this]    // capture le pointeur this
```

**Capturer par référence ne prolonge pas la vie de la variable.**

```cpp
std::function<int()> fabriquer(int valeur) {
    int local = valeur;
    return [&local] { return local * 2; };   // local est mort au retour
}
```

ASan appelle ça `stack-use-after-return`. C'est la faute des lambdas, et elle est facile à faire
dès qu'on passe une lambda à quelque chose qui la stocke — un rappel, une file de tâches, un
`std::function` membre.

**La règle** : capture par référence seulement si la lambda est **consommée immédiatement**
(passée à un algorithme sur la ligne d'à côté). Dès qu'elle est stockée ou renvoyée, capture par
valeur.

Et le piège de `[this]` : il capture le **pointeur**, pas l'objet. Une lambda membre stockée qui
survit à son objet référence un `this` mort. C++17 ajoute `[*this]` pour capturer une copie.

### `std::function` n'est pas gratuit

```cpp
auto lambda = [](int n) { return n * 2; };       // type unique, inlinable
std::function<int(int)> stockee = lambda;        // effacement de type
```

`std::function` peut stocker n'importe quel appelable de la bonne signature. Le prix : un appel
indirect qui ne s'inline pas, et **potentiellement une allocation** si l'objet capturé dépasse le
tampon interne.

Utilise-le quand tu as besoin de stocker des appelables hétérogènes. Pour un paramètre de fonction,
préfère un template :

```cpp
template <typename F>
void pour_chaque(const std::vector<int> &v, F fonction);
```

L'appel s'inline, il n'y a pas d'allocation, et c'est ce que font tous les algorithmes standard.

## Les ranges

```cpp
auto vue = nombres
         | std::views::filter([](int n) { return n % 2 == 0; })
         | std::views::transform([](int n) { return n * 10; });

for (int valeur : vue) { ... }
```

Deux gains par rapport aux algorithmes classiques :

**Plus de couples `begin()`/`end()`.** `std::ranges::sort(v)` au lieu de
`std::sort(v.begin(), v.end())`.

**Composition sans conteneur intermédiaire.** Filtrer puis transformer avec les algorithmes
classiques demande un vecteur temporaire pour le résultat intermédiaire. Une vue n'en crée aucun.

### Les vues sont paresseuses, et ne possèdent rien

**Rien n'est calculé tant qu'on ne parcourt pas.** Construire la vue ne coûte que quelques
pointeurs et les lambdas.

**Une vue ne copie pas le conteneur** — donc elle ne doit pas lui survivre. Renvoyer une vue sur un
conteneur local est le même bug qu'une référence pendante.

**Le prédicat peut être appelé plusieurs fois.** Sur une `filter_view`, `begin()` doit trouver le
premier élément retenu, et un parcours en deux temps réévalue. Un prédicat coûteux ou à effet de
bord n'a rien à faire dans une vue.

Pour matérialiser : `std::ranges::to<std::vector>()` en C++23, ou une boucle.

## `std::span`

```cpp
int somme(std::span<const int> valeurs);
```

Un pointeur plus une taille. Il remplace le couple `(const int*, size_t)` du C, avec trois
avantages : il connaît sa taille, il se parcourt comme un conteneur, et il accepte indifféremment
un `vector`, un `array`, un tableau brut ou une sous-vue.

```cpp
somme(vecteur);
somme(tableau);
somme(std::span(vecteur).subspan(1, 3));
```

Comme `string_view`, il **ne possède rien**. Même précaution.

C'est le bon type pour un paramètre « une suite d'éléments contigus que je vais lire » : plus
général que `const std::vector<T>&`, et sans forcer l'appelant à avoir un `vector`.

## Ce que ça coûte, en résumé

| Construction | Coût |
|---|---|
| algorithme standard avec lambda | identique à une boucle écrite à la main, souvent mieux |
| lambda passée à un template | inlinée, gratuite |
| lambda stockée dans `std::function` | appel indirect, parfois une allocation |
| vue de ranges | quelques pointeurs, rien de plus |
| `span` / `string_view` en paramètre | deux mots machine, aucune allocation |

L'abstraction du C++ moderne est réellement à coût nul **tant qu'on reste dans les templates**. Ce
qui coûte, c'est l'effacement de type : `std::function`, les fonctions virtuelles, les
`shared_ptr`. Ce sont des outils légitimes, à choisir en connaissance de cause.

## À retenir

1. Un algorithme nommé dit son intention et supprime les erreurs de bornes.
2. Comparer à `end()` avant de déréférencer.
3. Capture par référence uniquement si la lambda est consommée tout de suite.
4. `std::function` efface le type : appel indirect, parfois une allocation. Préfère un template.
5. Une vue est paresseuse, ne possède rien, et ne doit pas survivre à sa source.
6. `span` et `string_view` pour les paramètres en lecture.

**Exercices : `06_algos`.**

---

C'est la fin du premier bloc. Tu sais lire un mur d'erreurs de template, reconnaître une copie
inutile, faire confiance aux destructeurs, ne pas casser la génération des déplacements, éviter
l'invalidation, et écrire des algorithmes plutôt que des boucles.

La suite — templates et concepts, exceptions et leur prix, disposition mémoire, écrire son propre
`vector` — part de là.
