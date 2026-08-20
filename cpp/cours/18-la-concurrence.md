# 18 — La concurrence, au-delà des threads

Le chapitre 13 a posé le socle : `std::thread`, `std::jthread`, `std::mutex`, `std::atomic`, la
définition exacte d'une course ; celui-ci part de là sans le répéter. Tout ce qui est dit
« mesuré » vient de la machine de référence — arm64 macOS 27, Apple clang 21, libc++ 220106, dix
cœurs, `-std=c++23 -O2` — car la norme décrit un modèle, jamais une durée ni une fréquence.

## Les ordonnancements mémoire, motif par motif

**`relaxed`, pour un compteur.** Il garantit l'atomicité et l'ordre total des modifications **de
cet objet-là**, rien de plus — surtout aucun ordre avec **une autre** variable. Vérifié : huit
fils, 200 000 `fetch_add(relaxed)` chacun, 1 600 000 billets distincts, zéro manquant.

**`acquire`/`release`, pour un passage de témoin.** Le producteur écrit `donnee` en `relaxed` puis
`pret.store(i, release)` ; le consommateur fait `pret.load(acquire)` puis lit `donnee` en
`relaxed`. Le `store(release)` publie tout ce que le fil a écrit avant lui, et le `load(acquire)`
qui **voit cette valeur** en hérite — à deux conditions : le **même** objet des deux côtés, et un
lecteur qui lit bien la valeur écrite. **Le contre-exemple**, le même code en `relaxed` partout,
donnée et drapeau dans deux lignes de cache distinctes, 20 000 000 de tours :

| ordre du drapeau | drapeau déjà levé, donnée encore en retard |
|---|---|
| `relaxed` / `relaxed` | **243 043**, **665 645**, **841 588** fois sur 15 à 17 M de lectures |
| `release` / `acquire` | 0 |
| `seq_cst` / `seq_cst` | 0 |

Ce que ça casse tient à deux niveaux. **Le matériel** : sans arête happens-before, le cœur lecteur
voit un drapeau plus récent que la donnée annoncée. **Le compilateur** : si `donnee` n'était pas
atomique, ces accès seraient une course, donc un comportement indéfini, et il pourrait remonter
l'écriture après le drapeau. Le premier se mesure, le second s'autorise.

**`seq_cst`, pour ce dont on ne veut pas raisonner.** Ordre total unique sur toutes les opérations
`seq_cst` du programme. Le motif qui le réclame, et que `acquire`/`release` ne couvre pas : deux
fils écrivent chacun leur variable puis lisent celle de l'autre, et les voir toutes deux à zéro est
impossible dans un ordre total, autorisé sinon. Ce n'est pas le seul — tout raisonnement qui exige
que plusieurs fils s'accordent sur un **même ordre** entre des variables indépendantes est dans le
même cas. Le chapitre 13 l'a observé ici ; mon banc n'a rien vu sur
2 000 000 de tours, et mon premier banc du drapeau non plus. **C'est le modèle qui décide :
l'absence de mesure ne prouve rien.**

Le coût ne départage pas : non contendus, charge et rangement coûtent 0,24 ns quel que soit l'ordre
et `fetch_add` 1,59 ns ; à huit fils contendus, 289 ms en `relaxed` contre 285 en `seq_cst`. **Ici
c'est un choix de correction, pas de vitesse.**

## `compare_exchange` : la boucle

```cpp
long ancien = compteur.load(std::memory_order_relaxed), neuf;
do {
    neuf = calcul(ancien);                                      // recalcule a chaque tour
} while (!compteur.compare_exchange_weak(ancien, neuf, std::memory_order_release,   // succes
                                         std::memory_order_relaxed));               // echec
```

**En cas d'échec, `ancien` est écrasé par la valeur courante** — vérifié : un CAS d'attendu 99 sur
une variable valant 6 rend `false` et laisse `attendu` à 6, d'où pas de `load` de plus. **Le
candidat se recalcule dans la boucle** ; le calculer avant est le bug classique. Le second
ordonnancement est celui de la charge faite quand la comparaison échoue : la norme y interdit
`release` et `acq_rel` (ici, simple avertissement `diagnose_if`).

