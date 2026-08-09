# csharplings

Apprendre le C# en reparant du code cassé. 124 exercices, du premier point-virgule
jusqu'aux taxes que Godot et Unity font réellement payer.

## Démarrer

```
cd csharplings
dotnet run
```

C'est tout. Le programme s'arrête sur le premier exercice non terminé, affiche la
consigne, lance le code et te dit ce qui ne va pas. **Laisse-le tourner** : dès que tu
sauvegardes le fichier, il relance tout seul.

Ouvre le fichier qu'il t'indique dans un autre onglet, corrige, sauvegarde. Recommence.

## La boucle

1. `dotnet run` t'annonce un exercice et son fichier
2. tu corriges le fichier
3. tu sauvegardes → ça relance
4. quand les vérifications passent, tu mets `NotDone` à `false`
5. il passe automatiquement au suivant

Le `NotDone` sert à ça : le code peut marcher sans que tu aies compris. C'est toi qui
décides quand tu passes à la suite.

## Les autres commandes

```
dotnet run -- list              où j'en suis
dotnet run -- hint variables1   un indice
dotnet run -- solution flow2    la correction
dotnet run -- run godot3        relancer un exercice précis
dotnet run -- verify            vérifier que toutes les solutions passent
dotnet run -- quiz              le questionnaire, selon ton profil
dotnet run -- quiz 15_perf      seulement une section
dotnet run -- quiz list         combien de questions par section
dotnet run -- config            ton profil, et ce que chacun contient
dotnet run -- config unity      n'avoir que le C# et Unity
```

Regarder la solution n'est pas de la triche, mais lis l'indice d'abord.

## Le questionnaire

`NotDone` existe parce que le code peut marcher sans que tu aies compris. Le questionnaire
sert à la même chose, dans l'autre sens : **74 questions à quatre réponses**, une par leçon
marquante des 22 sections.

```
dotnet run -- quiz              celles de ton profil, mélangées
dotnet run -- quiz 16_memory    les 5 de cette section
```

Chaque réponse est suivie d'une explication — que tu aies juste ou faux, parce que tomber
juste par élimination n'apprend rien. À la fin, le score et la liste de ce qui est à revoir,
avec la commande pour réviser directement la bonne section.

Les questions ne portent pas sur la syntaxe mais sur ce qui surprend : pourquoi
`list[0].Modifie()` ne modifie rien, pourquoi `obj == null` rend `true` sur un objet Unity
détruit, combien de secondes un `float` perd en dix mille, et pourquoi le coût des appels
natifs n'apparaît dans aucun profil mémoire.

## Ton profil

Personne ne doit apprendre le moteur qu'il n'utilise pas. Un profil filtre le programme **et**
le questionnaire :

```
dotnet run -- config unity      ce que tu tapes si tu fais du Unity
dotnet run -- config godot      ...ou du Godot
dotnet run -- config pure       du C# et rien d'autre
dotnet run -- config all        tout, par curiosite
```

| Profil | Exercices | Questions | Ce que tu vois |
|---|---|---|---|
| `pure` | 100 | 54 | le langage, l'algo, l'optimisation. Aucun moteur |
| `godot` | 112 | 61 | tout ça, plus `09_godot`, `18_bridge`, et les deux exercices de `14_engine` en API Godot |
| `unity` | 112 | 67 | tout ça, plus les 12 de `19_unity` |
| `all` | 124 | 74 | les deux moteurs mélangés |

Le profil est stocké dans `.csharplings-profile`, qui n'est pas suivi par git : c'est ton
réglage, pas celui du dépôt. Par défaut, `all`.

**Ce qui est masqué n'est pas verrouillé.** `run`, `hint` et `solution` marchent sur n'importe
quel exercice, profil ou pas. Le profil décide seulement de ce que `dotnet run` te propose et de
ce que `list` affiche.

Côté démos, la correspondance est directe : `../demos/pure/`, `../demos/godot/`, `../demos/unity/`.

## Le programme

Les sections marquées d'un moteur ne sortent que dans le profil correspondant.

