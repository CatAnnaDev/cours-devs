# Godot 4 + C# — la feuille de triche

Tout ce qui est écrit ici compile contre GodotSharp 4.7.1 : les lignes sont recopiées de
`godot/recipes/`, que `dotnet build` vérifie. Pas de code approximatif.

## Les 10 règles qui évitent 90 % des bugs

| # | Règle |
|---|---|
| 1 | `GetNode` dans `_Ready`, **jamais** dans `_Process`. On récupère une fois, on stocke. |
| 2 | Tout ce qui bouge se multiplie par `delta`. Sans exception. |
| 3 | Un `+=` sur un événement C# exige un `-=` dans `_ExitTree`. Sinon : fuite. |
| 4 | `QueueFree()` pour détruire. Jamais `Dispose()` : sur un nœud il ne libère **rien**. |
| 5 | Avant de toucher un nœud gardé en mémoire : `IsInstanceValid(node)`. |
| 6 | `static` pour le comportement, **jamais** pour l'état d'une partie. |
| 7 | Physique et mouvement dans `_PhysicsProcess`, affichage et input dans `_Process`. |
| 8 | Les noms qui traversent (`"jump"`, `"UI/Bar"`) vont dans un `static readonly StringName`. |
| 9 | Une propriété du moteur n'est pas un champ : on la lit **une fois** dans une variable locale. |
| 10 | On ne touche pas l'arbre pendant un callback physique : `CallDeferred`. |

---

## Je veux… → j'écris…

### Nœuds et scènes

| Je veux | J'écris |
|---|---|
| Un enfant | `GetNode<Label>("UI/Score")` dans `_Ready` |
| Sans planter s'il manque | `GetNodeOrNull<Label>("UI/Score")` |
| Un nœud à nom unique (`%`) | `GetNodeOrNull<Label>("%HealthBar")` |
| Mon parent | `GetParentOrNull<Node2D>()` |
| Le désigner dans l'inspecteur | `[Export] private Label _score;` |
| Le joueur, où qu'il soit | `GetTree().GetFirstNodeInGroup("player")` |
| Tous les ennemis | `GetTree().GetNodesInGroup("enemies")` |
| Compter sans allouer la liste | `GetTree().GetNodeCountInGroup("enemies")` |
| M'inscrire dans un groupe | `AddToGroup("enemies")` dans `_Ready` |
| Instancier une scène | `[Export] PackedScene _p;` puis `_p.Instantiate<Node2D>()` |
| Ajouter au monde | `AddChild(node)` |
| Ajouter à la scène courante | `GetTree().CurrentScene.AddChild(node)` |
| Changer de parent | `node.Reparent(newParent)` |
| Copier un nœud | `(Node2D)node.Duplicate()` |
| Détruire | `node.QueueFree()` — à la fin de la frame, toujours sûr |
| Le détruire tout de suite | `node.Free()` — **jamais** pendant un callback physique ou une itération |
| Savoir s'il est déjà condamné | `node.IsQueuedForDeletion()` |
| Faire ça **après** la frame | `CallDeferred(MethodName.Spawn)` |
| Idem pour une propriété | `SetDeferred(Node2D.PropertyName.Visible, false)` |
| Idem avec une lambda | `Callable.From(Spawn).CallDeferred()` |
| L'endormir | `SetProcess(false)` / `SetPhysicsProcess(false)` |
| L'endormir complètement | `ProcessMode = ProcessModeEnum.Disabled` |
| Qu'il tourne même en pause | `ProcessMode = ProcessModeEnum.Always` |

### Input

| Je veux | J'écris |
|---|---|
| Touche maintenue | `Input.IsActionPressed(Jump)` |
| Touche **enfoncée cette frame** | `Input.IsActionJustPressed(Jump)` |
| Touche relâchée cette frame | `Input.IsActionJustReleased(Jump)` |
| Un axe −1 → +1 | `Input.GetAxis("move_left", "move_right")` |
| Un stick, déjà normalisé | `Input.GetVector("move_left", "move_right", "move_up", "move_down")` |
| La pression d'une gâchette | `Input.GetActionStrength(Jump)` |
| La souris en coordonnées monde | `GetGlobalMousePosition()` |
| Cacher / capturer la souris | `Input.MouseMode = Input.MouseModeEnum.Captured` |
| Consommer l'événement | `GetViewport().SetInputAsHandled()` dans `_UnhandledInput` |

