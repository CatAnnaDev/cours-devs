# csharplings-gamedev

Apprendre le C# pour le jeu vidéo, en réparant du code cassé.

Deux parties, dans cet ordre : les exercices, puis du vrai code à lire.

## 1. `csharplings/` — 196 exercices à réparer

```
cd csharplings
dotnet run
```

Le programme s'arrête sur le premier exercice non terminé, affiche la consigne, lance le code
et dit ce qui casse. Tu corriges le fichier, tu sauvegardes, il relance tout seul — environ
un dixième de seconde entre la sauvegarde et le verdict.

### Choisis ton moteur d'abord

Personne ne doit apprendre le moteur qu'il n'utilise pas. Un profil filtre le programme **et**
le questionnaire :

```
dotnet run -- config unity      pour un dev Unity
dotnet run -- config godot      pour un dev Godot
dotnet run -- config pure       du C# et rien d'autre
dotnet run -- config            voir les quatre profils et ce qu'ils contiennent
```

| Profil | Exercices | Questions | Ce que tu vois |
|---|---|---|---|
| `pure` | 166 | 121 | le langage, l'algo, l'optimisation. Aucun moteur |
| `godot` | 178 | 128 | + `09_godot`, `18_bridge`, et deux exercices de `14_engine` en API Godot |
| `unity` | 184 | 140 | + les 18 de `19_unity` |
| `all` | 196 | 147 | les deux moteurs mélangés (défaut) |

Ce qui est masqué n'est pas verrouillé : `run`, `hint` et `solution` marchent sur tout, profil
ou pas. Le profil décide seulement de ce que `dotnet run` te propose.

### Le programme

Du premier point-virgule manquant jusqu'à ton propre moteur à entités. Les sections marquées
d'un moteur ne sortent que dans le profil correspondant.

| Section | Contenu |
|---|---|
| `00_intro` | comment marche l'outil, lire une erreur de compilation |
| `01_variables` | déclarer, `var`, `const` et `readonly` |
| `02_types` | int/float/double, conversions, texte vers nombre |
| `03_flow` | if/else, switch expression, for/while, foreach |
| `04_methods` | signatures, paramètres optionnels, `out` et `ref` |
| `05_strings` | interpolation, manipulation, immuabilité |
| `06_collections` | tableaux, `List<T>`, `Dictionary<K,V>` |
| `07_oop` | classes, propriétés, héritage, interfaces, struct, enum |
| `08_advanced` | génériques, null, exceptions, LINQ, lambdas, events, async, records, pattern matching, tuples, opérateurs, extensions, `yield`, `IDisposable`, `Span` |
| `09_godot` **(godot)** | cycle de vie d'un nœud, delta time, `GetNode` et validité, signaux, singleton |
| `10_gamedev` | vecteurs, cooldowns, lissage, pooling, grilles, hitstop, zone morte et courbe de stick, encaisser un coup (recul, i-frames, registre de hits), ciblage avec hystérésis, viser une cible mobile, table de butin et compteur de pitié |
| `11_patterns` | machine à états, commandes annulables, bus d'événements, composition, services, données partagées |
| `12_math` | angles, easing, collisions, rayons, aléatoire à graine, béziers |
| `13_systems` | inventaire, calcul de dégâts, pathfinding, pas de temps fixe, grille spatiale, buffer d'entrée, sauvegarde |
| `14_engine` | boucle de rendu vs physique, cache de nœuds, actions d'entrée, gravité et saut, masques de collision, caméra, coroutines, tweens |
| `15_perf` | zéro allocation, boxing, texte et HUD, suppression en boucle, structs et réutilisation, étalement du travail |
| `16_memory` | pile et tas, `ref`/`out`/`in`, delegates et fuites, GC et générations, copies défensives |
| `17_ecs` | un mini ECS écrit à la main : identifiants à génération, stockage en colonnes, masques, itération sans allocation, systèmes et commandes différées |
| `18_bridge` **(godot)** | la frontière C#↔moteur : chaque propriété est un appel natif, les noms qui allouent en silence, le coût d'un signal, les collections recopiées, `CallDeferred` |
| `19_unity` **(unity)** | cycle de vie et où s'abonner, `== null` sur un objet détruit, `Time.deltaTime` et ses pièges, sérialisation, prix d'un `Update` par objet, les trois boucles, `GetComponent` mis en cache, coroutines, matériaux clonés, singleton, statiques qui survivent, IL2CPP, `ScriptableObject` (config ≠ état), le `transform` qui traverse la frontière, `Destroy` différé, `Rigidbody`/`FixedUpdate` et l'interpolation, charger et libérer un asset, le rebuild de `Canvas` |
| `20_time` | interpoler l'affichage entre deux pas de physique, timers sans dérive, `timeScale` et pause, prouver l'indépendance au framerate, le temps absolu et le `float` qui s'écroule |
| `21_physics` | tunneling et collision balayée, glisser au lieu de s'arrêter, requêtes sans allocation, intégration stable, sortir d'un mur sans trembler |
| `22_json` | sérialiser, désérialiser et le piège de la casse, les attributs, une façade `JsonConvert` et le prix des options, généricité partielle et totale, polymorphisme déclaré, un convertisseur pour `Vector2`, lecture partielle, sauvegardes versionnées, le coût mesuré |
| `23_linq` | l'exécution différée, ce que capture une lambda, écrire ses propres opérateurs avec `yield`, regrouper et joindre, trier et surtout ne pas trier, ensembles et égalité, suites infinies, le paramètre parcouru deux fois, `null`/`0`/`default`, le coût par image |
| `24_unsafe` | pointeurs et `fixed`, `stackalloc`, réinterpréter la mémoire avec `MemoryMarshal`, taille et alignement des structs, les vérifications de bornes, mémoire hors GC, modifier un struct dans une `List`, pointeurs de fonction, la frontière avec le natif |
| `25_threads` | la course de données prouvée par la mesure, verrouiller et les deux façons de se tromper, `Parallel.For` et l'accumulation locale, revenir sur le thread principal, le faux partage, annuler proprement, `Task`/`ValueTask`/`async void`, une file bornée producteur-consommateur |
| `26_binary` | plusieurs valeurs dans un entier, quantifier un float en deux octets, l'ordre des octets, varint et zigzag, un écrivain et un lecteur binaires, n'envoyer que ce qui a changé, détecter la corruption et la sauvegarde atomique, binaire contre JSON mesuré |
| `27_text` | découper sans allouer, la culture qui casse une sauvegarde, `string.Create` et `TryFormat`, le journal qui ne coûte rien quand il est éteint, comparer des noms coûte cher, un `char` n'est pas un caractère, un type qui sait se lire lui-même, formats et alignement |
| `28_reflect` | scanner les types, des données à côté du type, fabriquer un objet — trois façons trois prix, lire et écrire des propriétés par leur nom, fabriquer un générique à l'exécution, ce que le trim et IL2CPP suppriment |

