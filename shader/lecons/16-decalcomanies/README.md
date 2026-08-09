# 16 — Les décalcomanies : projeter sur une géométrie inconnue

## Ce qu'on fabrique

Un impact de balle qui épouse le mur, le sol et la caisse en même temps. Une flaque de sang qui
suit le relief. Un graffiti sur une façade irrégulière. Une trace de pneu, une fissure, un
marquage au sol.

Le principe : on pose une **boîte** dans la scène, et tout ce qui se trouve à l'intérieur reçoit
la texture — quelle que soit la géométrie, sans qu'elle sache quoi que ce soit du décal.

C'est la leçon la plus dense du bloc 2, et elle réunit tout : la profondeur (11), les espaces
(`00-bases/02`), le découpage (05) et les masques (04).

## L'idée : remonter de la profondeur à la position

Le shader tourne sur les faces d'une boîte invisible. Chaque pixel de cette boîte doit répondre à
une question : **quel point du décor est visible ici, et est-il dans ma boîte ?**

La réponse tient en trois transformations, et elles remontent le pipeline à l'envers :

```glsl
float brut = texture(depth_texture, SCREEN_UV).x;
vec4 vue = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, brut, 1.0);
vue.xyz /= vue.w;

vec3 position_monde = (INV_VIEW_MATRIX * vec4(vue.xyz, 1.0)).xyz;
vec3 position_locale = (inverse(MODEL_MATRIX) * vec4(position_monde, 1.0)).xyz;
```

| Étape | De quoi vers quoi |
|---|---|
| profondeur + UV d'écran | espace de découpe |
| `INV_PROJECTION_MATRIX` | **espace vue** |
| `INV_VIEW_MATRIX` | **espace monde** |
| `inverse(MODEL_MATRIX)` | **espace local du décal** |

Le chapitre `00-bases/02` décrivait le trajet objet → monde → vue → écran. Ici on le remonte
entièrement, dans l'autre sens. Une fois arrivé dans l'espace local du décal, tout devient
trivial : la boîte y est le cube unité centré sur l'origine.

```glsl
vec3 hors_boite = step(vec3(0.5), abs(position_locale));
if (hors_boite.x + hors_boite.y + hors_boite.z > 0.0) discard;

vec2 uv = position_locale.xz + 0.5;
```

Les UV du décal sont **deux des trois coordonnées locales**. C'est la projection : on ignore l'axe
selon lequel on projette, exactement comme le triplanar de la leçon 13 ignorait un axe à la fois.

## Le rejet par angle, et pourquoi il est indispensable

Sans lui, l'effet est inutilisable. Une surface presque parallèle à l'axe de projection reçoit la
texture **étirée sur toute sa longueur** — l'impact de balle devient une traînée verticale de
plusieurs mètres.

```glsl
vec3 normale_scene = normalize(cross(dFdx(position_monde), dFdy(position_monde)));
vec3 axe = normalize((MODEL_MATRIX * vec4(0.0, 1.0, 0.0, 0.0)).xyz);
if (abs(dot(normale_scene, axe)) < cos(radians(angle_max_degres))) discard;
```

**La normale de la scène, reconstruite depuis les dérivées.** `dFdx` et `dFdy` donnent la
variation de la position monde d'un pixel au suivant, en horizontal et en vertical. Ce sont deux
vecteurs tangents à la surface, et leur produit vectoriel est la normale. Aucune texture de
normales n'est nécessaire.

C'est une technique à retenir : **on peut toujours reconstruire une normale à partir d'une
position**, dès qu'on l'a par pixel.

**L'ordre des opérations compte** : les dérivées sont calculées **avant** les `discard`. Un `dFdx`
demande que les quatre pixels du bloc 2×2 soient encore actifs ; après un `discard`, le résultat
est indéfini. C'est une contrainte réelle et facile à violer sans s'en apercevoir.

## Godot

```glsl
render_mode blend_mix, cull_front, depth_draw_never, depth_test_disabled, unshaded;
```

Trois modes inhabituels, tous nécessaires :

**`cull_front`** — on dessine les faces **arrière** de la boîte, pas les avant. Pourquoi ? Parce
que si la caméra entre dans la boîte, ses faces avant sont derrière elle et disparaissent. Les
faces arrière, elles, restent visibles. Sans ça, le décal s'éteint dès qu'on s'approche.

