# Les références faibles en Godot 4 + C#

Tout ce qui est écrit ici est **exécuté**, pas seulement compilé.

| Banc | Résultat | Comment le rejouer |
|---|---|---|
| Dans le vrai moteur, Godot 4.7.1 mono | **28 / 28** | attache `weakrefs/WeakRefsSelfTest.cs` à un nœud, ou `godot --headless --path <projet>` |
| Côté .NET, Debug **et** Release | **18 / 18** | `cd demos/pure && dotnet run weak` |

Les deux bancs rendent un code de sortie non nul si une affirmation de cette page devient
fausse. Si tu changes une version de moteur et qu'une ligne d'ici ment, tu le sauras.

---

## En une phrase

Une référence normale **empêche** le ramasse-miettes de libérer l'objet. Une référence faible
**pointe sans retenir** : si plus personne d'autre ne le tient, l'objet part et la référence
faible devient vide.

C'est simple. Le problème, c'est que dans Godot cette phrase ne répond presque jamais à la
question que tu te poses réellement.

---

## Le vrai modèle : deux objets, et qui tue lequel

Un objet Godot en C#, ce sont **deux choses** : l'objet **natif**, dans le moteur, et le
**wrapper** C#, ta poignée dessus. Presque tous les malentendus viennent de croire qu'il n'y en
a qu'un.

Voici ce que fait réellement chaque opération. Chaque ligne est vérifiée par
`weakrefs/WeakRefsSelfTest.cs`.

| Opération | L'objet natif | Ton handle C# |
|---|---|---|
| `QueueFree()` | libéré **en fin de frame** | devient inutilisable |
| `Free()` | libéré **tout de suite** | devient inutilisable |
| `Dispose()` sur un `Node` | **intact** — rien n'est libéré | jeté |
| `Dispose()` sur une `Resource` | libéré (comptage à zéro) | jeté |
| le ramasse-miettes collecte le wrapper d'un `Node` | **intact** | disparaît |
| le ramasse-miettes collecte le wrapper d'une `Resource` | libéré | disparaît |
| tu perds la référence d'un nœud **dans l'arbre** | intact, l'arbre le possède | recréable par `GetNode` |
| tu perds la référence d'un nœud **hors de l'arbre** | **intact, et fuit pour toujours** | perdu |

Deux lectures importantes de ce tableau :

- **Le wrapper est une vue, pas le propriétaire.** Lâcher ta variable sur un nœud de l'arbre ne
  risque rien : `GetNode` t'en redonnera un neuf, parfaitement utilisable. Vérifié.
- **Lâcher un nœud qui n'est PAS dans l'arbre le fait fuir.** Le ramasse-miettes ne le libérera
  jamais, et le moteur te le dira à la fermeture : `1 ObjectDB instance was leaked at exit`.
  C'est le nœud orphelin, la fuite numéro un des projets Godot.

---

## `Dispose()` n'est pas `Free()`

Le piège le plus contre-intuitif de tout ce document, et il est mesuré.

```csharp
var node = new Node();
node.Dispose();
```

Résultat réel : **l'objet natif est toujours là**, `IsInstanceValid(node)` rend `false`, et
toucher `node.Name` lève `ObjectDisposedException`. Tu n'as rien libéré — tu as jeté ta seule
poignée sur un objet qui vit encore. Il est encore récupérable par son identifiant d'instance,
et c'est `Free()` qui le libère pour de bon.

Sur un nœud **dans l'arbre**, c'est pire : après `Dispose()` il reste dans l'arbre, toujours
trouvable par son nom, toujours fonctionnel. Tu as juste perdu ta variable, en silence.

Sur une `Resource`, en revanche, `Dispose()` fait bien tomber le comptage à zéro et libère.
Cohérent avec le modèle, mais ça rend la règle « ça dépend du type » encore plus piégeuse.

