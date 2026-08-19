# 13 — Les threads, et ce que le modèle mémoire garantit

`notions/concurrence.md` a posé le vocabulaire : concurrence contre parallélisme, ne rien partager
d'abord, ThreadSanitizer dès la première ligne. Ce chapitre donne ce que le **C++** met derrière
ces mots — garanties de la norme, prix de la bibliothèque, et ce que cette implémentation-ci fait
vraiment. Référence : Apple M4, 10 cœurs (4 P, 6 E), Apple clang 21, libc++, arm64 macOS. Tout
chiffre écrit ici a été lancé.

## La course de données, définie exactement

Deux accès **au même emplacement mémoire**, depuis deux fils différents, dont **au moins un est une
écriture** et **au moins un n'est pas atomique**, sans **relation happens-before** entre eux. Deux
lectures ne courent pas, deux écritures ordonnées par un verrou non plus, et deux écritures
atomiques concurrentes non plus — c'est précisément à ça que servent les atomiques. Et le dernier
mot compte le plus : un tel programme a un **comportement indéfini**, pas « un résultat
imprévisible ». Indéfini au sens du chapitre 11 du cours de C : le compilateur suppose que ça
n'arrive pas. Voici l'attente artisanale, et ce qu'il en reste :

```cpp
bool pret = false;
int donnee = 0;
int attendre() { while (!pret) {} return donnee; }   // aucune synchronisation
```
```
__Z8attendrev:                       ; compile en -O2 : il n'y a plus de boucle du tout
	ldr	w0, [x8, _donnee@PAGEOFF]    ; charge donnee, et retourne
	ret
```

Aucun fil n'ayant le droit d'écrire `pret` sans synchronisation, sa valeur ne change pas, la boucle
est donc vide ou infinie, et le compilateur la supprime. Le même raisonnement écrase les
incréments : quatre fils faisant chacun `++nu` un million de fois sur un `long long` global se
compilent, à `-O2`, en un `ldr` / `add #1000000` / `str` par fil. À `-O0`, où la boucle survit :

| version, 4 fils × 1 000 000 | attendu | obtenu, trois lancements |
|---|---|---|
| `long long nu; ++nu;` | 4 000 000 | 1 135 110 / 1 145 709 / 1 111 775 |
| `std::atomic<long long> at; ++at;` | 4 000 000 | 4 000 000 / 4 000 000 / 4 000 000 |

Il manque les trois quarts. Et à `-O2` la même course rend souvent **exactement** 4 000 000 — non
par correction, mais parce que le compilateur l'a réduite à une opération par fil. **Un test qui
passe ne prouve rien** : c'est la propriété caractéristique du comportement indéfini.

## Lancer un fil

Un `std::thread` démarre à la construction, ses arguments sont **copiés ou déplacés** dans son
stockage — d'où `std::ref` pour une référence. Ensuite `join()`, ou `detach()`, qui rend dangereuse
toute capture par référence. **Un `std::thread` encore joignable au moment de sa destruction
appelle `std::terminate`** : la norme refuse de joindre comme de détacher en silence. Ici,
`libc++abi: terminating` et `SIGABRT`, code 134, le message dépendant de l'implémentation. C++20
ajoute `std::jthread`, dont le destructeur demande l'arrêt puis **joint**.

```cpp
std::jthread ouvrier([&tours](std::stop_token arret) {      // premier parametre special
    while (!arret.stop_requested()) { ++tours; }
});
ouvrier.request_stop();     // le destructeur le ferait aussi, avant de joindre
```

Le `stop_token` n'interrompt rien de force : il **demande**, le corps doit le consulter. Rien en
C++ ne tue un fil de l'extérieur, et c'est volontaire : un fil tué en pleine section critique
laisserait un verrou pris. `hardware_concurrency()` renvoie 10 ici, P plus E, mais la norme n'en
fait qu'un indice et autorise 0. Une pile de fil coûte **512 Kio** sur macOS contre 8 176 Kio pour
le principal ; sous glibc, `RLIMIT_STACK`, souvent 8 Mio.

## Le mutex et les gardes

