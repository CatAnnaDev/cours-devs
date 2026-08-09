# Leçon 8 — Manipuler les entités — AVANCÉ

**Prérequis :** Leçon 7 (l'ECS), et la POO Java (leçons 11-12). **Objectif :** lire et modifier
des entités existantes (leur position, leur nom...), réagir aux dégâts, et même en **créer**.

> C'est de l'avancé : prends-le tranquillement. Tous les exemples ci-dessous viennent de
> vrais mods qui tournent. Et rappel de la Leçon 7 : tout passe par le trio
> **`Ref`** (quelle entité) + **`Store`** (le registre) + **`getComponent`** (tel composant).

---

## 1. Récupérer le `store` et une `ref`

Selon l'endroit où tu es :

- **Dans une commande joueur** (`AbstractPlayerCommand.execute`, Leçon 3) : le serveur te
  donne déjà `store` et `ref` (la ref de la joueuse). Tu n'as rien à faire.
- **À partir d'un monde** :
  ```java
  Store<EntityStore> store = world.getEntityStore().getStore();
  ```
- **Retrouver une entité par son UUID** :
  ```java
  Ref<EntityStore> ref = world.getEntityRef(monUuid);
  if (ref == null || !ref.isValid()) return; // l'entité n'existe pas/plus
  ```

---

## 2. Lire un composant d'une entité

Pour lire une info, on demande le composant correspondant. Exemple : la **position** est
dans le `TransformComponent`.

```java
TransformComponent t = store.getComponent(ref, TransformComponent.getComponentType());
if (t != null) {                       // getComponent peut renvoyer null : toujours vérifier !
    Vector3d pos = t.getPosition();    // Vector3d vient de org.joml : pos.x, pos.y, pos.z
    LOGGER.at(Level.INFO).log("position = %.1f %.1f %.1f", pos.x, pos.y, pos.z);
}
```

Autres composants utiles (mêmes principe) : `DisplayNameComponent` (le nom affiché),
`Player` (le composant joueur), `UUIDComponent` (l'identifiant), `ModelComponent` (l'apparence)...

---

## 3. Modifier une entité : ajouter / changer / retirer un composant

Le `store` permet d'écrire, pas seulement de lire :

```java
// Ajouter ou remplacer un composant (ici, changer le nom affiché)
store.putComponent(ref, DisplayNameComponent.getComponentType(),
        new DisplayNameComponent("Boss"));

// Retirer un composant
store.removeComponent(ref, DisplayNameComponent.getComponentType());
```

> `getComponent` = lire, `putComponent` = écrire/ajouter, `removeComponent` = enlever.
> Pour « modifier » une donnée, tu lis le composant, tu calcules la nouvelle valeur, et tu
> remets le composant à jour (ou tu modifies l'objet composant s'il est mutable).

---

## 4. La vie et les dégâts : le `DamageEventSystem`

Dans Hytale, on ne « pose » pas brutalement une valeur de points de vie. La vie change à
travers le **système de dégâts**. Pour intervenir (réduire, annuler, réagir aux dégâts), on
écrit un **système** qui hérite de `DamageEventSystem` et redéfinit `handle(...)` :

```java
import com.hypixel.hytale.server.core.modules.entity.damage.Damage;
import com.hypixel.hytale.server.core.modules.entity.damage.DamageEventSystem;

public class MaProtection extends DamageEventSystem {
    @Override
    public void handle(int index, ArchetypeChunk<EntityStore> chunk,
                       Store<EntityStore> store, CommandBuffer<EntityStore> cb,
                       Damage damage) {
        // 'damage' décrit l'attaque en cours : tu peux l'inspecter et la modifier
        // (par exemple, annuler les dégâts dans une zone protégée).
    }
}
```

C'est exactement le mécanisme d'une **protection PvP** : intercepter les dégâts et décider
quoi en faire. (On enregistre ce système au démarrage, comme un listener — voir Leçon 2.)

> Donc : pour « blesser » ou « soigner », pense **événement de dégâts**, pas « je règle le HP
> à la main ». C'est la façon idiomatique côté Hytale.

---

## 5. Créer (spawn) une entité

Le mécanisme général : on construit un **`Holder`** (un brouillon d'entité), on lui **ajoute
des composants**, puis on le **valide** dans le store. Voici le vrai schéma (ici une petite
entité « panneau de nom » au-dessus d'un bloc) :

```java
Store<EntityStore> store = world.getEntityStore().getStore();

Holder<EntityStore> holder = EntityStore.REGISTRY.newHolder();

// Un identifiant unique
holder.addComponent(UUIDComponent.getComponentType(), UUIDComponent.randomUUID());

// Une position dans le monde (x + 0.5 pour centrer sur le bloc)
holder.addComponent(TransformComponent.getComponentType(),
        new TransformComponent(new Vector3d(x + 0.5, y + 1.2, z + 0.5), Rotation3f.NaN));

// ... d'autres composants selon ce que tu veux (un nom, un modèle, etc.)
holder.addComponent(Nameplate.getComponentType(), new Nameplate("Coucou !"));

// On valide : l'entité apparaît vraiment dans le monde
Ref<EntityStore> ref = store.addEntity(holder, AddReason.SPAWN);
if (ref != null && ref.isValid()) {
    LOGGER.at(Level.INFO).log("Entité créée !");
}
```

Retiens la recette : **`newHolder()` → `addComponent(...)` (autant que nécessaire) →
`store.addEntity(holder, AddReason.SPAWN)`**.

Pour un **vrai mob** (un PNJ qui bouge et attaque), c'est le même mécanisme, mais il faut
ajouter plus de composants (un modèle, les composants de PNJ/IA...). C'est plus costaud : dis-
moi quand tu voudras t'y attaquer et on fera une leçon dédiée avec un exemple complet.

---

## Prudence (à lire avant de te lancer)

- **Le thread.** Ces opérations se font sur le **thread du monde** (comme dans le
  `execute(...)` d'`AbstractPlayerCommand`, Leçon 3). Ne les lance pas depuis n'importe où.
- **Toujours vérifier `null`** après `getComponent`, et **`ref.isValid()`** après avoir
  récupéré/créé une ref.
- **Attention à la perf** : ne crée pas / ne modifie pas des entités à chaque tick sans
  réfléchir (revois `notions/optimisations.md`).
- **Les noms exacts des imports** se trouvent vite avec l'autocomplétion de l'IDE, ou en
  regardant un mod existant — ne les apprends pas par cœur.

---

## Ce qu'il faut retenir

1. **Lire** : `store.getComponent(ref, Xxx.getComponentType())` (puis vérifier `null`).
2. **Modifier** : `store.putComponent(...)` / `store.removeComponent(...)`.
3. **La vie** passe par le **système de dégâts** (`DamageEventSystem.handle(...)`), pas par un
   HP qu'on règle à la main.
4. **Créer** : `newHolder()` → `addComponent(...)` → `store.addEntity(holder, AddReason.SPAWN)`.
5. Tout ça sur le **thread du monde**, avec des vérifications `null`/`isValid()`.

Quand tu veux la leçon « créer un vrai mob (PNJ avec IA) », dis-le-moi.