**La règle : ne mets jamais un `Node` dans un `using`.** `using var node = new Node();` ne
libère rien, il fabrique une fuite. Pour un nœud, c'est `QueueFree()`, ou `Free()` si tu es sûr
que rien n'est en train de l'itérer.

### `IsInstanceValid` peut être faux pour deux raisons

Conséquence directe : `IsInstanceValid(node)` rend `false` quand l'objet natif est parti,
**mais aussi** quand c'est seulement ton wrapper qui a été jeté. Dans les deux cas la réponse
utile est la même — « ne t'en sers pas » — mais si tu débogues une valeur `false` inattendue,
pense à `Dispose()` avant d'accuser une libération.

### `Free()` ou `QueueFree()` ?

| | `Free()` | `QueueFree()` |
|---|---|---|
| Quand | immédiatement | à la fin de la frame |
| Pendant un callback physique ou une itération | **dangereux** | sûr |
| `IsQueuedForDeletion()` | sans objet | rend `true` dès l'appel |
| Le défaut | non | **oui** |

Mesuré : juste après `Free()` l'identifiant est déjà invalide ; juste après `QueueFree()` il est
encore valide, et l'objet se sait condamné. D'où l'attente d'une frame avant de conclure quoi
que ce soit sur un nœud qu'on vient de supprimer.

---

## Le piège de fond : Godot a trois durées de vie, pas une

Avant de parler de « weak », il faut savoir **qui décide** de la mort de ton objet. Il y a
trois réponses différentes dans le même moteur.

| Type | Qui le libère | Le ramasse-miettes a-t-il son mot à dire ? |
|---|---|---|
| `Node` (et tout `GodotObject` non compté) | **toi**, avec `QueueFree()` — ou son parent | **non**, jamais |
| `RefCounted`, et donc `Resource` | le **comptage de références natif** | **oui, indirectement** |
| une classe C# ordinaire | le ramasse-miettes | oui |

Les conséquences sont énormes :

- Une référence faible sur un **`Node`** ne provoquera jamais sa destruction. Le nœud attend
  ton `QueueFree()`, point. La référence faible ne t'aide donc pas à *libérer* — au mieux à
  *savoir*.
- Une référence faible sur une **`Resource`** peut réellement la faire décharger, parce que le
  wrapper C# porte une part du comptage natif. C'est un vrai cas d'usage, et un vrai piège :
  ta fiche de stats peut se faire recharger sans que tu l'aies demandé.
- Le moment où ça arrive n'est **pas déterministe**. Le ramasse-miettes passe quand il veut.
- Et pour un `Node`, la conséquence est brutale : lâcher sa référence C# sans l'avoir mis dans
  l'arbre ni appelé `Free()` le fait **fuir définitivement**. Vérifié : après deux collectes
  complètes et six frames, le natif est toujours là.

---

## Les deux « weak » qui n'ont rien à voir

C'est la distinction la plus importante de cette page, et celle qui fait perdre le plus de
temps. Il existe deux mécanismes qui s'appellent tous les deux « référence faible », et **ils
ne répondent pas à la même question**.

| | `System.WeakReference<T>` | `GodotObject.WeakRef(obj)` / identifiant d'instance |
|---|---|---|
| Ce qu'il surveille | le **wrapper C#** est-il encore joignable ? | l'**objet natif** est-il encore vivant ? |
| Devient vide quand | le ramasse-miettes a collecté le wrapper | le natif a été libéré (`Free`, fin de `QueueFree`) |
| Sur un `Node` libéré | peut **encore te rendre le wrapper**, une coquille vide | rend vide, correctement |
| Coût | **24 octets mesurés** plus une poignée côté GC | `WeakRef` : un objet natif. Identifiant : **un `ulong`**, zéro allocation |
| Répond à « puis-je m'en servir ? » | **non**, pas seul | **oui** |

Autrement dit : sur un nœud, `System.WeakReference<Node>` répond à une question que tu ne te
poses pas. Tu veux savoir si le nœud est utilisable — c'est `IsInstanceValid`, ou l'identifiant
d'instance.