Deux mini-moteurs sont inclus pour que rien ne demande d'installation :
`support/MiniGodot.cs` (un vrai `Node` avec `_Ready`, `_Process`, `QueueFree`,
`IsInstanceValid`, un `SceneTree` qui avance image par image, et un compteur de
franchissements de la frontière) et `support/MiniUnity.cs` (un `UnityObject` dont l'opérateur
`==` ment comme le vrai, `MonoBehaviour`, les trois boucles, composants, `Time`, IL2CPP,
`Transform` avec son compteur de traversées, `ScriptableObject`, `Rigidbody`, `Canvas` et un
chargeur d'assets à comptage de références).
Tout le reste est du C# pur.

### Le questionnaire

Parce que le code peut marcher sans que tu aies compris — c'est exactement pour ça que le
`NotDone` existe.

```
dotnet run -- quiz              147 questions à quatre réponses, selon ton profil
dotnet run -- quiz 16_memory    seulement une section
dotnet run -- quiz list         combien de questions par section
```

Chaque réponse est suivie d'une explication, **que tu aies juste ou faux** : tomber juste par
élimination n'apprend rien. Le score final liste ce qui est à revoir avec la commande pour
réviser directement la bonne section.

### Les autres commandes

```
dotnet run -- list              où j'en suis
dotnet run -- hint <id>         un indice
dotnet run -- solution <id>     la correction
dotnet run -- run <id>          relancer un exercice précis
dotnet run -- verify            vérifier que les 196 solutions passent
```

## 2. `demos/` — du vrai code, trois cibles

Un dossier par cible, parce que `using Godot;` et `using UnityEngine;` ne cohabitent pas dans
un même projet. Tu déposes celui qui correspond à ton moteur.

| Dossier | Comment ça se lance | Vérifié ? |
|---|---|---|
| `pure/` | `dotnet run` | **oui**, c'est un exécutable |
| `godot/` | `dotnet build`, puis à déposer dans un projet Godot 4 | **oui**, compilé contre GodotSharp 4.7.1 |
| `unity/` | à ouvrir dans l'éditeur Unity | **non**, `UnityEngine.dll` n'existe pas sur NuGet |

`pure/` contient un PCG32 à flux séparés, un ring buffer, de la virgule fixe Q16.16, un arbre
de comportement et un A\* à tas binaire. Ça marche tel quel sous les deux moteurs.

Et six pages :

| Fichier | Contenu |
|---|---|
| `demos/README.md` | l'index détaillé des trois dossiers |
| `demos/CHEATSHEET.md` | le condensé Godot : 10 règles, ~130 lignes « je veux X → j'écris Y », quel nœud choisir, les pièges. Chaque ligne compile |
| `demos/UNITY-CHEATSHEET.md` | le même côté Unity, où les pièges sont souvent l'inverse |
| `demos/GODOT-UNITY.md` | la table de traduction Godot ↔ Unity et le 80/20 du métier |
| `demos/godot/WEAKREFS.md` | les références faibles en détail : les trois durées de vie de Godot, où s'en servir, où surtout pas, les erreurs, le diagnostic des fuites |
| `demos/godot/SINGLETONS.md` | singletons et `static` vs instance en détail |

## Prérequis

- **.NET SDK 10** — c'est tout ce qu'il faut pour les 196 exercices
- .NET SDK 8 en plus, si tu veux lancer `demos/godot` et `demos/pure` : ils restent sur `net8.0`
  parce que c'est ce que cible Godot 4.7, et que `demos/pure` doit se coller tel quel dans un
  projet Godot ou Unity
- Godot 4.x en version .NET, si tu veux lancer `demos/godot` dans un vrai projet (facultatif)
- Unity, si tu veux lancer `demos/unity` (facultatif)

## Note sur le style

Le code ne contient volontairement **aucun commentaire**. Les explications sont dans les
fichiers Markdown, dans les consignes que le runner affiche, et dans les messages des
vérifications elles-mêmes. Les identifiants sont censés se suffire à eux-mêmes.

Les sections d'optimisation ne se contentent pas d'expliquer : elles **mesurent**. Le runner
compile en Release et autorise le code non sûr, donc les octets que tu lis sont ceux de ton jeu
une fois exporté.
