# Guide d'apprentissage — neni_learn (Java)

Salut Neniri ! Ce dossier, c'est ta **base pour apprendre Java en partant de zéro**.
Tu y trouves deux choses :

- des **leçons** (`src/lecons/`) : du code commenté, que tu lis ET que tu exécutes ;
- des **exercices** (`src/exercices/`) : des bouts de code à compléter toi-même,
  avec une vérification automatique qui te dit si c'est juste.

Le principe est toujours le même à chaque étape :
**je lis → j'exécute → je m'entraîne → je vérifie.**

> Toutes les commandes de ce guide se lancent **depuis le dossier `java/`**.
> Ouvre un terminal à la racine du projet et place-toi dedans :
> ```
> cd java
> ```

---

## Étape 0 — Prérequis et installation

Avant de programmer, il faut **l'outil qui exécute le code** : le JDK (Java Development Kit).

**Prérequis :**
1. **Un JDK installé.** Vérifie-le en tapant dans le terminal :
   ```
   java -version
   ```
   Tu dois voir une ligne du type `openjdk version "25..."`. **Il te faut Java 22 minimum**
   (idéalement 25, c'est aussi ce qu'utilise Hytale).
   *(Le script `setup.sh` / `setup.bat` à la racine te dit si Java est bien installé.)*
   Si la commande est introuvable, installe un JDK depuis https://adoptium.net (Temurin).

2. **Un éditeur de code.** IntelliJ IDEA est idéal pour Java, mais pour démarrer n'importe
   quel éditeur de texte fait l'affaire.

3. **Savoir ouvrir un terminal** et s'y déplacer avec `cd`. C'est tout !

**Aucune connaissance préalable en programmation n'est requise** pour commencer.

---

## Comment lancer le projet

Bonne nouvelle : **pas besoin de compiler à la main.** Depuis Java 22, la commande
`java` compile et exécute directement un programme réparti en plusieurs fichiers.

| Ce que je veux faire | Commande |
|---|---|
| Voir **toutes** les leçons | `java src/Main.java` |
| Voir **une seule** leçon (ex. la n°3) | `java src/Main.java 3` |
| Lancer **les exercices** (vérification) | `java src/Tests.java` |

Quand tu lances les exercices, chaque ligne est annotée :

- `[OK]`   — ta réponse est **correcte**.
- `[RATÉ]` — ton code marche mais donne un **mauvais résultat** : relis l'énoncé.
- `[TODO]` — tu n'as **pas encore écrit** le code (le `throw ... UnsupportedOperationException` est toujours là).
- `[ERR]`  — ton code a **planté** (une erreur inattendue) : le message t'indique laquelle.

Objectif final : **tout passer en `[OK]`**.

---

## Le parcours, étape par étape

Suis les leçons **dans l'ordre** : chacune s'appuie sur la précédente.
Pour chaque étape : ses *prérequis*, son *objectif*, le *fichier à lire*, la *commande pour l'exécuter*,
et l'*exercice* pour t'entraîner.

### Leçon 1 — Variables et types
- **Prérequis :** Étape 0 terminée.
- **Objectif :** stocker des valeurs (nombres, texte, booléens) dans des variables et connaître les types de base.
- **Lis :** `src/lecons/Lecon01Variables.java`
- **Exécute :** `java src/Main.java 1`
- **Entraîne-toi :** `src/exercices/Ex01VariablesOperateurs.java` (1.1 à 1.4), puis `java src/Tests.java`.

### Leçon 2 — Opérateurs
- **Prérequis :** Leçon 1 (variables et types).
- **Objectif :** calculer (+ - * / %) et comparer (`==`, `<`, `&&`, `||`, `!`) des valeurs.
- **Lis :** `src/lecons/Lecon02Operateurs.java`
- **Exécute :** `java src/Main.java 2`
- **Entraîne-toi :** même fichier que la leçon 1 — `Ex01VariablesOperateurs.java`. Pense au piège de la division entière !

### Leçon 3 — Contrôle de flux (if / switch / boucles)
- **Prérequis :** Leçons 1 et 2 (tu dois savoir écrire une condition avec des opérateurs).
- **Objectif :** prendre des décisions (`if`, `switch`) et répéter des actions (`for`, `while`).
- **Lis :** `src/lecons/Lecon03Controle.java`
- **Exécute :** `java src/Main.java 3`
- **Entraîne-toi :** `src/exercices/Ex02Controle.java` (2.1 à 2.4).

### Leçon 4 — Méthodes (les « fonctions » de Java)
- **Prérequis :** Leçons 1 à 3.
- **Objectif :** découper ton code en blocs réutilisables qui prennent des paramètres et renvoient un résultat.
- **Lis :** `src/lecons/Lecon04Methodes.java`
- **Exécute :** `java src/Main.java 4`
- **Entraîne-toi :** `src/exercices/Ex03Methodes.java` (3.1 à 3.4).

### Leçon 5 — Tableaux et listes
- **Prérequis :** Leçons 3 (boucles) et 4 (méthodes).
- **Objectif :** regrouper plusieurs valeurs dans un tableau (taille fixe) ou une liste `ArrayList` (taille variable), et les parcourir.
- **Lis :** `src/lecons/Lecon05TableauxListes.java`
- **Exécute :** `java src/Main.java 5`
- **Entraîne-toi :** `src/exercices/Ex04TableauxListes.java` (4.1 à 4.4).

