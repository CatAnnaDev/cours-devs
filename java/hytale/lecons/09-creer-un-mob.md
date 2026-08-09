# Leçon 9 — Créer un vrai mob (PNJ avec IA) — AVANCÉ

**Prérequis :** Leçons 7 (ECS) et 8 (manipuler les entités), et la POO Java.
**Objectif :** faire apparaître un PNJ **qui a déjà son intelligence** (il bouge, réagit...).

> Bonne nouvelle, et c'est LA chose à comprendre : **tu ne codes pas l'IA à la main.**
> Dans Hytale, un PNJ et son comportement sont définis par un **rôle** (un type de PNJ
> prédéfini). Toi, tu **spawn un rôle** ; l'IA vient avec. Tout le reste découle de ça.

---

## Comment marche l'IA des PNJ (le mental model)

Un **rôle** (`Role`) décrit un type de créature : son apparence ET son cerveau. Ce cerveau
est fait de deux familles de briques :

- des **capteurs** (*sensors*) : ce que le PNJ « perçoit » (un joueur proche, un bruit...) ;
- des **actions / instructions** : ce qu'il fait en réaction (s'approcher, attaquer, fuir...).

Bref : un rôle = « perçois ceci → fais cela », répété en boucle. Ces rôles sont des
**données du jeu** (des assets, rangés du côté `Server/NPC/Roles`). Créer un rôle tout neuf,
c'est de l'édition d'assets très avancée — mais **spawn un rôle existant**, c'est simple, et
c'est ce qu'on fait ici.

---

## La recette pour faire apparaître un PNJ

Hytale fournit un plugin dédié, `NPCPlugin`, accessible de partout via `NPCPlugin.get()`.
Sa méthode `spawnNPC` fait tout le travail :

```java
import com.hypixel.hytale.server.npc.NPCPlugin;
import com.hypixel.hytale.server.core.universe.world.storage.EntityStore;
import com.hypixel.hytale.component.Ref;
import com.hypixel.hytale.component.Store;
import org.joml.Vector3d;
import org.joml.Vector3f;

// store = le registre du monde (sur le thread du monde) ; voir Leçon 8
String role = "NomDeRoleExistant";       // un rôle SPAWNABLE (voir plus bas)
Vector3d position = new Vector3d(x, y, z);
Vector3f rotation = new Vector3f();       // orientation ; 0 = par défaut

var resultat = NPCPlugin.get().spawnNPC(store, role, null, position, rotation);
if (resultat == null) {
    // le rôle n'existe pas / n'est pas spawnable -> rien n'apparaît
} else {
    Ref<EntityStore> npcRef = resultat.first();  // la ref du PNJ créé
    // Il est là, avec l'IA de son rôle ! Tu peux le manipuler (Leçon 8).
}
```

Les paramètres de `spawnNPC(store, npcType, groupType, position, rotation)` :
- `npcType` : le **nom du rôle** à faire apparaître ;
- `groupType` : un groupe/troupeau (« flock »), ou `null` pour un PNJ seul ;
- `position` : où (un `Vector3d`) — pense à viser un endroit **chargé** du monde ;
- `rotation` : l'orientation (un `Vector3f`).
- Renvoie une paire `(ref du PNJ, composant NPC)`, ou **`null`** si le rôle est inconnu.

---

## Trouver les noms de rôles (à faire chez toi)

Je ne peux pas deviner les noms de rôles exacts : ils dépendent des assets de TON install
(et de la version du jeu). La bonne méthode, c'est de **les lister au démarrage** et de
regarder la console :

```java
for (String nom : NPCPlugin.get().getRoleTemplateNames(true)) { // true = seulement les spawnables
    LOGGER.at(Level.INFO).log("[NeniLearn] rôle dispo : %s", nom);
}
```

Deux aides bien pratiques avant de spawn :
- `NPCPlugin.get().hasRoleName("X")` → `true` si le rôle existe ;
- `NPCPlugin.get().validateSpawnableRole("X")` → lève une erreur claire si « X » n'existe pas
  ou est un rôle **abstrait** (certains rôles servent de base et ne se spawn pas directement).

> Tu peux aussi voir les rôles via la commande `/npc` intégrée au jeu.

---

## Exemple complet : une commande `/neni mob`

On réutilise le schéma d'`AbstractPlayerCommand` (Leçon 3) : le serveur nous donne `store`,
la `ref` de la joueuse et le `world`, le tout sur le bon thread. On lit la position de la
joueuse (Leçon 8) et on fait apparaître le PNJ là.

```java
@Override
protected void execute(@Nonnull CommandContext context, @Nonnull Store<EntityStore> store,
                       @Nonnull Ref<EntityStore> ref, @Nonnull PlayerRef playerRef,
                       @Nonnull World world) {

    // 1) où est la joueuse ? (composant position, Leçon 8)
    TransformComponent t = store.getComponent(ref, TransformComponent.getComponentType());
    if (t == null) {
        context.sendMessage(Message.raw("Impossible de trouver ta position."));
        return;
    }
    Vector3d pos = t.getPosition();

    // 2) le rôle à spawn (remplace par un nom listé par getRoleTemplateNames(true))
    String role = "REMPLACE_MOI";
    if (!NPCPlugin.get().hasRoleName(role)) {
        context.sendMessage(Message.raw("Rôle inconnu : " + role));
        return;
    }

    // 3) on fait apparaître le PNJ à la position de la joueuse
    var resultat = NPCPlugin.get().spawnNPC(store, role, null,
            new Vector3d(pos.x, pos.y, pos.z), new Vector3f());

    if (resultat != null) {
        context.sendMessage(Message.raw("PNJ « " + role + " » apparu !"));
    } else {
        context.sendMessage(Message.raw("Le spawn a échoué."));
    }
}
```

(Comme toute sous-commande, on l'enregistre dans la commande parente avec `addSubCommand(...)`,
Leçon 3.)

---

## Prudence

- **Thread du monde** : `spawnNPC` se fait là où tu as un `store` valide (dans
  `execute(...)`, c'est déjà le cas). Ne le lance pas depuis un thread quelconque.
- **Rôle spawnable** : un rôle inconnu ou abstrait ne donne rien (`null`) — d'où le
  `hasRoleName` / `validateSpawnableRole`.
- **Position chargée** : fais apparaître le PNJ dans une zone active (près d'une joueuse,
  typiquement), sinon il peut ne pas s'initialiser correctement.
- **Perf** : ne spawn pas des PNJ en boucle à chaque tick (revois `../notions/optimisations.md`).

---

## Ce qu'il faut retenir

1. L'IA d'un PNJ vient de son **rôle** (capteurs → actions, défini en données). Tu **ne codes
   pas l'IA** : tu spawn un rôle.
2. **Faire apparaître** : `NPCPlugin.get().spawnNPC(store, role, null, position, rotation)`.
3. **Trouver les rôles** : `getRoleTemplateNames(true)` (et `hasRoleName` / `validateSpawnableRole`).
4. Tout ça sur le **thread du monde**, à une **position chargée**, sans en spammer.

Tu veux aller plus loin (un PNJ qui suit la joueuse, qu'on peut nommer, lui donner un objet,
le faire disparaître au bout d'un temps...) ? Dis-le-moi, on construira ça pas à pas.
