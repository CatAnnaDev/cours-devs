# Leçon 4 — Les événements (events)

**Prérequis :** Leçon 2. **Objectif :** faire réagir ton mod à ce qui se passe en jeu
(un joueur se connecte, se déconnecte...).

Fichier : `src/main/java/hytale/template/nenilearn/listeners/PlayerListener.java`

---

## L'idée : « quand X se produit, fais Y »

Le serveur déclenche en permanence des **événements** : un joueur se connecte, casse un
bloc, envoie un message... Ton mod peut **s'abonner** à ceux qui l'intéressent et exécuter
du code à chaque fois qu'ils se produisent.

C'est exactement le principe vu dans la base Java (leçon sur les méthodes) : tu fournis une
méthode, et **c'est le serveur qui l'appelle** au bon moment. On appelle ça un « écouteur »
(listener) ou une « réaction » (handler).

---

## S'abonner à un événement : `eventBus.register(...)`

Rappel (Leçon 2) : dans `setup()`, le plugin fait `new PlayerListener().register(eventBus)`.
Voici cette méthode `register` :

```java
public void register(EventRegistry eventBus) {
    eventBus.register(PlayerConnectEvent.class,    this::onPlayerConnect);
    eventBus.register(PlayerDisconnectEvent.class, this::onPlayerDisconnect);
}
```

Lis `eventBus.register(A, B)` comme une phrase :

- **A = `PlayerConnectEvent.class`** : l'événement qui m'intéresse (« quand un joueur se connecte »).
- **B = `this::onPlayerConnect`** : la méthode à appeler quand ça arrive. La syntaxe
  `this::onPlayerConnect` veut dire « la méthode `onPlayerConnect` de cet objet ». C'est une
  **référence de méthode** : on désigne la méthode sans l'appeler tout de suite — ce sera le
  serveur qui l'appellera.

---

## La réaction : que faire quand l'événement arrive

```java
private void onPlayerConnect(PlayerConnectEvent event) {
    String playerName = event.getPlayerRef() != null
            ? event.getPlayerRef().getUsername()
            : "Unknown";
    String worldName = event.getWorld() != null
            ? event.getWorld().getName()
            : "unknown";

    LOGGER.at(Level.INFO).log("[NeniLearn] Player %s connected to world %s",
            playerName, worldName);

    // TODO: ta logique d'arrivée du joueur ici
    // - envoyer un message de bienvenue
    // - charger les données du joueur
    // - annoncer l'arrivée aux autres
}
```

Ce qu'on apprend ici :

- La méthode reçoit l'**objet événement** (`PlayerConnectEvent event`), qui contient les
  infos du moment : qui se connecte, dans quel monde.
- **`event.getPlayerRef().getUsername()`** : le pseudo du joueur.
  **`event.getWorld().getName()`** : le nom du monde.
- Le code **vérifie d'abord que ce n'est pas `null`** (`getPlayerRef() != null ? ... : "Unknown"`).
  Souviens-toi de la leçon Java sur `null` : appeler une méthode sur `null` ferait planter
  le mod. Ici, si l'info manque, on met `"Unknown"` à la place. C'est une bonne habitude.

La déconnexion suit exactement le même modèle :

```java
private void onPlayerDisconnect(PlayerDisconnectEvent event) {
    String playerName = event.getPlayerRef() != null
            ? event.getPlayerRef().getUsername() : "Unknown";
    LOGGER.at(Level.INFO).log("[NeniLearn] Player %s disconnected", playerName);
}
```

---

## Le schéma général pour écouter un nouvel événement

1. **Écris une méthode** qui prend l'événement en paramètre :
   `private void onQuelqueChose(LEvent event) { ... }`.
2. **Abonne-la** dans `register(...)` :
   `eventBus.register(LEvent.class, this::onQuelqueChose);`.
3. **Utilise les infos** de `event` (avec une vérification `null` si besoin).

C'est tout. Le serveur s'occupe d'appeler ta méthode chaque fois que l'événement se produit.

---

## Ce qu'il faut retenir

1. Un **événement** = « quelque chose vient de se passer en jeu ». Ton mod **réagit**.
2. On s'abonne avec **`eventBus.register(LEvent.class, this::maMethode)`**.
3. La méthode de réaction reçoit l'objet **`event`**, riche en informations
   (`getPlayerRef()`, `getWorld()`...).
4. **Vérifie le `null`** avant d'utiliser une info, pour éviter les plantages.

**Entraîne-toi :** `exercices/exercices.md` → **Exercice 4** (souhaiter la bienvenue au joueur).
**Suite :** Leçon 5 — compiler, déployer et tester ton mod.