`std::mutex` a `lock()` et `unlock()`, et **on ne les écrit jamais** : après une exception lancée
entre les deux, `try_lock()` renvoie faux — le verrou n'est jamais rendu, et tout demandeur
ultérieur attend pour toujours. Le RAII du chapitre 03 s'applique tel quel, avec quatre gardes.

| garde | depuis | ce qu'elle fait | `sizeof` ici |
|---|---|---|---|
| `std::lock_guard` | C++11 | verrouille, déverrouille, rien d'autre | 8 |
| `std::unique_lock` | C++11 | déverrouillable, différable, transférable | 16 |
| `std::scoped_lock` | C++17 | **zéro à N verrous d'un coup**, sans interblocage | 8 pour un, 16 pour deux |
| `std::shared_lock` | C++14 | prise partagée sur un `std::shared_mutex` | 16 |

`lock_guard` par défaut ; `unique_lock` seulement quand il faut relâcher avant la fin du bloc, ou
le passer à une `std::condition_variable` — qui exige la forme à prédicat, `cv.wait(verrou, [&]{
return !file.empty(); });`, seule immunisée contre les réveils intempestifs.

### L'interblocage

```cpp
std::mutex a, b;
// fil 1 : lock_guard g1(a); lock_guard g2(b);    // fil 2 : g1(b) puis g2(a), ordre inverse
```

Mesuré : le programme se fige **après 26 tours de boucle**. Ce n'est pas un cas rare qu'un test
finirait par attraper, c'est immédiat. Deux remèdes, sur le même programme lancé une seconde :

| version | résultat |
|---|---|
| `lock_guard` dans l'ordre croisé | bloqué après 26 tours |
| **ordre total** : les deux fils prennent `a` puis `b` | 67 354 878 tours, jamais bloqué |
| **`scoped_lock g(a, b);`**, même écrit `(b, a)` dans l'autre fil | 61 411 642 tours, jamais bloqué |

`scoped_lock` n'impose pas d'ordre : il applique l'algorithme de `std::lock`, qui essaie, relâche
tout et recommence dès qu'un verrou résiste — l'ordre des arguments est sans importance. **Un ordre
total documenté quand les verrous sont pris loin l'un de l'autre, `scoped_lock` sinon.**

## `std::atomic` : ce qui est atomique, et ce qui ne l'est pas

`std::atomic<T>` garantit que l'opération est **indivisible**, qu'aucune lecture ne voit un
demi-résultat, et par défaut qu'elle est ordonnée avec les autres opérations `seq_cst`. Ce qu'elle
ne garantit pas compte davantage : **enchaîner deux opérations atomiques ne donne pas une opération
atomique.** D'abord la lecture-puis-écriture, qui perd des incréments — 8 fils, 200 000 tours
chacun, 1 600 000 attendus, et la première ligne n'en rend que **186 935 à 315 468** selon le
lancement.

```cpp
p.store(p.load() + 1);       // DEUX operations. un autre fil s'intercale entre les deux.
p.fetch_add(1);              // UNE. c'est aussi ce que fait ++p
```

Ensuite le test-puis-modification : les deux opérations sont atomiques, et le code a l'air d'un
garde-fou. Mesuré sur 8 fils synchronisés par une barrière, une seule place, 20 000 tours :
**10 377 à 12 069 tours** où plus d'un fil est passé. Plus d'un tour sur deux.

```cpp
if (places.load() < 1) places.fetch_add(1);   // FAUX : la place est prise entre les deux
int vu = places.load(std::memory_order_relaxed);                   // la version correcte
while (vu < PLAFOND && !places.compare_exchange_weak(vu, vu + 1)) {}
```

Zéro dépassement sur 20 000 tours. `compare_exchange_weak(attendu, voulu)` écrit `voulu` **si et
seulement si** la valeur courante est `attendu` ; sinon il écrase `attendu` par ce qu'il a trouvé
et rend faux, ce qui recharge la boucle gratuitement. `_weak` peut échouer sans raison, sans
importance en boucle ; `_strong` sert hors boucle. Piège : la comparaison porte sur les **bits**,
pas sur `operator==` — attendre `+0.0` sur un `std::atomic<double>` échoue face à un `-0.0` stocké.