```csharp
if (weak.TryGetTarget(out Node node) && GodotObject.IsInstanceValid(node))
    Use(node);
```

Les **deux** tests sont obligatoires si tu passes par `WeakReference<Node>`. Le premier dit que
le wrapper existe, le second que le natif derrière est encore là. Oublier le second, c'est se
préparer une erreur au premier accès à une propriété.

---

## Les cinq façons de pointer un nœud sans le posséder

| Approche | Ce que ça coûte | Verdict |
|---|---|---|
| `[Export] NodePath` + `GetNodeOrNull<T>(path)` | une recherche dans l'arbre par appel | **le défaut**. Simple, lisible dans l'inspecteur, jamais périmé |
| garder la référence + `IsInstanceValid` avant usage | rien | **le plus rapide**. À préférer dans une boucle chaude |
| `GetInstanceId()` puis `IsInstanceIdValid` + `InstanceFromId` | un `ulong` | **le vrai « weak » de Godot**. Ne retient rien, dit la vérité |
| `GodotObject.WeakRef(obj)` + `GetRef()` | un objet natif, un `Variant` à convertir | correct, mais l'identifiant fait pareil pour moins cher |
| `System.WeakReference<Node>` | un objet + une poignée GC, **et** il faut doubler le test | **presque jamais le bon outil** pour un nœud |

L'identifiant d'instance mérite d'être connu, parce que c'est exactement le motif de
`17_ecs/entities1` : un identifiant qui porte *où* et *quand*, et qui refuse de résoudre quand
l'occupant a changé. **Vérifié dans le moteur** : un identifiant libéré devient invalide, et
l'objet suivant en reçoit un différent — l'ancien ne désignera donc jamais le nouveau par
accident.

```csharp
private ulong _targetId;

public override void _Ready() => _targetId = _target.GetInstanceId();

public Node Resolve()
{
    if (!GodotObject.IsInstanceIdValid(_targetId))
    {
        _targetId = 0UL;

        return null;
    }

    return GodotObject.InstanceFromId(_targetId) as Node;
}
```

Les cinq sont côte à côte, compilées, dans `weakrefs/WeakUsage.cs` → `NodeReferenceWays`.

---

## `WeakReference<T>` : les règles d'usage

Là où il est vraiment à sa place : des objets **C# ordinaires**, gros, reconstructibles.

```csharp
if (weak.TryGetTarget(out Payload payload))
    Use(payload);
else
    Rebuild();
```

Trois règles, sans exception :

1. **Toujours `TryGetTarget`.** Bonne nouvelle : avec `WeakReference<T>` tu ne *peux pas* faire
   autrement. Vérifié par réflexion, le type générique n'expose que `TryGetTarget` et
   `SetTarget` — pas de `IsAlive`, pas de `Target`. Le piège du « je teste puis je
   déréférence », entre lesquels le ramasse-miettes peut passer, n'existe que sur l'ancien
   `WeakReference` non générique. Si tu vois `IsAlive` dans du code, c'est celui-là.
2. **Garde le résultat dans une variable locale** pendant que tu t'en sers. Ne rappelle pas
   `TryGetTarget` trois fois dans la même méthode : chaque appel peut donner une réponse
   différente.
3. **Sur un `GodotObject`, double le test** avec `IsInstanceValid`. Voir plus haut.

Et la règle qui n'est pas une règle d'usage mais de conception : si tu ne peux pas répondre à
« qu'est-ce que je fais quand c'est vide ? », tu n'as pas besoin d'une référence faible.

---

## `ConditionalWeakTable` : quand la clé est un objet

Attacher des données à un objet que tu ne possèdes pas, sans le retenir en vie et **sans avoir
à nettoyer**.

```csharp
private static readonly ConditionalWeakTable<Node, TargetNote> Notes = new();

Notes.GetOrCreateValue(node).TimesSeen++;

if (Notes.TryGetValue(node, out TargetNote note))
    Use(note);
```

