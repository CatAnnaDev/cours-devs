# Notions transverses — performance & structures de données

Neniri, ce dossier, ce n'est lié ni à un langage ni à un projet précis : c'est de la **culture
qui te servira partout** — en Rust, en Java, dans tes mods Hytale, et dans tout ce que tu
coderas plus tard.

À lire **après** les bases (les collections de `src/lecons/` et `java/`). Prends-les dans cet
ordre :

1. **`big-o.md`** — la « Big O sheet » : comprendre la **complexité**, c'est-à-dire savoir si
   ton code va rester rapide quand les données grossissent. C'est LA notion la plus rentable.
2. **`collections.md`** — les collections (tableau, `Vec`/`ArrayList`, `HashMap`, `HashSet`...) :
   laquelle choisir, et le coût de chaque opération.
3. **`optimisations.md`** — les petites optimisations qui marchent quel que soit le langage ou
   le but du projet (et celles spécifiques à un jeu/serveur comme Hytale).
4. **`json.md`** — comprendre et écrire le **JSON** sans erreur (le format du `manifest.json`
   et des données de jeu). À lire dès que tu touches à un fichier `.json`.

Une seule idée à garder en tête avant de commencer : **on écrit d'abord du code clair qui
marche ; on optimise ensuite, et seulement ce qui en a besoin.** Le reste de ce dossier
t'apprend à savoir *quoi* optimiser et *comment*.
