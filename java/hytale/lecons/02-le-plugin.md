# Leçon 2 — Le plugin et son cycle de vie

**Prérequis :** Leçon 1. **Objectif :** comprendre la classe principale du mod, les trois
moments de sa vie (`setup` / `start` / `shutdown`), et comment elle branche les commandes
et les événements.

Fichier : `src/main/java/hytale/template/nenilearn/NeniLearnPlugin.java`

---

## Le point d'entrée : `extends JavaPlugin`

Rappel de la Leçon 1 : le `manifest.json` pointe vers cette classe via `Main`. C'est donc
**la porte d'entrée** de ton mod.

```java
public class NeniLearnPlugin extends JavaPlugin {

    private static final HytaleLogger LOGGER = HytaleLogger.forEnclosingClass();
    private static NeniLearnPlugin instance;

    public NeniLearnPlugin(@Nonnull JavaPluginInit init) {
        super(init);
        instance = this;
    }

    public static NeniLearnPlugin getInstance() {
        return instance;
    }
```

Décortiquons :

- **`extends JavaPlugin`** : ta classe **hérite** de `JavaPlugin`, une classe de l'API.
  « Hériter » veut dire : elle récupère gratuitement plein de capacités du serveur
  (enregistrer des commandes, accéder aux événements...). Tu n'as plus qu'à remplir les trous.
- **Le constructeur** reçoit un `JavaPluginInit` et le passe à `super(init)` : c'est le
  serveur qui crée ton plugin et lui fournit ce dont il a besoin. Tu n'appelles jamais ce
  constructeur toi-même.
- **`instance` + `getInstance()`** : une astuce très courante (le « singleton »). Comme le
  serveur ne crée qu'UN seul exemplaire du plugin, on le range dans `instance` pour pouvoir
  le retrouver de partout ailleurs dans le code via `NeniLearnPlugin.getInstance()`.
- **`LOGGER`** : pour écrire des messages dans la console du serveur (voir plus bas).

---

## Les trois moments de la vie du mod

Le serveur appelle ces trois méthodes **à des moments précis**. Tu les « override »
(tu redéfinis) pour y mettre ton code.

```java
@Override
protected void setup() {
    LOGGER.at(Level.INFO).log("[NeniLearn] Setting up...");
    registerCommands();   // on branche les commandes
    registerListeners();  // on branche les événements
    LOGGER.at(Level.INFO).log("[NeniLearn] Setup complete!");
}

@Override
protected void start() {
    LOGGER.at(Level.INFO).log("[NeniLearn] Started!");
}

@Override
protected void shutdown() {
    LOGGER.at(Level.INFO).log("[NeniLearn] Shutting down...");
    instance = null;
}
```

- **`setup()`** : au chargement. C'est ICI qu'on **prépare et enregistre** tout (commandes,
  événements, configuration). La méthode la plus importante pour toi.
- **`start()`** : quand le serveur est lancé et prêt. Pour les actions de démarrage.
- **`shutdown()`** : à l'arrêt. Pour **nettoyer** (sauvegarder des données, libérer des
  ressources). Ici on remet `instance` à `null`.

> Analogie : `setup` = je m'installe et je branche mes appareils, `start` = j'allume,
> `shutdown` = j'éteins et je range avant de partir.

---

## Brancher les commandes et les événements

```java
private void registerCommands() {
    getCommandRegistry().registerCommand(new NeniLearnPluginCommand());
    LOGGER.at(Level.INFO).log("[NeniLearn] Registered /neni command");
}

private void registerListeners() {
    EventRegistry eventBus = getEventRegistry();
    new PlayerListener().register(eventBus);
}
```

- **`getCommandRegistry()`** vient de `JavaPlugin` (l'héritage !). On lui donne une commande
  à enregistrer : `registerCommand(new NeniLearnPluginCommand())`. → Leçon 3.
- **`getEventRegistry()`** donne accès au « bus d'événements » (`EventRegistry`), où l'on
  branche nos écouteurs. → Leçon 4.

Remarque : le vrai code entoure ces appels de `try { ... } catch (Exception e) { ... }`.
C'est une **sécurité** : si l'enregistrement échoue, le mod écrit un avertissement dans la
console au lieu de planter brutalement.

---

## Écrire dans la console : le logger

```java
LOGGER.at(Level.INFO).log("[NeniLearn] Player %s connected", playerName);
```

- `Level.INFO` = information normale ; `Level.WARNING` = avertissement.
- Le `%s` est un **trou** rempli par l'argument suivant (ici `playerName`). Pratique pour
  insérer des valeurs sans coller des `+` partout.
- Le préfixe `[NeniLearn]` permet de **repérer tes messages** au milieu de ceux du serveur.

C'est ton meilleur ami pour déboguer : quand tu te demandes « est-ce que mon code passe
ici ? », ajoute un `LOGGER...log(...)` et regarde la console.

---

## Ce qu'il faut retenir

1. La classe principale **`extends JavaPlugin`** et est désignée par `Main` dans le manifeste.
2. Trois méthodes-clés : **`setup`** (préparer/enregistrer), **`start`** (démarrer),
   **`shutdown`** (nettoyer).
3. On enregistre les commandes via **`getCommandRegistry()`** et les événements via
   **`getEventRegistry()`**.
4. **`LOGGER`** écrit dans la console : indispensable pour suivre ce que fait ton mod.

**Entraîne-toi :** `exercices/exercices.md` → **Exercice 1** (ajouter un message au démarrage).
**Suite :** Leçon 3 — les commandes.