`GetVector` est déjà normalisé : c'est lui qui évite la diagonale 41 % trop rapide.

### Temps, attente, animation

| Je veux | J'écris |
|---|---|
| Attendre 2 s | `await ToSignal(GetTree().CreateTimer(2.0), SceneTreeTimer.SignalName.Timeout);` |
| Attendre une frame | `await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);` |
| Un timer réutilisable | un nœud `Timer` : `_timer.Timeout += OnTimeout; _timer.Start();` |
| Combien reste-t-il ? | `_timer.TimeLeft` |
| Animer une propriété | `CreateTween().TweenProperty(this, "position", target, 0.5);` |
| Animer avec une courbe | `.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out)` |
| Deux animations en même temps | `tween.Parallel().TweenProperty(...)` |
| Une pause dans la chaîne | `tween.TweenInterval(0.2)` |
| Appeler quelque chose à la fin | `tween.TweenCallback(Callable.From(QueueFree))` |
| Annuler un tween | `tween.Kill()` |
| Jouer une animation | `_animator.Play("hit")` |
| Savoir quand elle finit | `_animator.AnimationFinished += OnAnimationFinished;` |
| Le nombre d'images par seconde | `Engine.GetFramesPerSecond()` |
| Ralentir tout le jeu | `Engine.TimeScale = 0.5;` |
| Mettre en pause | `GetTree().Paused = true;` |

### Physique et collisions

| Je veux | J'écris |
|---|---|
| Déplacer un personnage | `Velocity = ...; MoveAndSlide();` dans `_PhysicsProcess` |
| Savoir s'il touche le sol | `IsOnFloor()` |
| La normale du sol | `GetFloorNormal()` |
| La pente maximale marchable | `FloorMaxAngle = Mathf.DegToRad(46f);` |
| Glisser le long d'un mur | `velocity.Slide(normal)` |
| Rebondir dessus | `velocity.Bounce(normal)` |
| Un rayon d'ici à là | `PhysicsRayQueryParameters2D.Create(from, to, CollisionMask, _exclude)` |
| Le lancer | `GetWorld2D().DirectSpaceState.IntersectRay(query)` |
| Lire le résultat | `hit["position"].AsVector2()`, `hit["collider"].As<Node>()` |
| Tout ce qui est dans un cercle | `PhysicsShapeQueryParameters2D` + `IntersectShape(query, 32)` |
| M'exclure du test | `new Array<Rid> { GetRid() }` |
| Réagir à une entrée dans la zone | `Area2D` : `BodyEntered += OnBodyEntered;` |
| Couper une zone proprement | `SetDeferred(PropertyName.Monitoring, false)` |
| La gravité du projet | `ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle()` |

Un projectile rapide **traverse les murs** si tu ne testes que sa position. Lance un rayon de
sa position précédente à la nouvelle : `godot/recipes/Combat.cs` → `Bullet`.

### Visuel et son

| Je veux | J'écris |
|---|---|
| Retourner un sprite | `_sprite.FlipH = true;` |
| Le teinter | `_sprite.Modulate = Colors.Red;` |
| Le teinter sans toucher ses enfants | `_sprite.SelfModulate = ...` |
| Une couleur en TSV | `Color.FromHsv(0.5f, 1f, 1f)` |
| Le mettre devant | `ZIndex = 10;` |
| Le grossir | `Scale = Vector2.One * 1.5f;` |
| Jouer un son | `_sound.Play();` |
| Baisser le volume | `_sound.VolumeDb = -6f;` |
| Varier la hauteur | `_sound.PitchScale = 1.2f;` |
| Écrire dans un label | `_label.Text = "PV 100";` |
| Une barre de vie | `ProgressBar` : `MaxValue` et `Value` |
| La taille de l'écran | `GetViewport().GetVisibleRect().Size` |
| La taille de la fenêtre | `DisplayServer.WindowGetSize()` |

