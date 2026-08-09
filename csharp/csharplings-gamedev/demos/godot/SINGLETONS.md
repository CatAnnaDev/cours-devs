# Singletons, static et durée de vie (Godot 4 / C#)

## 1. Les trois façons de faire un singleton

### a) Autoload + propriété statique — `singletons/GameState.cs`

C'est le pattern par défaut en Godot. Le nœud est un autoload (Project Settings → Autoload),
donc l'engine le crée, l'ajoute sous `/root/` et le libère. La propriété `static Instance`
n'est qu'un raccourci d'accès, elle ne possède rien.

Points importants :
- `Instance` est assignée dans `_EnterTree`, pas `_Ready` : `_EnterTree` passe avant tous les
  `_Ready` des scènes, donc n'importe quel nœud peut appeler `GameState.Instance` dès son `_Ready`.
- `Instance` est remise à `null` dans `_ExitTree`. Sans ça, en fermant le jeu ou en rechargeant
  l'assembly depuis l'éditeur, tu gardes un pointeur vers un objet natif détruit → crash ou
  `ObjectDisposedException`.
- Le garde `if (Instance != null && Instance != this)` protège contre le cas où quelqu'un
  instancie la scène une deuxième fois à la main.

À utiliser quand : le singleton a besoin du cycle de vie Godot (`_Process`, signaux, timers,
resources, arbre de scène).

### b) Singleton C# pur `Lazy<T>` — `singletons/SaveService.cs`

Pas de `Node`, pas d'autoload, pas de `.tscn`. Construit à la première utilisation, thread-safe.
Constructeur privé pour que personne ne puisse en faire un deuxième.

À utiliser quand : le service ne touche pas à l'arbre de scène (I/O, config, sérialisation,
réseau, calcul). C'est plus simple, plus testable, et ça ne dépend pas de l'ordre des autoloads.

### c) Classe `static` pure — `singletons/StaticVsInstance.cs` (`DamageMath`, `SceneRoutes`)

Ce n'est pas vraiment un singleton, c'est un espace de noms de fonctions. Aucun état.

À utiliser quand : fonctions pures et constantes uniquement.

### Comment y accéder

```csharp
GameState.Instance.Score += 10;                       // rapide, mais couplage dur
GetNode<GameState>("/root/GameState");                 // testable, resolvable par chemin
[Export] public GameState State;                       // injection : le plus testable
```

Pour un vrai jeu, garder `Instance` pour le confort mais éviter de l'appeler dans 200 fichiers.
Récupérer la référence une fois dans `_Ready` et la stocker.

---

## 2. static ou pas static ?

La question à se poser : **est-ce qu'il peut logiquement y en avoir deux ?**

| Cas | Choix |
|---|---|
| Fonction pure : `ApplyArmor(damage, armor)` | `static` |
| Constante : chemins de scènes, layers, tags | `static readonly` / `const` |
| État partagé par tout le jeu et un seul par process | singleton (instance derrière `static Instance`) |
| État qui appartient à une entité (PV, inventaire, RNG) | **jamais** `static` |
| Quoi que ce soit qui doit être réinitialisé entre deux parties | **jamais** `static` |

### Pourquoi le `static` d'état fait mal en Godot

`BadGlobalCounter` dans `StaticVsInstance.cs` montre le piège :

1. **Rien ne le remet à zéro.** Recharger la scène ne réinitialise pas les statiques. Il faut y
   penser à la main, et un jour on oublie.
2. **Fuite mémoire.** Un `static` qui pointe vers un `Node` empêche le wrapper C# d'être collecté
   même après `QueueFree()`. Une liste statique d'ennemis qui n'est jamais vidée = fuite garantie.
3. **Rechargement d'assembly.** Quand l'éditeur recompile, les statiques sont réinitialisées
   silencieusement pendant que le jeu tourne. Les bugs qui en découlent sont insupportables à
   debug.
4. **Non testable, non parallélisable.** Deux tests qui touchent la même statique interfèrent.

Règle pratique : `static` pour le comportement et les constantes, instance pour l'état.
`RunCounter` (instance) vs `BadGlobalCounter` (static) dans le fichier montrent la même
fonctionnalité des deux côtés de la ligne.

### `readonly` et `sealed` tant qu'on y est

- `private readonly` sur les champs qui ne changent pas après le constructeur : le compilateur
  vérifie, et ça documente l'intention sans commentaire.
- `sealed` sur les classes non destinées à l'héritage : le JIT peut dévirtualiser les appels.

---

## 3. WeakReference et compagnie

Ce sujet a sa propre page, parce qu'il est plus profond qu'il n'y paraît et que la moitié des
réponses évidentes sont fausses dans Godot :

**→ [`WEAKREFS.md`](WEAKREFS.md)**

Tout y est **exécuté** : 28 affirmations vérifiées dans le moteur, 18 côté .NET. Elle couvre les trois durées de vie de Godot, la différence entre `System.WeakReference<T>` et
le `WeakRef` du moteur (elles ne répondent pas à la même question), les cinq façons de pointer
un nœud sans le posséder, le cache faible et son balayage obligatoire, le bus faible et le piège
qui l'annule en silence, les neuf erreurs à ne pas faire, et comment diagnostiquer avec
`PrintOrphanNodes()`.

Le lien avec cette page : un `static` qui pointe un `Node` est exactement la fuite décrite en
section 2. La référence faible n'est **pas** la réparation — trouver qui retient l'objet, si.
