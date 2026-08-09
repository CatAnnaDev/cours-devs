# 04 — La boucle d'itération, et les cinq réflexes de débogage

Un shader ne s'écrit pas, il se **règle**. Tu changes un chiffre, tu regardes, tu recommences —
cinquante fois par effet. Tout ce qui rallonge ce cycle te coûte la journée. Ce chapitre monte
la scène de test la plus rapide possible dans chaque moteur, puis installe les réflexes qui
remplacent le débogueur que tu n'auras jamais.

## La scène de test

### Godot — deux minutes, rechargement instantané

C'est le moteur le plus rapide pour apprendre : Godot recompile le `.gdshader` **à la
sauvegarde**, sans reconstruire quoi que ce soit, et sans même que le jeu tourne.

**Pour un effet 3D** (la majorité des leçons) :

1. Nouvelle scène, nœud racine `Node3D`.
2. Ajoute un `MeshInstance3D`, mets `Mesh` sur `New SphereMesh`. Une sphère montre les normales,
   la silhouette et l'éclairage d'un seul coup d'œil — c'est le meilleur objet de test.
3. `Material Override` → `New ShaderMaterial` → `Shader` → `Load` → le `.gdshader` de la leçon.
4. Ajoute une `DirectionalLight3D` et un `WorldEnvironment` (dans son `Environment`, mets
   `Background` sur `Sky` : sans ciel, tout est noir et tu chercheras longtemps).
5. Ajoute une `Camera3D`, recule-la de 3 mètres.

Ouvre le `.gdshader` dans l'éditeur intégré ou dans ton éditeur habituel : la vue 3D se met à
jour à chaque sauvegarde, **sans lancer le jeu**. C'est ça, la boucle courte.

**Pour un effet 2D** — c'est notre « banc » :

1. Nouvelle scène, racine `Control`, puis un `ColorRect` en `Full Rect`.
2. `Material` → `New ShaderMaterial` → charge le fichier `banc.gdshader` de la leçon.

Un `ColorRect` avec un `shader_type canvas_item`, c'est un rectangle de pixels où `UV` va de 0 à
1 : le terrain de jeu idéal pour comprendre une formule sans se battre avec un maillage, un
éclairage ou une caméra. Les leçons fournissent un `banc.gdshader` chaque fois que la maths de
l'effet se comprend mieux à plat.

### Unity URP — le réimport automatique

1. Nouvelle scène. Supprime tout sauf la caméra et la lumière directionnelle.
2. `GameObject` → `3D Object` → `Sphere`.
3. `Create` → `Material`, choisis le shader de la leçon en haut de son inspecteur, glisse le
   matériau sur la sphère.
4. Dans la barre de la vue Scene, active le mode **Shaded** et coche `Always Refresh` sur le
   panneau Game si l'effet est animé (sinon `_Time` n'avance pas hors mode Play).

Unity réimporte le `.shader` dès qu'il change sur le disque, et la vue se met à jour. La boucle
est un peu plus longue qu'en Godot (une à trois secondes), et **beaucoup** plus longue si tu
touches à un `#pragma multi_compile` — chaque variante se recompile.

**L'équivalent du banc 2D**, si tu veux tester une formule à plat : un `Quad` face caméra avec un
shader unlit qui renvoie ta formule à partir de `IN.uv`. Le squelette de la leçon 02 sert
directement à ça.

**Le réflexe qui sauve** : quand un shader Unity se comporte de façon absurde après une
modification, `Assets` → `Reimport` sur le fichier. Le cache de variantes est parfois plus têtu
que le fichier.

### Unreal — la prévisualisation dans le graphe

1. Ouvre le matériau. Le panneau `Viewport` en haut à gauche affiche déjà l'effet sur une sphère,
   sans rien poser dans le niveau.
2. Change l'objet de prévisualisation avec les boutons en bas du viewport (sphère, cube, plan,
   théière). **La sphère pour tout ce qui touche aux normales, le plan pour tout ce qui touche
   aux UV.**
3. Clic droit sur n'importe quel nœud → `Start Previewing Node`. Le viewport affiche **la sortie
   de ce nœud** au lieu du matériau complet.

Ce dernier point est le meilleur outil de débogage des trois moteurs, et personne ne s'en sert
assez. Tu remontes le graphe nœud par nœud jusqu'à trouver celui dont l'aperçu n'est pas ce que
tu croyais.

Le coût d'Unreal, c'est la compilation : `Apply` sur un matériau utilisé par cent objets peut
prendre une minute. D'où la règle : travaille sur un matériau de test isolé, et ne branche le
vrai qu'à la fin.

## Réflexe 1 — la couleur est ton `print`

Il n'y a pas de point d'arrêt dans un shader, pas de console, pas de pas-à-pas. La seule
observation possible, c'est **écrire la valeur suspecte à l'écran**.

