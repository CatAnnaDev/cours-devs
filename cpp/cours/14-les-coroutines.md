# 14 — Les coroutines

Tout ce qui est dit « mesuré » ici a été compilé et lancé sur la machine de référence : arm64
macOS, Apple clang 21, libc++ `_LIBCPP_VERSION` 220106. La norme définit un protocole ; elle ne
promet rien sur les tailles ni sur la disposition du cadre, et ce qui vient de l'implémentation est
signalé comme tel.

## Une fonction qui s'arrête au milieu

Une fonction ordinaire a une seule sortie : elle rend la main, son cadre de pile meurt, ses locales
et son point d'exécution avec lui. Une **coroutine**, elle, se suspend au milieu de son corps — sur
un `co_yield i` au fond d'une boucle — et reprend plus tard exactement là, `i` valant ce qu'il
valait à la suspension. Son état ne vit donc plus sur la pile de l'appelant : il vit dans un
**cadre de coroutine** alloué séparément, qui survit au retour. Tout le mécanisme est là, une pile
qui n'est plus une pile, et toute la complexité de ce chapitre en découle.

## Ce que C++20 te donne : presque rien

| Le langage donne | La bibliothèque donne |
|---|---|
| `co_await`, `co_yield`, `co_return` | `std::coroutine_handle<P>` |
| la transformation du corps en machine à états | `std::coroutine_traits<R, Args...>` |
| l'allocation et la libération du cadre | `std::suspend_always`, `std::suspend_never` |
| le protocole `promise_type` qu'il ira chercher | `std::noop_coroutine()` |