### Les maths de tous les jours

| Je veux | J'écris |
|---|---|
| Aller vers une cible | `GlobalPosition.DirectionTo(target)` |
| La distance | `GlobalPosition.DistanceTo(target)` |
| Comparer des distances | `DistanceSquaredTo` — pas de racine |
| Viser | `LookAt(target)` ou `Rotation = (target - GlobalPosition).Angle();` |
| Tourner par le plus court chemin | `Mathf.AngleDifference(from, to)` |
| Interpoler un angle | `Mathf.LerpAngle(a, b, w)` |
| Avancer d'un pas fixe | `Mathf.MoveToward(from, to, delta)` |
| Lisser sans dépendre du FPS | `Mathf.Lerp(a, b, 1f - Mathf.Exp(-force * delta))` |
| Le pourcentage entre deux bornes | `Mathf.InverseLerp(from, to, value)` |
| Borner | `Mathf.Clamp(v, min, max)` |
| Boucler dans un intervalle | `Mathf.Wrap(370f, 0f, 360f)` → `10` |
| Aligner sur une grille | `Mathf.Snapped(37f, 16f)` → `32` |
| Un aller-retour | `Mathf.PingPong(t, 3f)` |
| Un modulo qui reste positif | `Mathf.PosMod(-1, 4)` → `3` |
| Des coordonnées de case | `Vector2I` |

### Aléatoire

| Je veux | J'écris |
|---|---|
| Un entier dans un intervalle | `_rng.RandiRange(1, 6)` |
| Un flottant | `_rng.RandfRange(0f, 1f)` |
| Rejouer le même donjon | `_rng.Seed = 1234;` |
| Ne pas le rejouer | `_rng.Randomize();` |
| Vite fait, sans instance | `GD.RandRange(1, 6)`, `GD.Randf()` |

Un `RandomNumberGenerator` par système : le donjon et le butin ne doivent pas se décaler
l'un l'autre. Détaillé et mesuré dans `pure/Rng.cs`.

### Ressources et flux de scènes

| Je veux | J'écris |
|---|---|
| Charger une texture | `ResourceLoader.Load<Texture2D>("res://art/hero.png")` |
| Charger une scène | `GD.Load<PackedScene>("res://scenes/enemy.tscn")` |
| Vérifier qu'un fichier existe | `ResourceLoader.Exists(path)` |
| Charger sans geler | `ResourceLoader.LoadThreadedRequest(path)` puis `LoadThreadedGetStatus` |
| Changer de scène | `GetTree().ChangeSceneToFile("res://scenes/menu.tscn")` |
| Recharger la scène | `GetTree().ReloadCurrentScene()` |
| Quitter | `GetTree().Quit()` |
| Écrire une sauvegarde | `FileAccess.Open("user://save.txt", FileAccess.ModeFlags.Write)` |
| Savoir pourquoi ça a échoué | `FileAccess.GetOpenError()` |

`res://` est **en lecture seule** dans un jeu exporté. Tout ce qui s'écrit va dans `user://`.

### Diagnostiquer

| Je veux | J'écris |
|---|---|
| Afficher | `GD.Print($"valeur = {x}")` |
| Un avertissement visible dans l'éditeur | `GD.PushWarning("...")` |
| Une erreur qui remonte | `GD.PushError("...")` |
| Du texte formaté | `GD.PrintRich("[b]gras[/b]")` |
| Voir les formes de collision | `GetTree().DebugCollisionsHint = true;` |
| Compter les nœuds vivants | `Performance.GetMonitor(Performance.Monitor.ObjectNodeCount)` |
| Surveiller la mémoire | `Performance.GetMonitor(Performance.Monitor.MemoryStatic)` |
| Savoir si on est en debug | `OS.IsDebugBuild()` |
| Ce qui a fui, nommément | `Node.PrintOrphanNodes()` |
| Combien de nœuds orphelins | `Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount)` |