### Leçon 6 — Chaînes de caractères
- **Prérequis :** Leçons 4 (méthodes) et 5 (tableaux, pour `split`).
- **Objectif :** manipuler du texte : longueur, majuscules, recherche, découpage, comparaison avec `.equals`.
- **Lis :** `src/lecons/Lecon06Chaines.java`
- **Exécute :** `java src/Main.java 6`
- **Entraîne-toi :** `src/exercices/Ex05Chaines.java` (5.1 à 5.4).

### Leçon 7 — Classes, objets et enums
- **Prérequis :** Leçons 1 à 4 (variables, types, méthodes).
- **Objectif :** créer tes propres « modèles » (classes) avec des champs et des méthodes, fabriquer des objets avec `new`, et utiliser une `enum`.
- **Lis :** `src/lecons/Lecon07Classes.java`
- **Exécute :** `java src/Main.java 7`
- **Entraîne-toi :** `src/exercices/Ex06Classes.java` (6.1 à 6.4).

### Leçon 8 — Gestion des erreurs (exceptions / Optional)
- **Prérequis :** Leçons 4 (méthodes) et 7 (classes/objets).
- **Objectif :** gérer proprement « pas de valeur » avec `Optional`, et « opération qui échoue » avec `try`/`catch` et `throw`.
- **Lis :** `src/lecons/Lecon08Erreurs.java`
- **Exécute :** `java src/Main.java 8`
- **Entraîne-toi :** `src/exercices/Ex07Erreurs.java` (7.1 à 7.4).

### Leçon 9 — Valeurs, références et null
- **Prérequis :** Leçons 5 (listes) et 7 (objets).
- **Objectif :** comprendre la différence entre types primitifs (copiés) et objets (partagés par référence), et comment éviter les `NullPointerException`. *(Pas d'exercice dédié : c'est une leçon de compréhension, très utile pour déboguer plus tard.)*
- **Lis :** `src/lecons/Lecon09References.java`
- **Exécute :** `java src/Main.java 9`

### Leçon 10 — Collections en profondeur (et la mémoire)
- **Prérequis :** Leçon 5.
- **Objectif :** `ArrayList`, `HashMap`, `HashSet` : qui fait quoi, et **comment c'est rangé
  en mémoire** (tableau interne, table de hachage). Quand prendre quelle collection.
- **Lis :** `src/lecons/Lecon10Collections.java`
- **Exécute :** `java src/Main.java 10`
- **Entraîne-toi :** `src/exercices/Ex08Collections.java` (8.1 à 8.4).
- **Va plus loin :** le dossier `../notions/` (la « Big O sheet », les collections, les optimisations).

### Leçon 11 — Structurer une classe (static, public/private, final, getters/setters)
- **Prérequis :** Leçon 7 (classes/objets).
- **Objectif :** l'**encapsulation** : champs `private`, getters/setters, `static` vs instance,
  constantes `final`. La base d'un projet bien rangé.
- **Lis :** `src/lecons/Lecon11Encapsulation.java`
- **Exécute :** `java src/Main.java 11`
- **Entraîne-toi :** `src/exercices/Ex09Poo.java` (9.1).

### Leçon 12 — Héritage et interfaces (extends, implements)
- **Prérequis :** Leçon 11.
- **Objectif :** partager du comportement : `extends`, `super`, `@Override`, classes
  `abstract`, `implements` (interfaces) et le **polymorphisme**.
- **Lis :** `src/lecons/Lecon12HeritageInterfaces.java`
- **Exécute :** `java src/Main.java 12`
- **Entraîne-toi :** `src/exercices/Ex09Poo.java` (9.2 et 9.3).

---

## Méthode de travail conseillée

1. Lance la leçon (`java src/Main.java N`) et regarde ce qu'elle affiche.
2. Ouvre le fichier de la leçon et relis les commentaires en suivant l'affichage.
3. Ouvre l'exercice correspondant. Remplace le `throw new UnsupportedOperationException(...)`
   par ton code.
4. Lance `java src/Tests.java`. Vise le `[OK]`. Si c'est `[RATÉ]`, relis l'énoncé et corrige.
5. Passe à la suite quand tu es à l'aise (pas besoin que TOUT soit vert pour avancer,
   mais c'est plus satisfaisant !).

---

## Dépannage (erreurs fréquentes)

- **`command not found: java` (ou « java n'est pas reconnu » sous Windows)** → le JDK n'est
  pas installé ou pas dans le PATH. Installe-le depuis https://adoptium.net, puis rouvre le terminal.
- **`Error: Could not find or load main class ...`** → tu n'es pas dans le bon dossier.
  Place-toi à la racine du projet, fais `cd java`, puis relance.
- **`cannot find symbol`** à la compilation → souvent une faute de frappe dans un nom de
  variable ou de méthode, ou un point-virgule oublié. Le message indique le fichier et la ligne.
- **`reached end of file while parsing`** → il manque une accolade `}` quelque part.
- **`NumberFormatException`** → tu essaies de convertir en nombre un texte qui n'en est pas un.

---

## Pour aller plus loin (plus tard)

Quand tu seras à l'aise avec ces bases, les suites naturelles sont :
les **collections** (`Map`, `Set`), l'**héritage** et les **interfaces**, les **génériques**,
puis un **outil de build** (Maven ou Gradle) et un **framework de test** (JUnit) pour des
projets plus grands. Dis-le-moi quand tu veux et j'ajoute une « partie 2 ».

Et surtout : va lire le dossier **`../notions/`** (à la racine du dépôt). J'y ai mis la **« Big O
sheet »**, un guide sur les **collections** et un sur les **optimisations** — c'est valable
pour Java comme pour le reste, et ça te fera vraiment passer un cap.
