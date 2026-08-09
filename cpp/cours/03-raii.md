# 03 — RAII

## L'idée, en une phrase

**Une ressource est acquise par un constructeur et libérée par un destructeur.** Le nom complet
est « Resource Acquisition Is Initialization », et il est mauvais : ce qui compte n'est pas
l'acquisition, c'est que **la libération est garantie par le langage**.

```cpp
{
    std::vector<int> nombres(1000);
    if (condition) {
        return;              // libéré
    }
    peut_lever();            // libéré même si ça lève
}                            // libéré
```

Le destructeur d'un objet local est appelé à la sortie de la portée, **quel que soit le chemin**.
C'est la seule garantie dont on a besoin, et elle rend impossible toute une classe de bugs du C.

Compare avec l'équivalent C :

```c
int *nombres = malloc(1000 * sizeof(int));
if (condition) {
    free(nombres);          // à ne pas oublier
    return -1;              // ...sur chaque chemin de sortie
}
```

Chaque `return` ajouté plus tard par quelqu'un d'autre est une fuite potentielle. C'est le
mécanisme de la moitié des fuites en C.

## Ce n'est pas réservé à la mémoire

Tout ce qui s'ouvre et se ferme en profite :

| Ressource | Type RAII |
|---|---|
| mémoire | `unique_ptr`, `vector`, `string` |
| fichier | `std::fstream` |
| verrou | `std::lock_guard`, `std::scoped_lock` |
| socket, connexion, contexte graphique | ta propre classe |
| un état à restaurer | un garde de portée |

Le garde de portée, en huit lignes :

```cpp
class Garde {
public:
    explicit Garde(std::function<void()> action) : action_(std::move(action)) {}
    ~Garde() { action_(); }

    Garde(const Garde &) = delete;
    Garde &operator=(const Garde &) = delete;

private:
    std::function<void()> action_;
};
```

`Garde garde([&] { restaurer(); });` et la restauration est garantie. Note les deux `= delete` :
un garde copié exécuterait son action deux fois.

## `unique_ptr` : propriété exclusive

```cpp
auto sonde = std::make_unique<Sonde>(42);
```

Un `unique_ptr` possède le bloc, le libère à sa destruction, et **ne se copie pas**. Le transfert
se fait explicitement :

```cpp
auto second = std::move(premier);    // premier vaut maintenant nullptr
auto copie = premier;                // erreur de compilation
```

Cette interdiction est le point important : elle rend la propriété **visible dans le type**. Une
fonction qui prend un `unique_ptr<T>` par valeur annonce « je prends la propriété ». Une fonction
qui prend `T*` ou `T&` annonce « je regarde, je ne possède pas ».

**Ce que ça coûte** : rien. `unique_ptr` fait la taille d'un pointeur, et le compilateur produit
le même code qu'un `new`/`delete` bien placé. C'est de l'abstraction à coût nul, au sens strict.

**`make_unique` plutôt que `new`** : une seule mention du type, exception-safe, et pas de `new` nu
dans le code.

## `shared_ptr` : propriété partagée, et son prix

```cpp
auto partage = std::make_shared<Sonde>(7);
auto second = partage;               // use_count() vaut 2
```

Le bloc est libéré quand le dernier propriétaire disparaît. C'est pratique, et ce n'est pas
gratuit :

| Coût | Pourquoi |
|---|---|
| deux fois la taille d'un pointeur | l'objet et le bloc de contrôle |
| une incrémentation **atomique** par copie | pour rester correct entre threads |
| une allocation de plus | sauf avec `make_shared`, qui fusionne les deux |
| destruction non déterministe | c'est le dernier qui ferme, et on ne sait pas qui |

L'incrémentation atomique est le point à retenir : elle coûte bien plus qu'une incrémentation
normale, et elle la paie **même dans un programme mono-thread**.

D'où deux règles :

**Passe un `shared_ptr` par `const &`** quand la fonction ne stocke rien. Le passer par valeur
incrémente et décrémente le compteur pour rien.

**`shared_ptr` est un dernier recours, pas un défaut.** La question à se poser est : « qui possède
vraiment cet objet ? » Neuf fois sur dix, la réponse est « un seul propriétaire clair », et c'est
`unique_ptr` plus des références nues.

`weak_ptr` complète le tableau : il observe sans posséder, et sert à casser les cycles. Deux objets
qui se pointent mutuellement avec des `shared_ptr` ne sont **jamais** libérés — le comptage de
références ne détecte pas les cycles.

## L'ordre de destruction

**Inverse de l'ordre de construction.** C'est garanti, et c'est ce qui rend les dépendances
correctes : si `b` a été construit après `a` et s'appuie dessus, `b` est détruit en premier.

```cpp
{
    Trace a('a');
    Trace b('b');
    Trace c('c');
}                   // détruits : c, b, a
```

Vaut aussi pour les membres d'une classe : ils sont construits dans l'ordre de **déclaration** —
pas dans l'ordre de la liste d'initialisation, ce qui est une source d'avertissements — et détruits
dans l'ordre inverse.

## La règle qui découle de tout ça

> Ne fais jamais un `new` ou un `delete` nu.

Pas parce que c'est interdit, mais parce que **chaque `new` nu est une obligation à tenir sur tous
les chemins de sortie**, y compris ceux que quelqu'un ajoutera dans six mois, y compris ceux que
les exceptions créent sans qu'on les voie.

Ce que tu écris à la place :

| Au lieu de | Écris |
|---|---|
| `new T` | `std::make_unique<T>()` |
| `new T[n]` | `std::vector<T>(n)` |
| `malloc` / `free` | un conteneur |
| un `delete` sur chaque chemin | rien du tout |

Et si tu écris une classe qui possède vraiment une ressource brute — parce qu'elle vient d'une
bibliothèque C, par exemple — c'est le chapitre suivant.

## À retenir

1. Le destructeur d'un objet local passe sur **tous** les chemins de sortie.
2. RAII ne concerne pas que la mémoire : verrous, fichiers, états à restaurer.
3. `unique_ptr` coûte zéro et rend la propriété visible dans le type.
4. `shared_ptr` coûte un compteur atomique par copie : passe-le par `const &`, et ne l'utilise
   que quand la propriété est réellement partagée.
5. Destruction dans l'ordre inverse de la construction.
6. Aucun `new` nu.

**Exercices : `03_raii`.**
