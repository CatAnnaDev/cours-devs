# Unity + C# — la feuille de triche

Le pendant de `CHEATSHEET.md`, côté Unity. Les pièges ne sont pas les mêmes — plusieurs sont
l'exact inverse de ceux de Godot.

## Les 8 règles qui évitent 90 % des bugs

| # | Règle |
|---|---|
| 1 | Sur un objet Unity, on teste `== null`. **Jamais** `is null`, jamais `?.` |
| 2 | `GetComponent` dans `Awake`/`Start`, stocké. Jamais dans `Update` |
| 3 | Tout ce qui bouge se multiplie par `Time.deltaTime`. Sans exception |
| 4 | S'abonner dans `OnEnable`, se désabonner dans `OnDisable`. Pas `Start`/`OnDestroy` |
| 5 | Unity sérialise les **champs**, pas les propriétés |
| 6 | `renderer.material` **clone**. Pour une couleur par instance : `MaterialPropertyBlock` |
| 7 | Une `WaitForSeconds` réutilisée est gratuite ; `new` à chaque `yield` ne l'est pas |
| 8 | Les `static` ne se réinitialisent **pas** entre deux parties dans l'éditeur |

---

## Je veux… → j'écris…

| Je veux | J'écris |
|---|---|
| Un composant du même objet | `TryGetComponent(out Rigidbody body)` dans `Awake` |
| Un composant d'un enfant | `GetComponentInChildren<T>()` (attention : inclut l'objet lui-même) |
| Le désigner dans l'inspecteur | `[SerializeField] private Text _score;` |
| Créer un objet | `Instantiate(prefab, position, rotation)` |
| Détruire | `Destroy(gameObject)` (fin de frame) |
| Attendre 2 secondes | `yield return _twoSeconds;` avec un champ `static readonly` |
| Attendre en ignorant la pause | `yield return new WaitForSecondsRealtime(2f);` |
| Animer une propriété | une coroutine, ou DOTween |
| Prévenir les autres | `public event Action<int> Died;` (et `-=` dans `OnDisable`) |
| Un réglage éditable | `[SerializeField, Range(0f, 10f)] private float _speed = 5f;` |
| Des données partagées | un `ScriptableObject` avec `[CreateAssetMenu]` |
| Afficher un truc | `Debug.Log($"valeur = {x}")` |
| Un avertissement visible | `Debug.LogWarning("...", this)` — le second argument rend l'objet cliquable |

---

## `Update`, `FixedUpdate` ou `LateUpdate` ?

| | `Update` | `FixedUpdate` | `LateUpdate` |
|---|---|---|---|
| Fréquence | le framerate | fixe (50/s par défaut) | le framerate, après tous les `Update` |
| Delta | `Time.deltaTime` | `Time.fixedDeltaTime` | `Time.deltaTime` |
| Pour | input, UI, timers | `Rigidbody`, forces, physique | caméra qui suit, IK, tout ce qui lit une position finale |

Piège : lire l'input dans `FixedUpdate` rate des appuis, parce qu'il ne tourne pas à chaque frame.
On lit dans `Update`, on stocke, on applique dans `FixedUpdate`.

---

## Les pièges qui coûtent une soirée

| ✗ Faux | ✓ Juste |
|---|---|
| `if (target is null)` | `if (target == null)` |
| `target?.name` | `target != null ? target.name : null` |
| `GetComponent<T>()` dans `Update` | dans `Awake`, stocké dans un champ |
| `renderer.material.color = c` | `MaterialPropertyBlock` + `SetPropertyBlock` |
| `Physics.RaycastAll(...)` par frame | `Physics.RaycastNonAlloc(..., _buffer, ...)` |
| `yield return new WaitForSeconds(1f)` en boucle | un `static readonly WaitForSeconds` |
| `public int Score { get; set; }` sérialisé | `[SerializeField] private int _score;` |
| `Dictionary` dans l'inspecteur | deux `List` + `ISerializationCallbackReceiver` |
| `+=` dans `Start`, `-=` dans `OnDestroy` | `OnEnable` / `OnDisable` |
| 1000 scripts avec chacun son `Update` | un manager qui boucle |
| `static int _score;` jamais remis à zéro | un reset explicite au début de chaque partie |
| `transform.position += v` dans une boucle | lire dans un local, calculer, réécrire une fois |

---

## Ce que Unity ne sérialise pas (et ne le dit pas)

- les propriétés, même publiques, même auto
- `readonly`, `static`, `const`
- `Dictionary`, `HashSet`, `Queue`, `Stack`
- les interfaces et le polymorphisme — sauf `[SerializeReference]`
- les tableaux multidimensionnels (`int[,]`) — les jagged passent via une classe intermédiaire
- `null` pour un type valeur : un `int` non assigné vaut `0`, pas « rien »

Le champ disparaît simplement de l'inspecteur et de la sauvegarde. Silencieusement.

---

## Mémoire : les 4 choses à savoir

1. **Un objet Unity, c'est deux objets** : le wrapper C# et l'objet natif. `Destroy` tue le natif ;
   le wrapper survit tant qu'une référence managée le tient. D'où la surcharge de `==`.
2. **Les assets natifs ne sont pas ramassés par le GC** : matériaux, textures, meshes créés à
   l'exécution doivent être détruits à la main dans `OnDestroy`.
3. **IL2CPP compile en avance** : pas de `Reflection.Emit`, pas de génération de type à
   l'exécution, et les méthodes génériques virtuelles posent problème. Ce qui marche en éditeur
   peut casser sur console.
4. **Le rechargement de domaine est souvent désactivé** (Enter Play Mode Options) pour gagner du
   temps de compilation. Les `static` survivent alors d'une partie à l'autre, événements statiques
   compris.

---

## Par où commencer dans les fichiers

```
unity/MonoBehaviourLifecycle.cs   quand chaque fonction est appelee — LANCE CELUI-LA EN PREMIER
unity/DestroyedObjects.cs         == null contre is null, et pourquoi ?. ne protege pas
unity/SerializationRules.cs       ce que l'inspecteur voit, et ce qu'il jette
unity/UpdateManager.cs            mille Update contre un seul
unity/CoroutinesAndWaits.cs       les attentes qui allouent
unity/MaterialsAndPhysics.cs      le materiau qui se clone, les requetes NonAlloc
../pure/                          C# pur : ca marche aussi sous Unity, tel quel
```

`GODOT-UNITY.md` = la table de traduction si tu viens de Godot ou si tu y vas.
`../csharplings/` = 124 exercices, dont la section `19_unity` qui teste tout ce qui est ci-dessus.