---

## Quel nœud pour quel besoin ?

| Besoin | Nœud |
|---|---|
| Un point dans l'espace, un conteneur | `Node2D` |
| Une image | `Sprite2D` |
| Une image animée | `AnimatedSprite2D` |
| Un personnage qu'on pilote | `CharacterBody2D` + `CollisionShape2D` |
| Un objet soumis à la physique | `RigidBody2D` |
| Un mur, un sol | `StaticBody2D` |
| Une zone qui détecte | `Area2D` |
| La caméra | `Camera2D` |
| De l'interface qui ne bouge pas avec la caméra | `CanvasLayer` |
| Du texte, un bouton, une barre | `Label`, `Button`, `ProgressBar` |
| Un compte à rebours | `Timer` |
| Un son | `AudioStreamPlayer` (ou `2D` pour du spatialisé) |
| Des animations de propriétés | `AnimationPlayer` |
| Un décor en cases | `TileMapLayer` |
| Des particules | `GpuParticles2D` |
| Un rayon permanent | `RayCast2D` |
| Un balayage de forme | `ShapeCast2D` |

---

## L'arbre d'une scène type

```
Player            CharacterBody2D   ← le script vit ici
├── Sprite        AnimatedSprite2D
├── Collision     CollisionShape2D
├── Hurtbox       Area2D            ← ce qui me touche
│   └── Shape     CollisionShape2D
├── Health        Node              ← un composant, pas de l'heritage
├── Coyote        Timer
└── Camera        Camera2D
```

Le script est sur la **racine**, jamais éparpillé. Les capacités sont des enfants : c'est ce
qui rend `HealthComponent` réutilisable sur un ennemi comme sur une caisse.

---

## `[Export]` : toutes les formes

| Ce que je veux dans l'inspecteur | J'écris |
|---|---|
| Un nombre réglable | `[Export] public float Speed { get; set; } = 300f;` |
| Avec un curseur borné | `[Export(PropertyHint.Range, "0,600,10")] public int Speed { get; set; }` |
| Une scène à instancier | `[Export] public PackedScene Bullet { get; set; }` |
| Un nœud à désigner | `[Export] public Node2D Target { get; set; }` |
| Un chemin de nœud | `[Export] public NodePath TargetPath { get; set; }` |
| Une image | `[Export] public Texture2D Icon { get; set; }` |
| Du texte multiligne | `[Export(PropertyHint.MultilineText)] public string Dialogue { get; set; }` |
| Un fichier | `[Export(PropertyHint.File, "*.tscn")] public string Scene { get; set; }` |
| Un dossier | `[Export(PropertyHint.Dir)] public string Folder { get; set; }` |
| Une liste | `[Export] public Array<string> Tags { get; set; } = new();` |
| Des couches de collision | `[Export(PropertyHint.Layers2DPhysics)] public uint Mask { get; set; }` |
| Une couleur sans alpha | `[Export(PropertyHint.ColorNoAlpha)] public Color Tint { get; set; }` |
| Un choix dans un enum | `[Export] public Tween.TransitionType Curve { get; set; }` |
| Ranger l'inspecteur | `[ExportGroup("Mouvement")]`, `[ExportCategory("Combat")]` |

Toutes ces formes sont dans `godot/recipes/Snippets.cs`, qui compile.

---

## Les signaux qu'on branche le plus

| Signal | Sur quoi | Quand |
|---|---|---|
| `BodyEntered` | `Area2D` | un corps entre dans la zone |
| `AreaEntered` | `Area2D` | une autre zone entre |
| `Timeout` | `Timer` | le compte à rebours finit |
| `AnimationFinished` | `AnimationPlayer` | l'animation se termine |
| `Pressed` | `Button` | on clique |
| `Finished` | `Tween` | l'animation de propriété finit |
| `TreeExiting` | n'importe quel `Node` | il quitte l'arbre |
| `Timeout` | `SceneTreeTimer` | ce qu'on `await` |

