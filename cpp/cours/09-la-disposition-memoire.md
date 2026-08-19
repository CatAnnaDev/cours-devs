# 09 — La disposition mémoire, le cache, et ce qu'on paie à chaque accès

Le chapitre 08 du cours de C a montré comment les champs d'une structure se rangent en mémoire :
alignement, remplissage, et le tri du plus exigeant au moins exigeant qu'il faut faire soi-même,
le compilateur n'ayant pas le droit de réordonner. Ce chapitre-ci répond à la
question qu'il laissait ouverte — **pourquoi est-ce que ça change le temps d'exécution ?** Tout ce
qui est dit « mesuré » vient de la machine de référence : Apple M4, Apple clang 21, libc++, arm64.

## La hiérarchie mémoire, en chiffres

Les tailles du M4 viennent de `sysctl hw.perflevel0` ; ses latences d'un parcours de pointeurs en
ordre aléatoire, où chaque accès dépend du précédent — rien ne se recouvre, on mesure une latence
et pas un débit. Sa fréquence, mesurée par une chaîne d'additions dépendantes, est de 3,94 GHz,
soit 0,25 ns par cycle. Les valeurs x86-64 ne sont **pas** mesurées ici : ce sont les ordres de
grandeur publics des cœurs récents (Zen 4 chez AMD, Golden Cove chez Intel).

| Niveau | M4 : taille | M4 : latence mesurée | x86-64 : taille | x86-64 : latence |
|---|---|---|---|---|
| registre | 31 × 64 bits | 0 | 16 × 64 bits | 0 |
| L1 données (cœurs P) | 128 Kio | 0,93 ns — ~4 cycles | 32 à 48 Kio | ~1 ns — 4 à 5 cycles |
| L2 (cluster P) | 16 Mio pour 4 cœurs | 5,0 à 8,4 ns — ~20 à 33 | 1 à 2 Mio par cœur | ~4 ns — 14 à 16 |
| L3 ou cache système | non exposé par `sysctl` | 18 ns au bord du L2 — ~70 | 16 à 32 Mio partagés | ~15 ns — 40 à 60 |
| RAM | — | 92 à 99 ns — ~360 à 390 | — | 70 à 100 ns — 250 à 400 |

Apple Silicon n'a pas de L3 par cœur : au-delà du L2 il y a un cache système partagé avec le GPU,
d'où la ligne mesurée mais non nommée. Une addition entière coûte un cycle et le cœur en lance
plusieurs par cycle ; un accès qui rate tous les niveaux en coûte 350 à 400. **Le processeur
exécute plus de mille instructions dans le temps qu'il met à recevoir un octet de la RAM.** Sur
tout code qui touche à des données, **ce qui coûte n'est plus le calcul, c'est d'aller chercher les
octets**, et il arrive régulièrement qu'ajouter du calcul pour éviter un accès soit gagnant.

## La ligne de cache

Le cache ne stocke pas des octets, il stocke des **lignes**. Lire un seul `char` en RAM fait
remonter toute la ligne qui le contient, à tous les niveaux ; écrire un seul octet salit toute la
ligne. Elle fait **64 octets sur x86-64** chez Intel comme chez AMD, et **128 sur Apple Silicon** —
`sysctl hw.cachelinesize` renvoie 128 sur cette machine, qui y range donc 32 `int`. Une boucle qui
lit un `int` sur deux touche exactement autant de lignes qu'une boucle qui les lit tous : elle ne
va pas deux fois plus vite. Une boucle qui lit un `int` tous les 128 octets paie une ligne par
élément, trente-deux fois plus. **La seule métrique qui compte pour une boucle sur des données,
c'est le nombre de lignes distinctes touchées** — ni le nombre d'octets utiles, ni le nombre
d'instructions.

## Ce que le C++ ajoute au C sur la disposition

### `alignof` et `alignas`

`alignof(T)` donne l'alignement requis, `alignas(N)` l'impose. Le C les a aussi depuis C11 ; le C++
en fait des mots-clés depuis C++11 et s'en sert dans sa bibliothèque.

```cpp
alignof(int)         // 4        alignof(double) : 8       alignof(void *) : 8
alignof(long double) // 8 sur arm64 macOS, 16 sur x86-64 — comme std::max_align_t
```

