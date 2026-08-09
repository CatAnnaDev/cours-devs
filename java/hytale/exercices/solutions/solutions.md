# Corrigés des exercices

À regarder **après avoir essayé** ! Compare avec ta solution ; l'important est de comprendre
*pourquoi* ça marche, pas seulement de recopier.

---

## Corrigé 1 — Message de démarrage

Dans `NeniLearnPlugin.java`, méthode `start()` :

```java
@Override
protected void start() {
    LOGGER.at(Level.INFO).log("[NeniLearn] Started!");
    LOGGER.at(Level.INFO).log("[NeniLearn] Use /neni help for commands");
    LOGGER.at(Level.INFO).log("[NeniLearn] Mod prêt — codé par Neniri !"); // <- ta ligne ajoutée
}
```

---

## Corrigé 2 — La commande `/neni ping`

**Nouveau fichier** `commands/PingSubCommand.java` :

```java
package hytale.template.nenilearn.commands;

import com.hypixel.hytale.server.core.Message;
import com.hypixel.hytale.server.core.command.system.CommandContext;
import com.hypixel.hytale.server.core.command.system.basecommands.CommandBase;

import javax.annotation.Nonnull;

/**
 * /neni ping - Répond "pong !"
 */
public class PingSubCommand extends CommandBase {

    public PingSubCommand() {
        super("ping", "Répond pong");
        this.setPermissionGroup(null);
    }

    @Override
    protected boolean canGeneratePermission() {
        return false;
    }

    @Override
    protected void executeSync(@Nonnull CommandContext context) {
        context.sendMessage(Message.raw("pong !"));
    }
}
```

**Activation** dans `NeniLearnPluginCommand.java` (constructeur) :

```java
this.addSubCommand(new HelpSubCommand());
this.addSubCommand(new InfoSubCommand());
this.addSubCommand(new ReloadSubCommand());
this.addSubCommand(new UISubCommand());
this.addSubCommand(new PingSubCommand());   // <- ligne ajoutée
```

---

## Corrigé 3 — Alias + entrée dans l'aide

**Alias** dans le constructeur de `PingSubCommand` :

```java
public PingSubCommand() {
    super("ping", "Répond pong");
    this.addAliases(new String[]{"pong"});   // /neni pong marche aussi
    this.setPermissionGroup(null);
}
```

**Entrée dans l'aide** : dans `HelpSubCommand.executeSync(...)`, ajoute une ligne :

```java
context.sendMessage(Message.raw("/neni ui - Open the dashboard UI"));
context.sendMessage(Message.raw("/neni ping - Répond pong !"));   // <- ligne ajoutée
context.sendMessage(Message.raw("========================"));
```

---

## Corrigé 4 — Message de bienvenue

Dans `PlayerListener.java`, méthode `onPlayerConnect(...)`, après le log existant :

```java
LOGGER.at(Level.INFO).log("[NeniLearn] Player %s connected to world %s", playerName, worldName);
LOGGER.at(Level.INFO).log("[NeniLearn] Bienvenue %s sur le serveur !", playerName); // <- ajout
```

### Bonus — une notification à l'écran du joueur

En s'inspirant de `NeniLearnDashboardUI` (qui utilise déjà `NotificationUtil`).

Ajoute ces imports en haut de `PlayerListener.java` :

```java
import com.hypixel.hytale.server.core.Message;
import com.hypixel.hytale.server.core.util.NotificationUtil;
import com.hypixel.hytale.protocol.packets.interface_.NotificationStyle;
```

Puis, dans `onPlayerConnect(...)`, quand le joueur n'est pas `null` :

```java
if (event.getPlayerRef() != null) {
    NotificationUtil.sendNotification(
        event.getPlayerRef().getPacketHandler(),
        Message.raw("NeniLearn"),
        Message.raw("Bienvenue " + playerName + " !"),
        NotificationStyle.Success
    );
}
```

> On vérifie `!= null` d'abord (réflexe de la leçon Java sur `null`) : si l'info du joueur
> manque, on n'essaie pas de lui envoyer la notification.

---

## Corrigé 5 — Textes de l'interface

Dans `Dashboard.ui` :

```
  // Titre
  Label {
    Text: "Tableau de bord NeniLearn";   // <- modifié
    ...
  }

  // Status text
  Label #StatusText {
    Text: "Bienvenue sur NeniLearn !";   // <- modifié
    ...
  }
```

Recompile (`./gradlew deployToServer`), relance le serveur, puis `/neni ui` pour voir le
résultat. (Le `.ui` est embarqué dans le `.jar` : il faut donc bien redéployer.)
