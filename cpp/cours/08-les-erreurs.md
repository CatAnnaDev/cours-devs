# 08 — Les erreurs : exceptions, noexcept, optional, expected

## Trois façons de signaler une erreur

```cpp
int  ouvrir(const char *chemin);                              // code de retour
File ouvrir(const char *chemin);                              // lève en cas d'échec
std::expected<File, Erreur> ouvrir(const char *chemin);       // valeur ou cause
```

Ce ne sont pas trois goûts, ce sont trois réponses à deux questions. **L'appelant peut-il faire
quelque chose ?** Si oui, l'erreur fait partie de l'interface et doit apparaître dans le type de
retour. Sinon, elle n'a rien à faire dans toutes les signatures de la chaîne d'appels.

**Le cas est-il rare ?** Hors du fonctionnement nominal : un fichier absent quand on parcourt un
dossier n'est pas rare, un disque plein l'est.

| Le cas est… | L'appelant peut agir | L'appelant ne peut que remonter |
|---|---|---|
| **fréquent, attendu** | `optional` / `expected` | `expected`, remonté à la main |
| **rare, exceptionnel** | exception | exception |

Le code de retour à la C — un `int` négatif, `errno` — ne gagne nulle part : il s'ignore
silencieusement, il ne compose pas, et il ne marche ni dans un constructeur ni dans un opérateur.
Et la faute la plus fréquente n'est pas de choisir le mauvais outil, c'est de lever pour un cas
nominal : « clé absente du cache », « fin de fichier », « saisie invalide ». Ce sont des
`optional`.

## Ce que coûte réellement une exception

**Sur le chemin normal : rien.** Le modèle utilisé par tous les compilateurs actuels s'appelle
*zero-cost*. Aucune instruction ajoutée dans un bloc `try`, aucun drapeau posé, aucun registre
réservé. Le compilateur produit à la place des **tables de déroulement** (`.eh_frame`,
`.gcc_except_table`), rangées dans des sections séparées du binaire, qui disent pour chaque adresse
de code quels objets détruire et quels `catch` essayer. Le prix permanent est de la **taille de
binaire**, pas du temps : entrer dans un `try` ne coûte aucune instruction.

**Quand elle part : très cher.**

| Étape | Ce qui se passe |
|---|---|
| `throw` | allocation de l'objet exception hors pile (`__cxa_allocate_exception`) |
| déroulement | lecture et décodage des tables, cadre par cadre |
| chaque cadre | appel des destructeurs des objets locaux |
| `catch` | comparaison de type dynamique (RTTI) à chaque candidat |

Ordre de grandeur : **1 à 10 µs** pour traverser quelques cadres, contre quelques nanosecondes pour
un retour de fonction. Le déroulement a longtemps pris un verrou global pour retrouver les tables,
et lever depuis plusieurs threads ne passait pas à l'échelle ; GCC 12 et la glibc 2.35 ont réglé ce
point précis, mais le problème reste entier sur les chaînes d'outils plus anciennes.

**Une exception pour l'exceptionnel. Jamais pour un flot de contrôle normal.**

## RAII est la condition pour que tout ça tienne

C'est le point du chapitre 03, et c'est ici qu'il sert. Une exception crée des chemins de sortie
**invisibles dans le code source** : chaque appel est un `return` potentiel.

```cpp
void charger() {
    Texture *t = new Texture("mur.png");
    analyser(t);                 // si ça lève, t fuit
    delete t;
}
```

Il n'y a pas de `try` à ajouter proprement : il en faudrait un par ressource, et la version écrite
à la main est un empilement de `try`/`catch`/`throw;` illisible.

```cpp
void charger() {
    auto t = std::make_unique<Texture>("mur.png");
    analyser(t.get());           // si ça lève, ~unique_ptr passe
}
```

Le destructeur est appelé pendant le déroulement, comme sur un `return`. **Un programme sans `new`
nu est sûr face aux exceptions presque gratuitement ; un programme qui en contient ne peut pas
l'être.**

## Les quatre garanties

Une fonction annonce ce qui reste vrai si elle échoue.

| Garantie | Ce qu'elle promet |
|---|---|
| **aucune** | rien. L'objet peut être inutilisable, la mémoire peut avoir fui. |
| **de base** | pas de fuite, les invariants tiennent, mais la valeur peut avoir changé. |
| **forte** | tout ou rien : si ça lève, l'objet est exactement comme avant. |
| **`noexcept`** | ça ne lève pas. Si ça lève quand même, `std::terminate`. |

Dans la bibliothèque standard : **forte** pour `vector::push_back` (sous condition, voir plus bas)
et `map::insert` ; **de base** pour `vector::insert` au milieu et les algorithmes qui modifient ;
**`noexcept`** pour `size`, les destructeurs, et — sous conditions d'allocateur ou de type —
`swap` et les déplacements de conteneurs.