C'est presque toujours meilleur qu'un `Dictionary<Node, T>` :

| | `Dictionary<Node, T>` | `ConditionalWeakTable<Node, T>` |
|---|---|---|
| Retient la clé en vie | **oui** — fuite garantie | non |
| Retient la valeur | oui | seulement tant que la clé vit |
| Nettoyage | **à ta charge** | automatique, par le ramasse-miettes |
| Énumérable | oui | non, et c'est voulu |

La contrepartie : tu ne peux pas la parcourir. Si tu as besoin de lister, c'est que ce n'est pas
l'outil.

Un détail Godot : la table suit le **wrapper C#**. Un nœud libéré dont le wrapper vit encore
garde son entrée. Donc là aussi, `IsInstanceValid` avant de s'en servir.

---

## Le cache faible : le motif et son piège

L'idée : garder des assets lourds tant que quelqu'un d'autre s'en sert, les laisser partir
sinon.

```csharp
private readonly Dictionary<TKey, WeakReference<TValue>> _entries = new();

public TValue Get(TKey key)
{
    if (_entries.TryGetValue(key, out WeakReference<TValue> slot) && slot.TryGetTarget(out TValue cached))
        return cached;

    TValue created = _factory(key);
    _entries[key] = new WeakReference<TValue>(created);

    return created;
}
```

**Le piège, et il est systématique :** le dictionnaire garde les **clés** et les objets
`WeakReference` **vides** pour toujours. Mesuré sur le `WeakCache.cs` de ce dossier : après
avoir chargé 20 assets et laissé le ramasse-miettes passer, **les 20 entrées mortes étaient
toujours dans le dictionnaire**. Ton cache « qui ne retient rien » fuit donc lui-même,
lentement, une entrée morte à la fois. D'où un `Sweep()` périodique — dans
`weakrefs/WeakCache.cs`, il part tous les 32 échecs.

Et la question qu'il faut se poser avant d'écrire tout ça :

| Situation | Le bon outil |
|---|---|
| Assets lourds, durée de vie décidée par le reste du jeu | cache faible **avec** balayage |
| Clés qui sont des objets | `ConditionalWeakTable`, qui balaie tout seul |
| Cache court terme, besoin de prévisibilité | **un LRU à taille bornée**. Pas de faible du tout |
| Objets légers | rien du tout : la poignée coûte plus que l'objet |

Un cache faible est **imprévisible par construction** : les objets survivent jusqu'au prochain
passage du ramasse-miettes, puis disparaissent tous d'un coup. Pour un jeu, un LRU borné est
souvent le meilleur choix, parce qu'il donne un plafond mémoire connu.

---

## Le bus faible : le motif et LE piège

En C#, `bus.Truc += OnTruc;` fait que **le bus tient l'abonné en vie**, pas l'inverse. C'est la
première cause de fuite mémoire en C#, et en gamedev ça donne des objets morts qui continuent
de réagir.

Deux solutions, dans cet ordre de préférence :

1. **Se désabonner proprement** dans `_ExitTree`. Déterministe, gratuit, lisible.
2. **Un bus à références faibles**, quand tu ne contrôles pas la durée de vie des abonnés.

Le motif, dans `weakrefs/WeakEventBus.cs` :

```csharp
public void Subscribe<TOwner>(TOwner owner, Action<TOwner, TPayload> handler)
    where TOwner : class
{
    _subscriptions.Add(new Subscription
    {
        Owner = new WeakReference<object>(owner),
        Invoke = (o, payload) => handler((TOwner)o, payload),
    });
}
```

Remarque la signature : le handler reçoit le propriétaire **en paramètre**. Ce n'est pas de la
coquetterie, c'est toute la sécurité du motif.

### Le piège qui annule tout

Le bus garde le propriétaire en **faible**. Mais il garde le **handler** en fort. Donc si ton
handler capture le propriétaire, le bus le retient quand même — et ton bus « faible » ne l'est
plus du tout.

