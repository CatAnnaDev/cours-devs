# Leçon 10 — Faire suivre un PNJ à la joueuse — AVANCÉ

**Prérequis :** Leçons 7 (ECS), 8 (manipuler les entités) et 9 (créer un mob).
**Objectif :** faire en sorte qu'un PNJ **te poursuive / te suive**.

> L'idée clé (et c'est rassurant) : tu ne programmes toujours **pas** de déplacement à la
> main. Un PNJ se dirige tout seul vers sa **cible marquée** grâce à la navigation de son
> rôle. Donc « suivre la joueuse » = **te désigner comme la cible marquée du PNJ**. C'est
> exactement le mécanisme d'un *taunt* / d'aggro.

---

## Le principe : la « cible marquée »

Chaque PNJ peut avoir une **cible marquée** (en interne, un « slot » nommé `"LockedTarget"`).
Quand on y met une entité, le rôle du PNJ (sa navigation, son IA) le fait **avancer vers
cette cible**. On met la joueuse dedans → le PNJ la suit.

> Important : ça marche si le rôle du PNJ **sait poursuivre une cible** (c'est le cas des
> rôles hostiles / combattants). Un rôle totalement passif n'ira pas forcément vers la cible.

---

## La chaîne pour définir la cible (vrai code)

À partir de la `ref` du PNJ (`npcRef`) et de la `ref` de la joueuse (`joueuseRef`) :

```java
import com.hypixel.hytale.server.npc.entities.NPCEntity;
import com.hypixel.hytale.server.npc.role.Role;
import com.hypixel.hytale.server.npc.role.support.MarkedEntitySupport;

// 1) Le composant NPC de l'entité (null si ce n'est pas un PNJ)
NPCEntity npc = store.getComponent(npcRef, NPCEntity.getComponentType());
if (npc == null) return;

// 2) Son rôle (= son cerveau)
Role role = npc.getRole();
if (role == null) return;

// 3) Le gestionnaire de cibles du rôle
MarkedEntitySupport marked = role.getMarkedEntitySupport();
if (marked == null) return;

// 4) On désigne la joueuse comme cible -> le PNJ se dirige vers elle
marked.setMarkedEntity(MarkedEntitySupport.DEFAULT_TARGET_SLOT, joueuseRef);
```

- **Arrêter de suivre** : remets la cible à `null` :
  ```java
  marked.setMarkedEntity(MarkedEntitySupport.DEFAULT_TARGET_SLOT, null);
  ```
- **Lire la cible actuelle** :
  ```java
  Ref<EntityStore> cible = marked.getMarkedEntityRef(MarkedEntitySupport.DEFAULT_TARGET_SLOT);
  ```

---

## Comment obtenir le `npcRef` ?

Deux cas pratiques :

1. **Tu viens de le faire apparaître** (Leçon 9) : `spawnNPC(...)` te renvoie sa ref via
   `.first()`. Garde-la.
2. **Tu vises un PNJ existant** : Hytale fournit un utilitaire qui te donne l'entité regardée :
   ```java
   import com.hypixel.hytale.server.core.util.TargetUtil;
   Ref<EntityStore> npcRef = TargetUtil.getTargetEntity(ref, store); // 'ref' = la joueuse
   ```
   (renvoie `null` si elle ne vise rien.)

---

## Prudence

- **Thread du monde** : comme toujours, fais ça là où tu as un `store` valide (dans
  `execute(...)` d'`AbstractPlayerCommand`, c'est bon).
- **Le rôle doit poursuivre sa cible** : sur un PNJ passif, marquer une cible ne suffit pas à
  le faire bouger.
- **Vérifie les `null`** à chaque étape (composant, rôle, support).

---

## Le fichier `.java` complet : créer un PNJ + le faire suivre

Voici une sous-commande **complète** : `/neni mob` fait apparaître un PNJ à ta position
**et** le fait te suivre dans la foulée. Copie ce fichier dans
`hytale/template/src/main/java/hytale/template/nenilearn/commands/`, **remplace `ROLE`** par
un rôle spawnable de ton install (Leçon 9 : `getRoleTemplateNames(true)`), puis enregistre-le
dans `NeniLearnPluginCommand` avec `this.addSubCommand(new MobFollowSubCommand());`.