## `is_always_lock_free`, et le verrou en douce

`std::atomic<T>` accepte tout type trivialement copiable. Au-delà de ce que le processeur sait
faire d'un coup, la bibliothèque **prend un verrou**, sans que rien dans le code ne le montre.

| `T` | `sizeof(T)` | `sizeof(atomic<T>)` | `is_always_lock_free` | `is_lock_free()` |
|---|---|---|---|---|
| `bool`, `int`, `long`, `void *`, `double`, `long double` | 1 à 8 | idem | oui | oui |
| `struct { int a, b, c; }` | 12 | **16** | oui | oui |
| `struct { long a, b; }` | 16 | 16 | oui | oui |
| `struct { long a, b, c; }` | 24 | 24 | **non** | non |

Le type de 12 octets est **rembourré à 16**, taille que le processeur traite d'un coup ; sur arm64
la limite est 16 octets ; sur x86-64 elle reste à 8 tant que la cible n'a pas `cmpxchg16b`, ce qui
est le cas de la ligne de base générique de Linux mais pas de celle de macOS, où 16 octets sont
déjà sans verrou. Et le type de
24 octets ne grossit pas : le verrou n'est pas dedans, la compilation le dit —

```
	mov	w0, #24
	bl	___atomic_store    ; appel a la bibliotheque d'execution, qui prend un verrou
```

Le prix est cohérent : 4,9 ns par écriture d'un `atomic<24 octets>` contre 1,6 ns pour un
`fetch_add` sur `atomic<long>`, l'ordre de grandeur d'un `lock_guard` non contendu : c'en est un.
`is_always_lock_free` est `constexpr`, donc bon pour un `static_assert`. Note libc++ :
`std::atomic<std::shared_ptr<T>>` de C++20 **n'est pas implémenté** ici : la compilation échoue sur
`requires that 'T' be a trivially copyable type`.

## Le modèle mémoire

Le défaut, `memory_order_seq_cst`, donne la **cohérence séquentielle** : toutes les opérations
`seq_cst` du programme s'insèrent dans un unique ordre total que tous les fils voient pareillement.
Une opération `relaxed` ou `acquire`, elle, n'entre pas dans cet ordre.
Seul modèle conforme à l'intuition, d'où le défaut. Les autres : **`release` sur une écriture,
`acquire` sur la lecture correspondante**, le passage de témoin — tout ce que l'écrivain a fait
**avant** son `store(release)` est visible pour le lecteur **après** son `load(acquire)` qui voit
cette valeur, soit un happens-before fait main ; **`relaxed`**, atomique et rien de plus, bon pour
un compteur dont personne ne lit l'ordre et faux dès qu'il signale une donnée prête ;
**`acq_rel`**, les deux rôles.

| opération | `relaxed` | `acquire` / `release` | `seq_cst` |
|---|---|---|---|
| charger | `ldr` | `ldapr` | `ldar` |
| ranger | `str` | `stlr` | `stlr` |
| `fetch_add` | `ldadd` | `ldadda` / `ldaddl` | `ldaddal` |

Sur x86-64, la même compilation donne `movl` pour **charge relaxed, charge acquire, charge seq_cst
et rangement release** : le matériel ordonne déjà tout ça, seul le rangement `seq_cst` diffère
(`xchgl`) et `fetch_add` est `lock incl` partout. **C'est pourquoi un code faux en `relaxed`
traverse des années de production sur x86-64 et casse au premier portage sur arm64.**

### Le motif drapeau plus donnée

```cpp
// producteur                          // consommateur
donnee.store(42, relaxed);             while (pret.load(acquire) == 0) {}
pret.store(1, release);                lire(donnee.load(relaxed));
```

L'écriture de `donnee` est **avant** le release, donc visible après l'acquire : elle n'a pas besoin
d'ordre. En `relaxed` des deux côtés, le drapeau peut devenir visible avant la donnée. À gauche un
million de tours du motif ; à droite, sur 500 000 tours, le test qui sépare `acquire`/`release` de
`seq_cst` — deux fils font « j'écris ma variable, je lis celle de l'autre », et les voir toutes
deux à zéro est impossible dans un ordre total, possible sinon.