```csharp
_bus.Subscribe(this, static (self, message) => self.OnScore(message));
```

```csharp
_bus.Subscribe(this, (_, message) => Count(message));
```

La première ligne est correcte. La seconde fuit : la lambda capture `this` pour appeler
`Count`, ce `this` part dans le delegate, le delegate est gardé en fort par le bus, et le nœud
ne sera jamais collecté.

**Les deux compilent, et le banc de test le prouve à l'exécution** : avec la lambda `static`
l'abonné est collecté ; avec celle qui capture, il survit à la collecte **et réagit encore** à
une publication, alors que plus personne au monde ne le connaît. Rien dans le type ne te
prévient. Les deux sont dans
`weakrefs/WeakUsage.cs` sous les noms `CorrectSubscriber` et `LeakingSubscriber`, pour que la
différence soit lisible côte à côte.

La parade est un mot-clé : **`static`** devant la lambda. Le compilateur refuse alors toute
capture, et il te le dit à la compilation au lieu de te le laisser découvrir dans un profileur
trois mois plus tard.

### Et les signaux Godot dans tout ça ?

Un signal Godot (`[Signal]` + `EmitSignal`) **ne souffre pas** du même problème : la connexion
vit côté natif et elle est coupée quand l'un des deux nœuds est libéré. Tu n'as pas besoin d'un
bus faible pour des signaux.

C'est un `+=` sur un `event` C# classique qui demande de la discipline. La règle simple : si les
deux bouts sont des nœuds, prends un signal. Si l'un des deux n'est pas un nœud, c'est là que
la question se pose.

---

## Où il ne faut surtout pas en mettre

| Situation | Pourquoi c'est une mauvaise idée | À faire à la place |
|---|---|---|
| Sur une fuite qu'on n'a pas comprise | tu caches le symptôme, la cause reste | trouver **qui** retient l'objet |
| Sur des nœuds, pour savoir s'ils vivent | ça répond à la mauvaise question | `IsInstanceValid`, ou l'identifiant d'instance |
| Sur des objets légers | la poignée GC coûte plus que l'objet | une référence normale |
| Pour du cache court terme | disparition imprévisible, par vagues | un LRU à taille bornée |
| Sur des `RefCounted` / `Resource` sans y réfléchir | tu peux provoquer des rechargements | garder une référence forte le temps voulu |
| À la place d'un `-=` dans `_ExitTree` | tu remplaces du déterministe par du hasard | se désabonner |
| Pour « libérer » un nœud | une référence faible ne libère rien | `QueueFree()` |
| Sur `this` dans son propre objet | ça n'a aucun sens, tu es vivant | rien |

---

## Les erreurs à ne pas faire

1. **`IsAlive` puis déréférencement.** Entre les deux lignes, le ramasse-miettes peut passer.
   → `TryGetTarget`, toujours.
2. **Oublier `IsInstanceValid`** sur un nœud sorti d'une `WeakReference`. Le wrapper existe, le
   natif est mort, la première propriété touchée lève.
3. **Un `Dictionary<K, WeakReference<V>>` sans balayage.** Les clés et les poignées vides
   s'accumulent : le cache anti-fuite devient la fuite. → `Sweep()`, ou
   `ConditionalWeakTable`.
4. **Un handler de bus faible qui capture son propriétaire.** Le motif est annulé, en silence.
   → lambda `static` prenant le propriétaire en paramètre.
5. **Croire qu'une référence faible déclenche la destruction.** Elle observe, elle n'agit pas.
6. **`GC.Collect()` en production** pour « forcer » le comportement. C'est un outil de démo — il
   y en a un dans `weakrefs/WeakReferenceBasics.cs` uniquement pour rendre la transition
   visible. Dans un jeu, un `GC.Collect()` est une pause visible.
7. **Compter sur le moment.** Rien ne garantit qu'un objet sans référence forte soit collecté
   avant longtemps. Ne construis pas de logique de jeu sur « il devrait être parti ».
