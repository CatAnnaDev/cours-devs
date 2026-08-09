# 02 — Les valeurs : copier, déplacer, ne rien faire

Le chapitre le plus rentable du cours. Presque toutes les différences de performance entre du C++
naïf et du C++ correct tiennent ici.

## Trois façons de passer un argument

```cpp
void par_valeur(std::string texte);         // copie (ou déplacement)
void par_reference(const std::string &t);   // rien du tout
void par_reference_modifiable(std::string &t);
```

| Ce que fait la fonction | Signature |
|---|---|
| lire seulement | `const T &` |
| modifier l'argument de l'appelant | `T &` |
| **stocker** une copie | `T` par valeur, puis `std::move` |
| lire un type minuscule (`int`, pointeur, `string_view`) | `T` par valeur |

La deuxième ligne du tableau est la plus importante et la plus ignorée : **prendre par valeur ce
qu'on va seulement lire est une copie complète, gratuite en syntaxe et chère à l'exécution**.

Le seuil « minuscule » se situe autour de deux mots machine. Un `int`, un `double`, un pointeur, un
`string_view` : par valeur, c'est plus rapide qu'une indirection. Un `std::string`, un `vector`,
une structure de trois champs : par référence constante.

## Copier contre déplacer

```cpp
std::string source = "un très long texte ...";

std::string copie = source;              // duplique tout le contenu
std::string deplacee = std::move(source); // vole le pointeur : trois mots
```

Une copie duplique la ressource. Un déplacement la **vole**, en laissant la source dans un état
valide mais vide.

C'est pour ça qu'un `std::vector` de dix mille éléments se déplace en trois assignations de
pointeurs, quel que soit son contenu, alors que le copier alloue et construit dix mille objets.

## `std::move` ne déplace rien

C'est le nom le plus trompeur de la bibliothèque standard.

```cpp
template <typename T>
constexpr std::remove_reference_t<T> &&move(T &&valeur) noexcept {
    return static_cast<std::remove_reference_t<T> &&>(valeur);
}
```

C'est un `static_cast`. Il ne s'exécute pas, il ne bouge aucun octet. Il change le **type** de
l'expression, ce qui fait choisir au compilateur la surcharge de déplacement plutôt que celle de
copie.

Trois conséquences :

**Sans constructeur de déplacement, `std::move` copie.** Silencieusement. C'est tout le sujet du
chapitre 04.

**Après un `std::move`, l'objet source est valide mais indéterminé.** On peut lui réaffecter une
valeur, on peut le détruire, on ne doit pas lire son contenu en supposant quoi que ce soit.

**`std::move` sur un `const` ne fait rien.** Un `const std::string&` déplacé sélectionnera quand
même la copie, parce qu'on ne peut pas piller un objet constant. Le bug est silencieux ; la Sonde
le révèle.

## L'élision, et pourquoi `return std::move(x)` est une faute

```cpp
std::string fabriquer() {
    std::string local = "abc";
    return local;              // zéro copie, zéro déplacement
}

std::string resultat = fabriquer();
```

Depuis C++17, le compilateur **construit l'objet directement à l'emplacement final**. Il n'y a pas
de copie à supprimer : il n'y en a jamais eu.

```cpp
    return std::move(local);   // FORCE un déplacement inutile
```

En transformant l'expression en rvalue, on empêche l'élision et on ajoute un déplacement. C'est
l'anti-optimisation la plus répandue du C++, et les compilateurs la signalent
(`-Wpessimizing-move`).

**La règle** : `return local;`, toujours. `std::move` au retour ne sert que dans un cas rare —
renvoyer un membre d'un objet qu'on est en train de détruire.

## Le paramètre puits

Quand une fonction **stocke** son argument, la bonne recette est de le prendre par valeur puis de
le déplacer :

```cpp
void ranger(std::string texte) {
    stock_.push_back(std::move(texte));
}
```

| Appel | Ce qui se passe |
|---|---|
| `ranger(chaine)` | une copie (à l'entrée) + un déplacement |
| `ranger(std::move(chaine))` | un déplacement + un déplacement |
| `ranger("littéral")` | une construction + un déplacement |

Une copie dans le pire cas, aucune dans le meilleur, et **une seule signature**. L'alternative —
deux surcharges, `const T&` et `T&&` — économise un déplacement au prix d'un doublement du code, et
explose combinatoirement dès qu'il y a deux paramètres.

Le `std::move` **dans** la fonction n'est pas optionnel : sans lui, le paramètre est une lvalue et
`push_back` copie.

## Les références qui survivent à leur cible

```cpp
const std::string &construire(int score) {
    std::string texte = "score:" + std::to_string(score);
    return texte;              // texte n'existe plus
}
```

Exactement le même bug qu'en C avec un pointeur, avec une syntaxe qui ne le crie pas. Le
compilateur avertit, ASan le détecte (`stack-use-after-return`).

Le cas plus vicieux, où personne n'avertit :

```cpp
const std::string &nom = obtenir_objet().nom();   // objet détruit à la fin de la ligne
```

Une référence à un membre d'un temporaire ne prolonge **pas** la vie du temporaire. Seule une
référence liée **directement** au temporaire le fait :

```cpp
const std::string &texte = std::string("abc");    // prolongé jusqu'à la fin de la portée
```

Cette règle a assez d'exceptions pour qu'on ne s'y fie pas. En pratique : **quand tu doutes,
prends par valeur**. Le compilateur élidera la plupart du temps.

## Ce que ça coûte, en ordres de grandeur

Pour un `std::string` de cent caractères :

| Opération | Coût approximatif |
|---|---|
| passer par `const &` | rien |
| déplacer | trois assignations |
| copier | une allocation + cent octets copiés |
| construire depuis un littéral | une allocation + copie |

L'allocation domine tout le reste. C'est pour ça que « éviter les copies » veut presque toujours
dire « éviter les allocations », et pourquoi `std::string_view` existe : une vue non propriétaire
sur des caractères, deux mots machine, aucune allocation.

```cpp
void afficher(std::string_view texte);   // accepte string, littéral, sous-chaîne, sans copier
```

Avec la même précaution que pour les références : **une vue ne possède rien**, et ne doit pas
survivre à ce qu'elle regarde.

## À retenir

1. `const T &` pour lire, `T` par valeur puis `std::move` pour stocker, `T` par valeur pour les
   types minuscules.
2. `std::move` est un cast : il ne déplace rien, il choisit une surcharge.
3. Sans constructeur de déplacement, `std::move` copie en silence.
4. `return local;` — jamais `return std::move(local);`.
5. Une référence ou une vue ne prolonge pas la vie de ce qu'elle regarde.
6. Le coût réel d'une copie, c'est l'allocation.

**Exercices : `02_valeurs`.**