**`depth_test_disabled`** — pour la même raison : les faces arrière de la boîte sont souvent
*derrière* le décor, donc rejetées par le test de profondeur. On le coupe, et c'est le `discard`
du shader qui décide.

**`depth_draw_never`** — le décal ne doit rien écrire dans le tampon.

**Le `inverse(MODEL_MATRIX)` est cher.** Godot 4 ne fournit pas de matrice inverse du modèle, et
l'inverser coûte plusieurs dizaines d'instructions **par pixel**. La bonne pratique en production :
la calculer une fois par image dans un script et la passer en uniforme.

```gdscript
materiau.set_shader_parameter("modele_inverse", global_transform.affine_inverse())
```

Le shader gagne alors une bonne partie de son coût. La version fournie utilise `inverse()` pour
rester lisible et autonome.

## Unity URP

```hlsl
float3 positionWS = ComputeWorldSpacePosition(uvEcran, brut, UNITY_MATRIX_I_VP);
float3 positionOS = TransformWorldToObject(positionWS);
```

Deux lignes contre quatre : URP fournit la matrice inverse vue-projection toute prête
(`UNITY_MATRIX_I_VP`) et la matrice inverse du modèle (`UNITY_MATRIX_I_M`, utilisée par
`TransformWorldToObject`). **Pas d'inversion par pixel** — c'est une vraie différence de coût avec
la version Godot.

```hlsl
clip(0.5 - max(max(distanceBord.x, distanceBord.y), distanceBord.z));
```

Le test de boîte en une ligne : la plus grande des trois distances doit rester sous 0.5.

**Les états de rendu** :

```
Blend SrcAlpha OneMinusSrcAlpha
ZWrite Off
ZTest Always
Cull Front
```

`ZTest Always` est l'équivalent de `depth_test_disabled`.

**URP a aussi un système de décals natif** (`Decal Renderer Feature` + shader `Decal`), qui gère
le tri, l'estompage par distance et le mélange des normales. Il est meilleur que ce shader pour la
production. Écris celui-ci d'abord : c'est exactement ce que fait le système natif, et tu sauras
le régler.

## Unreal

Voir `unreal.md`. Unreal a un **`Material Domain: Deferred Decal`** natif : tout le calcul de
cette leçon est fait par le moteur. Les points à connaître : les `Decal Blend Mode` (dont `Stain`
et `Normal`, très utiles), l'axe de projection qui est **-X** et non Y, et le `Decal Response` des
matériaux du décor, qui permet à une surface de refuser les décals.

## Le banc

`banc.gdshader` montre la scène **en coupe**, vue de côté. Le terrain ondule, une boîte de décal
se déplace de gauche à droite.

La ligne de surface change de couleur selon ce qui lui arrive :

| Couleur | Sens |
|---|---|
| gris | hors de la boîte |
| **rouge** | dans la boîte, mais **rejetée par l'angle** |
| **vert** | dans la boîte et acceptée : le décal s'applique |

Ce qu'il faut y voir :

**Les flancs raides sont rouges.** C'est exactement ce qu'on veut : sans le rejet, la texture
s'étirerait sur toute la pente. Descends `angle_max_degres` à 20 : presque tout devient rouge et
le décal ne s'applique plus que sur les plats. Monte à 85 : tout est vert, et en vrai tu verrais
d'affreuses traînées.

**Le décal disparaît quand la surface sort du haut ou du bas de la boîte.** C'est la troisième
dimension du test, celle qu'on oublie quand on pense « projection ». Une boîte trop plate rate les
creux ; une boîte trop haute déborde sur la géométrie qu'il y a derrière.

**Le compromis n'a pas de bonne réponse.** Une boîte grande attrape tout mais risque de peindre
l'objet situé derrière le mur ; une boîte serrée est précise mais rate les surfaces irrégulières.
C'est un réglage de designer, pas de programmeur — et c'est pour ça que les moteurs exposent la
taille de la boîte comme un objet manipulable dans l'éditeur.

## Les pièges