8. **Mélanger les deux modèles** sur un `RefCounted` : le comptage natif et le ramasse-miettes
   décident ensemble, et raisonner devient très difficile. Sur une `Resource`, préfère une
   référence forte explicite et une durée de vie que tu choisis.
9. **Utiliser un bus faible parce que se désabonner est fastidieux.** Le fastidieux est
   déterministe ; le pratique est imprévisible.
10. **`using var node = new Node();` ou `node.Dispose()` pour « libérer ».** Ça ne libère rien
    du tout sur un `Node` : ça jette ta poignée et laisse l'objet fuir. → `QueueFree()`.
11. **Créer un nœud et perdre sa référence sans l'ajouter à l'arbre.** Il fuit pour toujours,
    silencieusement. → `AddChild` tout de suite, ou `QueueFree()` si finalement tu n'en veux
    plus. `PrintOrphanNodes()` te les nomme.

---

## Ce qu'il faut faire, dans l'ordre

1. **Diagnostiquer avant de coder.** Une fuite, c'est quelqu'un qui retient. C'est presque
   toujours l'un des trois : une variable `static`, une collection jamais vidée, un événement
   jamais désabonné. Tu trouves le coupable, tu corriges le coupable.
2. **Préférer la propriété déterministe.** `-=` dans `_ExitTree`, `Clear()` dans `_ExitTree`,
   `QueueFree()` quand c'est fini. Le faible n'arrive qu'après.
3. **Pour un nœud** : garder la référence et tester `IsInstanceValid`, ou un `NodePath`, ou un
   identifiant d'instance. Dans cet ordre selon que tu privilégies la vitesse, la lisibilité
   dans l'inspecteur, ou la robustesse.
4. **Pour attacher des données à un objet** : `ConditionalWeakTable`. Il balaie tout seul.
5. **Si tu écris un dictionnaire faible** : écris le balayage en même temps, pas plus tard.
6. **Si tu écris un bus faible** : impose la lambda `static` dans ta propre API, et documente-le
   à côté de la signature.
7. **Mesurer.** Une intuition sur la mémoire est presque toujours fausse.

---

## Tester tout ça est plus délicat qu'il n'y paraît

En écrivant les bancs de test de cette page, deux pièges m'ont donné de faux échecs. Ils valent
d'être connus, parce qu'ils touchent n'importe qui essaie de vérifier un comportement de
ramasse-miettes.

**1. Un `TryGetTarget` juste avant la collecte garde l'objet en vie.** Même avec un discard :

```csharp
Check(weak.TryGetTarget(out _), "avant");
Collect();
Check(!weak.TryGetTarget(out _), "apres");   // echoue : le premier appel a laisse un temporaire
```

La référence forte sortie par le premier appel reste dans un temporaire de pile pendant la
collecte. Il faut faire la vérification « avant » **dans une autre méthode**, celle qui a créé
l'objet, et ne toucher à la référence faible qu'après avoir collecté.

**2. Debug et Release ne donnent pas le même résultat.** En Debug, les variables locales restent
vivantes jusqu'à la fin de leur méthode. Un objet qu'on croit lâché ne l'est pas. La parade est
de créer et lâcher l'objet dans une méthode `[MethodImpl(MethodImplOptions.NoInlining)]` qui ne
rend rien qui le référence.

Les deux bancs de cette page passent en Debug **et** en Release, justement parce qu'ils
appliquent ces deux règles.

Et la morale dépasse le test : si faire disparaître un objet exprès demande ces précautions, ne
construis surtout pas de logique de jeu sur « il devrait être parti maintenant ».

---

## Diagnostiquer

Godot donne les outils, ils sont peu connus.

