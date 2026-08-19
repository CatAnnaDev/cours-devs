# 10 — Le polymorphisme, et ce que chaque forme coûte

## La question posée

Un seul code d'appel, plusieurs types : `total(formes)` doit marcher pour des carrés et des cercles
sans que la boucle connaisse la liste des formes.

```cpp
double total(??? formes) {
    double somme = 0;
    for (const auto &forme : formes) { somme += forme.aire(); }
    return somme;
}
```

Tout tient dans le `???`. Ce qu'on met dedans décide **à quel moment le corps de `aire()` est
choisi**, et ce moment décide du reste : le prix d'un appel, la taille d'un objet, la possibilité
d'ajouter une forme sans recompiler l'appelant. C++ répond trois fois, quatre avec `std::variant`.

| | virtuel | template / CRTP | effacement de type |
|---|---|---|---|
| Moment du choix | exécution | compilation | exécution |
| Coût d'un appel | 2 lectures + branchement indirect | nul, l'appel est inlinable | idem virtuel, plus une déréférence |
| Taille de l'objet | + 8 octets (le `vptr`) | + 0 octet | 8 (`unique_ptr`) ou 32 (`std::function`) |
| Extensibilité | ouverte : un `.cpp` de plus suffit | fermée : chaque type recompile l'appelant | ouverte, sans héritage imposé |
| Temps de compilation | un seul corps de boucle | un corps par type | un corps par type stocké |

Tous les chiffres de ce chapitre sont mesurés sur la plateforme du cours, arm64 macOS avec Apple
clang 21 et libc++. Les tailles d'objets valent sur les autres ABI Itanium 64 bits ; sur une cible
32 bits, seul le `vptr` passe de 8 à 4, pas les `double` qui entrent dans les mêmes calculs. Les
tailles des types de bibliothèque — `std::function`, `std::variant` — sont propres à libc++.

## Le virtuel, mécaniquement

La norme ne prononce jamais les mots « table virtuelle » : elle exige seulement que l'appel
atteigne le *final overrider* du type dynamique. Ce qui suit est l'implémentation, pas le langage —
l'ABI Itanium, que suivent clang et GCC sur macOS et Linux ; MSVC fait autrement.

| Déclaration | `sizeof` | `alignof` |
|---|---|---|
| `struct { int a; };` | 4 | 4 |
| `struct { int a; virtual void f(); };` | **16** | **8** |
| la même, avec une **seconde** fonction virtuelle | 16 | 8 |
| `struct { double x, y; };`, puis avec `virtual ~T()` | 16, puis 24 | 8 |

Deuxième ligne contre troisième : **la première fonction virtuelle coûte 8 octets, les suivantes ne
coûtent rien.** Ce qui entre dans l'objet n'est pas la table mais un pointeur vers elle, le `vptr`,
placé au début — d'où 8 de `vptr`, 4 de `int`, 4 de bourrage, l'alignement passant de 4 à 8. Une
classe héritant de deux bases polymorphes indépendantes porte **deux** `vptr` : avec les `A` et `B`
du tableau ci-dessus, qui portent chacun un `int`, `struct AB : A, B { int x; };` mesure **32**.

### La table, et l'appel

```
Vtable for 'Forme' (5 entries).           obtenu avec  c++ -Xclang -fdump-vtable-layouts
   0 | offset_to_top (0)
   1 | Forme RTTI
       -- (Forme, 0) vtable address --    <- c'est ici que pointe le vptr
   2 | Forme::~Forme() [complete]
   3 | Forme::~Forme() [deleting]
   4 | double Forme::aire() const [pure]
```

Une table par **classe**, jamais par objet : mille cercles partagent une table et paient mille fois
8 octets de `vptr`. Le destructeur occupe deux emplacements, donc `aire()` est le troisième, 16
octets après le `vptr`. `double via_base(const Forme &f) { return f.aire(); }` tient alors, à
`-O2`, en trois instructions arm64 :

```
ldr  x8, [x0]        // le vptr, au tout debut de l'objet
ldr  x1, [x8, #16]   // l'emplacement de aire(), 2 entrees plus loin
br   x1              // branchement indirect (ici en position terminale)
```

### Ce que ça empêche vraiment

