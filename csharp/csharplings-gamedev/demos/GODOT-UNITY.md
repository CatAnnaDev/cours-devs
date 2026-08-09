# Ce qu'on utilise vraiment en gamedev — et la traduction Godot ↔ Unity

Deux moteurs, le même C#. Ce qui change, c'est le vocabulaire, une différence
d'architecture qu'il faut comprendre une bonne fois, et une taxe que chacun te fait payer.

Si tu n'utilises qu'un seul des deux, dis-le au programme d'exercices et il se tait sur
l'autre : `cd ../csharplings && dotnet run -- config unity` (ou `godot`).

---

## 1. La différence de fond

| | Godot | Unity |
|---|---|---|
| Brique de base | un **Node**, qui EST déjà quelque chose (Sprite2D, Area2D…) | un **GameObject** vide, qui n'est rien |
| On ajoute du comportement | en héritant (`class Player : CharacterBody2D`) ou en ajoutant des nœuds enfants | en accrochant des **components** (`MonoBehaviour`) |
| Composition | possible, recommandée | obligatoire, c'est le seul modèle |
| Un script | hérite d'un type de nœud | hérite de `MonoBehaviour` |

En clair : sous Godot tu **es** un `CharacterBody2D`. Sous Unity tu es un GameObject
qui **a** un `Rigidbody` et un `PlayerController`.

Conséquence pratique : le pattern composant de `demos/godot/gameplay/Components.cs`
(`HealthComponent` en nœud enfant) est la façon d'écrire du Godot qui ressemble à
Unity — et c'est en général la meilleure façon d'écrire du Godot tout court.

---

## 2. La table de traduction

### Cycle de vie

| Godot | Unity |
|---|---|
| `_EnterTree()` | `Awake()` |
| `_Ready()` | `Start()` |
| `_Process(double delta)` | `Update()` + `Time.deltaTime` |
| `_PhysicsProcess(double delta)` | `FixedUpdate()` + `Time.fixedDeltaTime` |
| `_ExitTree()` | `OnDestroy()` / `OnDisable()` |
| `_Input(InputEvent e)` | `Update()` + `Input.*`, ou le New Input System |

Piège : sous Godot `delta` est un **`double`**, il faut caster en `(float)`.
Sous Unity `Time.deltaTime` est déjà un `float`.

### Trouver et créer des choses

| Godot | Unity |
|---|---|
| `GetNode<Label>("UI/Score")` | `transform.Find("UI/Score").GetComponent<Text>()` |
| `GetNodeOrNull<T>(path)` | `GetComponent<T>()` (rend `null` si absent) |
| `[Export] private Label _score;` | `[SerializeField] private Text _score;` |
| `_prefab.Instantiate()` | `Instantiate(prefab)` |
| `AddChild(node)` | `transform.SetParent(parent)` |
| `node.QueueFree()` | `Destroy(gameObject)` |
| `IsInstanceValid(node)` | `node != null` (Unity surcharge `==`) |

### Le reste

| Godot | Unity |
|---|---|
| `GD.Print(x)` | `Debug.Log(x)` |
| `Position`, `GlobalPosition` | `transform.localPosition`, `transform.position` |
| `[Signal]` + `EmitSignal` | `UnityEvent`, ou un `event Action` C# |
| Autoload | singleton `MonoBehaviour` + `DontDestroyOnLoad` |
| `Resource` (`.tres`) | `ScriptableObject` |
| `Area2D` + `BodyEntered` | `Collider` en `isTrigger` + `OnTriggerEnter` |
| `CharacterBody2D.MoveAndSlide()` | `CharacterController.Move()` |
| `CreateTween()` | coroutine, ou DOTween |
| `await ToSignal(timer, ...)` | `yield return new WaitForSeconds(...)` |
| `Input.IsActionPressed("jump")` | `Input.GetButton("Jump")` |
| `GetTree().ChangeSceneToFile(...)` | `SceneManager.LoadScene(...)` |

`Mathf.Lerp`, `Mathf.Clamp`, `Vector2`, `Vector3` : **identiques** des deux côtés.

Une vraie différence : en 2D, **Godot a Y qui descend**, Unity a Y qui monte.
`Vector2.Up` vaut `(0, -1)` sous Godot et `(0, 1)` sous Unity.

---

## 3. Le 80/20 : ce que tu écris tous les jours

Par ordre de fréquence réelle. C'est ça qu'il faut maîtriser, le reste attendra.

| Ce que tu écris tout le temps | Où l'apprendre |
|---|---|
| `float`, casts, division entière | `csharplings` 02_types |
| `Vector2` : direction, distance, portée | `csharplings` vectors1 |
| Tout multiplier par `delta` | `csharplings` godot2, `demos/godot/gameplay/DeltaTime.cs` |
| `Lerp` / `MoveToward` pour lisser | `csharplings` smoothing1 |
| Cooldowns et timers | `csharplings` timers1 |
| `List<T>` et `Dictionary<K,V>` | `csharplings` 06_collections |
| `enum` + `switch` pour les états | `csharplings` enums1, `demos/godot/gameplay/StateMachine.cs` |
| Classes, propriétés, interfaces | `csharplings` 07_oop |
| Events pour découpler | `csharplings` events1, `demos/godot/gameplay/SignalsVsEvents.cs` |
| Grilles et tilemaps | `csharplings` grid1 |
| Object pooling | `csharplings` pool1, `demos/godot/gameplay/ObjectPool.cs` |
| Tout multiplier par `delta`, mais pour de vrai | `csharplings` `20_time` : interpolation entre pas de physique, timers sans dérive, indépendance au framerate prouvée à deux fréquences |
| Ne pas traverser les murs | `csharplings` `21_physics` : collision balayée, glissement, requêtes sans allocation |
| Ranger mille entités sans ramer | `csharplings` `17_ecs` : colonnes, masques, itération sans allocation |