**`alignas` ne peut qu'augmenter.** `alignas(1) double d;` ne compile pas : *requested alignment is
less than minimum alignment of 8 for type 'double'*. Rien de portable ne permet de demander moins
que l'alignement naturel — `#pragma pack` est une extension hors norme, qui produit des accès non
alignés. Et **un `alignas` sur un membre pousse la taille de toute la structure**, puisque la
taille reste un multiple de l'alignement : `struct { char c; alignas(64) int n; }` a un alignement
de 64, met `n` au décalage 64, et fait 128 octets.

Depuis C++17 un type sur-aligné est correctement alloué : `new Bloc` avec `alignas(128)` renvoie
une adresse multiple de 128, et `std::vector<Bloc>` aussi. `__STDCPP_DEFAULT_NEW_ALIGNMENT__` vaut
16 sur les deux ABI ; au-delà, le compilateur appelle la surcharge prenant `std::align_val_t`.

### La disposition standard

Héritage, niveaux d'accès et fonctions virtuelles peuvent empêcher le compilateur de ranger les
champs comme le ferait une `struct` C. `std::is_standard_layout_v<T>` répond à la question « cette
classe a-t-elle la disposition qu'aurait la structure C équivalente ? ».

```cpp
struct MemeAcces  { int a; int b; };                    // disposition standard
struct MixteAcces { public: int a; private: int b; };   // NON : deux niveaux d'accès
struct Virtuelle  { int a; virtual ~Virtuelle(); };     // NON : fonction virtuelle
```

Ce que ça achète : `offsetof` est valide — sinon clang émet `-Winvalid-offsetof` —, l'adresse de
l'objet est celle de son premier membre, et le type peut traverser une frontière C. À ne pas
confondre avec `std::is_trivially_copyable_v<T>`, le trait qui autorise le `memcpy` : indépendants.

### La base vide et `[[no_unique_address]]`

Deux objets distincts ont deux adresses distinctes, donc **une classe vide fait 1 octet**. Mais une
classe de **base** vide n'a pas besoin de place propre, et le compilateur la fait disparaître —
c'est pour ça que comparateurs et allocateurs ont longtemps été hérités plutôt que stockés. C++20
donne l'outil direct, sans tordre la hiérarchie :
```cpp
struct Vide {};
struct MembreVide  { Vide v; int n; };      // sizeof : 8 — 1 octet + 3 de remplissage + 4
struct DeriveVide : Vide { int n; };        // sizeof : 4 — la base vide ne coûte rien

struct Comparateur { bool operator()(int, int) const; };
template <typename T, typename C> struct Membre   { C c; T *debut; T *fin; };
template <typename T, typename C> struct NoUnique { [[no_unique_address]] C c; T *debut; T *fin; };
// sizeof(Membre<int, Comparateur>) : 24      sizeof(NoUnique<int, Comparateur>) : 16
```

Deux membres vides de types **différents** peuvent partager une adresse ; deux du **même** type ne
le peuvent pas, mais l'un recouvre quand même un autre champ : dans
`struct { [[no_unique_address]] Vide a, b; int n; }`, `sizeof` vaut 4, `a` est au décalage 0, `b`
au 1 et `n` au 0. L'attribut est une **permission** ; MSVC l'ignore et propose
`[[msvc::no_unique_address]]`.

### Ce que coûte une fonction virtuelle

Une seule fonction virtuelle, et l'objet gagne un **pointeur de table virtuelle** de 8 octets, en
tête. Il se paie une fois par objet, quel que soit le nombre de méthodes, et une classe dérivée
n'en ajoute pas un deuxième — tant que l'héritage est **simple**. Chaque base polymorphe
supplémentaire en ajoute un : deux vptr et 32 octets pour une classe qui hérite de deux bases
virtuelles, et l'héritage virtuel en rajoute encore.

```cpp
struct Simple        { int a; double b; };                            // sizeof : 16
struct AvecVirtuelle { int a; double b; virtual ~AvecVirtuelle(); };  // sizeof : 24
struct Base   { int a; virtual void f(); };           // sizeof : 16 — vptr 8 + int 4 + 4 perdus
struct Derive : Base { int b; void f() override; };   // sizeof : 16 — b occupe les 4 perdus
```

Le vrai coût n'est pas ces 8 octets, c'est ce qu'ils impliquent : l'appel lit le vptr, lit une
entrée de la table, puis saute — **deux dépendances mémoire avant de savoir où aller**, et un
prédicteur de branchement aveugle dès que le type varie d'un élément à l'autre.

## Tableau de structures contre structure de tableaux