**`weak` ou `strong`.** `strong` n'échoue que si la comparaison échoue vraiment ; `weak` peut aussi
échouer alors que la valeur correspondait. Sans instruction CAS unique, le compilateur émet une
paire charge-exclusive / rangement-exclusif dont le second échoue si quoi que ce soit a touché la
réservation : `weak` compile en `ldaxr` puis `stlxr` et sort, `strong` ajoute un `cbnz` qui
reboucle. Mesuré, 5 000 000 de CAS avec la bonne valeur attendue, personne d'autre sur la cible :

| construction | seul | avec une voisine martelée dans la même ligne |
|---|---|---|
| `weak`, sans LSE | 7 à 17 échecs | 47 à 841 échecs |
| `strong`, sans LSE | 0 | 0 |
| `weak` **et** `strong`, avec LSE (défaut) | 0 | 0 |

À qualifier : **sur cette cible**, Apple arm64 avec LSE par défaut, clang émet `casal` des deux
côtés — une instruction unique, donc zéro échec parasite et un code **identique** ; la différence
n'apparaît qu'avec `-Xclang -target-feature -Xclang -lse` ou sur armv8.0. D'où la règle : **`weak`
dès qu'il y a une boucle**, qui traite déjà l'échec ; **`strong` sans boucle**, tentative unique.

## Une pile sans verrou, en insertion seule

```cpp
class PileEmpilable {
  public:
    struct Noeud { int valeur; Noeud *suivant; };
    void empiler(Noeud *neuf) noexcept {
        neuf->suivant = tete_.load(std::memory_order_relaxed);
        // l argument attendu EST neuf->suivant : l echec le rafraichit tout seul
        while (!tete_.compare_exchange_weak(neuf->suivant, neuf, std::memory_order_release,
                                            std::memory_order_relaxed)) {}
    }
    Noeud *saisir_tout() noexcept { return tete_.exchange(nullptr, std::memory_order_acquire); }
  private:
    std::atomic<Noeud *> tete_{nullptr};
};
```

Ce qui la rend faisable, et manquera au retrait : **un seul mot partagé**, `tete_`, le reste
s'écrivant sur un nœud qu'aucun autre fil ne peut atteindre ; **aucun déréférencement d'un pointeur
d'autrui** ; **une publication en une opération atomique**, `std::atomic<Noeud *>` faisant 8 octets
et lock-free ici. Vérifié : 800 000 nœuds retrouvés, zéro doublon, TSan silencieux.

## Le retrait sans verrou : ABA, un autre métier

Le retrait naïf lit `tete` puis boucle sur `compare_exchange_weak(vieux, vieux->suivant, ...)`, et
il tient deux bugs en deux lignes : `vieux->suivant` déréférence un nœud qu'un autre fil a
peut-être déjà dépilé et libéré, et même sans libération, **le CAS compare une adresse, pas une
identité**. Reproduction déterministe — fil A mis en pause entre sa lecture et son CAS, fil B
libéré par un drapeau, départ `A -> B -> C` :

| étape | fil A | fil B | pile |
|---|---|---|---|
| 1 | lit `tete` = A, lit `A->suivant` = B | | A B C |
| 2 | *en pause* | dépile A | B C |
| 3 | *en pause* | dépile B | C |
| 4 | *en pause* | rempile A, donc `A->suivant` = C | A C |
| 5 | CAS(`tete`, A vers B) : **réussit** | | **B C** |