| Section | Contenu |
|---|---|
| `00_intro` | comment marche cet outil, lire une erreur de compilation |
| `01_variables` | déclarer, `var`, `const` et `readonly` |
| `02_types` | int/float/double, conversions, texte vers nombre |
| `03_flow` | if/else, switch expression, for/while, foreach |
| `04_methods` | signatures, paramètres optionnels, `out` et `ref` |
| `05_strings` | interpolation, manipulation, immuabilité |
| `06_collections` | tableaux, `List<T>`, `Dictionary<K,V>` |
| `07_oop` | classes, propriétés, héritage, interfaces, struct, enum |
| `08_advanced` | génériques, null, exceptions, LINQ, lambdas, events, async, records, pattern matching, tuples, opérateurs, extensions, `yield`, `IDisposable`, `Span` |
| `09_godot` **(godot)** | cycle de vie, delta time, GetNode, signaux, singleton |
| `10_gamedev` | vecteurs, cooldowns, lissage, pooling, grilles |
| `11_patterns` | machine à états, commandes annulables, bus d'événements, composition, services, données partagées |
| `12_math` | angles, easing, collisions, rayons, aléatoire à graine, béziers |
| `13_systems` | inventaire, calcul de dégâts, pathfinding, pas de temps fixe, grille spatiale, buffer d'entrée, sauvegarde |
| `14_engine` | boucle de rendu vs physique, cache de nœuds, actions d'entrée, gravité et saut, masques de collision, caméra, coroutines, tweens |
| `15_perf` | zéro allocation, boxing, texte et HUD, suppression en boucle, structs et réutilisation, étalement du travail |
| `16_memory` | pile et tas, `ref`/`out`/`in`, delegates et fuites, GC et générations, copies défensives |
| `17_ecs` | identifiants à génération, stockage en colonnes, masques de composants, itération sans allocation, systèmes et commandes différées, colonnes contre objets mesuré |
| `18_bridge` **(godot)** | franchissements de la frontière C#↔moteur comptés, `StringName` et conversions implicites, coût réel d'un signal, collections moteur recopiées, appels différés |
| `19_unity` **(unity)** | cycle de vie et où s'abonner, `== null` contre `is null` sur un objet détruit, `Time.deltaTime` et ses pièges, règles de sérialisation, prix d'un `Update` par objet, les trois boucles et `LateUpdate`, `GetComponent` mis en cache, attentes de coroutine, clonage de matériau, singleton qui survit au changement de scène, statiques qui survivent à la partie, contraintes IL2CPP |
| `20_time` | interpolation de l'affichage entre pas de physique, timers sans dérive, `timeScale` et pause, indépendance au framerate prouvée, temps absolu et effondrement du `float` |
| `21_physics` | tunneling et collision balayée, glissement et pente maximale, requêtes sans allocation, intégration semi-implicite, dépénétration sans tremblement |

## La section Godot

`09_godot` tourne sur un mini-moteur maison (`support/MiniGodot.cs`) : un vrai `Node` avec
`_Ready`, `_Process`, `AddChild`, `QueueFree`, `IsInstanceValid` et un `SceneTree` qui
avance image par image. Deux exercices de `14_engine` s'en servent aussi.

Pas besoin de lancer Godot, ça tourne dans le terminal en une seconde. Mais le code que
tu écris est celui que tu écriras dans le vrai moteur.

`godot3` t'apprend que seul `IsInstanceValid` dit la vérité après un `QueueFree`. Le pourquoi
complet — les deux objets, les trois durées de vie, et ce que `Dispose()` fait vraiment — est
dans `../demos/godot/WEAKREFS.md`, où chaque affirmation est vérifiée en exécutant le moteur.

## La section gamedev

`10_gamedev` est la plus utile si tu veux faire des jeux : ce sont les cinq calculs
que tu réécriras dans chaque projet, quel que soit le moteur. Aller vers une cible,
gérer un temps de recharge, lisser un mouvement, recycler des objets, convertir une
case de grille en pixels.

Le code est du C# pur : il marche tel quel sous Unity aussi.

## Faire un vrai jeu

`11_patterns`, `12_math` et `13_systems` sont la partie « on assemble ».
Chaque exercice est un morceau que tu retrouveras tel quel dans ton projet :