Un tableau de structures range chaque entité d'un bloc ; une structure de tableaux range chaque
champ dans son propre tableau.
```cpp
struct Particule { float x, y, z, vx, vy, vz; int identifiant; bool actif; };  // sizeof : 32
std::vector<Particule> tableau;                                   // tableau de structures
struct Particules { std::vector<float> x, y, z, vx, vy, vz; };    // structure de tableaux
```

Chiffrons une boucle qui **ne lit que `x`** sur 2 000 000 d'entités, lignes de 128 octets :

| | octets utiles | octets traversés | lignes touchées |
|---|---|---|---|
| tableau de structures | 7,6 Mio | 61 Mio | 500 000 |
| structure de tableaux | 7,6 Mio | 7,6 Mio | 62 500 |

Le rapport vaut exactement `sizeof(Particule) / sizeof(float)`, soit 8, et il ne dépend **pas** de
la taille de la ligne tant que la structure y tient : sur x86-64 les deux colonnes doublent et le
rapport reste 8. Mesuré, avec une somme d'entiers que le compilateur vectorise :

| `sizeof` structure | Éléments | Lignes en tableau | Lignes en structure | Tableau | Structure | Rapport |
|---|---|---|---|---|---|---|
| 32 octets | 8 000 000 | 2 000 000 | 250 000 | 3,63 ms | 0,47 ms | ×7,7 |
| 64 octets | 4 000 000 | 2 000 000 | 125 000 | 3,60 ms | 0,23 ms | ×15,6 |
| 128 octets | 2 000 000 | 2 000 000 | 62 500 | 4,59 ms | 0,12 ms | ×39,7 |

**Le temps suit le nombre de lignes**, et c'est net pour la structure de tableaux : il se divise
par deux à chaque fois que les lignes se divisent par deux. Le tableau de structures, lui, garde
ses deux millions de lignes sur les trois essais et paie quand même 25 % de plus à la dernière,
quand le pas atteint 128 octets — un pas que le préchargeur suit moins bien. Le nombre d'éléments
et celui des instructions, eux, ne prédisent rien. Une précaution d'honnêteté : le même test avec
une somme de `float` donne ×1,2 au lieu de ×7,7, parce que la chaîne de dépendances des additions
flottantes — non associatives, donc non vectorisables — domine tout. La boucle est alors limitée
par le calcul, précisément le cas où il ne faut pas toucher à la disposition.

### Quand la structure de tableaux est le mauvais choix

- **La boucle lit tous les champs** : même nombre de lignes des deux côtés, et N flux concurrents
  qui se disputent préchargeurs et TLB — la version en tableaux devient plus lente.
- **Les accès sont aléatoires** : toucher l'entité `i` coûte une ligne en tableau de structures,
  et N lignes en structure de tableaux, une par champ.
- **On insère, on supprime, une entité voyage** : N tableaux à garder cohérents, un `erase` oublié
  décale tout en silence, et l'entité n'existe nulle part comme objet à passer ou sérialiser.
- **Ce n'est pas une boucle chaude** : `particules.x[i]` se lit moins bien que `p.x`.

Souvent meilleure que les deux extrêmes : **couper la structure en deux**, les champs de la boucle
chaude d'un côté, le reste de l'autre. L'entité reste identifiable, la traversée est divisée par
trois ou quatre.

## Le préchargeur

Les latences du premier tableau sont celles d'un accès **imprévisible**. Un parcours séquentiel ne
les paie presque jamais, parce que le matériel devine : chaque cœur contient des **préchargeurs**
qui observent les adresses demandées, et dès qu'ils reconnaissent une progression régulière — la
ligne suivante, ou un pas constant — ils lancent les chargements d'eux-mêmes, plusieurs lignes en
avance. Mesure : 8 000 000 d'accès aux **mêmes** 32 Mio, une fois en ordre croissant (1,32 ms), une
fois en ordre aléatoire (14,78 ms). Mêmes données, mêmes accès, mêmes instructions, ×11.

Ce qui casse la prédiction est toujours la même chose : **l'adresse suivante n'est connue qu'après
avoir lu la précédente.** Une liste chaînée range l'adresse du nœud suivant *dans* le nœud
courant ; un arbre (`std::map`, `std::set`) saute par niveau ; un `std::vector<std::unique_ptr<T>>`
se précharge bien mais chaque déréférencement repart ailleurs ; un hachage a l'imprévisibilité pour
principe. Le prix, mesuré sur une somme de 4 000 000 d'`int` :