La garantie de base est le **minimum non négociable** : une fonction qui laisse un objet cassé
derrière elle est un bug, pas un compromis. La forte se paie — souvent une copie et un `swap` — et
ne vaut pas toujours son prix.

## `noexcept` et le piège de `vector`

`noexcept` n'est pas une décoration, c'est une information dont la bibliothèque standard **se
sert**.

Quand un `vector` réalloue, il déplace N éléments vers le nouveau bloc. S'il en déplace 500 et que
le 501ᵉ lève, l'ancien bloc est à moitié vidé : impossible de revenir en arrière, la garantie forte
est perdue. Le `vector` refuse ce risque via `std::move_if_noexcept`.

**Si le constructeur de déplacement n'est pas `noexcept` et que le type est copiable, le `vector`
copie à chaque réallocation.**

Silencieusement. Chiffrons sur un `vector<Nom>` rempli par `push_back` jusqu'à un million
d'éléments, chaque `Nom` contenant une chaîne allouée. Avec le facteur de croissance 2 de libstdc++
et de libc++, les réallocations successives transfèrent 1 + 2 + 4 + … + 524 288 éléments, soit un
million de transferts — un par élément, en moyenne :

| Constructeur de déplacement | Ce que fait la réallocation | Coût total |
|---|---|---|
| `noexcept` | ~1 000 000 de déplacements, trois pointeurs chacun | quelques ms |
| non `noexcept` | ~1 000 000 de **copies**, une allocation + un `memcpy` chacune | quelques centaines de ms |

Un facteur 20 à 50, pour un mot-clé oublié. C'est le piège le plus cher du C++ moderne : il ne se
voit ni à la compilation, ni à la lecture, ni dans un profil naïf, puisque le temps part dans
`operator new` et pas dans ton code.

**Marque `noexcept` tes constructeurs et affectations par déplacement** : ils volent des pointeurs,
ils ne lèvent pas. Avec la règle de zéro (chapitre 04), le compilateur les génère `noexcept` dès
que tous les membres le sont — vérifie-le :

```cpp
static_assert(std::is_nothrow_move_constructible_v<Nom>);
```

Ailleurs, `noexcept` se met sur ce qui ne peut vraiment pas échouer : accesseurs triviaux, `swap`.
Le mettre sur une fonction qui lève est pire que de ne rien mettre : `std::terminate` immédiat.

## Attraper

```cpp
try {
    charger();
} catch (const std::out_of_range &e) {       // le plus dérivé d'abord
    journal(e.what());
} catch (const std::exception &e) {          // le plus général ensuite
    journal(e.what());
}
```

**Par référence constante, toujours.** Attraper par valeur provoque un **découpage** : l'objet est
copié dans une variable du type écrit dans le `catch`, et tout ce qui venait du type dérivé
disparaît — dont le `what()` surchargé, qui redevient le message générique de la classe de base.
L'information utile est perdue au moment précis où on en a besoin.

**L'ordre compte.** Les `catch` sont essayés **dans l'ordre écrit**, pas du plus spécifique au plus
général : le premier qui accepte gagne, donc un `catch (const std::exception &)` placé en tête
avale tout et rend mortes les clauses suivantes.

**`catch (...)`** attrape tout, sans accès à l'objet. Il est légitime à trois endroits : à la
frontière d'une API C, parce qu'une exception qui traverse du C est un comportement indéfini ; à
l'entrée d'un thread, parce qu'une exception qui s'en échappe appelle `std::terminate` ; dans
`main`, pour journaliser avant de mourir. Ailleurs, il masque des bugs — et s'il ne sert qu'à
nettoyer, la réponse est un destructeur. Pour relancer telle quelle après avoir journalisé :
`throw;` nu, jamais `throw e;` qui recopie et découpe.

## Rien ne sort d'un destructeur

Depuis C++11, **un destructeur est implicitement `noexcept`**. Une exception qui en sort n'est pas
propagée : elle appelle `std::terminate`, et le programme meurt sans dérouler quoi que ce soit.

La raison est la **double exception** : pendant un déroulement, les destructeurs des objets locaux
sont appelés ; si l'un d'eux lève à son tour, il y a deux exceptions en vol et aucune règle sensée
pour choisir laquelle gagne.

Un destructeur qui fait quelque chose de faillible — fermer un fichier, valider une transaction —
doit donc **avaler et journaliser** :

```cpp
~Transaction() {
    try {
        if (!validee) {
            annuler();
        }
    } catch (...) {
    }
}
```

Et comme avaler une erreur est mauvais, l'opération faillible se propose **aussi** en méthode
explicite (`commit()`, `close()`) dont l'appelant lit le résultat ; le destructeur n'est que le
filet de sécurité. C'est exactement ce que fait `std::fstream`.