C'est le point à comprendre avant le reste, sous peine de perdre une soirée : **aucun type de
coroutine utilisable** là-dedans, pas de tâche, pas de générateur, pas d'ordonnanceur, pas
d'entrées-sorties. `std::suspend_always` est une structure vide de 1 octet et `coroutine_handle` un
pointeur nu de 8 octets, mesurés. Le premier générateur standard, `std::generator`, n'arrive qu'en
**C++23**, et vérifié en le lançant ici il **n'existe pas** : `#include <generator>` donne `fatal
error: 'generator' file not found`, et `__cpp_lib_generator` n'est jamais défini, même en
`-std=c++2c`. `<coroutine>`, lui, est là, et `__cpp_impl_coroutine` comme `__cpp_lib_coroutine` y
valent tous deux 201902. D'autres bibliothèques standard fournissent `<generator>` ; ici, non, et
**écrire son type est donc obligatoire.**

## Trois mots-clés, et rien dans la signature

| Mot-clé | Ce qu'il devient réellement |
|---|---|
| `co_await e` | suspend éventuellement, en interrogeant l'*awaitable* `e` |
| `co_yield v` | strictement `co_await promesse.yield_value(v)` |
| `co_return v` | appelle `promesse.return_value(v)`, puis va à la suspension finale |

**Un seul de ces trois mots dans le corps suffit.** D'où le piège de relecture : `Generateur<int>
compte(int, int)` et `std::vector<int> compte(int, int)` se relisent pareil. **Rien dans la
signature ne dit qu'une fonction est une coroutine.** Seul le corps décide, et c'est alors au type
de retour de fournir le protocole ; sans lui, `error: this function cannot be a coroutine:
'std::coroutine_traits<Tache>' has no member named 'promise_type'`. Sept contextes les refusent,
même patron de message : une fonction `constexpr` (`'co_return' cannot be used in a constexpr
function`), une fonction `consteval`, une variadique à la C, un constructeur, un **destructeur**,
une fonction à **type de retour déduit** (`auto`), et `main`.

## Le `promise_type`, membre par membre

Le compilateur cherche `std::coroutine_traits<Retour, Args...>::promise_type`, par défaut
`Retour::promise_type` ; il le construit **dans** le cadre, avant d'entrer dans le corps.

| Membre | Quand il est appelé | Obligatoire |
|---|---|---|
| `get_return_object()` | après la construction de la promesse, avant tout le reste | oui |
| `initial_suspend()` | `co_await`é avant la première instruction du corps | oui |
| `final_suspend() noexcept` | `co_await`é après `co_return` ou la fin du corps | oui, et `noexcept` |
| `return_void()` / `return_value(v)` | sur `co_return;` / `co_return v;` | l'un, jamais les deux |
| `unhandled_exception()` | dans le `catch (...)` qui entoure tout le corps | oui |
| `yield_value(v)` | sur `co_yield v` | seulement si `co_yield` |
| `await_transform(e)` | sur chaque `co_await e` du corps, s'il existe | non |
| `operator new` / `operator delete` | pour allouer et libérer le cadre | non |
| `get_return_object_on_allocation_failure()` | si `operator new` peut rendre `nullptr` | non |

`final_suspend` réclame `noexcept` parce qu'à ce point le corps est fini : aucun `catch` ne peut
plus rattraper quoi que ce soit. `unhandled_exception` **est** ce `catch (...)`, et la laisser vide
avale toutes les exceptions du corps. Les trois erreurs les plus fréquentes, messages tels quels :

```
error: the expression 'co_await __promise.final_suspend()' is required to be non-throwing
error: no member named 'yield_value' in 'Tache::promise_type'
error: the coroutine promise type 'promise_type' declares both 'return_value' and 'return_void'
```

### Le squelette complet

```cpp
template <typename T>
class Generateur {
public:
    struct promise_type {
        T courant;
        std::exception_ptr erreur;
        Generateur get_return_object() {
            return Generateur(std::coroutine_handle<promise_type>::from_promise(*this));
        }
        std::suspend_always initial_suspend() noexcept { return {}; }
        std::suspend_always final_suspend() noexcept { return {}; }
        std::suspend_always yield_value(T v) { courant = std::move(v); return {}; }
        void return_void() {}
        void unhandled_exception() { erreur = std::current_exception(); }
    };
    explicit Generateur(std::coroutine_handle<promise_type> p) : poignee_(p) {}
    Generateur(const Generateur &) = delete; Generateur &operator=(const Generateur &) = delete;
    Generateur(Generateur &&a) noexcept : poignee_(std::exchange(a.poignee_, {})) {}
    Generateur &operator=(Generateur &&a) noexcept {
        if (this != &a) { if (poignee_) { poignee_.destroy(); }
                          poignee_ = std::exchange(a.poignee_, {}); }
        return *this;
    }
    ~Generateur() { if (poignee_) { poignee_.destroy(); } }
    bool avancer() { poignee_.resume(); return !poignee_.done(); }
    const T &valeur() const { return poignee_.promise().courant; }
private:
    std::coroutine_handle<promise_type> poignee_;
};
```

Avec un `begin()` rendant un itérateur d'entrée et un `end()` rendant `std::default_sentinel_t`, il
satisfait `std::ranges::input_range` — vérifié par `static_assert` — et se consomme au `for` de
plage, y compris derrière `std::views::filter`.

## Paresseux ou impatient : `initial_suspend`

`initial_suspend()` est `co_await`é **avant** la première instruction du corps : le type qu'il rend
décide si le corps démarre à l'appel ou attend le premier `resume()`. Mesuré avec un compteur
global incrémenté à la première ligne du corps :

| `initial_suspend()` rend | après l'appel | après le premier `resume()` |
|---|---|---|
| `std::suspend_always` | `corps_entre = 0`, `done() = false` | `corps_entre = 1`, `done() = true` |
| `std::suspend_never` | `corps_entre = 1`, `done() = true` | — |

L'ordre, lui, ne dépend pas du choix : la promesse est construite, `get_return_object()` est
appelé, **puis** `initial_suspend()` est `co_await`é — vérifié par trace, imposé par la norme, donc
`get_return_object()` ne peut jamais observer un résultat du corps. **Un générateur veut
`suspend_always`**, sinon le corps court jusqu'au premier `co_yield` avant que l'appelant tienne le
générateur ; une tâche lancée puis oubliée veut `suspend_never`, mais alors une coroutine sans
suspension est déjà `done()` au retour, et l'oublier fuit son cadre.

## Le `coroutine_handle`, et qui détruit

`std::coroutine_handle<P>` fait **8 octets** (mesuré), est trivialement copiable et **ne possède
rien** : c'est un pointeur nu sur le cadre. `resume()` reprend, `done()` dit si la coroutine est à
sa suspension finale, `destroy()` détruit le cadre, `promise()` donne la promesse. Sur cette
implémentation, ce cadre commence par deux pointeurs de fonction : `dladdr` sur ses deux premiers
mots rend `travail.resume` et `travail.destroy`, et la promesse est à l'offset 16. Rien de cela
n'est normatif, mais tout s'explique par là — `done()` **est** le test « le pointeur de reprise
est-il nul », mesuré, il passe de `0x102f24c68` à `0x0` à la fin. D'où le comportement de
`resume()` sur une coroutine terminée : **indéfini** selon la norme, saut à l'adresse zéro en
pratique, `SEGV on unknown address 0x000000000000`, `pc points to the zero page`.

**Personne ne détruit le cadre à ta place** — dès lors que `final_suspend()` rend `suspend_always`,
qui est le cas de tout générateur. Ni la fin du corps, ni `done()`, ni la sortie de portée de la
poignée : `coroutine_handle` n'a pas de destructeur. Avec `suspend_never` en `final_suspend`, en
revanche, le contrôle sort du corps et le cadre se détruit **seul** : un `destroy()` derrière
serait une double libération. Seul `destroy()` libère, et l'appeler deux fois est illégal. D'où la
**règle de cinq** sur le type générateur : copie interdite — elle donnerait deux propriétaires,
donc deux `destroy()` —, déplacement qui vole la poignée par `std::exchange`, destructeur qui
appelle `destroy()`. Et l'oubli est **silencieux ici** : mille cadres alloués sans un seul
`destroy()`, sortie avec le code 0 sans un mot, ASan répondant `detect_leaks is not supported on
this platform`. Sous Linux, LeakSanitizer, actif par défaut avec ASan, signalerait la fuite — non
mesuré ici, mais c'est la différence à connaître.

## Le piège des paramètres

Les paramètres sont **copiés dans le cadre** à l'appel, pour survivre aux suspensions. Compté avec
une sonde qui incrémente à chaque copie et à chaque déplacement :

| Appel | copies | déplacements |
|---|---|---|
| `par_valeur(sonde)`, `sonde` étant une lvalue | 1 | 1 |
| `par_valeur(Sonde(8))` | 0 | 1 |
| `par_ref(sonde)`, paramètre `const Sonde &` | **0** | **0** |

La dernière ligne est le bug. Une référence est copiée **en tant que référence** : ce qui atterrit
dans le cadre est l'adresse, pas l'objet. Le paramètre survit, ce qu'il désigne non, et un
temporaire passé par `const &` meurt à la fin de l'expression complète, **avant la première
reprise** :

```
ERROR: AddressSanitizer: stack-use-after-scope on address 0x000102c29057
    #3 lettres(std::string const&) (.resume) c_ref.cpp:33
    [64, 88) 'ref.tmp' (line 37) <== Memory access at offset 87 is inside this variable