- une machine à états qui refuse les transitions impossibles
- un historique annuler/refaire à deux piles
- un bus où le score, les quêtes et le son réagissent sans se connaître
- de quoi viser, tourner par le plus court chemin, savoir ce qu'un garde voit
- un aléatoire à graine qui rejoue exactement le même donjon
- un pathfinding, une grille spatiale, un pas de temps fixe
- le buffer d'entrée et le coyote time, les deux astuces qui rendent un
  platformer agréable
- une sauvegarde qui survit à un fichier incomplet

Ça reste du C# pur, sans Godot : tout tourne dans le terminal.

## Les réflexes moteur

`14_engine` est la section « ce que tu fais tous les jours », valable Godot **et** Unity :

- `_Process` contre `_PhysicsProcess` (= `Update` contre `FixedUpdate`), et pourquoi la
  caméra doit passer après sa cible (`ProcessPriority`, = `LateUpdate`)
- cacher `GetNode` / `GetComponent` au lieu de chercher 60 fois par seconde
- « enfoncée » contre « vient d'être enfoncée », et la diagonale qui va 41 % trop vite
- déduire la vitesse de saut d'une hauteur voulue, friction sans repartir en arrière
- les masques de collision (une couche par bit, et les valeurs sont des puissances de deux)
- caméra : zone morte, bornes du niveau, secousse en trauma²
- des coroutines maison en `IEnumerator` — le modèle exact de Unity, en trente lignes
- un tween qui atterrit pile sur la cible et ne prévient qu'une fois

## Les optimisations

`15_perf` ne se contente pas d'expliquer : **les vérifications comptent les octets alloués.**
`GC.GetAllocatedBytesForCurrentThread()` mesure vraiment, donc un exercice échoue si ton
code alloue dans la boucle chaude. Le runner compile en **Release**, donc les chiffres que
tu lis sont ceux de ton jeu une fois exporté, pas ceux d'un build de debug.

- une boucle `for` sur une `List` doit rendre **0 octet** ; le même calcul en LINQ, non
- une structure sans `IEquatable` utilisée comme clé de dictionnaire s'emballe dans un
  objet à chaque comparaison — mesurable, et invisible autrement
- un `foreach` derrière `IEnumerable<T>` alloue son énumérateur ; derrière `List<T>`, non
- un HUD qui ne reconstruit son texte que quand la valeur change
- retirer d'une liste sans sauter d'éléments, et la suppression par échange
- 1000 objets recréés par frame contre un tableau de structs réutilisé
- étaler le travail sur plusieurs frames, et en faire moins quand c'est loin

## Ce qui se passe en RAM

`16_memory` est la section « arrête de deviner ». Elle mesure au lieu d'expliquer :

- **un objet vide coûte 24 octets** — l'en-tête que tout objet du tas porte. Un `int` de
  plus est gratuit (il tient dans le remplissage), le quatrième coûte 8 octets.
- **1000 structures de 20 octets = une seule allocation de 20 024 octets**, contiguë.
  1000 objets équivalents, ce sont 1000 allocations éparpillées.
- **une lambda qui ne capture rien : 0 octet** (elle est mise en cache). Dès qu'elle
  capture une variable locale : 96 octets, à chaque passage.
- **emballer un `int` dans un `object` alloue — sauf si personne ne garde la boîte** :
  en Release le compilateur supprime purement et simplement une boîte dont le résultat
  ne sert à rien. Mesuré dans les deux sens.
- **200 000 objets jetables → collections gen0 réelles ; le même travail avec un tampon
  réutilisé → zéro.** Une collection gen0 est rapide, mais c'est une *pause*, et le
  budget d'une frame est de 16 ms.
- un objet qui survit à une collection est **promu** en génération supérieure.

Et le langage qui va avec :

- ce que contient vraiment une variable : une structure **est** la valeur, un objet n'est
  qu'une adresse — et un paramètre objet passe cette adresse **par copie**, ce qui explique
  pourquoi réassigner le paramètre ne change rien dehors
