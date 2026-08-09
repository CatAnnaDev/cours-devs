# 02 — Les espaces

Presque tous les bugs de shader que tu vas écrire viennent d'ici : **deux vecteurs comparés
alors qu'ils ne vivent pas dans le même repère**. L'effet a l'air presque juste, il bouge quand
la caméra bouge alors qu'il ne devrait pas, ou l'inverse. Ce chapitre coûte vingt minutes et
t'en économise cinquante.

## L'idée

Une position, ce sont trois nombres. Trois nombres ne veulent rien dire tant qu'on ne sait pas
**à partir d'où on compte**. Le même sommet du nez d'un personnage a des coordonnées
différentes selon qu'on compte depuis le centre du personnage, depuis le centre du monde, ou
depuis l'œil de la caméra. Ce sont les mêmes points, dans des **espaces** différents.

On passe d'un espace au suivant en multipliant par une matrice. C'est tout ce qu'une matrice
fait ici : traduire des coordonnées d'un repère vers un autre.

## Les cinq espaces, dans l'ordre

### 1. Espace objet (*object* / *local* / *model*)

L'origine est le pivot de l'objet, tel qu'il a été modélisé. Si le modeleur a mis le pivot aux
pieds du personnage, sa tête est à `(0, 1.8, 0)` — que le personnage soit en haut d'une montagne
ou à l'autre bout de la carte.

**C'est l'espace des attributs de sommet.** `positionOS` en Unity, `VERTEX` dans le `vertex()` de
Godot : espace objet.

À quoi il sert : tout ce qui doit **suivre l'objet**. Un dégradé du bas vers le haut d'un
personnage, un masque sur la moitié gauche d'une épée, une dissolution qui part des pieds. En
espace objet, l'effet tourne et se déplace avec l'objet — ce qui est presque toujours ce qu'on
veut.

### 2. Espace monde (*world*)

L'origine est le centre de la scène, commune à tout le monde.

À quoi il sert : tout ce qui doit **ignorer l'objet**. Le vent qui balaie une forêt entière doit
utiliser la position monde, sinon deux arbres identiques oscillent en phase et ça se voit
immédiatement. Le triplanar (leçon 13), la neige qui se dépose sur les faces tournées vers le
haut, un brouillard par altitude : espace monde.

```glsl
vec3 position_monde = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
```

```hlsl
float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
```

**Le `1.0` et le `0.0` à la fin ne sont pas décoratifs.** Une position s'écrit `vec4(p, 1.0)` :
elle subit la rotation *et* la translation. Une direction s'écrit `vec4(d, 0.0)` : le zéro annule
la translation, parce qu'une direction n'a pas de position. Transformer une normale avec un
`1.0` est une erreur classique, et le résultat est spectaculairement faux.

### 3. Espace vue (*view* / *camera* / *eye*)

L'origine est la caméra, qui regarde vers `-Z` (en OpenGL et en Godot).

À quoi il sert : moins souvent directement — mais **Godot y travaille par défaut**. Dans le
`fragment()` d'un shader `spatial`, `NORMAL`, `VIEW`, `VERTEX` sont **en espace vue**. C'est le
piège numéro un des gens qui arrivent d'Unity.

Ça n'empêche rien tant que tu compares des choses entre elles : `dot(NORMAL, VIEW)` est correct,
puisque les deux sont dans le même espace. Mais dès que tu veux comparer à une direction du
monde, il faut convertir :

```glsl
vec3 normale_monde = (INV_VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz;
```

### 4. Espace de découpe (*clip*)

Après la matrice de projection. C'est un espace en quatre dimensions, où la quatrième composante
`w` sert à faire la perspective. C'est **ce que ton shader de sommets doit obligatoirement
produire** :

```hlsl
OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
```

En Godot, tu n'écris pas cette ligne : le moteur la fait pour toi à partir de `VERTEX`. C'est
pour ça qu'un shader Godot paraît plus court — la plomberie est cachée.

### 5. Espace écran

Après la division par `w` puis la remise à l'échelle : des pixels, ou des UV de 0 à 1 sur toute
la fenêtre.

À quoi il sert : tout ce qui lit **ce qui a déjà été rendu** — réfraction du verre (leçon 12),
distorsion de chaleur, post-traitement (25).

```glsl
vec4 derriere = texture(screen_texture, SCREEN_UV);
```

```hlsl
float2 uvEcran = GetNormalizedScreenSpaceUV(IN.positionCS);
half3 derriere = SampleSceneColor(uvEcran);
```

**Le piège** : les UV d'écran collent à l'écran, pas à l'objet. Si tu t'en sers pour poser une
texture sur une surface, la texture ne bouge pas quand l'objet bouge — l'objet a l'air d'être
une fenêtre découpée dans un papier peint. C'est parfois exactement l'effet voulu, souvent pas.

## Le tableau de décision

| Je veux que mon effet… | Espace |
|---|---|
| tourne et se déplace avec l'objet | objet |
| reste fixe dans le monde pendant que l'objet bouge | monde |
| dépende de l'orientation de la caméra | vue |
| colle à l'écran | écran |
| dépende de la texture posée par le modeleur | UV (ce n'est pas un espace 3D, mais c'est le cinquième choix) |

Pose-toi la question **avant** d'écrire la ligne. « Que doit-il se passer si je fais tourner
l'objet ? Si je bouge la caméra ? » Les deux réponses désignent l'espace.

## Le cas des normales

Une normale n'est pas une direction comme les autres. Si l'objet est mis à l'échelle de façon
non uniforme (2 en X, 1 en Y), la matrice du modèle **incline mal** les normales : elles ne sont
plus perpendiculaires à la surface, et l'éclairage devient faux.

La correction mathématique est de transformer les normales par la **transposée de l'inverse** de
la matrice du modèle. Les moteurs le font pour toi si tu utilises leur fonction :

```hlsl
float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
```

Utilise-la. Écrire `mul((float3x3)UNITY_MATRIX_M, normalOS)` marche tant que personne ne met
l'objet à l'échelle — c'est-à-dire jusqu'au jour où quelqu'un le fait.

## L'espace tangent, en une phrase

Une normal map (leçon 10) contient des normales exprimées **relativement à la surface** : « un
peu vers la droite de la surface, un peu vers le haut de la surface ». Ça permet de réutiliser la
même texture sur n'importe quelle forme. Pour s'en servir, il faut la convertir vers l'espace
monde à l'aide du repère TBN (tangente, bitangente, normale) porté par le maillage. On y revient
en détail à la leçon 10 ; retiens juste que c'est un sixième espace, local à chaque point de la
surface.

## L'exercice qui ancre tout

Prends un cube, applique-lui un shader qui affiche une position en couleur, et fais tourner le
cube :

```glsl
ALBEDO = fract(position);
```

Essaie successivement avec la position objet, puis la position monde. Avec la position objet, le
motif tourne avec le cube — il est peint dessus. Avec la position monde, le cube semble se
déplacer **dans** un motif fixe, comme s'il était taillé dans un bloc de marbre.

Vingt secondes de manipulation, et tu ne confondras plus jamais les deux.

## À retenir

1. Trois nombres ne veulent rien dire sans leur repère.
2. `vec4(p, 1.0)` pour une position, `vec4(d, 0.0)` pour une direction.
3. Godot travaille en espace vue dans `fragment()` ; Unity te laisse choisir.
4. Les normales se transforment avec la fonction du moteur, pas à la main.
5. Choisis l'espace en te demandant ce qui doit arriver quand l'objet tourne.