Deux lectures et un saut indirect : sur un cœur moderne, un site d'appel monomorphe est bien prédit
et ne coûte presque rien. **Le prix n'est pas l'indirection, c'est l'inlining.** Le compilateur
ignore quel corps sera exécuté, donc tout ce qui suit l'inlining tombe : propagation de constantes,
code mort, déroulage, vectorisation. Vérifié à `-O2`,
`for (auto *f : formes) { somme += f->aire(); }` traite **un** élément par tour (`ldr`, `ldr`,
`ldr`, `blr`, `fadd`) là où la même boucle sur des objets concrets, section CRTP, est déroulée par
8 et vectorisée. **Le facteur est là, pas dans le `blr`.**

## Les quatre fautes qui vont avec

### Le destructeur non virtuel

```cpp
struct Base   { virtual void f() {} };          // pas de destructeur virtuel
struct Derive : Base { std::string gros = std::string(100, 'x'); };
Base *p = new Derive;
delete p;                                       // comportement indefini
```

La norme : détruire par un pointeur vers une base au destructeur non virtuel est un comportement
indéfini. Avec les options du cours, le destructeur de `Derive` n'est jamais appelé, la
`std::string` fuit, le programme continue et rend 0 — et **ni ASan ni UBSan ne disent rien**,
LeakSanitizer n'étant pas actif sur macOS. Le seul filet est le compilateur : `-Wall` allume
`-Wdelete-non-abstract-non-virtual-dtor`, qui prévient **au site du `delete`** ;
`-Wnon-virtual-dtor`, qui prévient à la déclaration de la classe, n'est ni dans `-Wall` ni dans
`-Wextra`. **Remède** : `virtual ~Base() = default;` dès la première fonction virtuelle — ou, pour
interdire la destruction par la base, un destructeur `protected` non virtuel, qui fait échouer le
`delete p`.

### Le découpage à la copie

La faute est dans le type, pas dans le corps : `void afficher(Forme f);` la commet une fois par
appel, `std::vector<Forme> formes;` la commet à chaque élément. `sizeof(Forme)` vaut 8 et
`sizeof(Carre)` vaut 16 : copier un `Carre` dans un `Forme` ne copie **aucune donnée**. Le
sous-objet `Forme` ne contient que le `vptr`, que le constructeur de copie réinstalle sur la table
de `Forme`, et les 8 octets du `double` de `Carre` sont simplement perdus. Mesuré, la même forme
rend 9 par `const &` et **0** par valeur, sans un avertissement ni un rapport de sanitizer.
**Remède** : passer par `const Forme &`, stocker des `std::unique_ptr<Forme>`, et rendre la base
abstraite ou non copiable (`Forme(const Forme &) = delete;`), ce qui transforme la faute en erreur
de compilation.

### La signature qui dérape

```cpp
struct Base   { virtual void dessiner(double echelle) const; };
struct Derive : Base { void dessiner(float echelle) const; };    // ne redefinit rien
```

Un `const` oublié, un `float` au lieu d'un `double`, une lettre inversée : la dérivée n'est pas un
*override*, elle **masque** celle de la base, et l'appel par la base part dans `Base`.

| Cas | Avec `-Wall -Wextra` | Avec `override` |
|---|---|---|
| signature différente, **même nom** | `-Woverloaded-virtual` (avertissement) | **erreur** de compilation |
| nom mal orthographié | **rien du tout** | **erreur** de compilation |

`-Woverloaded-virtual` vient de `-Wall` seul chez clang — GCC le réserve à `-Wextra` — et ne voit
rien quand le nom
diffère. **Remède** : `override` sur chaque redéfinition. Ajouter `noexcept` dans la dérivée reste
légal : une spécification d'exception plus stricte est autorisée pour un *override*.

### L'appel virtuel dans un constructeur ou un destructeur

```cpp
struct Base {
    Base() { std::printf("%s\n", nom().c_str()); }   // appelle toujours Base::nom
    virtual std::string nom() const { return "Base"; }
};
struct Derive : Base { std::string nom() const override { return "Derive"; } };
```

Pendant le constructeur de `Base`, le type dynamique de l'objet **est** `Base` : les membres de
`Derive` n'existent pas encore, et le langage garantit qu'aucune fonction ne viendra les lire — le
`vptr` est réécrit à chaque étage de la construction, et symétriquement de la destruction. C'est la
règle, pas un défaut d'implémentation, et elle est silencieuse : ni avertissement, ni sanitizer,
juste la mauvaise fonction. Le cas **pur**, lui, est d'une autre nature : c'est le seul des deux
qui soit un comportement indéfini, et c'est aussi le seul qui fasse du bruit. Clang prévient par
défaut
(`-Wcall-to-pure-virtual-from-ctor-dtor`) et le programme meurt sur
`libc++abi: Pure virtual function called!`, code de sortie 134. **Remède** : sortir l'étape
polymorphe du constructeur, vers une fonction `initialiser()` appelée par une fabrique.