```java
package hytale.template.nenilearn.commands;

import com.hypixel.hytale.component.Ref;
import com.hypixel.hytale.component.Store;
import com.hypixel.hytale.server.core.Message;
import com.hypixel.hytale.server.core.command.system.CommandContext;
import com.hypixel.hytale.server.core.command.system.basecommands.AbstractPlayerCommand;
import com.hypixel.hytale.server.core.modules.entity.component.TransformComponent;
import com.hypixel.hytale.server.core.universe.PlayerRef;
import com.hypixel.hytale.server.core.universe.world.World;
import com.hypixel.hytale.server.core.universe.world.storage.EntityStore;
import com.hypixel.hytale.server.npc.NPCPlugin;
import com.hypixel.hytale.server.npc.entities.NPCEntity;
import com.hypixel.hytale.server.npc.role.Role;
import com.hypixel.hytale.server.npc.role.support.MarkedEntitySupport;
import org.joml.Vector3d;
import org.joml.Vector3f;

import javax.annotation.Nonnull;

/**
 * /neni mob — fait apparaître un PNJ à la position de la joueuse, puis le fait la suivre.
 */
public class MobFollowSubCommand extends AbstractPlayerCommand {

    // REMPLACE par un rôle spawnable de ton install
    // (liste-les avec NPCPlugin.get().getRoleTemplateNames(true)).
    private static final String ROLE = "REMPLACE_MOI";

    public MobFollowSubCommand() {
        super("mob", "Fait apparaître un PNJ qui te suit");
        this.setPermissionGroup(null);
    }

    @Override
    protected boolean canGeneratePermission() {
        return false;
    }

    @Override
    protected void execute(@Nonnull CommandContext context,
                           @Nonnull Store<EntityStore> store,
                           @Nonnull Ref<EntityStore> ref,        // ref = la joueuse
                           @Nonnull PlayerRef playerRef,
                           @Nonnull World world) {

        // 1) Le rôle existe-t-il et est-il spawnable ?
        if (!NPCPlugin.get().hasRoleName(ROLE)) {
            context.sendMessage(Message.raw("Rôle inconnu : " + ROLE
                    + ". Liste-les avec getRoleTemplateNames(true)."));
            return;
        }

        // 2) Position de la joueuse (composant Transform, Leçon 8)
        TransformComponent t = store.getComponent(ref, TransformComponent.getComponentType());
        if (t == null) {
            context.sendMessage(Message.raw("Impossible de trouver ta position."));
            return;
        }
        Vector3d pos = t.getPosition();

        // 3) Faire apparaître le PNJ (son IA vient avec le rôle, Leçon 9)
        var resultat = NPCPlugin.get().spawnNPC(
                store, ROLE, null,
                new Vector3d(pos.x, pos.y, pos.z),
                new Vector3f());
        if (resultat == null) {
            context.sendMessage(Message.raw("Le spawn a échoué."));
            return;
        }
        Ref<EntityStore> npcRef = resultat.first();

        // 4) Le faire suivre : on désigne la joueuse comme sa cible marquée
        NPCEntity npc = store.getComponent(npcRef, NPCEntity.getComponentType());
        if (npc == null) {
            context.sendMessage(Message.raw("PNJ apparu, mais composant NPC introuvable."));
            return;
        }
        Role role = npc.getRole();
        if (role == null) {
            context.sendMessage(Message.raw("PNJ apparu, mais sans rôle (il ne suivra pas)."));
            return;
        }
        MarkedEntitySupport marked = role.getMarkedEntitySupport();
        if (marked == null) {
            context.sendMessage(Message.raw("PNJ apparu, mais il ne peut pas cibler."));
            return;
        }
        marked.setMarkedEntity(MarkedEntitySupport.DEFAULT_TARGET_SLOT, ref); // ref = toi

        context.sendMessage(Message.raw("PNJ « " + ROLE + " » apparu, il te suit !"));
    }
}
```

> Honnêteté : je ne peux pas compiler ce fichier pour toi ici (il faut le serveur Hytale),
> mais chaque appel vient du code réel d'Hytale et d'un mod qui tourne déjà. Si l'IDE souligne
> un import, laisse-le le résoudre (Alt+Entrée) — les classes existent bien.

---

## Ce qu'il faut retenir

1. « Suivre » = **désigner la joueuse comme cible marquée** du PNJ
   (`marked.setMarkedEntity(DEFAULT_TARGET_SLOT, joueuseRef)`).
2. La chaîne : `store.getComponent(npcRef, NPCEntity...)` → `npc.getRole()` →
   `role.getMarkedEntitySupport()`.
3. **Arrêter** : remettre la cible à `null`. **Viser un PNJ** : `TargetUtil.getTargetEntity(ref, store)`.
4. Ça marche si le rôle **sait poursuivre** une cible (rôles hostiles/combattants).

Pour aller plus loin (le nommer, le faire disparaître après un délai, lui donner un objet),
dis-le-moi.
