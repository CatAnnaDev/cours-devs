# Leçon 5 — Compiler, déployer et tester

**Prérequis :** Leçons 2 à 4 (avoir du code à tester). **Objectif :** transformer ton code
en `.jar`, le mettre dans le serveur, le lancer et vérifier le résultat en jeu. C'est ta
boucle « est-ce que ça marche ? » — l'équivalent du `cargo test` des bases précédentes.

Toutes les commandes se lancent **dans le dossier du template**. Depuis la racine du
projet :

```
cd hytale/template
```

---

## Étape A — Compiler ton mod en `.jar`

```
./gradlew shadowJar
```

- `./gradlew` est le **lanceur Gradle** fourni avec le projet (pas besoin d'installer Gradle).
  Sur Mac/Linux on écrit `./gradlew` ; sous Windows ce serait `gradlew.bat`.
- `shadowJar` est la tâche qui empaquète **ton code + tes dépendances** en un seul fichier.
- Résultat attendu : `BUILD SUCCESSFUL`, et un fichier apparaît ici :
  ```
  build/libs/nenilearn-1.0.0.jar
  ```

Si tu vois `BUILD FAILED`, lis le message : Gradle indique **le fichier et la ligne** de
l'erreur (souvent une faute de frappe, un `;` oublié ou une accolade manquante).

> La toute première compilation peut être longue (Gradle télécharge ses outils, et au besoin
> le JDK 25). Les suivantes sont rapides.

---

## Étape B — Déployer le `.jar` dans le serveur

```
./gradlew deployToServer
```

Cette tâche (définie dans `build.gradle`) compile si besoin **puis copie** le `.jar` dans :

```
server/mods/
```

C'est le dossier où le serveur Hytale va **chercher les mods** à charger. Tu peux vérifier
que `nenilearn-1.0.0.jar` s'y trouve bien.

> Astuce : `deployToServer` dépend de la compilation, donc lancer cette seule commande
> suffit généralement (elle (re)compile + copie).

---

## Étape C — Lancer le serveur et tester en jeu

1. **Démarre le serveur** (depuis le dossier `server/`, avec son `HytaleServer.jar`).
2. **Regarde la console** : ton mod doit afficher ses messages de la Leçon 2, par exemple :
   ```
   [NeniLearn] Setting up...
   [NeniLearn] Registered /neni command
   [NeniLearn] Registered player event listeners
   [NeniLearn] Setup complete!
   [NeniLearn] Started!
   ```
   Si tu vois ces lignes, **ton mod est chargé** !
3. **Rejoins le serveur** dans le jeu et teste :
   - tape `/neni help` → tu dois voir la liste des commandes ;
   - tape `/neni info` → les infos du mod ;
   - connecte-toi/déconnecte-toi → la console affiche
     `[NeniLearn] Player <pseudo> connected ...` (l'événement de la Leçon 4).

---

## Ta boucle de développement (à répéter)

```
1. Je modifie un fichier .java
2. ./gradlew deployToServer        (recompile + copie)
3. Je relance le serveur
4. Je teste la commande / l'action en jeu
5. Je lis la console pour vérifier
   → si KO, je corrige et je recommence au point 1
```

**Modifie une seule chose à la fois.** Ainsi, si quelque chose casse, tu sais immédiatement
quoi regarder.

---

## Dépannage

- **`BUILD FAILED` avec `cannot find symbol`** → faute de frappe dans un nom de classe,
  méthode ou variable ; ou un `import` manquant.
- **`BUILD FAILED` avec `reached end of file while parsing`** → une accolade `}` manquante.
- **Le mod ne se charge pas (aucune ligne `[NeniLearn]`)** → vérifie le `manifest.json` :
  le champ `Main` doit correspondre EXACTEMENT au nom complet de ta classe principale
  (package + classe). Vérifie aussi que le `.jar` est bien dans `server/mods/`.
- **Une commande « n'existe pas » en jeu** → as-tu bien ajouté la sous-commande avec
  `addSubCommand(...)` dans `NeniLearnPluginCommand` ? As-tu redéployé et relancé le serveur ?

---

## Ce qu'il faut retenir

1. **`./gradlew shadowJar`** fabrique le `.jar` (`build/libs/nenilearn-1.0.0.jar`).
2. **`./gradlew deployToServer`** le copie dans `server/mods/`.
3. On **lance le serveur**, on lit la console (`[NeniLearn] ...`) et on **teste en jeu**.
4. La boucle est : *modifier → déployer → relancer → tester → lire la console*.

**Suite :** Leçon 6 — pour aller plus loin, un aperçu de l'interface (UI).