## `override` et `final`

`override` ne change **rien** au code produit : c'est une assertion vérifiée par le compilateur,
« cette fonction en redéfinit une ». Elle attrape les dérapages du tableau précédent, et une faute
que rien d'autre ne voit — le jour où la signature change **dans la base**, tous les `override` des
dérivées deviennent des erreurs au lieu de surcharges muettes.

`final` interdit toute redéfinition ultérieure, sur une fonction ou sur une classe entière, et
c'est la seule des deux qui puisse changer le code produit. Vérifié à `-O2` sur trois formes du
même appel :

| Appel | Code produit |
|---|---|
| `f.aire()` sur `const Forme &`, base abstraite | 3 instructions, branchement indirect |
| `c.aire()` sur `const Cercle &`, `Cercle` non `final` | 3 instructions, branchement indirect |
| `c.aire()` sur `const Carre &`, `Carre` **`final`** | `ldr d0, [x0,#8]` puis `fmul d0, d0, d0` |

La deuxième ligne surprend. `Cercle` est concret, on voit laquelle des deux fonctions on veut —
mais le compilateur ne le sait pas : rien n'interdit à une autre unité de traduction de dériver de
`Cercle`. Avec `final` cette possibilité disparaît, le type dynamique devient connu, et le corps
est inliné jusqu'à disparaître. Même effet sans `final` quand l'objet est une variable locale dont
le compilateur voit la construction :

```cpp
double aire_locale(double v) {
    Carre carre;
    carre.cote = v;                 // pas Carre carre{v} : une classe a fonctions virtuelles
    const Forme &f = carre;         // n'est pas un agregat, l'initialisation par accolades echoue
    return f.aire();
}
```

À `-O2`, la fonction entière se réduit à `fmul d0, d0, d0` puis `ret` : le type dynamique est
connu sur place, la table n'est jamais consultée.

## Le CRTP : le polymorphisme sans table

```cpp
template <typename Concret>
struct FormeStatique {
    double aire() const { return static_cast<const Concret *>(this)->aire_impl(); }
};
struct Carre : FormeStatique<Carre> {
    double cote;
    double aire_impl() const { return cote * cote; }
};
```

La base est un template paramétré par sa propre dérivée : *Curiously Recurring Template Pattern*.
Le `static_cast` est résolu à la compilation, et légal parce que `Carre` **est** un
`FormeStatique<Carre>`. Aucune fonction virtuelle, donc aucune table et rien à payer :
`sizeof(FormeStatique<Carre>)` vaut 1 et `sizeof(Carre)` vaut **8**, exactement son `double`, la
base vide étant absorbée par l'optimisation de base vide — l'équivalent virtuel mesure 16. Et
l'appel est inlinable : `somme += f.aire()` sur un `span<const Carre>` compile à `-O2` en un corps
déroulé par 8, avec des `fmul.2d` qui traitent deux `double` par instruction.

**Ce qu'on perd, et c'est beaucoup.** `FormeStatique<Carre>` et `FormeStatique<Cercle>` sont deux
types sans rapport : pas de base commune, donc **pas de conteneur hétérogène**. Pas de choix à
l'exécution non plus, le type devant être connu au site d'appel, ce qui interdit de lire une forme
dans un fichier. Et tout ce qui touche à ces types devient template, avec le prix du chapitre 07 :
un corps généré par type, du code en en-tête, des erreurs qui remontent la chaîne d'instanciation.

## L'effacement de type

Troisième réponse : garder le choix à l'exécution sans imposer d'héritage, pour qu'un `Carre`
ordinaire, qui ne dérive de rien, entre dans le conteneur. Le principe est une interface virtuelle
**cachée** derrière une classe à valeur :

```cpp
class Dessinable {
    struct Concept { virtual ~Concept() = default; virtual double aire() const = 0; };
    template <typename T>
    struct Modele final : Concept {
        explicit Modele(T v) : objet(std::move(v)) {}
        double aire() const override { return objet.aire(); }
        T objet;
    };
    std::unique_ptr<Concept> trait_;
public:
    template <typename T>
    Dessinable(T objet) : trait_(std::make_unique<Modele<T>>(std::move(objet))) {}
    double aire() const { return trait_->aire(); }
};
```