### Ce qui sert nettement moins qu'on croit

- **L'héritage profond.** `Enemy : Character : Entity : ...` est un piège classique.
  En pratique : composition + interfaces. Deux niveaux d'héritage maximum.
- **LINQ dans une boucle de jeu.** Parfait pour du setup ou de l'UI, mais chaque
  `Where().Select()` alloue. Dans un `_Process`, écris la boucle.
- **`async`/`await`.** Utile sous Godot. Sous Unity, la culture est aux coroutines
  (ou UniTask). Ne t'y attaque pas en premier.
- **Les génériques compliqués.** Savoir lire `List<T>` suffit longtemps.
- **Les design patterns du livre.** Tu as besoin de trois choses : composant,
  machine à états, et un singleton ou deux. Le reste viendra si le besoin arrive.

---

## 4. La taxe de chaque moteur

C'est la partie que personne ne raconte, et elle est différente des deux côtés. Deux sections
d'exercices entières y passent, et elles ne sortent que dans le profil correspondant.

### Godot : la frontière C# ↔ natif — `csharplings` `18_bridge`

Ton code C# et le moteur sont deux mondes. Chaque aller-retour se paie, et **ça n'alloue rien**,
donc ça n'apparaît dans aucun profil de ramasse-miettes.

| Ce qui coûte | Mesuré |
|---|---|
| une propriété comme `Position` n'est **pas** un champ : chaque lecture est un appel natif | un déplacement écrit naïvement paie 4 franchissements, groupé 2. Sur 1000 objets à 60 fps : **240 000 contre 120 000 appels par seconde** |
| une chaîne littérale là où le moteur attend un nom se convertit implicitement | 100 `Emit("died")` fabriquent **100 objets** ; le même nom en `static readonly`, **zéro** |
| un signal fait voyager ses arguments dans un tableau | **104 octets** par émission à deux arguments, mesurés dans le moteur, soit 6240 par seconde à 60 images. Un `event Action<int>` : **0**. Sans argument : **0** aussi |
| un tableau moteur et une `List<T>` ne partagent pas leur mémoire | recopier 1000 entiers du moteur vers une `List<int>` : **8464 octets**. Les lire à l'index sans convertir : **0** |

### Unity : la plateforme — `csharplings` `19_unity`

Chez Unity la taxe n'est pas la frontière, c'est ce que l'éditeur et le compilateur imposent.
Et plusieurs pièges sont **l'exact inverse** de ceux de Godot.

| Ce qui surprend | Le détail |
|---|---|
| `obj == null` rend **true** sur un objet détruit, `obj is null` rend **false** | Godot exige `IsInstanceValid` parce que le test null ment par omission ; Unity ment par excès. Même cause : deux objets, un managé et un natif |
| Unity sérialise les **champs**, jamais les propriétés, et pas les `Dictionary` | sans un mot d'avertissement. D'où l'aplatissement en deux listes |
| chaque `Update()` est un appel du moteur vers ton code | 1000 scripts sur 10 frames = **10 000 franchissements**, un manager = **10** |
| `renderer.material` **clone** au premier accès | cent ennemis teintés, cent matériaux natifs que le GC ne prendra jamais. La vraie réponse est `MaterialPropertyBlock` |
| `Time.deltaTime` lu depuis `FixedUpdate` rend le **pas fixe** | le code a l'air correct des deux côtés |
| IL2CPP compile en avance | un type que personne n'instancie n'a pas de constructeur généré : une fabrique par réflexion marche dans l'éditeur et **échoue sur console** |
| les `static` survivent à la partie | dès que le rechargement de domaine est désactivé, événements statiques compris |

Le condensé de tout ça : `CHEATSHEET.md` pour Godot, `UNITY-CHEATSHEET.md` pour Unity.

---

### Les erreurs qui coûtent le plus cher en gamedev

1. Oublier `delta` → le jeu va deux fois plus vite sur un écran 120 Hz.
2. `GetNode` / `GetComponent` dans `_Process` / `Update` → chute de framerate.
3. `Instantiate` en boucle sans pool → à-coups du garbage collector.
4. `+=` sur un event sans `-=` → fuite mémoire, et des callbacks sur des objets morts.
5. Comparer des `float` avec `==` → la condition ne se déclenche jamais.
6. Mettre l'état de la partie en `static` → rien ne se réinitialise entre deux parties.

Les six sont couverts, avec le code faux et le code juste côte à côte, dans
`demos/godot/pieges/Pieges.cs` et `demos/CHEATSHEET.md`.

---

## 5. Si tu passes de l'un à l'autre

**Unity → Godot** : ta réaction sera « où sont les components ? ». Réponse : les
nœuds enfants les remplacent. Garde ton réflexe de composition, c'est le bon.

**Godot → Unity** : ta réaction sera « pourquoi mon GameObject ne fait rien ? ».
Parce qu'il est vide. Tout comportement vient d'un component que tu ajoutes.
Et pense à `Time.deltaTime`, il n'arrive pas en paramètre.

Dans les deux sens, le C# ne change pas. Sur les 124 exercices, **100 sont du C# pur** qui
servent quel que soit le moteur — les vecteurs, les timers, l'ECS, le temps, la physique,
l'optimisation, la mémoire. Seuls 24 sont spécifiques, 12 par moteur.

Et `demos/pure/` est entièrement réutilisable des deux côtés : un aléatoire à graine à flux
séparés, un ring buffer, de la virgule fixe pour du déterministe, un arbre de comportement,
un A\* à tas binaire. C'est du C# et rien d'autre, ça se lance avec `dotnet run`.
