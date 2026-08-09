# Exercices — Modder pas à pas

Ici tu **modifies le template** (le dossier `hytale/template/`) pour le faire évoluer.
Comme un mod ne se « lance » pas tout seul, on vérifie en jeu (revois la Leçon 5) :

> **Boucle de vérification :** `./gradlew deployToServer` → relancer le serveur → tester
> en jeu → lire la console.

Chaque exercice indique : la **leçon** liée, **ce qu'il faut faire**, le **résultat attendu**,
et où trouver le **corrigé** (`solutions/solutions.md`) si tu bloques.
Fais-les dans l'ordre. Bon courage, tu vas y arriver !

---

## Exercice 1 — Un message de démarrage (Leçon 2)

**Fichier :** `src/main/java/hytale/template/nenilearn/NeniLearnPlugin.java`

**À faire :** dans la méthode `start()`, ajoute une ligne de log qui écrit un message à toi,
par exemple : `[NeniLearn] Mod prêt — codé par <ton prénom> !`.

**Indice :** copie le modèle déjà présent :
`LOGGER.at(Level.INFO).log("[NeniLearn] ton message");`

**Résultat attendu :** au démarrage du serveur, ta ligne apparaît dans la console, juste
après `[NeniLearn] Started!`.

---

## Exercice 2 — Ta première commande : `/neni ping` (Leçon 3)

**À faire :**
1. Crée un nouveau fichier `commands/PingSubCommand.java`, sur le modèle de
   `HelpSubCommand` : une classe `extends CommandBase`, nom `"ping"`, qui dans `executeSync`
   envoie le message `pong !`.
2. Dans `NeniLearnPluginCommand.java`, **active-la** en ajoutant une ligne :
   `this.addSubCommand(new PingSubCommand());`.

**Résultat attendu :** en jeu, taper `/neni ping` affiche `pong !`.

**Pièges classiques :** ne pas oublier l'`addSubCommand`, et bien recompiler/redéployer
avant de tester.

---

## Exercice 3 — Soigner la commande (Leçon 3)

**À faire :**
1. Donne un **alias** à ta commande ping : dans son constructeur, ajoute
   `this.addAliases(new String[]{"pong"});` pour que `/neni pong` marche aussi.
2. Ajoute une ligne dans `HelpSubCommand` pour que `/neni help` **liste** ta nouvelle
   commande : `context.sendMessage(Message.raw("/neni ping - Répond pong !"));`.

**Résultat attendu :** `/neni pong` fonctionne comme `/neni ping`, et `/neni help` mentionne
désormais la commande ping.

---

## Exercice 4 — Souhaiter la bienvenue (Leçon 4)

**Fichier :** `src/main/java/hytale/template/nenilearn/listeners/PlayerListener.java`

**À faire :** dans `onPlayerConnect(...)`, après la ligne de log existante, ajoute un log
personnalisé qui souhaite la bienvenue au joueur par son pseudo, par exemple :
`[NeniLearn] Bienvenue <pseudo> sur le serveur !`.

**Indice :** la variable `playerName` contient déjà le pseudo ; réutilise le `%s` :
`LOGGER.at(Level.INFO).log("[NeniLearn] Bienvenue %s sur le serveur !", playerName);`

**Résultat attendu :** quand un joueur se connecte, la console affiche ton message de
bienvenue avec son pseudo.

**Bonus (avancé) :** envoie une vraie **notification à l'écran** du joueur, en t'inspirant
du code de `NeniLearnDashboardUI` (qui utilise `NotificationUtil.sendNotification(...)`).
Le corrigé montre comment faire.

---

## Exercice 5 — Changer l'interface (Leçon 6 — optionnel)

**Fichier :** `src/main/resources/Common/UI/Custom/nenilearn/Dashboard.ui`

**À faire :** change le titre de l'écran (`Text: "NeniLearn Dashboard";`) et le texte de
bienvenue (`#StatusText`, `Text: "Welcome to NeniLearn!";`) pour mettre les tiens, en français.

**Résultat attendu :** en jeu, `/neni ui` ouvre l'écran avec tes nouveaux textes.

---

## Et après ?

Quand ces exercices sont acquis, tu sais déjà : ajouter une commande, réagir à un événement,
écrire dans la console, modifier une interface, et compiler/déployer/tester. C'est la base de
**tout** mod ! Pour la suite (créer un bloc, un objet, un PNJ, une recette...), dis-le-moi et
je t'ajoute les leçons qu'il faut.
