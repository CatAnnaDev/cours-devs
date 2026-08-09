# Leçon 1 — La structure d'un projet de mod

**Prérequis :** les bases de Java (classes, méthodes). **Objectif :** comprendre comment
un dossier de code Java devient un mod Hytale chargeable par le serveur.

Tous les chemins ci-dessous sont relatifs au template, c'est-à-dire le dossier `hytale/template/`.

---

## 1. La carte d'identité du mod : `manifest.json`

Fichier : `src/main/resources/manifest.json`

```json
{
  "Group": "hytale.template",
  "Name": "NeniLearn",
  "Version": "1.0.0",
  "Description": "A Hytale server mod",
  "Authors": [{"Name": "anna"}],
  "Main": "hytale.template.nenilearn.NeniLearnPlugin",
  "LoadOrder": "POSTWORLD",
  "IncludesAssetPack": true
}
```

C'est **le fichier le plus important pour le serveur** : quand il charge ton `.jar`, il lit
ce manifeste pour savoir quoi faire.

- `Name`, `Version`, `Description`, `Authors` : les infos affichées de ton mod.
- `Main` : **la classe à lancer** au démarrage. Ici `...NeniLearnPlugin`. Si tu renommes
  ou déplaces cette classe, tu DOIS mettre cette ligne à jour, sinon le serveur ne trouve
  plus ton mod.
- `LoadOrder: "POSTWORLD"` : à quel moment charger le mod (ici, après que le monde est prêt).
- `IncludesAssetPack: true` : le mod embarque aussi des ressources (ici, l'écran d'interface).

> À retenir : **le serveur entre dans ton mod par la classe indiquée dans `Main`.**

Ce fichier est au format **JSON** : une virgule en trop ou un guillemet oublié, et ton mod ne
se charge plus. Si tu n'es pas à l'aise avec le JSON, lis `../notions/json.md` (tout y est).

---

## 2. La recette de compilation : `build.gradle`

Gradle est l'outil qui **transforme ton code en `.jar`**. Le fichier `build.gradle` est sa
recette. Pas besoin de tout comprendre ; voici les lignes qui comptent.

```gradle
plugins {
    id 'java'
    id 'com.gradleup.shadow' version '8.3.0'   // pour fabriquer un "fat jar"
}

dependencies {
    compileOnly files('libs/HytaleServer.jar') // l'API du serveur
    compileOnly 'com.google.code.findbugs:jsr305:3.0.2' // annotations @Nonnull/@Nullable
    implementation 'com.google.code.gson:gson:2.10.1'   // librairie pour lire/écrire du JSON
}

java {
    toolchain { languageVersion = JavaLanguageVersion.of(25) } // on compile en Java 25
}
```

Trois idées clés :

- **`compileOnly files('libs/HytaleServer.jar')`** : c'est la fameuse « boîte à outils ».
  `compileOnly` veut dire : « j'ai besoin de cette API **pour compiler**, mais je ne
  l'inclus PAS dans mon `.jar` final » — car le serveur la fournit déjà. La mettre dedans
  créerait des doublons.
- **`implementation 'gson...'`** : une vraie dépendance, elle, sera **incluse** dans ton jar.
  C'est à ça que sert le plugin `shadow` : empaqueter ton code + tes dépendances en un seul jar.
- **`toolchain ... 25`** : ton code est compilé pour Java 25. Si ce JDK manque, Gradle peut
  le télécharger automatiquement.

Plus bas dans le fichier, des **tâches** pratiques sont définies (on s'en sert en Leçon 5) :

- `shadowJar` : fabrique le `.jar` final → `build/libs/nenilearn-1.0.0.jar`.
- `deployToServer` : copie ce `.jar` dans `server/mods/`.

---

## 3. Le reste en bref

- `settings.gradle` : juste le nom du projet → `rootProject.name = 'nenilearn'`.
- `.hytale/project.json` : des infos pour l'outillage Hytale (où est le jar, quelle tâche
  de build utiliser). Tu n'as presque jamais à y toucher.
- `libs/HytaleServer.jar` : **l'API du serveur**. On la lit (pour coder avec), on ne la
  modifie jamais.
- `server/` : un serveur de test. Après `deployToServer`, ton mod arrive dans `server/mods/`.

---

## Ce qu'il faut retenir

1. Le **manifest.json** dit au serveur quel mod charger et **par quelle classe entrer** (`Main`).
2. **Gradle** (`build.gradle`) compile ton code en `.jar`, en s'appuyant sur l'API
   `HytaleServer.jar` (en `compileOnly`).
3. Le résultat final est **un seul `.jar`** rangé dans le dossier `mods/` du serveur.

**Suite :** Leçon 2 — on ouvre la classe principale `NeniLearnPlugin` et son cycle de vie.