Déclarer le sien : `[Signal] public delegate void DiedEventHandler();` puis
`EmitSignal(SignalName.Died)`. Écouter : `truc.Died += OnDied;` — **et `-=` dans `_ExitTree`**.

---

## `_Process` ou `_PhysicsProcess` ?

| | `_Process` | `_PhysicsProcess` |
|---|---|---|
| Fréquence | variable (le FPS) | fixe (60/s) |
| Pour | UI, caméra, input, timers, effets | déplacement, collisions, IA, `MoveAndSlide` |
| Piège | dépend de la machine du joueur | ne pas y mettre de dessin |

La caméra doit passer **après** sa cible : `ProcessPriority = 100;`.

---

## static ou pas static ?

**La question :** est-ce qu'il peut logiquement en exister deux ?

| Cas | Réponse |
|---|---|
| `ApplyArmor(damage, armor)` — fonction pure | ✓ `static` |
| Chemins de scènes, constantes, `StringName` | ✓ `static readonly` / `const` |
| PV, inventaire, position | ✗ jamais |
| Ce qui doit repartir à zéro entre 2 parties | ✗ jamais |
| Le gestionnaire audio, le save | singleton (instance derrière `static Instance`) |

**Comportement → static. État → instance.**

---

## struct ou class ?

| | `struct` | `class` |
|---|---|---|
| Passé à une méthode | **copié** | **partagé** |
| Bon pour | petites valeurs : position, dégâts, couleur | tout le reste |
| Défaut recommandé | `readonly record struct` | `sealed class` |

Si tu hésites : **`class`**. Le `struct` mal utilisé crée des bugs silencieux (voir
`godot/bases/StructVsClass.cs`).

---

## Quelle collection ?

| Besoin | Type |
|---|---|
| Taille connue et figée | `float[]` |
| Ça grossit, l'ordre compte | `List<T>` |
| Retrouver par identifiant | `Dictionary<K,V>` |
| « Est-ce que je l'ai déjà vu ? » | `HashSet<T>` |
| File d'attente | `Queue<T>` |
| Annuler / historique | `Stack<T>` |
| Ça doit traverser vers le moteur | `Godot.Collections.Array<T>` |

Les deux dernières familles ne partagent **pas** leur mémoire : convertir recopie.

---

## Les pièges qui coûtent une soirée

| ✗ Faux | ✓ Juste |
|---|---|
| `if (a == b)` sur des `float` | `Mathf.IsEqualApprox(a, b)` |
| `int ratio = current / max;` | `float ratio = (float)current / max;` |
| `GetNode` dans `_Process` | cacher dans `_Ready` |
| `node.Free()` pendant un callback physique | `node.QueueFree()` |
| `using var node = new Node();` | `QueueFree()` — `Dispose` ne libère rien sur un nœud, il fabrique une fuite |
| Instancier un nœud puis perdre sa référence sans `AddChild` | il fuit pour toujours : `Node.PrintOrphanNodes()` te le nomme |
| `if (node != null)` sur un nœud libéré | `if (IsInstanceValid(node))` |
| `Position += Vector2.Right * 5f;` | `... * speed * (float)delta;` |
| `bus.Event += Handler;` sans `-=` | `-=` dans `_ExitTree` |
| Concaténer des strings chaque frame | ne mettre à jour que si la valeur a changé |
| `static List<Enemy>` qu'on ne vide jamais | retirer dans `_ExitTree` |
| `EmitSignal("died")` | `EmitSignal(SignalName.Died)` |
| `Input.IsActionPressed("jump")` par frame | un `static readonly StringName` |
| Lire `Position` trois fois dans un calcul | une variable locale, puis réécrire une fois |
| `input.X + input.Y` sans normaliser | `Input.GetVector(...)` |
| Ajouter un nœud dans `_PhysicsProcess` | `CallDeferred` |
| Écrire dans `res://` | `user://` |
| Tester la position d'un projectile rapide | un rayon de l'ancienne à la nouvelle position |
| Repousser hors d'un mur sans toucher la vitesse | annuler la composante normale aussi |