- `ref` / `out` / `in`, les `ref` locals et les `ref` returns : modifier un élément de
  tableau **en place**, sans jamais le recopier
- les delegates en vrai : multicast, valeur de retour du dernier seulement, une exception
  qui tue silencieusement le reste de la chaîne, et surtout `-=` avec une nouvelle lambda
  qui **ne désabonne rien** — la fuite mémoire la plus répandue en gamedev
- les copies défensives : appeler une méthode sur un champ `readonly` de type structure
  travaille sur une copie. Le code compile, tourne, et ne fait rien.

## Écrire son propre moteur à entités

`17_ecs` est la section « maintenant tu construis l'outil ». Six exercices, et à la fin tu
as un mini ECS complet, écrit de ta main, sans bibliothèque. C'est là que tout ce qui
précède se rejoint : les `ref` de `16_memory`, le `Span` de `08_advanced`, la suppression
par échange de `15_perf`, les masques de bits de `14_engine`, le tampon de commandes de
`11_patterns`.

- **un identifiant porte deux choses : où et quand.** Le slot dit où, la génération dit
  quel occupant. Sans elle, un slot recyclé fait résoudre l'ancien identifiant sur la
  nouvelle entité — le pointeur fantôme, exactement ce que `IsInstanceValid` évite dans
  Godot, mais en 30 lignes que tu contrôles.
- **une colonne par composant**, dense, plus une table creuse qui dit où est la ligne
  d'une entité. Retirer se fait par échange avec la dernière ligne : la mémoire reste
  contiguë, donc le parcours reste gratuit.
- **un bit par type de composant** : savoir si une entité répond à une requête devient
  une seule opération binaire. `(bits & requis) == requis`, et non `!= 0`.
- **un énumérateur en structure**, reconnu par `foreach` sans passer par `IEnumerable` :
  **0 octet** par parcours. La même boucle en `yield return` : 56 octets, à chaque appel,
  60 fois par seconde, par requête.
- **naître et mourir passent par un tampon** appliqué à la fin de la frame. Tous les
  systèmes d'une même frame voient donc le même monde, et une entité condamnée finit sa
  frame au lieu de disparaître au milieu.
- et la conclusion, chiffres affichés : 10 000 particules coûtent **400 056 octets en
  objets contre 160 080 en deux colonnes**. Mais surtout — et c'est la vraie leçon —
  un tableau d'objets parcouru à l'index alloue **zéro** lui aussi. Le problème n'a
  jamais été « objet contre structure » : c'est `IEnumerable<T>`, LINQ, et une `List<T>`
  qui grandit sans capacité annoncée (**131 400 octets au lieu de 40 056**).

## La frontière C# ↔ moteur

`18_bridge` est la section la plus spécifiquement Godot, et la plus invisible. Elle tourne sur
`support/MiniGodot.cs`, qui compte désormais les franchissements de la frontière.

- **une propriété du moteur n'est pas un champ.** Chaque lecture de `Position` est un appel
  natif. Mesuré ici : un déplacement écrit naïvement paie **4 franchissements**, le même lu et
  écrit une seule fois en paie **2**. Sur 1000 objets à 60 images par seconde, c'est
  **240 000 appels contre 120 000** — et comme ça n'alloue rien, ça n'apparaît dans aucun
  profil de ramasse-miettes. C'est exactement pour ça que personne ne le trouve.
- **une chaîne littérale passée là où le moteur attend un nom se convertit implicitement.**
  Cent émissions écrites `Emit("died")` fabriquent **cent objets** ; le même nom gardé dans un
  `static readonly` en fabrique **zéro**. C'est à ça que servent les `SignalName.X` et
  `PropertyName.X` que Godot te génère.
- **un signal fait voyager ses arguments dans un tableau**, et `params` en fabrique un neuf à
  chaque émission. Le mini-moteur en compte 56 ; le vrai moteur, mesuré, en alloue **104**.
  Un `event Action<int>` : **0**, et il donne un `int` typé au lieu d'un tableau à indexer et
  à convertir. Signal moteur pour ce que l'éditeur doit brancher, event C# pour le reste.