| Conteneur | Temps | Rapport |
|---|---|---|
| `std::vector<int>` | 0,27 ms | ×1 |
| `std::list<int>` remplie par `push_back`, jamais réordonnée | 3,47 ms | ×13 |
| `std::list<int>` dont les nœuds ont été mélangés | 350,64 ms | **×1322** |

Les 350 ms font 88 ns par élément : **la latence RAM mesurée en tête de chapitre, à un cheveu
près** — l'empreinte de la liste tombe juste sous celle du test de latence, qui monte à 99 ns sur
de plus grands volumes. La
liste mélangée ne fait rien d'autre qu'attendre, un élément après l'autre — et le cas « jamais
réordonnée » est le cas *favorable*, celui d'une liste fraîchement construite. C'est tout le
contenu de la règle du chapitre 05, « `vector` d'abord » : sur le papier `std::list` insère en
temps constant n'importe où, mais **il faut d'abord arriver à l'endroit où insérer**, et ce trajet
coûte une latence RAM par élément traversé, pendant que `std::vector` décale des octets contigus à
pleine bande passante.

## Le faux partage

Le cache est **cohérent** : deux cœurs ne peuvent pas détenir la même ligne modifiable en même
temps, et un cœur qui veut écrire prend la ligne en exclusivité et l'invalide chez les autres.
Cette cohérence travaille sur la ligne, pas sur la variable. Deux fils qui écrivent deux compteurs
**différents** rangés dans la même ligne se la volent à chaque incrémentation, alors qu'ils ne
partagent logiquement rien. C'est le **faux partage**. Mesuré sur `std::vector<Compteur>` avec
`struct Compteur { std::atomic<long> n{0}; };`, 4 fils, 20 000 000 d'incrémentations
`memory_order_relaxed` chacun :

| Disposition des compteurs | Temps | Rapport |
|---|---|---|
| voisins, 8 octets chacun | 858,7 ms | ×23 |
| `alignas(64)` | 52,8 ms | ×1,4 |
| `alignas(128)` | 37,3 ms | ×1 |

Deux enseignements. D'abord un facteur de cet ordre — le ×23 mesuré ici dépend du placement des
fils sur les cœurs et varie d'un lancement à l'autre — sans la moindre erreur de logique : le code
est correct, aucun
outil ne le dira, il est seulement vingt-trois fois trop lent. Ensuite **`alignas(64)` peut ne pas
suffire ici**, et la raison est plus intéressante qu'un simple chiffre : `hw.cachelinesize` renvoie
128 pour toute la machine, mais le granule de faux partage mesuré vaut 64 octets sur les cœurs
performance et 128 sur les cœurs efficacité. Deux fils que l'ordonnanceur place sur des cœurs E se
volent donc encore la ligne à 64 octets d'écart, et le résultat dépend d'un placement sur lequel
tu n'as pas la main. `alignas(128)` couvre les deux cas. C++17 a pourtant ajouté dans `<new>` la
constante censée donner le bon chiffre, `std::hardware_destructive_interference_size` ; son état
de support est le vrai sujet.

| Chaîne d'outils | Valeur | Remarque |
|---|---|---|
| Apple clang 21 / libc++, arm64 | **256** | volontairement au-dessus de la ligne de 128 |
| Apple clang 21 / libc++, x86-64 | **64** | la taille exacte de la ligne |
| GCC / libstdc++, x86-64 | 64 | avertit avec `-Winterference-size` |

Ces constantes sont `constexpr` : leur valeur est figée à la compilation, elle entre dans la taille
des objets, **donc dans l'ABI**. Un en-tête partagé entre deux binaires compilés pour des machines
différentes donne deux dispositions incompatibles, et c'est exactement ce que GCC signale. En
pratique : sers-t'en dans un programme compilé d'un seul tenant, écris `alignas(128)` quand tu veux
un chiffre valable partout, et n'oublie pas le remède qui bat l'alignement — **ne pas partager**.
Un accumulateur local à chaque fil, additionné une fois à la fin, ne touche aucune ligne commune.

## Ce qui alloue sans le dire

Une allocation ne coûte pas que le temps passé dans `operator new` : elle coûte surtout une **ligne
ailleurs**, puisque le bloc rendu n'a aucune raison d'être à côté de ce qui le désigne. Le bon
compteur n'est pas le nombre d'octets, c'est **le nombre d'indirections que traverse la boucle**.