```glsl
COLOR = vec4(vec3(ma_valeur), 1.0);
```

```hlsl
return half4(maValeur.xxx, 1.0);
```

Noir partout ? Elle vaut zéro. Blanc partout ? Elle sature. Un dégradé alors que tu attendais un
uni ? Elle dépend de quelque chose que tu n'avais pas vu. **Cette ligne résout la moitié des
bugs de shader**, à condition d'y penser avant d'avoir passé vingt minutes à relire le code.

Pour une valeur qui n'est pas entre 0 et 1, mets-la à l'échelle : `ma_valeur * 0.1` si tu la
soupçonnes grande, `ma_valeur * 10.0` si tu la soupçonnes minuscule.

## Réflexe 2 — le négatif est invisible

Affiche `NORMAL` directement et tu verras un objet dont un tiers est **noir uni**. Ce n'est pas
que la normale y est nulle : elle y est **négative**, et l'écran ne sait afficher aucune valeur
sous zéro. Toute la moitié négative de tes données est confondue avec le zéro.

Le diagnostic en une ligne, positif en vert, négatif en rouge :

```glsl
COLOR = vec4(max(0.0, -x), max(0.0, x), 0.0, 1.0);
```

Et la convention universelle pour afficher une valeur signée comme une normale :

```glsl
COLOR = vec4(x * 0.5 + 0.5, 1.0);
```

C'est exactement ce que fait une normal map : `-1..1` rangé dans `0..1`. Quand tu vois une
texture bleu-lavande, tu regardes ce codage, pas une couleur.

## Réflexe 3 — `smoothstep` plutôt que `if`

Un masque circulaire, trois versions.

```glsl
float d = distance(UV, vec2(0.5));
float disque = step(d, 0.3);
```

Bord en escalier : le pixel est dedans ou dehors.

```glsl
float disque = smoothstep(0.3, 0.28, d);
```

Bord propre. **L'ordre des deux premiers arguments décide du sens** : du grand vers le petit, on
obtient 1 à l'intérieur. Les inverser retourne le masque — c'est la façon la plus courante et la
plus rapide d'inverser quelque chose sans écrire `1.0 - x`.

```glsl
float bord = fwidth(d);
float disque = smoothstep(0.3 + bord, 0.3 - bord, d);
```

`fwidth` mesure de combien la valeur change entre ce pixel et son voisin. S'en servir comme
largeur de transition donne un bord d'exactement **un pixel**, que l'objet soit collé à la
caméra ou à cinquante mètres. C'est l'astuce des contours qui ne bavent pas ; elle resservira aux
leçons 08 et 26.

## Réflexe 4 — animer, c'est faire entrer le temps quelque part

| Formule | Comportement |
|---|---|
| `sin(TIME * k)` | va-et-vient doux entre -1 et 1 |
| `sin(TIME * k) * 0.5 + 0.5` | le même, entre 0 et 1 |
| `fract(TIME * k)` | monte de 0 à 1 puis retombe d'un coup : le défilement |
| `TIME * k` | croît sans fin — parfait pour une rotation, **dangereux** ailleurs |

Le dernier mérite un avertissement qui te sauvera un jour : un `float` perd sa précision quand il
grandit. Après quelques heures de jeu, `TIME` vaut des dizaines de milliers, et une animation
basée dessus se met à saccader visiblement. Les valeurs qui doivent tourner en boucle passent
toujours par `fract` ou `sin`, jamais par une accumulation nue.

## Réflexe 5 — isole avant d'accuser

Quand un effet composé de quatre couches est faux, ne relis pas les quatre. Sors-en une :

```glsl
COLOR = vec4(vec3(masque_de_bruit), 1.0);
return;
```

Tu regardes une couche, tu la valides, tu passes à la suivante. C'est l'équivalent shader du
bissection, et c'est trois fois plus rapide que la relecture.

## Le tableau des symptômes

| Ce que tu vois | Cherche d'abord |
|---|---|
| objet rose (Unity) / violet (Godot) | le shader n'a pas compilé, va lire l'erreur |
| tout noir | une valeur négative ou nulle, ou pas de lumière dans la scène |
| tout blanc | une valeur qui sature : divise pour voir |
| le bord bave | il manque `smoothstep`, ou `fwidth` |
| ça ne bouge pas | le temps n'entre nulle part, ou la vue n'est pas rafraîchie hors Play |
| l'effet suit la caméra alors qu'il ne devrait pas | mauvais espace, relis `02-les-espaces.md` |
| l'effet est retourné verticalement | UV inversées entre moteurs : `uv.y = 1.0 - uv.y` |
| ça marche dans l'éditeur, pas en jeu | une variante de shader n'a pas été compilée : Godot `.import`, Unity Shader Variant Collection |

Avec ces cinq réflexes tu es équipée. **Leçon 01.**