Le programme imprime `pile finale : B C` et rend A au fil A. Les dégâts : **A rendu deux fois** —
double `delete` si les nœuds venaient du tas — et **B, que le fil B possède déjà, revenu en tête**
avec un `suivant` périmé. Le CAS n'a pas fauté : `tete` valait bien A, mais pas le **même** A.
Trois parades, aucune gratuite. **L'étiquette** : la tête devient `{pointeur, compteur}`,
incrémenté à chaque modification, et le CAS porte sur la paire — `std::atomic<Tete>` de 16 octets
est `is_always_lock_free` ici et compile en un `caspal` unique ; rejoué, le CAS du fil A
**échoue**, pile `A C`. **La réclamation différée** : ne jamais recycler un nœud tant qu'un fil
peut pointer dessus, par pointeurs de danger, époques ou RCU — C++26 ajoute `<hazard_pointer>` et
`<rcu>`, **aucun des deux n'existe ici**. **Le comptage de références** :
`std::atomic<std::shared_ptr<T>>` (C++20) rend « lire le pointeur et incrémenter son compteur »
atomique, mais il est **absent d'ici aussi**, faute de spécialisation partielle.

Franchement, ce n'est pas un exercice de débutant : il faut prouver l'absence d'ABA, savoir quand
un nœud peut être rendu à l'allocateur, et tester avec un outil que le runner ne peut pas charger.
La décision par défaut reste un `std::mutex`.

## L'initialisation une seule fois

Une variable locale `static` est construite au premier passage, une seule fois, tous fils
confondus : garantie **normative** depuis C++11. Ce que clang émet à `-O2` pour
`static Lourd unique;` est **un double verrouillage écrit par le compilateur** :

```
    ldaprb  w8, [x8]         ; x8 = adresse de l octet de garde ; charge ACQUIRE
    tbz     w8, #0, LBB0_2   ; bit 0 a ZERO ? pas encore construit, on saute au chemin lent
LBB0_1:                      ; sinon on tombe ici : adresse de l objet, puis ret
LBB0_2:                      ; ___cxa_guard_acquire, constructeur, ___cxa_guard_release
```

Vérifié : 16 fils, 1 000 appels chacun, une seule construction par point d'initialisation, chemin
rapide à 0,262 ns, TSan silencieux ; si le constructeur lance, l'initialisation compte pour non
faite et le fil suivant réessaie. `std::call_once(drapeau, [] { ... })` couvre ce qui n'est pas une
variable locale : lambda appelée **une** fois, 0,299 à 0,361 ns.

```cpp
// le double verrouillage ecrit a la main, faux sans atomique :
Lourd *instance_ptr = nullptr;               // PAS atomique : c est tout le bug
if (instance_ptr == nullptr) {               // lecture HORS du verrou
    std::lock_guard g(verrou);
    if (instance_ptr == nullptr) instance_ptr = new Lourd;      // ecriture SOUS le verrou
}
return instance_ptr;
```

Deux accès au même objet non atomique, l'un écrivant, sans happens-before : la définition d'une
course, donc un comportement indéfini. TSan la nomme :
`Read of size 8 ... by thread T2: dcl.cpp:14` contre
`Previous write ... by thread T1 (mutexes: write M0): dcl.cpp:16`. Rien n'ordonne la construction
de l'objet et la publication du pointeur, donc un fil peut voir un pointeur non nul vers un objet
pas construit. La correction tient en un type — `std::atomic<Lourd *>`, `load(acquire)` hors du
verrou, `load(relaxed)` dedans, `store(release)` après le `new` — soit ce que le `static` local
donne déjà.

## Les outils de coordination de C++20

| outil | en-tête | ce qu'il résout | réutilisable | taille |
|---|---|---|---|---|
| `std::latch` | `<latch>` | attendre que N évènements soient arrivés, **une** fois | non | 8 o |
| `std::barrier` | `<barrier>` | resynchroniser N fils à **chaque** phase, avec action de fin | oui | 40 o |
| `std::counting_semaphore<N>` | `<semaphore>` | ne laisser que N fils à la fois dans une zone | oui | 8 o |
| `atomic::wait` / `notify` | `<atomic>` | dormir jusqu'à ce qu'**un mot** change | oui | 0 o |

Quatre fragments, et **quatre programmes différents** : chacun suppose le nombre de fils annoncé
par son compteur. Recopiés à la suite dans un `main` monofil, les trois derniers se bloquent pour
toujours — un `barrier{3}` où un seul fil arrive attend les deux autres jusqu'à la fin des temps.

Le `latch`, compteur à usage unique, ici avec un patron et quatre ouvriers :