**La petite optimisation de chaîne.** Une `std::string` courte range ses caractères dans l'objet.
Sur libc++, arm64 comme x86-64, `sizeof(std::string)` vaut 24, la capacité sans allocation est de
**22 caractères**, et la première allocation arrive au vingt-troisième : un bloc de 25 octets si la
chaîne est construite directement à cette taille, de 48 si elle y arrive par croissance
(`push_back`, `+=`), la croissance étant géométrique. Sur
libstdc++, l'objet fait 32 octets et le seuil est de 15. Une chaîne courte n'alloue donc rien ; ce
qui coûte, c'est la chaîne longue recopiée en boucle, et le remède est `std::string_view`.

**Les conteneurs à nœuds.** Un nœud par élément, chacun alloué séparément ; tailles relevées en
interceptant `operator new` : 24 octets par `std::list<int>` pour 4 utiles, 40 par
`std::map<int, int>` pour 8. Un million d'éléments, c'est 24 Mo pour 4 Mo de données et un million
d'allocations dispersées, là où le `std::vector<int>` équivalent fait 4 Mo en une seule — d'où la
victoire du `std::vector<std::pair<K, V>>` trié du chapitre 05, et de `std::flat_map` en C++23.

**`std::function`** possède un tampon interne et alloue silencieusement dès que la capture le
dépasse. Sur libc++ arm64, l'objet fait 32 octets, une capture de 16 tient sur place et une de 24
alloue ; sur libc++ x86-64, l'objet fait 48 octets, 24 tient encore et 32 alloue. Le seuil n'est ni
normé ni portable, et trois `shared_ptr` capturés le dépassent partout. Dans une boucle chaude,
passe plutôt le foncteur en paramètre gabarit : le type reste concret et l'appel s'inline, alors
qu'un appel à travers `std::function` est indirect comme un appel virtuel.

**`std::shared_ptr`** fait **16 octets** : un pointeur vers l'objet, un vers le bloc de contrôle
qui tient les compteurs fort et faible. `std::shared_ptr<int> p(new int(5))` demande **deux**
allocations, de 4 puis 32 octets ; `std::make_shared<int>(5)` n'en demande qu'**une**, de 32 —
l'objet et le bloc fusionnent, une ligne au lieu de deux, au prix d'une mémoire rendue seulement à
la disparition du dernier `weak_ptr`. Le coût principal est ailleurs : les compteurs sont
**atomiques**, donc copier un `shared_ptr` est une opération verrouillée sur une ligne partagée, et
le même copié depuis plusieurs fils leur fait se disputer la ligne de son bloc de contrôle — du
partage bien réel, cette fois, que nul alignement ne répare. Passe `const std::shared_ptr<T> &`,
ou `const T &` quand la fonction ne prend pas part à la propriété.
```cpp
std::vector<std::unique_ptr<Entite>> par_pointeur;   // une indirection imprevisible par element
std::vector<Entite> a_plat;                         // aucune
```

La première version est le motif par défaut de beaucoup de code C++, et elle transforme un
parcours séquentiel en parcours aléatoire : le tableau de pointeurs se précharge parfaitement, et
chaque déréférencement paie quand même sa latence. Quand les entités doivent être polymorphes ou
stables en adresse, c'est justifié ; sinon, la réponse est un `std::vector<Entite>` plus des
**indices** stables, comme au chapitre 05.

## À retenir

1. Un accès RAM coûte 350 à 400 cycles, un calcul rien : compte les allers-retours.
2. Le cache transfère des lignes — 64 octets sur x86-64, 128 sur Apple Silicon : compte-les.
3. `alignas` ne peut qu'augmenter, et pousse `sizeof` d'autant : la taille reste un multiple.
4. Une base vide ne coûte rien, un membre vide 1 octet : `[[no_unique_address]]` corrige ça.
5. Une fonction virtuelle coûte 8 octets par objet, une fois, et deux dépendances par appel.
6. La structure de tableaux divise le temps à peu près par `sizeof(structure) / sizeof(champ)`
   quand la boucle ne lit qu'un champ — davantage encore aux grands pas, que le préchargeur suit
   mal — et perd dès qu'elle les lit tous ou saute au hasard.
7. Deux fils qui écrivent la même ligne se la volent : `alignas(128)`, ou un accumulateur local.

**Exercices : `09_layout`.**