| Je veux savoir | J'écris |
|---|---|
| Quels nœuds ont été créés et jamais libérés | `Node.PrintOrphanNodes();` |
| Combien de nœuds vivent | `Performance.GetMonitor(Performance.Monitor.ObjectNodeCount)` |
| Combien sont orphelins | `Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount)` |
| Combien d'objets Godot en tout | `Performance.GetMonitor(Performance.Monitor.ObjectCount)` |
| Combien de références tient une `Resource` | `resource.GetReferenceCount()` — vaut `1` pour une resource simplement tenue |
| Si un identifiant désigne encore quelque chose | `GodotObject.IsInstanceIdValid(id)` |

Un **nœud orphelin** est un nœud instancié, jamais ajouté à l'arbre, et jamais libéré. C'est la
fuite la plus fréquente dans un projet Godot, et elle n'a rien à voir avec les références
faibles : c'est un `Instantiate()` dont on a perdu le résultat sans appeler `QueueFree()`.

La sortie réelle ressemble à ça :

```
27044873684 - Stray Node: JamaisAjoute (Type: Node) (Source:)
```

L'identifiant, le nom, le type, et le fichier source s'il y en a un. `PrintOrphanNodes()` à la
fermeture du jeu doit afficher **zéro**. Si ce n'est pas le cas, tu as
une fuite, et aucune référence faible ne la réparera.

La bonne boucle de travail : note le compteur de nœuds au début d'un niveau, rejoue le niveau
dix fois, regarde s'il est revenu au même chiffre. S'il monte, tu as le nom de ton bug avant
d'avoir ouvert un profileur.

---

## Résumé en un tableau

| Ce que je veux vraiment | La réponse |
|---|---|
| Savoir si ce nœud est utilisable | `IsInstanceValid(node)` |
| Pointer un nœud sans le retenir | son identifiant d'instance, ou un `NodePath` |
| Attacher des données à un nœud | `ConditionalWeakTable<Node, T>` |
| Un cache d'assets lourds | cache faible **avec** balayage, ou LRU borné |
| Un bus qui ne retient pas ses abonnés | bus faible **et** lambda `static` |
| Ne plus fuir sur des événements | `-=` dans `_ExitTree`. C'est tout |
| Libérer un nœud | `QueueFree()` |
| Le libérer tout de suite, hors callback | `Free()` |
| M'en débarrasser avec `Dispose()` | **jamais**. Ça ne libère rien sur un nœud |
| Garder une référence à un nœud de l'arbre | pas nécessaire : `GetNode` en redonne une |
| Savoir si mon handle est utilisable | `IsInstanceValid` — faux si le natif est parti **ou** si le handle a été jeté |
| Comprendre pourquoi ça fuit | `PrintOrphanNodes()` et les trois suspects |

---

## Les fichiers

| Fichier | Contenu |
|---|---|
| `weakrefs/WeakRefsSelfTest.cs` | **le banc de test du moteur** : attache-le à un nœud, il vérifie les 28 affirmations de cette page et rend un code de sortie |
| `../pure/WeakRefs.cs` | **le banc de test .NET** : `cd demos/pure && dotnet run weak`, 18 affirmations, Debug et Release |
| `weakrefs/WeakReferenceBasics.cs` | la transition vivant → collecté, rendue visible par un `GC.Collect()` de démo |
| `weakrefs/LifetimeProbe.cs` | les cinq mécanismes côte à côte : identifiant, `WeakRef` de Godot, `WeakReference<T>`, `ConditionalWeakTable`, comptage d'une `Resource` |
| `weakrefs/WeakUsage.cs` | les cinq façons de pointer un nœud, et l'abonné correct contre l'abonné qui fuit |
| `weakrefs/WeakCache.cs` | le cache faible et son balayage |
| `weakrefs/WeakEventBus.cs` | le bus faible, avec la signature qui pousse vers la lambda `static` |
| `weakrefs/GodotObjectLifetime.cs` | le double objet wrapper / natif, et `ResolveOrForget` |
| `SINGLETONS.md` | les managers globaux, et pourquoi un `static` d'état fuit |
| `../CHEATSHEET.md` | le condensé Godot |
