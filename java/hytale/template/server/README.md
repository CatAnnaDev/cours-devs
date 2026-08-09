# Le dossier serveur (pour tester ton mod)

Pour **écrire et compiler** ton mod, tu n'as besoin de rien de plus que ce qui est déjà là.
Mais pour le **tester en jeu**, il te faut un serveur Hytale — et le serveur a besoin des
fichiers du jeu, qui sont énormes (plusieurs Go). Je ne les ai donc pas mis dans le projet,
sinon il serait impossible à t'envoyer.

## Ce que tu dois poser ici (une seule fois)

Copie dans ce dossier `server/` les deux fichiers qui viennent de ton installation de Hytale :

- `HytaleServer.jar`  (le serveur ; ~118 Mo)
- `Assets.zip`        (les ressources du jeu ; plusieurs Go)

> Astuce : `HytaleServer.jar` est déjà présent dans `../libs/HytaleServer.jar`. Le script de
> setup à la racine du projet (`setup.sh` / `setup.bat`) peut le copier ici tout seul. Il ne
> te restera qu'à ajouter `Assets.zip`.

## Ensuite

Quand tu lances `./gradlew deployToServer` depuis `hytale/template/`, ton mod compilé est
copié automatiquement dans `server/mods/`. Tu n'as plus qu'à démarrer le serveur et à
rejoindre la partie pour voir ton mod en action. Tout est expliqué dans la **Leçon 5**.