Le virtuel est toujours là, devenu un détail d'implémentation. Mesuré : `sizeof(Dessinable)` vaut
**8**, et `d.aire()` compile en **quatre** instructions à `-O2`, une lecture de plus que l'appel
virtuel nu pour déréférencer le `unique_ptr`. `std::function` est ce motif appliqué à l'appel ; son
prix sur la libc++ d'Apple clang 21 :

| Mesure | Valeur |
|---|---|
| `sizeof(std::function<void()>)` | **32** octets (tampon de 24 + 1 pointeur) |
| capture tenant dans le tampon | jusqu'à **16** octets ; **17** octets allouent |
| `alignof` du callable au-dessus de 8, ou copie non `noexcept` | une allocation, même à 8 octets |
| `std::function` vide, ou depuis un pointeur de fonction | zéro allocation |

Le seuil de 16 et non 24 s'explique : le tampon doit loger l'enveloppe complète, qui porte
elle-même un `vptr` de 8 octets. Deux références capturées passent, **trois allouent**. Les seuils
ne sont pas normatifs : la norme n'exige aucun tampon interne et n'en fixe aucune taille, elle
garantit seulement qu'une construction depuis un pointeur de fonction ou un `reference_wrapper` ne
lève pas, et recommande d'éviter l'allocation pour les petits appelables. libstdc++ et MSVC ont
d'autres seuils. À l'appel, `f()` ajoute au saut indirect un test
de vacuité qui peut lever `std::bad_function_call`.

## Choisir

1. **Ensemble fermé et connu de toi** — trois formes, huit jetons, quatre états : `std::variant`
   plus `std::visit`.
2. **Ensemble ouvert, choix à l'exécution** — un greffon, une forme lue dans un fichier : une base
   abstraite, destructeur virtuel et `override` partout.
3. **Type connu au site d'appel** — un comparateur, une politique d'allocation : un template, ou un
   CRTP si la base doit fournir du code aux dérivées.
4. **Callables hétérogènes à stocker** — des rappels, une file de tâches : l'effacement de type,
   `std::function` ou ta propre classe pour éviter l'allocation.

```cpp
using Forme = std::variant<Cercle, Carre, Triangle>;
somme += std::visit([](const auto &f) { return f.aire(); }, forme);
```

La première branche est la voie moderne, et pas la moins chère par principe. L'objet est **plat** :
`sizeof(std::variant<Cercle, Carre, Triangle>)` vaut 24 sur libc++, soit le plus gros membre (16)
plus un index de 4 octets et son bourrage. Pas d'allocation, pas de `vptr`, une copie ordinaire, et
le compilateur oblige à traiter tous les cas. En échange l'ensemble est figé : **ajouter une forme
oblige à recompiler tout ce qui `visit`**. Le coût d'appel n'est pas nul non plus — sur libc++,
`std::visit` sur trois alternatives compile à `-O2` en un test de l'état *valueless*, une lecture
dans une table de pointeurs de fonctions et un **saut indirect**, soit la forme de l'appel virtuel
plus une branche. D'autres implémentations produisent un `switch` : choix de bibliothèque, pas
garantie du langage.

## À retenir

1. Le moment du choix décide de tout : exécution pour le virtuel et l'effacement de type,
   compilation pour les templates et le CRTP.
2. La **première** fonction virtuelle ajoute 8 octets à l'objet et porte son alignement à 8 ; les
   suivantes sont gratuites, la table étant partagée par toute la classe.
3. Le prix du virtuel n'est pas le saut indirect, c'est **l'inlining perdu** — et avec lui la
   vectorisation.
4. Quatre fautes l'accompagnent : le découpage à la copie est la seule totalement silencieuse, la
   signature qui dérape et l'appel virtuel ne le sont que dans leur variante la plus courante, et
   le destructeur non virtuel est la seule que `-Wall` attrape à coup sûr.
5. `override` sur chaque redéfinition et `virtual ~Base()` dès la première fonction virtuelle :
   `-Wall` n'en attrape que deux sur quatre, les sanitizers aucune.
6. `final` peut supprimer l'appel indirect ; sans lui, même un type concret reste appelé
   indirectement, car une autre unité de traduction pourrait en dériver.
7. Le CRTP donne un appel inlinable et zéro octet ajouté, au prix du conteneur hétérogène ;
   `std::variant` garde l'objet plat mais fige la liste ; `std::function` alloue dès 17 octets de
   capture sur la libc++ d'Apple clang 21.

**Exercices : `10_polymorphisme`.**