---

## Le coût invisible : la frontière C# ↔ natif

Une propriété du moteur n'est pas un champ : **chaque accès est un appel natif**. Et comme ça
n'alloue rien, ça n'apparaît dans aucun profil mémoire — c'est pour ça que personne ne le trouve.

| Ce qui coûte | Mesuré |
|---|---|
| lire `Position` plusieurs fois par calcul | version naïve 4 appels, version groupée 2. Sur 1000 nœuds à 60 fps : **240 000 contre 120 000 par seconde** |
| `Emit("died")` avec une chaîne littérale | 100 émissions = **100 objets** ; nom gardé = **0** |
| un signal à deux arguments | **104 octets** par émission, mesurés dans le moteur ; un `event Action<int>` : **0** |
| lire 1000 entiers **depuis** un tableau moteur vers une `List<int>` | **8464 octets**. Les lire à l'index sans convertir : **0** |
| un signal **sans** argument | gratuit, tableau vide partagé |

Chiffres mesurés dans `../csharplings/` section `18_bridge`.

---

## Quand ça rame : où regarder, dans l'ordre

1. **Le profileur** (Debugger → Profiler), pas au hasard. Regarde d'abord si c'est le script ou le rendu.
2. **Combien de nœuds ?** `Performance.Monitor.ObjectNodeCount`. Des milliers de nœuds coûtent avant tout le reste.
3. **Des `GetNode` dans `_Process` ?** C'est le premier réflexe à vérifier.
4. **Des propriétés moteur lues en boucle ?** Voir juste au-dessus.
5. **La mémoire monte-t-elle sans redescendre ?** Une `static` non vidée, une liste, un `+=` sans `-=`.
6. **Du LINQ dans `_Process` ?** Chaque `Where().Select()` alloue.
7. **Des `Instantiate` en boucle ?** Pool.
8. **Trop de raycasts par frame ?** Groupe-les, ou espace-les dans le temps.

---

## Ce qui casse à l'export, mais pas dans l'éditeur

| Symptôme | Cause |
|---|---|
| La sauvegarde ne s'écrit pas | `res://` est en lecture seule → `user://` |
| Une ressource est introuvable | casse du nom de fichier : Windows tolère, Linux et Android non |
| Une scène chargée par nom construit à l'exécution manque | rien ne la référence, elle n'est pas embarquée → un `[Export] PackedScene` |
| Des raccourcis de debug actifs chez le joueur | encadre-les avec `OS.IsDebugBuild()` |
| Des `GD.Print` partout | ils restent dans l'export : ça coûte, et ça fuit des infos |
| Un script `[Tool]` casse l'éditeur | il tourne **dans** l'éditeur, `_Ready` compris |

---

## Les conventions qui surprennent

| | Godot 2D |
|---|---|
| Axe Y | **descend** — `Vector2.Up` vaut `(0, -1)` |
| Rotations | en **radians** (`Mathf.DegToRad` pour convertir) |
| Unités | pixels |
| Physique | 60 pas par seconde par défaut |
| Couches de collision | un **bit** par couche : 1, 2, 4, 8… |
| `delta` | un `double`, à caster en `(float)` |

---

## Trois façons de gérer un « manager » global

| Approche | Fichier | Quand |
|---|---|---|
| Autoload + `static Instance` | `godot/singletons/GameState.cs` | il a besoin de l'arbre de scène (signaux, `_Process`) |
| `Lazy<T>` C# pur | `godot/singletons/SaveService.cs` | il ne touche pas à l'arbre (save, config, réseau) |
| `static class` | `godot/singletons/StaticVsInstance.cs` | que des fonctions et des constantes |