```

`ref.tmp` est le temporaire de l'appelant, déjà mort. Et prendre le paramètre **par valeur** ne
sauve pas si la valeur est elle-même une référence déguisée : le même code avec un
`std::string_view` donne le heap-use-after-free réel, le tampon de la chaîne temporaire ayant été
rendu au tas — à condition que la chaîne dépasse les 22 caractères de la petite optimisation, faute
de quoi c'est un `stack-use-after-scope` que l'on obtient, sur le tampon interne de l'objet.

```
ERROR: AddressSanitizer: heap-use-after-free on address 0x6060000001a0
    #0 lettres(std::string_view) (.resume) c_vue.cpp:34
0x6060000001a0 is located 0 bytes inside of 56-byte region [0x6060000001a0,0x6060000001d8)
freed by thread T0 here: #1 std::basic_string<...>::~basic_string()  #2 main c_vue.cpp:38
```

**La règle : une coroutine prend ses paramètres par valeur propriétaire.** Pas de `const &`, pas de
`string_view`, pas de pointeur sur un temporaire, pas de lambda capturant par référence.

## `co_await` et les awaitables

`co_await e` se résout en trois temps : si la promesse a `await_transform`, `e` lui est passé ; si
le résultat a un `operator co_await`, il est appliqué ; l'objet obtenu est l'*awaitable*, et doit
fournir `await_ready()`, `await_suspend(poignee)`, appelé seulement si le premier rend faux, et
`await_resume()`, dont la valeur est celle de l'expression. Trace, condensée, d'un corps qui
enchaîne `co_await Trace{pret}` puis `co_await Trace{pas_pret}` :

```
resume 1 : [corps] debut / await_ready(pret) -> true / await_resume(pret) / [corps] a = 42 /
           await_ready(pas pret) -> false / await_suspend(pas pret)
