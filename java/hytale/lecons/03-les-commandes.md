# Leçon 3 — Les commandes

**Prérequis :** Leçon 2. **Objectif :** comprendre comment fonctionne `/neni ...`, créer
des sous-commandes et envoyer des messages au joueur.

Fichiers : `src/main/java/hytale/template/nenilearn/commands/`

---

## L'idée : une commande parente + des sous-commandes

Tu connais peut-être des commandes en jeu comme `/neni help`, `/neni info`...
Ici, `neni` est la **commande parente**, et `help`, `info`, `reload`, `ui` sont ses
**sous-commandes**. C'est comme un menu avec des entrées.

### La commande parente : `AbstractCommandCollection`

Fichier : `commands/NeniLearnPluginCommand.java`

```java
public class NeniLearnPluginCommand extends AbstractCommandCollection {

    public NeniLearnPluginCommand() {
        super("neni", "NeniLearn plugin commands");  // nom + description

        this.addSubCommand(new HelpSubCommand());
        this.addSubCommand(new InfoSubCommand());
        this.addSubCommand(new ReloadSubCommand());
        this.addSubCommand(new UISubCommand());
    }

    @Override
    protected boolean canGeneratePermission() {
        return false; // pas de permission requise pour la commande de base
    }
}
```

- **`extends AbstractCommandCollection`** : une commande qui **regroupe** des sous-commandes.
- `super("neni", "...")` : le **nom** tapé en jeu (`/neni`) et sa description.
- `addSubCommand(...)` : on **ajoute** chaque sous-commande au menu. Pour en créer une
  nouvelle, tu ajouteras une ligne ici (c'est l'Exercice 2 !).
- `canGeneratePermission()` renvoie `false` : tout le monde peut taper `/neni`.

---

## Une sous-commande simple : `CommandBase`

Fichier : `commands/HelpSubCommand.java`

```java
public class HelpSubCommand extends CommandBase {

    public HelpSubCommand() {
        super("help", "Show available commands");  // nom + description
        this.setPermissionGroup(null);
    }

    @Override
    protected boolean canGeneratePermission() {
        return false;
    }

    @Override
    protected void executeSync(@Nonnull CommandContext context) {
        context.sendMessage(Message.raw("=== NeniLearn Commands ==="));
        context.sendMessage(Message.raw("/neni help - Show this help message"));
        context.sendMessage(Message.raw("/neni info - Show plugin information"));
    }
}
```

Les points importants :

- **`extends CommandBase`** : une sous-commande « feuille » (qui fait vraiment quelque chose).
- Le constructeur fixe son **nom** (`"help"` → `/neni help`) et sa description.
- **`executeSync(CommandContext context)`** : LA méthode qui s'exécute quand on tape la
  commande. « Sync » = exécutée de façon simple et immédiate.
- **`context.sendMessage(Message.raw("..."))`** : envoie une ligne de texte à la personne
  qui a tapé la commande. `Message.raw(...)` = un message en texte brut.

> Le schéma à retenir pour TOUTE sous-commande simple :
> 1. une classe `extends CommandBase`,
> 2. un constructeur `super("nom", "description")`,
> 3. la méthode `executeSync` qui fait le travail,
> 4. et on l'ajoute avec `addSubCommand(...)` dans la commande parente.

---

## Lire l'état du mod depuis une commande

Fichier : `commands/InfoSubCommand.java`

```java
@Override
protected void executeSync(@Nonnull CommandContext context) {
    NeniLearnPlugin plugin = NeniLearnPlugin.getInstance();   // le singleton de la Leçon 2

    context.sendMessage(Message.raw("Name: NeniLearn"));
    context.sendMessage(Message.raw("Version: 1.0.0"));
    context.sendMessage(Message.raw("Status: " + (plugin != null ? "Running" : "Not loaded")));
}
```

Ici on récupère le plugin avec `getInstance()` (vu en Leçon 2) pour afficher son état.
Note l'usage de l'opérateur ternaire `condition ? siVrai : siFaux`, déjà croisé dans la base Java.

---

## La sous-commande joueur : `AbstractPlayerCommand` (aperçu)

Fichier : `commands/UISubCommand.java`

Certaines actions ont besoin du **joueur** et du **monde** (par exemple ouvrir une interface).
Pour ça, on utilise `AbstractPlayerCommand` au lieu de `CommandBase` :

```java
public class UISubCommand extends AbstractPlayerCommand {
    public UISubCommand() {
        super("ui", "Open the plugin dashboard");
        this.addAliases(new String[]{"dashboard", "gui"}); // /neni dashboard, /neni gui marchent aussi
    }

    @Override
    protected void execute(CommandContext context, Store<EntityStore> store,
                           Ref<EntityStore> ref, PlayerRef playerRef, World world) {
        context.sendMessage(Message.raw("Opening NeniLearn Dashboard..."));
        // ... (ouverture de l'interface — détaillée en Leçon 6)
    }
}
```

Deux nouveautés utiles dès maintenant :

- **`addAliases(...)`** : des **noms alternatifs**. Ici `/neni ui`, `/neni dashboard` et
  `/neni gui` font la même chose.
- La méthode s'appelle **`execute(...)`** (pas `executeSync`) et reçoit en plus le joueur
  (`playerRef`) et le monde (`world`). On l'utilise quand on a besoin du contexte du joueur.
  Le détail (Store, Ref, EntityStore) est de la programmation « par composants » (ECS) :
  on l'explique en Leçon 6, pas besoin de tout saisir maintenant.

---

## Ce qu'il faut retenir

1. **Commande parente** = `AbstractCommandCollection`, qui contient des sous-commandes.
2. **Sous-commande simple** = `CommandBase` + `executeSync(context)`.
3. **`context.sendMessage(Message.raw("..."))`** parle à la personne qui a tapé la commande.
4. Pour agir sur un joueur précis (UI, etc.), on passe à **`AbstractPlayerCommand`** + `execute(...)`.
5. On **active** une sous-commande en l'ajoutant avec `addSubCommand(...)` dans la parente.

**Entraîne-toi :** `exercices/exercices.md` → **Exercices 2 et 3**.
**Suite :** Leçon 4 — les événements.