Détail qui compte : assigner `Instance` dans **`_EnterTree`** (pas `_Ready`), et la remettre à
`null` dans `_ExitTree`.

---

## Mémoire : les 5 choses à savoir

1. **Un `Node` C#, c'est deux objets** : le wrapper C# et l'objet natif. `QueueFree()` tue le natif, pas le wrapper. D'où `IsInstanceValid`.
2. **`Dispose()` fait l'inverse** : il jette le wrapper et laisse le natif vivre. Sur un nœud, ça ne libère rien — ça fabrique une fuite. Mesuré, détaillé dans `godot/WEAKREFS.md`.
3. **Ce qui retient un objet en vie** : une variable `static`, une `List` jamais vidée, un événement jamais désabonné. C'est presque toujours l'un des trois.
4. **Un objet vide coûte 24 octets** d'en-tête. 1000 structures dans un tableau, c'est **une** allocation ; 1000 objets, c'est 1000 allocations éparpillées.
5. **`WeakReference<T>`** pointe sans retenir — mais sur un nœud elle répond à la mauvaise question : le wrapper C# peut survivre au natif. Pour un nœud, c'est `IsInstanceValid` ou un identifiant d'instance. Détaillé dans `godot/WEAKREFS.md`.

---

## Par où commencer dans les fichiers

Dans l'ordre, du plus utile au plus pointu :

```
godot/bases/NodeLifecycle.cs      quand chaque fonction est appelee — LANCE CELUI-LA EN PREMIER
godot/bases/GetNodeWays.cs        les 6 facons de recuperer un noeud
godot/gameplay/DeltaTime.cs       pourquoi tout se multiplie par delta
godot/pieges/Pieges.cs            faux / juste, cote a cote
godot/recipes/Snippets.cs         toutes les lignes de cette page, compilees
godot/recipes/Movement.cs         ennemi qui poursuit, saut qui pardonne, camera qui suit
godot/recipes/Combat.cs           projectile qui ne traverse pas, degats de zone, cooldown, HUD
godot/recipes/Flow.cs             pause, fondu de scene, porte a cle, sauvegarde, pool
godot/bases/NullSafety.cs         ?. ?? ??= : ne plus jamais crasher sur null
godot/bases/CollectionsChoice.cs  quelle collection pour quel besoin
godot/bases/StructVsClass.cs      copie vs partage
godot/gameplay/Components.cs      composer plutot qu'heriter (HealthComponent)
godot/gameplay/CharacterMovement.cs  MoveAndSlide complet, coyote time, buffer de saut
godot/gameplay/SignalsVsEvents.cs signaux Godot vs events C#
godot/gameplay/StateMachine.cs    machine a etats (version simple + version propre)
godot/gameplay/ObjectPool.cs      recycler au lieu d'allouer
godot/gameplay/AsyncInGodot.cs    await, Tween, threads
godot/bridge/Marshalling.cs       le cout des allers-retours vers le moteur
godot/data/GameData.cs            Resource partagee, chargement en tache de fond
godot/singletons/                 les managers globaux           -> voir godot/SINGLETONS.md
godot/weakrefs/                   references faibles et fuites   -> voir godot/WEAKREFS.md
```

Tout ce dossier compile : `cd demos/godot && dotnet build`.

---

## Les autres pages

| Fichier | Contenu |
|---|---|
| `UNITY-CHEATSHEET.md` | le même condensé côté Unity, où les pièges sont souvent l'inverse |
| `GODOT-UNITY.md` | table de traduction Godot ↔ Unity, et le 80/20 de ce qu'on écrit vraiment |
| `godot/WEAKREFS.md` | les références faibles en détail : les trois durées de vie de Godot, les cinq façons de pointer un nœud, les pièges du cache et du bus faibles |
| `godot/SINGLETONS.md` | singletons et `static` en détail |
| `../pure/` | du C# pur exécutable : aléatoire à graine, ring buffer, virgule fixe, arbre de comportement, A\* |
| `../csharplings/` | 124 exercices à réparer, avec runner, corrections et questionnaire |
