# Comprendre et écrire du JSON (sans se tromper)

Neniri, le **JSON** est partout : c'est le format dans lequel on écrit des **configurations**
et des **données**. En modding Hytale, tu en croises tout le temps — par exemple le
`manifest.json` (la carte d'identité de ton mod, vue en Leçon 1 d'Hytale) et les fichiers de
données du jeu (objets, recettes, PNJ...). Savoir le lire et l'écrire **sans faire d'erreur de
syntaxe** te fera gagner un temps fou.

Bonne nouvelle : JSON, c'est petit. En 10 minutes tu sais tout.

---

## À quoi ça sert

JSON = *JavaScript Object Notation*. C'est juste une façon **d'écrire des données en texte**,
lisible par toi ET par les programmes. On s'en sert pour :
- **configurer** un logiciel ou un mod (des réglages),
- **stocker** des données (une liste d'objets, des stats...),
- **échanger** des infos entre programmes (sur internet, etc.).

---

## Les briques de base

Une donnée JSON est **une valeur**. Une valeur peut être :

| Type | Exemple | Remarque |
|---|---|---|
| chaîne de texte (*string*) | `"Bonjour"` | **toujours** entre guillemets DOUBLES |
| nombre | `42`, `3.14`, `-7` | pas de guillemets, le point pour les décimales |
| booléen | `true`, `false` | en minuscules ! (pas `True`) |
| vide | `null` | « rien » |
| objet | `{ ... }` | des paires **clé : valeur** |
| tableau (*array*) | `[ ... ]` | une **liste** de valeurs |

### L'objet `{ }` — des paires clé/valeur

```json
{
  "nom": "Neniri",
  "age": 25,
  "estAdmin": true
}
```
- Chaque entrée est une **clé** (toujours entre guillemets doubles) suivie de `:` puis d'une **valeur**.
- On **sépare** les entrées par une **virgule** — sauf après la dernière.

### Le tableau `[ ]` — une liste

```json
{
  "couleurs": ["rouge", "vert", "bleu"],
  "scores": [10, 20, 30]
}
```
- Les éléments sont séparés par des virgules.
- Un tableau peut contenir n'importe quoi, même des objets.

### On imbrique autant qu'on veut

```json
{
  "joueuse": {
    "nom": "Neniri",
    "inventaire": [
      { "objet": "épée", "quantite": 1 },
      { "objet": "pomme", "quantite": 12 }
    ]
  }
}
```
Lis ça de l'extérieur vers l'intérieur : un objet, qui a une clé `joueuse` (un objet), qui a
une clé `inventaire` (un tableau d'objets). L'**indentation** ne change rien pour la machine,
mais elle t'aide ÉNORMÉMENT à t'y retrouver — garde-la propre.

---

## Les règles strictes (le JSON ne pardonne rien)

Le JSON est très à cheval sur la syntaxe. Voici les règles qui causent 99 % des erreurs :

1. **Guillemets DOUBLES obligatoires** pour les clés ET les chaînes. Jamais d'apostrophes `'`.
2. **Pas de virgule après le dernier** élément (la fameuse « trailing comma »).
3. **Une virgule entre chaque** paire/élément (ni en trop, ni en moins).
4. **Pas de commentaires** ! `//` et `/* */` n'existent pas en JSON pur.
5. **Autant de fermantes que d'ouvrantes** : chaque `{` a son `}`, chaque `[` son `]`.
6. `true`, `false`, `null` en **minuscules**, sans guillemets.

---

## Les erreurs fréquentes (❌ à éviter → ✅ correct)

**Virgule en trop à la fin :**
```json
❌ { "a": 1, "b": 2, }
✅ { "a": 1, "b": 2 }
```

**Apostrophes au lieu de guillemets :**
```json
❌ { 'nom': 'Neniri' }
✅ { "nom": "Neniri" }
```

**Clé sans guillemets :**
```json
❌ { nom: "Neniri" }
✅ { "nom": "Neniri" }
```

**Virgule manquante entre deux entrées :**
```json
❌ { "a": 1 "b": 2 }
✅ { "a": 1, "b": 2 }
```

**Un commentaire (interdit) :**
```json
❌ { "vie": 20 // les points de vie }
✅ { "vie": 20 }
```

**Booléen mal écrit / mis entre guillemets :**
```json
❌ { "actif": True }      ❌ { "actif": "true" }
✅ { "actif": true }
```

**Crochet/accolade non fermé :**
```json
❌ { "liste": [1, 2, 3 }
✅ { "liste": [1, 2, 3] }
```

**Confondre `;` (n'existe pas en JSON) avec `,` :**
```json
❌ { "a": 1; "b": 2 }
✅ { "a": 1, "b": 2 }
```

> Le piège le plus courant, et de loin, c'est la **virgule en trop** (règle 2) et la **virgule
> manquante** (règle 3). Quand un JSON « ne marche pas », regarde les virgules d'abord.

---

## Comment vérifier qu'un JSON est valide

- **Ton éditeur t'aide** : IntelliJ (et la plupart) **soulignent en rouge** l'endroit fautif
  et affichent un message du genre « `,` expected » ou « Trailing comma ». Lis le message : il
  pointe la ligne du problème.
- **Un validateur en ligne** : colle ton texte dans un « JSON validator / formatter » (par
  ex. jsonlint.com). Il te dit où ça casse et peut **ré-indenter** proprement.
- **Astuce mentale** : compte tes `{` et `}`, tes `[` et `]`. S'il en manque un, c'est souvent là.

---

## Côté Hytale

- Le **`manifest.json`** de ton mod est un JSON (Leçon 1 d'Hytale) : si tu y mets une virgule
  en trop ou oublies un guillemet, **ton mod ne se chargera pas**. Vérifie-le en premier si
  le serveur ne voit pas ton mod.
- Les **données du jeu** (objets, recettes, PNJ...) sont aussi en JSON. Même règles, mêmes
  pièges. Modifier une valeur, c'est facile ; il suffit de ne pas casser la syntaxe autour.

---

## À retenir

1. JSON = des **valeurs** : texte `"..."`, nombre, `true`/`false`, `null`, objet `{}`, tableau `[]`.
2. **Guillemets doubles** partout (clés et textes), **virgules entre** les éléments, **pas
   après le dernier**, **pas de commentaires**.
3. Les bugs viennent presque toujours d'une **virgule** ou d'un **guillemet/accolade** mal placé.
4. En cas de doute, **laisse ton éditeur ou un validateur** te montrer la ligne fautive.