- **un tableau moteur et une `List<T>` ne sont pas deux vues sur la même mémoire** : la
  conversion recopie. Sommer mille entiers en reconvertissant coûte **4056 octets** dans le
  mini-moteur, **8464** dans le vrai ; les lire à l'index sans convertir : **0** des deux côtés.
  On convertit une fois, au chargement.
- **on ne modifie pas l'arbre au milieu d'un callback physique** : on met l'appel dans une file
  vidée à la fin de la frame. Et cette file est prise en photo avant d'être vidée, sinon une
  chaîne d'appels différés tournerait en boucle infinie dans une seule frame.

## Ce que Unity impose en plus

`19_unity` tourne sur un second mini-moteur, `support/MiniUnity.cs` : un `UnityObject` dont
l'opérateur `==` ment exactement comme le vrai, un `MonoBehaviour`, une `Scene` qui compte
les appels du moteur, un `MeshRenderer` qui clone son matériau, et le sérialiseur avec ses
règles. Tout tourne au terminal, sans installer Unity.

C'est la section qui rend le programme réellement bi-moteur, parce que **les pièges de Unity
sont l'inverse de ceux de Godot** :

- **`objet == null` rend `true` sur un objet détruit**, alors que `objet is null` rend `false`.
  Unity surcharge l'opérateur ; le motif, lui, ne passe pas par l'opérateur. Et le `?.` ne
  protège de rien : la référence n'est pas nulle. Là où Godot exige `IsInstanceValid` parce que
  le test null ment par omission, Unity ment par excès. Même cause dans les deux cas : deux
  objets, un managé et un natif, qui ne meurent pas ensemble.
- **Unity sérialise les champs, pas les propriétés**, et ne sait pas sérialiser un
  `Dictionary`. Sans un mot d'avertissement : le champ disparaît de l'inspecteur et de la
  sauvegarde. D'où l'aplatissement en deux listes dans `OnBeforeSerialize`, et la
  reconstruction dans `OnAfterDeserialize`.
- **chaque `Update()` est un appel du moteur vers ton code.** Mesuré ici : 1000 scripts sur
  10 frames = **10 000 franchissements**, le même travail dans un seul manager = **10**. Le
  « manager pattern » n'est pas un style, c'est une nécessité à l'échelle. Et un `Update` que
  tu n'écris pas ne coûte rien : Unity ne branche que ce que tu déclares — mais un `Update`
  **vide**, lui, est branché et facturé.
- **trois boucles par frame, pas une.** Une caméra qui suit sa cible dans `Update` a une frame
  de retard si son script passe avant : mesuré, l'écart vaut exactement un déplacement de
  frame. `LateUpdate` passe après **tous** les `Update`, donc l'ordre des scripts n'a plus
  d'importance. Et à 60 images pour 50 pas de physique, certaines frames en jouent **zéro** et
  d'autres **deux** : un appui lu dans `FixedUpdate` est perdu, ou compté double.
- **le cycle de vie, et surtout où s'abonner.** `OnEnable` rejoue à *chaque* réactivation, et la
  destruction passe par `OnDisable` avant `OnDestroy` : un seul endroit à écrire couvre les deux
  cas. Avec `Start`/`OnDestroy`, un script désactivé reste abonné — et réactivé, il ne se
  réabonne jamais. Vérifié dans les deux sens, compteur d'abonnés en main.
- **`Time.deltaTime` lu depuis `FixedUpdate` rend le pas fixe**, pas le temps de la frame. Le
  code a l'air correct des deux côtés. Plus deux voisins : un gel de deux secondes arrive
  **plafonné à un tiers de seconde**, et `unscaledDeltaTime` est celui des menus.
- **un singleton Unity tient à trois détails** : poser l'instance dans `Awake`,
  `DontDestroyOnLoad` pour survivre au changement de scène, et surtout ne remettre la propriété
  à `null` que « si c'est encore moi » — sinon un doublon qui se détruit efface le vrai. Et le
  garde s'écrit `== null` : en `is null`, une référence périmée casserait le singleton pour
  toute la partie.