## `std::optional` : peut-être une valeur

```cpp
std::optional<int> vers_entier(std::string_view texte);

if (auto n = vers_entier(saisie)) {
    utiliser(*n);
}
```

À utiliser quand **l'absence n'est pas une erreur** : une clé pas encore dans un cache, un champ
facultatif, une recherche qui ne trouve rien. Il n'y a rien à expliquer : il n'y a rien.

```cpp
o.has_value()          // ou simplement if (o)
o.value_or(0)          // valeur de repli, l'argument est évalué dans tous les cas
*o                     // accès nu, comportement indéfini si vide
o.value()              // lève std::bad_optional_access si vide
```

`operator*` et `value()` ne sont pas interchangeables : le premier s'écrit après avoir testé, le
second coûte un test et peut lever. Dans une boucle chaude, teste une fois et déréférence.

**Aucune allocation.** Un `optional<T>` est un `T` et un booléen dans le même objet, avec le
remplissage d'alignement : `sizeof(optional<int>)` vaut 8. Il ne construit `T` que s'il est plein,
le détruit quand on le vide, et se copie comme son contenu.

## `std::expected` (C++23) : une valeur ou une cause

```cpp
enum class Erreur { introuvable, droits, corrompu };

std::expected<Config, Erreur> lire(const std::filesystem::path &p) {
    if (!exists(p)) return std::unexpected(Erreur::introuvable);
    ...
}
```

C'est `optional` plus la raison. À utiliser dès que **l'absence a une cause qu'on veut
transmettre** : l'appelant ne réagit pas pareil à « droits insuffisants » et à « fichier corrompu
». L'erreur est visible dans la signature, et il n'y a ni allocation ni déroulement : c'est une
union discriminée.

Les opérations monadiques évitent la cascade de `if` :

```cpp
auto resultat = lire(chemin)
              .and_then(valider)             // si valeur : appelée, renvoie un expected<Config, E>
              .or_else(charger_defauts)      // si erreur : appelée, renvoie un expected<Config, E>
              .transform(&Config::port);     // si valeur : appelée, renvoie une valeur nue
```

L'erreur traverse toute la chaîne sans être testée une seule fois, et le premier échec
court-circuite la suite. `optional` a les mêmes depuis C++23. Le prix : `sizeof(expected<T, E>)`
est la taille du plus grand des deux plus le discriminant, donc préfère un `E` léger.

## Et quand les exceptions sont interdites

`-fno-exceptions` existe, et des domaines entiers l'imposent : jeux sur consoles, embarqué, noyaux,
temps réel dur. Les raisons sont réelles : taille du binaire, déroulement au temps non borné,
verrou global, ABI absente sur certaines cibles.

**Ce qu'on perd :**

- **Un constructeur ne peut plus échouer proprement.** C'est la perte centrale : il n'a pas de
  valeur de retour, l'exception était son seul moyen de dire « je n'ai pas pu ». Il faut revenir à
  un constructeur infaillible plus une fonction `init()`, ou à une **fabrique statique** renvoyant
  un `expected` — et vivre avec un invariant qui n'est plus garanti par le type.
- Les opérateurs et les conversions, qui n'ont pas non plus de canal d'erreur.
- **La bibliothèque standard lève quand même** : `vector::at`, `std::stoi`, `std::filesystem`, et
  surtout `operator new`. Avec `-fno-exceptions`, un `throw` de la bibliothèque appelle
  `std::terminate` — l'erreur ne disparaît pas, elle devient un arrêt brutal.

**Ce qu'on met à la place :** `expected` partout, des fabriques `static std::expected<T, E>
creer(...)` au lieu des constructeurs faillibles, `new (std::nothrow)` ou un allocateur maison, des
accès vérifiés à la main, et souvent une bibliothèque de conteneurs écrite pour ce mode. Le style
qui en résulte est correct, explicite, et nettement plus verbeux.

## À retenir

1. La question qui tranche : l'appelant peut-il agir, et le cas est-il rare ? Fréquent, c'est
   `optional` ou `expected` ; rare, c'est une exception.
2. Une exception ne coûte rien tant qu'elle ne part pas, et des microsecondes quand elle part.
   Jamais dans un flot de contrôle normal.
3. Sans RAII, les exceptions font fuir. Aucun `new` nu, et le problème disparaît.
4. Garantie de base au minimum ; garantie forte quand elle vaut son prix.
5. Constructeur et affectation par déplacement `noexcept`, sinon `vector` copie à chaque
   réallocation.
6. `catch (const T &)` — par valeur, ça découpe et `what()` ne dit plus rien. Du plus dérivé au
   plus général.
7. Rien ne sort d'un destructeur : il est `noexcept`, et une exception pendant un déroulement tue
   le programme.

**Exercices : `08_erreurs`.**