```cpp
std::latch depart{1}, arrivee{4};
for (int i = 0; i < 4; ++i)
    fils.emplace_back([&] { depart.wait(); travailler(); arrivee.count_down(); });
depart.count_down();                                   // les quatre partent ensemble
arrivee.wait();                                        // le patron attend les quatre
```

La `barrier`, réutilisable, avec une action de fin de phase — les deux lignes vivent dans **chacun
des trois** fils :

```cpp
std::barrier barriere{3, []() noexcept { fin_de_phase(); }};
for (int p = 0; p < 5; ++p) barriere.arrive_and_wait();
```

Le sémaphore compteur, N places prises et rendues, dans chaque fil :

```cpp
std::counting_semaphore<3> jetons{3};
jetons.acquire();  /* au plus trois fils ici */  jetons.release();
```

Et `atomic::wait`, pour dormir sur un mot sans mutex — le `wait` dans le dormeur, les deux autres
lignes dans le réveilleur, sans quoi personne ne réveille personne :

```cpp
std::atomic<int> feu{0};
feu.wait(0);                                                    // fil dormeur
feu.store(1, std::memory_order_release); feu.notify_one();      // fil reveilleur
```

Tailles mesurées ici, contre 64 o pour `std::mutex` et 48 pour `std::condition_variable`. Le
`latch` : `try_wait()` rend `false` avant, `true` après, et il ne se remet pas à zéro. Le
`barrier` : fonction de fin appelée **5** fois pour 5 phases et 3 fils, une par phase et non une
par arrivée ; la norme la veut `noexcept`, cette libc++ ne le vérifie pas. Le sémaphore : 12 fils,
2 000 tours, maximum observé dans la zone = **3**, jamais 4, et il **n'appartient à personne** —
tout fil peut le rendre, d'où un passage de main et non une protection d'invariant.
`atomic::wait` : il peut rendre la main sans raison, donc toujours dans une boucle, et sur 200 000
allers-retours il coûte **0,06 à 0,14 µs** contre **2,28 µs** au mutex plus condition variable.

## Un pool de tâches minimal

```cpp
class Pool {
  public:
    explicit Pool(unsigned n) { while (n--) fils_.emplace_back([this] { boucle(); }); }
    ~Pool() {
        { std::lock_guard garde(verrou_); arret_ = true; }   // annonce SOUS le verrou
        reveil_.notify_all();                                // reveil HORS du verrou
    }
    template <typename F> auto soumettre(F fonction) -> std::future<std::invoke_result_t<F>> {
        std::packaged_task<std::invoke_result_t<F>()> tache(std::move(fonction));
        auto futur = tache.get_future();
        {
            std::lock_guard garde(verrou_);
            if (arret_) throw std::runtime_error("pool arrete");
            file_.emplace(std::move(tache));
        }
        reveil_.notify_one();
        return futur;
    }
  private:
    void boucle() {
        for (;;) {
            std::packaged_task<void()> tache;
            {
                std::unique_lock garde(verrou_);
                reveil_.wait(garde, [this] { return arret_ || !file_.empty(); });
                if (file_.empty()) return;      // arret demande ET file videe
                tache = std::move(file_.front()); file_.pop();
            }
            tache();                            // hors du verrou : le pool reste ouvert
        }
    }
    std::mutex verrou_;
    std::condition_variable reveil_;
    std::queue<std::packaged_task<void()>> file_;
    bool arret_ = false;
    std::vector<std::jthread> fils_;            // DERNIER : detruit en premier, joint en premier
};
```

Lancé : 10 000 tâches, somme 50 005 000 comme attendu, somme des carrés 333 383 335 000, et un
`std::domain_error` lancé dans une tâche remonte au `get()` de l'appelant — propre sous les options
du runner comme sous ThreadSanitizer.

**L'ordre de déclaration.** Les membres sont détruits dans l'ordre inverse de la déclaration :
`fils_` en dernier veut dire détruit en premier, donc les `jthread` sont joints **avant** que
mutex, condition et file ne disparaissent. Remonte `fils_` en tête et l'objet se démonte sous les
ouvriers : **4 plantages sur 5**, code 134, `mutex lock failed: Invalid argument`.

