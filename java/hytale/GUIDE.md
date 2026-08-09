# Créer ton premier mod Hytale (en Java)

Coucou Neniri, c'est anna. On y est : la **dernière étape**, celle qui t'a donné envie de
te lancer — **faire tes propres mods Hytale** !

Tu as appris les bases de la programmation (le dossier `src/` en Rust et `java/` en Java).
Maintenant on met tout ça en pratique sur un vrai projet de mod, écrit en **Java**.

Je t'ai préparé un **projet template tout prêt**, rangé ici :

```
hytale/template/
```

> Dans tout ce guide, ce dossier, je l'appelle **« le template »**. C'est LUI que tu vas
> lire, modifier et compiler. Le reste du dossier `hytale/` (ce guide, les leçons, les
> exercices), c'est juste le **cours** que je t'ai écrit.

Le principe ne change pas : **tu lis → tu comprends → tu modifies → tu testes.**
La seule nouveauté : un mod ne se lance pas avec une petite commande. Il faut le **compiler**
en fichier `.jar`, puis le **charger sur un serveur Hytale** pour le voir en action.

---

## Étape 0 — Ce qu'il te faut

Avant de commencer (le script `setup.sh` / `setup.bat` à la racine vérifie tout ça pour toi) :

1. **Les bases de Java.** Si tu ne les as pas encore vues, finis d'abord `java/GUIDE.md` :
   variables, méthodes, classes/objets, événements... tout te servira ici.
