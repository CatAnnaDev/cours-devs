# Vérification

Tous les shaders du cours sont **compilés pour de vrai** avant d'être publiés. Pas relus : compilés,
par Godot et par Unity, en ligne de commande.

```bash
./verif/verifier.sh          # les deux moteurs
./verif/verifier.sh godot    # Godot seul, quelques secondes
./verif/verifier.sh unity    # Unity seul, quelques minutes la premiere fois
```

Sortie attendue :

```
== Godot : /Applications/Godot_mono.app/Contents/MacOS/Godot
   27 shaders copies
   OK : 27 shaders compilent
== Unity : /Applications/Unity/Hub/Editor/6000.5.7f1/Unity.app/Contents/MacOS/Unity
   URP 17.5.0
   13 shaders copies
   VERIF | OK | 01-couleur-et-parametres_unity.shader | avertissements: 0
   ...
   VERIF | BILAN | 13 shaders, 0 en erreur
```

Code de sortie `0` si tout passe, `1` sinon. Utilisable tel quel dans un hook ou une CI.

## Comment ça marche

Le script fabrique **un projet jetable** par moteur dans un dossier temporaire, y copie tous les
`.gdshader` et `.shader` trouvés sous `lecons/`, et lance le moteur en mode headless. Aucun projet
n'est créé ni modifié dans le cours.

**Godot** — `verif.gd` charge chaque `.gdshader`, en fait un `ShaderMaterial` et l'assigne à un
`MeshInstance3D`. Le parseur du langage de shader de Godot s'exécute au chargement : toute erreur
de syntaxe, tout `uniform` mal formé, tout built-in inexistant sort en `SHADER ERROR` sur la
sortie standard.

**Unity** — `VerifShaders.cs` est un script d'éditeur lancé par `-executeMethod`. Il réimporte
chaque `.shader` puis interroge `ShaderUtil.GetShaderMessages`, qui renvoie les messages du
compilateur HLSL. Le projet jetable déclare la version d'URP **fournie avec l'éditeur installé**,
donc la vérification fonctionne hors ligne.

## Ce que ça attrape et ce que ça n'attrape pas

**Attrapé** : syntaxe, types incompatibles, fonctions inconnues, built-ins qui n'existent pas ou
plus, `#include` introuvable, propriété déclarée dans `Properties` mais absente du `CBUFFER`,
redéfinition d'une constante intégrée.

**Pas attrapé** : le rendu. Un shader qui compile peut être parfaitement laid ou parfaitement
faux. La vérification garantit que **le code que tu copies part sans erreur**, pas que l'effet
soit réglé — ça, c'est la section « Les pièges » de chaque leçon.

Unity ne compile à l'import qu'un sous-ensemble de variantes, celles de la plateforme courante.
Une erreur qui n'apparaîtrait que dans une variante non compilée passerait au travers.

## Variables d'environnement

| Variable | Effet |
|---|---|
| `GODOT` | chemin du binaire Godot, si la détection automatique échoue |
| `UNITY` | chemin du binaire Unity |

La détection cherche `godot` dans le `PATH`, puis `/Applications/Godot*.app`, et pour Unity la
version la plus récente sous `/Applications/Unity/Hub/Editor/`.

## Et Unreal ?

Les leçons Unreal sont des graphes de nœuds décrits en texte, pas des fichiers compilables : il
n'y a rien à vérifier automatiquement. Les extraits HLSL des nœuds `Custom` sont en revanche
courts et repris de la version Unity, elle-même compilée.
