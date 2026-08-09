# demos — du vrai code, pas des exercices

Trois dossiers, un par cible. Chaque fichier raconte ce qu'il fait pendant qu'il tourne.

```
pure/    C# pur, sans moteur      → dotnet run          (exécutable, vérifié)
godot/   Godot 4 en C#            → dotnet build        (compilé contre GodotSharp 4.7.1)
unity/   Unity en C#              → à ouvrir dans Unity  (non vérifié par machine, voir plus bas)
```

La séparation par moteur n'est pas cosmétique : `godot/` et `unity/` ne peuvent pas cohabiter
dans le même projet, les deux `using` s'excluent. Tu déposes le dossier qui correspond à ton
moteur, et tu ignores l'autre.

---

## `pure/` — C# pur, ça tourne dans le terminal

```
cd demos/pure
dotnet run              tout
dotnet run astar        une seule démo
dotnet run -- list      la liste
```

| Fichier | Ce que tu y trouves |
|---|---|
| `Rng.cs` | PCG32 : aléatoire à graine, **flux séparés** (ouvrir un coffre ne décale plus le donjon), tirage pondéré, mélange, gaussienne |
| `RingBuffer.cs` | historique à taille fixe : buffer d'entrée, anneau de snapshots réseau. Alloué une fois, jamais réalloué |
| `FixedPoint.cs` | virgule fixe Q16.16 : ce qu'on achète n'est pas l'exactitude, c'est que **65530 sera 65530 partout** |
| `BehaviourTree.cs` | sélecteur, séquence à mémoire, inverseur, décorateur de cooldown, blackboard. Le garde qui patrouille, poursuit et tire |
| `AStar.cs` | A\* avec tas binaire, et la leçon mesurée : **204 → 20 cases** sur terrain dégagé, **209 → 170** dès qu'un mur force un détour |
| `WeakRefs.cs` | **le banc de test de `godot/WEAKREFS.md`** : 18 affirmations sur `WeakReference`, `ConditionalWeakTable`, le cache et le bus faibles, vérifiées en Debug et en Release |

Ça marche tel quel sous Godot comme sous Unity : c'est du C# et rien d'autre.

---

## `godot/` — Godot 4

Le dossier contient un `Godot.Demos.csproj` qui sert **uniquement** à typechecker les fichiers
hors de l'éditeur :

```
cd demos/godot
dotnet build            0 warning, 0 erreur contre l'API réelle
```

| Dossier | Contenu |
|---|---|
| `bases/` | cycle de vie d'un nœud, les 6 façons de récupérer un nœud, `?.` et `??`, struct contre class, quelle collection |
| `gameplay/` | delta time, composants, signaux contre events, machine à états, pooling, `async`, **`CharacterMovement.cs`** (`MoveAndSlide`, coyote time, buffer de saut, saut à hauteur variable) |
| `recipes/` | **les recettes derrière le cheatsheet** : `Snippets.cs` (toutes les lignes de `CHEATSHEET.md`, compilées), `Movement.cs` (ennemi qui poursuit, saut qui pardonne, caméra qui suit), `Combat.cs` (projectile qui ne traverse pas, dégâts de zone, cooldown, HUD silencieux), `Flow.cs` (pause, fondu de scène, porte à clé, sauvegarde `user://`, pool) |
| `bridge/` | **`Marshalling.cs`** : `StringName` et `NodePath` gardés, lectures de `Position` groupées, `Variant`, collections moteur recopiées, `CallDeferred` |
| `data/` | **`GameData.cs`** : `Resource` personnalisée avec `[GlobalClass]` (l'équivalent du `ScriptableObject`), 500 instances pour une fiche, chargement en tâche de fond avec progression |
| `singletons/` | autoload + `static Instance`, `Lazy<T>` pur, classe statique — détails dans `SINGLETONS.md` |
| `weakrefs/` | `WeakReference`, identifiants d'instance, `ConditionalWeakTable`, cache faible, bus faible, le double objet wrapper/natif — **tout est expliqué dans `godot/WEAKREFS.md`**, et `WeakRefsSelfTest.cs` le vérifie dans le moteur (28/28). `WeakCache.cs` et `WeakEventBus.cs` sont du C# pur réutilisable ailleurs |
| `pieges/` | faux / juste côte à côte |

Deux pages longues vivent dans ce dossier :

- **`godot/WEAKREFS.md`** — les références faibles pour de vrai : les trois durées de vie de Godot, pourquoi `System.WeakReference<Node>` répond à la mauvaise question, les cinq façons de pointer un nœud, le cache et le bus faibles avec leurs pièges, et comment diagnostiquer une fuite
- **`godot/SINGLETONS.md`** — les managers globaux, et pourquoi un `static` d'état fuit

---

## `unity/` — Unity

| Fichier | Contenu |
|---|---|
| `MonoBehaviourLifecycle.cs` | l'ordre réel : `Awake` → `OnEnable` → `Start` → `FixedUpdate` → `Update` → `LateUpdate` → `OnDisable` → `OnDestroy`, plus `OnValidate` |
| `DestroyedObjects.cs` | `== null` rend **true** sur un objet détruit, `is null` rend **false**, et `?.` ne protège de rien. `Destroy` différé contre `DestroyImmediate` |
| `SerializationRules.cs` | ce que Unity sérialise et ce qu'il ignore en silence, dictionnaire aplati en deux listes, `ScriptableObject` partagé |
| `UpdateManager.cs` | le manager pattern complet : files d'ajout/retrait, retrait par échange, `Time.deltaTime` lu une fois |
| `CoroutinesAndWaits.cs` | `WaitForSeconds` mis en cache, `yield return null`, `WaitForSecondsRealtime` pour ce qui ignore la pause |
| `MaterialsAndPhysics.cs` | `.material` qui clone contre `MaterialPropertyBlock` (la vraie réponse), et les requêtes physiques `NonAlloc` |

**Ces fichiers ne sont pas vérifiés par machine.** `UnityEngine.dll` est livré avec l'éditeur et
n'existe pas sur NuGet, donc contrairement à `godot/` et `pure/` je ne peux pas les compiler ici.
Ils s'en tiennent volontairement à de l'API stable depuis des années. Si quelque chose ne passe
pas dans ta version d'Unity, c'est là qu'il faut regarder en premier.

---

## Les pages

| Fichier | Contenu |
|---|---|
| `CHEATSHEET.md` | le condensé Godot : les 7 règles, « je veux X → j'écris Y », les pièges |
| `UNITY-CHEATSHEET.md` | le même pour Unity |
| `GODOT-UNITY.md` | la table de traduction Godot ↔ Unity et le 80/20 du métier |
| `godot/WEAKREFS.md` | les références faibles en détail : quoi, quand, où surtout pas, les erreurs, le diagnostic |
| `godot/SINGLETONS.md` | singletons et `static` en détail |
| `../csharplings/` | 124 exercices à réparer, avec runner, corrections et questionnaire |