**Le protocole d'arrêt.** `arret_` s'écrit sous le verrou, `notify_all()` après l'avoir relâché ;
le prédicat `arret_ || !file_.empty()` et la sortie sur `file_.empty()` font qu'un ouvrier réveillé
**vide ce qui reste** avant de partir. Pour jeter les tâches en attente on testerait `arret_`
d'abord, mais dis-le dans l'API : un appelant qui garde un `future` attendrait sinon.

**Le type de tâche.** `std::move_only_function` (C++23) est **absent de cette libc++** et
`std::function` exige un appelable copiable, ce qu'un `packaged_task` n'est pas : la file tient des
`std::packaged_task<void()>` enveloppant celui de l'appelant, qui porte résultat et exception.

## Ce qui ne se teste pas

Prends la course la plus petite possible : `bool pret` et `int constructions` globaux, huit fils
qui font chacun une fois `if (!pret) { pret = true; ++constructions; }`. **1 500 lancements** :
`constructions != 1` **zéro** fois, la suite est verte. Sous TSan, **un seul** lancement suffit.

La raison de l'écart : un test observe **un** entrelacement parmi un nombre astronomique, et lequel
dépend de l'ordonnanceur, de la charge, des fréquences, des options ; rien ne le rejoue. TSan
n'observe pas d'entrelacement — il tient une horloge vectorielle par fil et une trace par mot
mémoire, et signale **toute paire d'accès au même mot, dont au moins un écrit, sans happens-before
entre eux** : l'entrelacement fautif n'a pas besoin d'avoir eu lieu, il suffit que les deux accès
aient été exécutés. D'où sa limite exacte, **TSan ne voit rien de ce qui ne s'exécute pas** :
l'incrément derrière un branchement jamais pris donne 0 avertissement contre 1. Prix sur le pool :
0,02 s à 0,09 s, 3,9 à 69,7 Mo.

Enfin, la contrainte qui concerne ce cours : TSan et AddressSanitizer réimplantent tous deux la
disposition de la mémoire virtuelle et l'interception des allocations, et le pilote refuse —
`clang++: error: invalid argument '-fsanitize=address' not allowed with '-fsanitize=thread'`. Le
runner de `cpplings` compile avec `-fsanitize=address,undefined`, donc **aucun exercice de cette
section ne verra une course signalée par un outil** : ils la rendent visible par un **chiffre**, un
compteur qui manque, un plafond dépassé, un nœud rendu deux fois. Lis le nombre qui cloche.

## À retenir

1. `relaxed` garantit l'atomicité et l'ordre total des modifications **d'un seul objet** :
   1 600 000 billets distincts, mais un drapeau en `relaxed` a laissé voir la donnée en retard
   841 588 fois.
2. `seq_cst` par défaut, `acquire`/`release` quand tu peux nommer la paire et son lecteur : non
   contendus les trois ordres coûtent le même temps ici, c'est un choix de correction.
3. `weak` dans une boucle, `strong` sans boucle. Avec LSE les deux donnent `casal` et zéro échec
   parasite ; sans LSE `weak` échoue à vide 7 à 841 fois sur 5 000 000, `strong` cache une boucle.
4. L'insertion sans verrou marche parce qu'un seul mot est partagé et qu'aucun pointeur d'autrui
   n'est déréférencé : 800 000 nœuds, zéro doublon, TSan silencieux.
5. Le retrait est un autre métier : un CAS compare une adresse, pas une identité. Étiquette,
   réclamation différée ou comptage de références — les trois manquent en partie à cette libc++.
6. La variable locale `static` **est** un double verrouillage correct écrit par le compilateur :
   `ldaprb` sur la garde puis `__cxa_guard_acquire`. À la main sans atomique, c'est une course.
7. Un pool correct tient à trois détails : `fils_` déclaré en dernier — sinon 4 plantages sur 5 —,
   l'arrêt annoncé sous le verrou et notifié hors du verrou, et une file de callables déplaçables.

**Exercices : `18_concurrence`.**