- **`GetComponent` dans `Update`, c'est 60 recherches par seconde** pour un résultat qui ne
  change jamais : **60 contre 1** en cherchant dans `Awake`. C'est le `cache1` de la section
  Godot, avec `GetComponent` au lieu de `GetNode`. Et l'opérateur `==` coûte lui aussi un appel
  natif — **1000 contre 1** en sortant le test de la boucle.
- **`yield return new WaitForSeconds(0.1f)` fabrique un objet par battement.** Une attente est
  une durée : un `static readonly` suffit, le timing ne change pas. Et `yield return null` est
  gratuit.
- **lire `renderer.material` ne lit rien : ça clone.** Une propriété, pas une méthode. Cent
  ennemis teintés, cent matériaux — et ce sont des objets natifs, que le ramasse-miettes ne
  prendra jamais. Il faut un `Destroy` pour chacun.
- **IL2CPP compile en avance** : un type que personne n'instancie dans le code n'a pas de
  constructeur généré, donc une fabrique par réflexion marche dans l'éditeur et **échoue sur
  console**. La parade est une table de `() => new Machin()` : chaque construction est visible,
  rien n'est supprimé, et un type oublié se remarque à la lecture de la table. Mesuré au
  passage : les deux coûtent **exactement 24 octets**, donc on ne choisit pas la table pour la
  performance — on la choisit parce qu'elle ne peut pas ne pas exister.
- **les `static` survivent à la partie** dès qu'on désactive le rechargement de domaine, ce que
  presque toutes les équipes font pour gagner du temps de compilation. Le score reprend où il
  en était, et l'événement statique garde les abonnés de la partie d'avant : des objets morts
  qui répondent encore, et qui restent en vie à cause de l'abonnement lui-même.

## Le temps, celui du moteur

`20_time` est la section « ce que les deux moteurs te demandent de comprendre sur le temps ».
`13_systems/fixed1` t'a fait écrire l'accumulateur et son `Alpha` ; ici on s'en sert enfin.

- **l'affichage direct de la position physique fait un escalier.** Quatre frames de rendu
  identiques, puis un saut d'un pixel. Il faut garder deux états et afficher entre les deux —
  c'est le *physics interpolation* de Godot et l'interpolation des `Rigidbody` de Unity, et
  tu l'écriras à la main pour tout ce qui n'est pas un corps rigide. Avec, en prime, le piège
  que personne ne voit venir : un téléport doit **remettre les deux états**, sinon l'objet
  traverse l'écran en glissant.
- **un timer qui remet son compteur à zéro dérive** : 83 déclenchements au lieu de 99 sur dix
  secondes, mesuré. Soustraire l'intervalle règle la dérive ; retenir l'instant du prochain
  déclenchement règle tout le reste.
- **mettre en pause, c'est `timeScale = 0`** — mais le menu de pause, les fondus de son et le
  matchmaking tournent sur le temps réel. Deux deltas, et savoir lequel chaque chose consomme.
  Et le compteur d'images se calcule sur le temps réel : `1 / Delta` pendant une pause donne
  l'infini.
- **on ne fait pas confiance, on vérifie** : la même simulation à 60 et à 240 images par
  seconde doit finir au même endroit. `Lerp(valeur, cible, 0.1f)` échoue de **20 unités**.
  Et une intégration naïve change ta hauteur de saut selon l'écran du joueur — alors que
  prendre la **moyenne** de l'ancienne et de la nouvelle vitesse tombe pile sur la valeur
  exacte, à n'importe quel pas de temps.
- **un `float` tient un delta, pas un temps absolu.** Accumuler `1/60` en `float` pendant
  10 000 secondes dérive de **28 secondes**. Et passé 524 288 secondes — six jours de
  fonctionnement — un `float` ne peut plus représenter un écart d'une frame : l'horloge
  s'arrête net, en silence. D'où le temps absolu en `double` dans les deux moteurs, et le
  comptage en pas entiers dès qu'il faut rejouer à l'identique.

## Ce que la physique impose

`21_physics` est la section « les bugs que tout le monde se prend ». `12_math/collision1` t'a
appris à savoir si deux formes se touchent ; ici on répare ce qui arrive ensuite.