**Le décal se peint sur des objets situés derrière le mur.** La boîte est trop profonde. C'est le
défaut structurel des décals en boîte, et il n'a pas de remède parfait : on réduit la profondeur
et on accepte.

**Le décal disparaît quand on s'approche.** `cull_front` ou `depth_test_disabled` manquent.

**Le décal est étiré en longues traînées.** Rejet par angle absent ou trop permissif.

**Le décal apparaît sur les objets transparents, ou pas du tout sur eux.** Pas du tout : ils
n'écrivent pas dans le tampon de profondeur. Même limite qu'aux leçons 11 et 12.

**Des artefacts en bordure des objets.** La normale reconstruite par `dFdx` est fausse là où la
profondeur saute d'un objet au fond. On voit un liseré de pixels rejetés ou acceptés à tort. Le
remède propre est de lire la texture de normales du moteur quand elle existe (Godot :
`hint_normal_roughness_texture` ; URP : `_CameraNormalsTexture` avec la Depth Normals feature).

**Le décal ne bouge pas avec l'objet sur lequel il est posé.** Normal : il est ancré au monde.
Pour un impact sur un objet mobile, il faut attacher le décal à l'objet dans la hiérarchie de la
scène — c'est du travail de moteur, pas de shader.

**Ça rame avec cent décals.** Voir plus bas.

## Ce que ça coûte

**Par pixel de la boîte, pas par pixel du décal.** C'est le point à comprendre : une boîte qui
occupe un quart de l'écran fait tourner le shader sur un quart de l'écran, même si la texture
finale ne couvre que trois cents pixels. Tous les pixels rejetés ont quand même été calculés.

Conséquences :

**Une boîte serrée coûte moins cher.** Pas seulement plus juste : moins cher.

**L'estompage par taille à l'écran est la première optimisation.** Un décal qui occupe moins de
quelques pixels ne se voit pas ; ne le dessine pas. Unreal l'expose (`Fade Screen Size`), les
autres se scriptent facilement.

**Cent impacts de balle, c'est cent draw calls et cent boîtes.** Les moteurs sérieux les
regroupent ou les cuisent dans une texture après un délai. Une limite de décals actifs avec
recyclage du plus ancien est la solution universelle — et c'est une décision de gameplay autant
que de rendu.

**Le `inverse()` par pixel en Godot** est un coût évitable, voir plus haut.

## À toi

1. **Passe la matrice inverse en uniforme.** Un script qui la met à jour, et compare le coût.
   C'est une optimisation de deux lignes pour un gain réel — le genre de chose qu'on ne fait
   jamais parce qu'on ne sait pas que `inverse()` est cher.
2. **Un décal qui ne remplace que la normale.** N'écris pas `ALBEDO`, seulement la normale : tu
   obtiens une bosselure — un impact en creux sans changer la couleur. C'est le mode `Normal`
   d'Unreal, et c'est étonnamment utile.
3. **Un estompage par angle au lieu d'un rejet.** Remplace le `discard` par un `smoothstep` sur
   l'alpha. Le décal s'efface au lieu de se couper net, et le résultat est nettement meilleur.
4. **Une flaque qui grandit.** Un uniforme `avancement` de 0 à 1, combiné avec la dissolution de
   la leçon 05 appliquée à l'envers. Trois leçons empilées, un effet complet.
5. **Trouve la limite.** Pose un décal à cheval sur un mur et sur le sol, et regarde-le des deux
   côtés. Puis mets un objet fin juste derrière le mur, dans la boîte. Le décal traverse. C'est
   le défaut que tous les jeux ont, et savoir qu'il est structurel évite de le chercher pendant
   deux heures.

---

**Fin du bloc 2.** Tu sais maintenant déplacer des sommets, mentir sur les normales, lire la
profondeur et la couleur déjà rendues, texturer sans UV, creuser sans géométrie, animer depuis un
atlas et projeter sur une géométrie inconnue.

Les trois dernières leçons ont un point commun qui n'est pas un hasard : **elles lisent ce que le
moteur a déjà produit**. C'est le basculement du bloc 2, et c'est ce qui ouvre le bloc 5, les
effets d'écran.

Le bloc 3 attaque l'interaction : la neige qu'on écrase, le bouclier qui encaisse, les animations
cuites dans une texture, et mille objets dessinés en une passe.