| ordre | drapeau levé, donnée encore à 0 | et : les deux lectures à zéro |
|---|---|---|
| `relaxed` | **5 à 274** sur 1 082 à 10 027 tours levés | **480 098 à 480 788** fois |
| `relaxed` + `atomic_thread_fence(seq_cst)` | — | 0 |
| `release` / `acquire` | **34 et 48** sur 2 lancements sur 10 | 0 |
| `seq_cst` | 0 sur 213 461 à 244 358 tours levés | 0 |

Regarde bien l'avant-dernière ligne : le **modèle** C++ autorise (0,0) en `release`/`acquire`, qui
n'ordonne pas un rangement suivi d'une charge — et sur cette machine, ça **se voit**. Huit
lancements sur dix ne montrent rien, deux en montrent trente à cinquante. Le `ldapr` que clang émet
est un acquire RCpc : il n'ordonne pas le `stlr` qui le précède. C'est la meilleure illustration
possible de la règle : **ne conclus jamais de la mesure à la garantie**, dans un sens comme dans
l'autre. Règle de survie : `seq_cst` par défaut, `relaxed` sur un compteur pur, une paire
`acquire`/`release` pour un passage de témoin que tu peux écrire au tableau.

## Les outils de C++20

| outil | en-tête | ce qu'il résout | réutilisable |
|---|---|---|---|
| `std::latch` | `<latch>` | attendre que N tâches aient fini, une fois | non |
| `std::barrier` | `<barrier>` | resynchroniser N fils à chaque phase, avec action de fin | oui |
| `std::counting_semaphore<N>` | `<semaphore>` | limiter à N les fils dans une zone | oui |
| `atomic::wait` / `notify_one` | `<atomic>` | dormir jusqu'à ce qu'une valeur change, sans mutex | oui |

Vérifié à l'exécution : le `latch` libère après les 4 `count_down()` ; la fonction de fin d'un
`barrier(3, ...)` tourne une fois par phase, cinq pour cinq `arrive_and_wait()` ; un
`counting_semaphore<3>` lâché sur 12 fils n'en laisse jamais plus de **3** dans la zone ;
`feu.wait(0)` rend la main dès qu'un autre fil écrit puis appelle `notify_one()`. Tous minuscules :
8 octets pour `latch` et `counting_semaphore`, 40 pour `barrier<>`, contre 64 pour un `std::mutex`.
`atomic::wait` remplace l'attente active.

## `std::async`, `std::future`, et l'exception qui traverse un fil

