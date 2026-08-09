# Leçon 6 — Pour aller plus loin : l'interface (UI) — AVANCÉ

**Prérequis :** Leçons 1 à 5, et être à l'aise avec les classes/objets.
**Objectif :** comprendre *globalement* comment marche l'écran ouvert par `/neni ui`.
Ne t'inquiète pas si tout n'est pas clair : c'est la partie la plus avancée du template.
L'idée est de savoir **où regarder** quand tu voudras toucher à l'interface plus tard.

Fichiers :
- `src/main/resources/Common/UI/Custom/nenilearn/Dashboard.ui` (l'apparence)
- `src/main/java/hytale/template/nenilearn/ui/NeniLearnDashboardUI.java` (le comportement)

---

## 1. Le « quoi » : le fichier `.ui` (l'apparence)

Le fichier `Dashboard.ui` décrit **à quoi ressemble** l'écran : un titre, un texte de statut,
deux boutons. C'est un fichier de mise en page (un peu comme du HTML pour une page web).

```
Group {
  Anchor: (Width: 400, Height: 280);
  Background: #141c26(0.98);

  Label { Text: "NeniLearn Dashboard"; ... }
  Label #StatusText { Text: "Welcome to NeniLearn!"; ... }   // <- a un identifiant #StatusText

  TextButton #RefreshButton { Text: "Refresh"; ... }          // <- bouton "Rafraîchir"
  TextButton #CloseButton  { Text: "Close";   ... }           // <- bouton "Fermer"
}
```

À retenir :
- Des **éléments** : `Group` (conteneur), `Label` (texte), `TextButton` (bouton).
- Les **`#Identifiants`** (`#StatusText`, `#RefreshButton`...) servent à **désigner** un
  élément depuis le code Java (pour le modifier ou réagir à un clic).
- Pour changer un texte ou une couleur affichés, c'est ICI que ça se passe.

---

## 2. Le « comment » : la classe Java (le comportement)

Fichier : `NeniLearnDashboardUI.java`. Elle **`extends InteractiveCustomUIPage`** : une page
d'interface avec laquelle le joueur peut interagir. Deux méthodes comptent.

### `build(...)` — afficher la page et brancher les boutons

```java
public void build(Ref<EntityStore> ref, UICommandBuilder cmd,
                  UIEventBuilder evt, Store<EntityStore> store) {
    cmd.append(LAYOUT);   // charge l'apparence (le fichier Dashboard.ui)

    // "quand on clique sur #RefreshButton, envoie l'action 'refresh'"
    evt.addEventBinding(CustomUIEventBindingType.Activating, "#RefreshButton",
            new EventData().append("Action", "refresh"), false);

    evt.addEventBinding(CustomUIEventBindingType.Activating, "#CloseButton",
            new EventData().append("Action", "close"), false);
}
```

- `cmd.append(LAYOUT)` affiche la mise en page du `.ui`.
- `addEventBinding(...)` **relie un bouton à une action** : cliquer sur `#RefreshButton`
  enverra l'action `"refresh"` à la méthode suivante.

### `handleDataEvent(...)` — réagir aux clics

```java
public void handleDataEvent(Ref<EntityStore> ref, Store<EntityStore> store, UIEventData data) {
    switch (data.action) {
        case "refresh":
            refreshCount++;
            UICommandBuilder cmd = new UICommandBuilder();
            cmd.set("#StatusText.Text", "Refreshed " + refreshCount + " time(s)!"); // modifie le texte
            this.sendUpdate(cmd, false);   // envoie la mise à jour à l'écran
            // ... + une petite notification "Success"
            break;
        case "close":
            this.close();   // ferme la page
            break;
    }
}
```

- C'est un **`switch`** (vu dans la base Java) sur le nom de l'action.
- `cmd.set("#StatusText.Text", "...")` **change le texte** de l'élément `#StatusText` puis
  `sendUpdate(...)` l'envoie au joueur : l'écran se met à jour en direct.
- `this.close()` ferme la page.

---

## 3. Deux notions « pro » que tu croiseras

- **Le codec (`UIEventData.CODEC`)** : un traducteur. Les données échangées entre le serveur
  et l'écran du joueur voyagent « emballées » ; le codec dit comment **emballer/déballer**
  le champ `Action`. Tu n'as qu'à suivre le modèle existant pour ajouter un champ.

- **L'ECS : `Store<EntityStore>`, `Ref<EntityStore>`, `getComponent(...)`** : Hytale range
  tout ce qui existe en jeu (joueurs, entités...) sous forme de **composants**. Pour obtenir
  l'objet `Player` à partir d'une référence, on fait par exemple :
  ```java
  Player player = store.getComponent(ref, Player.getComponentType());
  ```
  Retiens juste l'idée : `Ref` = « quelle entité », `Store` = « le registre où la lire »,
  `getComponent` = « donne-moi telle facette de cette entité ». On approfondira si tu veux
  créer des entités ou des blocs.

---

## Ce qu'il faut retenir

1. Une UI = **deux fichiers** : le `.ui` (apparence) + une classe Java (comportement).
2. Les **`#Identifiants`** relient un élément de l'écran au code.
3. `build(...)` **affiche** et **branche les boutons** ; `handleDataEvent(...)` **réagit**
   aux clics (souvent via un `switch` sur l'action).
4. `codec` et `ECS` sont des notions avancées : tu sais maintenant **à quoi elles servent**
   et **où les retrouver**.

Bravo, tu as fait le tour du template ! Pour la pratique, file sur `exercices/exercices.md`.
Et quand tu voudras créer un **bloc**, un **objet**, un **PNJ** ou une **recette de craft**,
dis-le-moi : on ajoutera les leçons correspondantes.