2. **Le template**, déjà rangé dans `hytale/template/` (✅ je l'ai mis pour toi).
3. **Un éditeur Java** : IntelliJ IDEA est parfait. Ouvre le dossier `hytale/template/` avec.
4. **Java 25.** Le projet utilise un « toolchain » Gradle : si Java 25 te manque, Gradle
   peut le télécharger tout seul à la première compilation. Tu n'as rien à installer à la main.
5. **Un serveur Hytale** pour tester. Pose ton `HytaleServer.jar` et ton `Assets.zip` dans
   `hytale/template/server/` (le `README.md` de ce dossier t'explique tout).

Tu n'as **jamais fait de mod** ? Parfait, on part de zéro, je t'explique tout.

---

## C'est quoi un mod Hytale, en vrai ?

- Un mod (ou « plugin ») est un **programme Java** que le serveur Hytale **charge au démarrage**.
- Le serveur te prête une **boîte à outils** (une API) rangée dans `HytaleServer.jar` : des
  classes toutes faites pour réagir aux joueurs, créer des commandes, afficher des messages...
- Ton code **utilise** cette boîte à outils. Quand une joueuse se connecte ou tape une
  commande, le serveur appelle TON code.
- À la fin, ton projet est **compilé** en un fichier `.jar` que le serveur range dans son
  dossier `mods/`.

En une phrase : **tu écris des classes Java, le serveur les exécute au bon moment.**

---

## Le tour du template (à quoi sert chaque fichier)

```
hytale/template/
├── build.gradle         <- la "recette" de compilation (outils, dépendances, tâches)
├── settings.gradle      <- le nom du projet
├── .hytale/project.json <- infos pour l'outillage Hytale
├── libs/HytaleServer.jar <- l'API du serveur (la boîte à outils) — n'y touche pas
├── server/              <- là où tu poses ton serveur pour tester (voir son README)
└── src/main/
    ├── resources/
    │   ├── manifest.json   <- la CARTE D'IDENTITÉ du mod (nom, version, classe principale)
    │   └── Common/UI/Custom/nenilearn/Dashboard.ui  <- l'écran de l'interface (avancé)
    └── java/hytale/template/nenilearn/
        ├── NeniLearnPlugin.java        <- LE POINT D'ENTRÉE du mod (cycle de vie)
        ├── commands/                   <- les commandes /neni ...
        │   ├── NeniLearnPluginCommand.java  (la commande "parente" /neni)
        │   ├── HelpSubCommand.java          (/neni help)
        │   ├── InfoSubCommand.java          (/neni info)
        │   ├── ReloadSubCommand.java        (/neni reload)
        │   └── UISubCommand.java            (/neni ui — ouvre l'interface)
        ├── listeners/PlayerListener.java    <- réagit aux connexions/déconnexions
        └── ui/NeniLearnDashboardUI.java      <- la logique de l'interface (avancé)
```

---

## Ton parcours, étape par étape

Suis les leçons **dans l'ordre**. Chacune te dit ses **prérequis**, t'explique une partie du
template avec le vrai code, puis t'envoie vers un exercice pour t'entraîner.

### Leçon 1 — La structure d'un projet de mod
- **Prérequis :** les bases de Java (classes, méthodes).
- **Tu vas comprendre :** `build.gradle`, `manifest.json` et le rôle de `HytaleServer.jar`,
  c'est-à-dire comment un dossier de code devient un mod.
- **Ouvre :** `lecons/01-structure-du-projet.md`

### Leçon 2 — Le plugin et son cycle de vie
- **Prérequis :** Leçon 1.
- **Tu vas comprendre :** `NeniLearnPlugin` : `setup()`, `start()`, `shutdown()`, comment il
  enregistre commandes et événements, et comment écrire dans la console (le logger).
- **Ouvre :** `lecons/02-le-plugin.md`
- **Entraîne-toi :** `exercices/exercices.md` → Exercice 1.

### Leçon 3 — Les commandes
- **Prérequis :** Leçon 2.
- **Tu vas savoir :** créer des commandes `/neni ...` (la commande parente et ses
  sous-commandes), et envoyer des messages à la joueuse.
- **Ouvre :** `lecons/03-les-commandes.md`
- **Entraîne-toi :** `exercices/exercices.md` → Exercices 2 et 3.

### Leçon 4 — Les événements (events)
- **Prérequis :** Leçon 2.
- **Tu vas savoir :** réagir à ce qui se passe en jeu (une joueuse se connecte/déconnecte)
  avec `EventRegistry`.
- **Ouvre :** `lecons/04-les-events.md`
- **Entraîne-toi :** `exercices/exercices.md` → Exercice 4.

### Leçon 5 — Compiler, déployer, tester
- **Prérequis :** Leçons 2 à 4 (avoir du code à tester).
- **Tu vas savoir :** transformer ton code en `.jar`, le déployer dans le serveur, le lancer
  et vérifier en jeu. C'est ta boucle « est-ce que ça marche ? ».
- **Ouvre :** `lecons/05-compiler-et-tester.md`

### Leçon 6 — Pour aller plus loin : l'interface (UI) — AVANCÉ
- **Prérequis :** Leçons 1 à 5, et être à l'aise avec les classes/objets.
- **Tu vas survoler :** comment marche l'écran `/neni ui`. À regarder seulement une fois le
  reste bien acquis — ne te bloque pas dessus.
- **Ouvre :** `lecons/06-aller-plus-loin-ui.md`

### Leçon 7 — Comprendre l'ECS — AVANCÉ
- **Prérequis :** Leçons 1 à 6, et la leçon Java sur l'héritage/interfaces.
- **Tu vas comprendre :** l'architecture que Hytale utilise pour les joueuses, créatures et
  objets (entités / composants / systèmes), et d'où viennent les `Store`, `Ref`,
  `getComponent` croisés en Leçon 6.
- **Ouvre :** `lecons/07-ecs.md`

### Leçon 8 — Manipuler les entités — AVANCÉ
- **Prérequis :** Leçon 7 (l'ECS).
- **Tu vas savoir :** lire un composant (position, nom...), modifier une entité
  (`putComponent`/`removeComponent`), réagir aux dégâts (`DamageEventSystem`) et **créer**
  une entité (`Holder` → `addEntity`).
- **Ouvre :** `lecons/08-manipuler-les-entites.md`

### Leçon 9 — Créer un vrai mob (PNJ avec IA) — AVANCÉ
- **Prérequis :** Leçons 7 et 8.
- **Tu vas savoir :** faire apparaître un PNJ qui a déjà son IA, via les **rôles**
  (`NPCPlugin.get().spawnNPC(...)`), et comment lister les rôles disponibles.
- **Ouvre :** `lecons/09-creer-un-mob.md`

### Leçon 10 — Faire suivre un PNJ à la joueuse — AVANCÉ
- **Prérequis :** Leçons 7, 8 et 9.
- **Tu vas savoir :** faire poursuivre/suivre un PNJ en le **ciblant** sur la joueuse
  (`marked.setMarkedEntity(...)`). Contient le **`.java` complet** « créer un PNJ + le faire suivre ».
- **Ouvre :** `lecons/10-pnj-qui-suit.md`

---

## Ta boucle de travail (l'équivalent de « cargo test »)

Pour un mod, « vérifier » se fait en 4 temps (je détaille tout en Leçon 5). Place-toi
d'abord dans le dossier du template :

```
cd hytale/template
```

1. **Compiler** : `./gradlew shadowJar` → produit `build/libs/nenilearn-1.0.0.jar`.
   *(Sous Windows, écris `gradlew.bat` au lieu de `./gradlew`.)*
2. **Déployer** : `./gradlew deployToServer` (copie le `.jar` dans `server/mods/`).
3. **Lancer le serveur** et le rejoindre dans le jeu.
4. **Tester** : tape la commande (ex. `/neni help`) ou déclenche l'événement (connecte-toi),
   et regarde le résultat en jeu + les lignes `[NeniLearn] ...` dans la console.

Chaque exercice te dit **quel résultat attendre**, et tu as les corrigés complets dans
`exercices/solutions/` pour comparer si tu bloques.

---

## Mes petits conseils

- **Change une seule chose à la fois, puis recompile.** Si ça casse, tu sais quoi regarder.
- **Lis les lignes de la console.** Ton mod affiche des `[NeniLearn] ...` très utiles.
- **Ne touche pas à `HytaleServer.jar`** : c'est la boîte à outils du serveur, pas ton code.
- **Garde le `manifest.json` à jour** : si tu renommes la classe principale, change `Main`.
- **Pour la performance**, va voir le dossier `notions/` (à la racine) : la « Big O sheet »,
  les collections, et surtout `optimisations.md` qui a une section spéciale jeux/serveurs
  (ne pas allouer à chaque tick, ne pas bloquer le thread du monde...).

Quand tu seras à l'aise et que tu voudras créer un **bloc**, un **objet**, un **PNJ** ou une
**recette de craft**, dis-le-moi et je t'ajoute les leçons qu'il faut. On avance à ton rythme.