resume 2 : await_resume(pas pret) / [corps] b = 42 / return_void / final_suspend
```

Deux choses à y lire. `await_ready()` vrai **saute** `await_suspend` : c'est le raccourci qui rend
`co_await` presque gratuit quand le résultat est déjà disponible. Et `await_suspend` est appelé
**après** la suspension, donc la poignée qu'il reçoit est déjà reprenable, y compris d'un autre
fil.

| Retour de `await_suspend` | Effet |
|---|---|
| `void` | reste suspendue ; le contrôle revient à l'appelant de `resume()` |
| `bool` | `true` : reste suspendue. `false` : reprend tout de suite, sans repasser par l'appelant |
| `std::coroutine_handle<>` | suspend, puis reprend **celle-là** : c'est le transfert symétrique |

La troisième forme n'est pas une commodité, c'est une question de pile. Appeler `cible.resume()`
**depuis** `await_suspend` empile un cadre de pile de plus par maillon ; rendre la poignée fait
sauter le contrôle sans rien empiler. Mesuré sur une chaîne de 1000 coroutines : **48 064 octets**
de pile pour la première forme, **64 octets** pour la seconde, en O(1). `std::noop_coroutine()` est
la poignée neutre quand il n'y a personne à reprendre.

## Ce que ça alloue

Le cadre part **sur le tas**. La norme autorise l'implémentation à l'élider mais ne le garantit
jamais, et ne dit rien de sa taille. Mesuré en surchargeant `promise_type::operator new`, qui en
reçoit la taille exacte :

| Coroutine | cadre à `-O0` | cadre à `-O1` et au-delà |
|---|---|---|
| corps vide, `co_return` | 24 | 24 |
| `for (i < n) { co_yield i; }` | 32 | 32 |
| `long long t[64]` déclaré dans un **bloc interne**, mort avant la suspension | **40** | **32** |
| `long long t[64]` en portée de **fonction** | 552 | 544 |
| `long long t[64]` lu **à travers** les suspensions | 552 | 552 |

Les 24 octets minimaux sont, sur cette implémentation, les deux pointeurs de fonction et l'indice
de suspension ; la promesse s'ajoute par-dessus à partir de l'offset 16 — une promesse de 32 octets
porte le cadre à 56. La leçon des trois dernières lignes n'est pas celle qu'on attend : ce qui
décide, ce n'est pas le niveau d'optimisation, c'est **la portée de déclaration de la locale**. Un
tableau déclaré dans un bloc interne quitte le cadre dès `-O0` ; le même en portée de fonction y
reste à tous les niveaux, `-O3` compris. Le compilateur honore les fins de portée, il ne fait pas
d'analyse de vivacité pour toi. L'élision, elle, se constate niveau par niveau, compteur
d'`operator new` à l'appui : le cadre est bien alloué à `-O0` et à `-Oz`, mais **élidé** à `-O1`,
`-O2`, `-O3`, `-Os`, `-Og`, et même à `-O2 -fsanitize=address` — puis réalloué dès qu'on ajoute
`-fsanitize=undefined`.

Deux enseignements : l'élision exige que l'appelant voie le corps — à travers deux unités de
traduction, le cadre est alloué à tous les niveaux — et c'est **UBSan**, pas ASan, qui la bloque,
donc dans le runner des exercices le cadre est toujours là. L'échappatoire est un `operator new`
membre, qui prime sur le global :

```cpp
static void *operator new(std::size_t taille) noexcept {   // arene a curseur ; ne leve jamais
    std::size_t alignee = (taille + 15) / 16 * 16;
    if (curseur + alignee > sizeof(arene)) { return nullptr; }
    void *bloc = arene + curseur; curseur += alignee; return bloc;
}
static void operator delete(void *, std::size_t) noexcept {}
static Generateur get_return_object_on_allocation_failure() { return Generateur(nullptr); }
```

Mesuré : cent générateurs successifs, **4800 octets** pris dans l'arène — cent cadres de 48, la
promesse d'un générateur portant une valeur courante et un `exception_ptr` — et
**zéro** appel à l'`operator new` global. La dernière fonction autorise le `nullptr` au lieu d'une
levée ; sans elle, ce `nullptr` serait un comportement indéfini.

## À quoi ça sert vraiment

**Générateurs paresseux** : produire une suite sans la matérialiser, le corps s'écrivant comme une
boucle ordinaire, l'appelant n'en tirant que ce qu'il consomme. **Entrées-sorties asynchrones** :
`await_ready` teste la disponibilité, `await_suspend` enregistre la poignée auprès du système,
`await_resume` livre l'octet — mais **rien de tout cela n'est fourni**, ni boucle d'événements, ni
sockets asynchrones, ni exécuteurs, `__cpp_lib_senders` n'étant défini nulle part ici, même en
`-std=c++2c`. **Machines à états** : l'état *est* le point de suspension, au lieu d'une énumération
et d'un `switch` ; un ordonnanceur coopératif tient en une `std::deque<std::coroutine_handle<>>` et
une boucle qui dépile et reprend : deux travailleurs de trois tours s'y entrelacent en `A0 B0 A1 B1
A2 B2`, mesuré.

Et la remarque honnête, mesurée en deux unités de traduction à `-O2`, sur cent mille éléments :
**1,38 ns** par élément pour une boucle écrite à la main, **1,38 ns** pour un itérateur écrit à la
main, **3,06 ns** pour le générateur coroutine. L'itérateur est gratuit ; la coroutine coûte 1,7 ns
de plus par élément, le prix d'un aller-retour suspension/reprise que le compilateur ne peut pas
inliner. Pour un simple générateur, **un itérateur écrit à la main est souvent plus rapide et
toujours plus lisible** ; la coroutine ne gagne que sur un état réellement compliqué — boucles
imbriquées, récursion, protocole — là où l'itérateur devient illisible.

## À retenir

1. Une coroutine garde ses locales entre deux reprises : elles vivent dans un cadre sur le tas.
2. C++20 ne fournit **aucun type utilisable** : `<generator>` est C++23 et **absent de cette
   libc++**. Écrire son `promise_type` est le minimum vital, pas un exercice de style.
3. `co_await`, `co_yield` ou `co_return` suffit — et **rien dans la signature ne le montre**.
4. `final_suspend` doit être `noexcept` ; `return_void` et `return_value` s'excluent ;
   `initial_suspend` choisit entre paresseux (`suspend_always`) et impatient (`suspend_never`).
5. `coroutine_handle` fait 8 octets et ne possède rien : **personne n'appelle `destroy()` à ta
   place**, et sur macOS l'oubli est silencieux. D'où la règle de cinq, non copiable, déplaçable.
6. Une coroutine prend ses paramètres **par valeur propriétaire** : un `const &` ou un
   `string_view` sur un temporaire est un use-after-free garanti dès la première suspension.
7. Le cadre est alloué sauf élision, jamais garantie : ici `-O1` et au-delà l'élident, `-O0` et
   UBSan non. Et pour un simple générateur, l'itérateur à la main reste deux fois plus rapide.

**Exercices : `14_coroutines`.**