- **une balle à 6000 pixels par seconde traverse un mur de 10 pixels.** Elle est devant à la
  frame N, derrière à la frame N+1, et un test de chevauchement ne voit rien. La parade est
  de tester le **trajet** — un balayage par tranches, qui en plus donne l'instant exact du
  contact — ou de découper le déplacement.
- **contre un mur, s'arrêter net est un bug.** Il faut garder la part du mouvement parallèle
  à la surface : c'est une ligne de produit scalaire, et c'est ce que font `MoveAndSlide` et
  le `CharacterController`. Avec le test que tout le monde oublie : ne corriger **que** si on
  rentre dans la surface, sinon le sol mange le saut du joueur.
- **soixante requêtes par frame qui rendent chacune une liste, c'est soixante listes par
  frame.** D'où les API où l'appelant fournit le tampon — `RaycastNonAlloc` chez Unity, et la
  raison pour laquelle un `IntersectRay` qui rend un dictionnaire coûte cher chez Godot.
  Mesuré ici : **0 octet contre 504**. Avec le piège maison : un tampon trop petit perd des
  résultats en silence.
- **le même ressort, les mêmes chiffres, deux lignes échangées** : l'un oscille à 1,01
  d'amplitude, l'autre part à 4 × 10¹³. Mettre à jour la vitesse **avant** la position n'est
  pas une question de style. Et un amortissement se règle par seconde (`Pow(garde, delta)`) :
  ton `0.98` par frame valait 29,8 à 60 images et 0,8 à 240.
- **repousser un corps hors d'un mur ne suffit pas.** Si on ne touche pas à sa vitesse il
  repart dedans, accélère, et finit par traverser les vingt pixels du sol en une frame. Il
  faut annuler la composante de vitesse dirigée vers la surface, et seulement celle-là.

## Godot, Unity, et le niveau de C#

Le runner, lui, tourne sur **.NET 10** : c'est ce qui compile et exécute tes corrections. Ne te
fie donc pas à « ça passe ici » pour juger de la portabilité — le moteur que tu vises est en
retard, et c'est lui qui décide.

Godot 4 tourne en .NET 6 (4.0–4.2) puis .NET 8 (4.3+). **Unity, lui, n'est pas en .NET 6** :
Unity 2021.3 → Unity 6 utilisent Mono/IL2CPP avec l'API .NET Standard 2.1 et un
`LangVersion` figé à **C# 9**. Si tu veux du code qui se colle dans les deux, la contrainte
réelle est donc C# 9, plus stricte que .NET 6.

Presque tout ici tient dans C# 9 : `record`, `init`, `new()`, les patterns relationnels
(`< 100`, `and`, `or`), `Span`, `stackalloc`, `HashCode.Combine`,
`GC.GetAllocatedBytesForCurrentThread`. Trois exceptions, signalées dans leur consigne :

- `patterns1` utilise les **motifs de liste** (`[1, .., 3]`) — C# 11, Godot oui, Unity non
- `bus1` utilise `record struct` — C# 10, Godot oui, Unity non (écris un `readonly struct`)
- `masks1` utilise `BitOperations.PopCount` — ce n'est pas une question de langage mais de
  bibliothèque : arrivé avec .NET Core 3.0, donc Godot oui, Unity non. Côté Unity, compte
  les bits à la main (décale et masque), le reste de l'exercice ne change pas

Les `namespace X;` en fin de ligne sont du C# 10 aussi : c'est le style de l'outil, pas du
code destiné à être collé tel quel dans un projet Unity.

## Et après

- `../demos/CHEATSHEET.md` — le condensé Godot en tableaux
- `../demos/GODOT-UNITY.md` — la table de traduction Godot ↔ Unity et le 80/20 du métier
- `../demos/` — du vrai code Godot 4, à attacher à des nœuds

## Si quelque chose casse

- `.sandbox/` est régénéré à chaque lancement, tu peux le supprimer
- pour repartir de zéro sur un exercice, recopie-le depuis `solutions/` et re-casse-le
- les fichiers de `csharplings/` sont exclus du projet Godot, ils ne peuvent pas casser ton jeu