**Une exception ne peut pas remonter d'un `std::thread`.** Le `try` autour de la construction
n'attrape rien : le fil meurt sur `std::terminate`, et le processus avec — `terminating due to
uncaught exception of type std::runtime_error`, code 134. `std::future` stocke le résultat **ou**
l'exception et le ressort au `get()`. Trois producteurs : `std::async`, qui lance le travail ;
`std::packaged_task`, qu'on donne à un `std::thread` ou à une file pour choisir où elle tourne ;
`std::promise`, où l'on pose la valeur depuis un rappel.

```cpp
std::future<int> f = std::async(std::launch::async, calcul, -1);
try { f.get(); } catch (const std::invalid_argument &e) { /* relancee dans le fil appelant */ }
```

Quatre pièges vérifiés. **`std::async` sans politique explicite** peut choisir `deferred`, auquel
cas rien ne tourne avant le `get()` : écris toujours `std::launch::async`. **Le destructeur du
`future` rendu par `async` bloque** jusqu'à la fin de la tâche — un `std::async(...)` dont on
ignore le retour a mis 200 ms à sortir du bloc. **`get()` ne se fait qu'une fois** : ensuite
`valid()` est faux, et un second `get()` est indéfini — ici une segmentation, pas une exception.
**`std::shared_future`**, elle, se relit.

## Ce que ça coûte

Mesures à `-O2`, sans sanitizer, non contendues sauf mention.

| opération | coût | remarque |
|---|---|---|
| `atomic` charge, `relaxed` ou `seq_cst` | 0,24 ns | un `ldr` ou un `ldapr` : presque gratuit |
| `atomic` `fetch_add`, `relaxed` | 1,60 ns | environ 6 cycles à 3,94 GHz |
| `atomic` `fetch_add`, `seq_cst` | 1,60 ns | **le même** : `ldaddal` ne coûte rien de plus ici |
| `mutex` `lock` + `unlock` | 4,2 ns | non contendu, jamais d'appel système |
| `atomic<24 octets>` écriture | 4,9 ns | le verrou caché de la section précédente |
| `shared_mutex` `lock_shared` | 9,2 ns | plus cher qu'un `mutex` : réserve-le aux lectures longues |
| création + `join` d'un `std::thread` | **14,3 µs** | 11,1 µs via `std::async` ; 3 400 `lock`/`unlock` |

**Un fil coûte quatorze microsecondes à créer** : toute tâche plus courte perd à être parallélisée,
d'où les groupes de fils persistants. Sous contention, 8 fils et 5 000 000 d'incréments chacun sur
**le même** compteur : `std::atomic` partagé en `relaxed`, 795 à 852 ms ; `std::mutex` partagé, 690
à 706 ms ; un compteur **local par fil** additionné à la fin, **8 à 10 ms**. Quatre-vingts fois
moins cher, et le mutex bat l'atomique dès qu'il y a foule : un fil qui attend dort. Rappel du
chapitre 09, revérifié sur 4 compteurs atomiques **distincts** : voisins à 8 octets, 43 à 141 ms ;
`alignas(64)`, 9 ms ; `alignas(128)`, 9 ms. Facteur 15, et ici `alignas(64)` suffit — le premier
chiffre mesuré est toujours le plus haut, méfie-toi de la première boucle chronométrée.
**La seule optimisation qui gagne à tous les coups reste de ne pas partager.**

## Le seul outil qui voit les courses

Aucune des mesures de ce chapitre n'était une détection : c'étaient des symptômes. Le seul outil
qui **voit** la course est **ThreadSanitizer**, `-fsanitize=thread`. Il instrumente chaque accès,
tient un vecteur d'horloges par fil, et signale toute paire d'accès sans happens-before même si
l'entrelacement fautif n'a pas eu lieu ce jour-là ; sur le compteur de la première section, il
nomme le fil, la ligne, la variable et le lieu de création. Il est **incompatible avec
AddressSanitizer** : les deux réimplantent la même chose — disposition de la mémoire virtuelle,
interception des allocations, carte des octets valides. Le pilote refuse.

```
clang++: error: invalid argument '-fsanitize=address' not allowed with '-fsanitize=thread'
```

Le runner de `cpplings` compile avec `-fsanitize=address,undefined`, qui attrape les débordements
et dépassements d'entier des autres chapitres. **Aucune course ne sera donc signalée par un outil
dans ces exercices** : ils la rendent visible par un **chiffre** — un compteur qui manque, un
plafond dépassé, une donnée lue à zéro. Coût de TSan : 4,22 ns par `lock`/`unlock` sans sanitizer,
4,22 ns avec `-fsanitize=address,undefined`, **65 ns** avec `-fsanitize=thread`, soit ×15. Outil de
développement, sur une compilation séparée.

## À retenir

1. Une course est un comportement indéfini : le compilateur supprime l'attente, écrase l'incrément.
2. Un `std::thread` joignable détruit abandonne le programme ; `std::jthread` joint tout seul.
3. Jamais de `lock`/`unlock` à la main : `lock_guard` par défaut, `scoped_lock` pour plusieurs.
4. Deux opérations atomiques n'en font pas une : `fetch_add`, `compare_exchange` s'il y a un test.
5. Au-delà de 16 octets sur arm64, `std::atomic` cache un verrou : lis `is_always_lock_free`.
6. `seq_cst` par défaut, `relaxed` pour un compteur pur, `acquire`/`release` pour un passage de
   témoin : sur x86-64 la différence ne se voit pas, sur arm64 elle se compte en milliers d'écarts.
7. Un fil coûte 14 µs à créer, un compteur partagé quatre-vingts fois un compteur local : ne
   partage pas, et si tu partages, aligne sur une ligne de cache.

**Exercices : `13_threads`.**
