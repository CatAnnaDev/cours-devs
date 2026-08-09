# 11 — La profondeur : écume au bord de l'eau, particules douces

## Ce qu'on fabrique

Une eau qui **sait où est le fond** : transparente et claire près de la rive, opaque et sombre au
large, avec une ligne d'écume irrégulière qui épouse exactement le contour du terrain — quel que
soit ce terrain, sans que rien n'ait été peint.

Et, avec le même calcul en trois lignes : les particules qui cessent de couper le décor à
l'emporte-pièce.

## L'idée : lire la profondeur déjà rendue

Rappel du chapitre `00-bases/01` : un pixel ne sait rien de ses voisins, et rien de ce qui a été
dessiné avant. **Sauf** si on le lui donne — et le moteur peut le lui donner sous la forme du
**tampon de profondeur**, une texture plein écran contenant, pour chaque pixel, la distance de la
surface la plus proche déjà dessinée.

Un objet **transparent**, dessiné après tous les opaques, peut donc lire cette texture et savoir
ce qu'il y a derrière lui. C'est la clé de cette leçon, et de la 12, et de la 16, et de tout le
bloc 5.

```
epaisseur = profondeur_de_ce_qui_est_derriere - profondeur_de_ma_surface
```

Cette seule soustraction donne, selon le contexte :

| Interprétation | Effet |
|---|---|
| l'épaisseur d'eau traversée | teinte, transparence, absorption |
| la distance à la rive | **écume** |
| la distance entre une particule et le décor | **fondu doux** |
| la distance à un objet | un halo d'intersection, un bouclier qui s'allume au contact (leçon 18) |

## Le piège de la profondeur non linéaire

La valeur stockée dans le tampon **n'est pas une distance**. C'est une valeur entre 0 et 1,
répartie de façon très inégale : énormément de précision près de la caméra, presque plus au loin.
C'est voulu — c'est là que la précision est utile — mais ça veut dire qu'on ne peut pas soustraire
deux valeurs brutes et espérer un résultat qui ait un sens.

Il faut la **linéariser** : la reconvertir en mètres. Chaque moteur a sa façon de faire, et c'est
la seule vraie difficulté de la leçon.

## Godot

```glsl
uniform sampler2D depth_texture : hint_depth_texture, filter_nearest;

void fragment() {
    float brut = texture(depth_texture, SCREEN_UV).x;
    vec4 vue = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, brut, 1.0);
    float profondeur_fond = -vue.z / vue.w;
    float profondeur_surface = -VERTEX.z;

    float epaisseur = max(profondeur_fond - profondeur_surface, 0.0);
    ...
}
```

**Ligne par ligne, parce qu'elle est dense.**

`SCREEN_UV * 2.0 - 1.0` remet les UV d'écran (0 à 1) dans l'intervalle des coordonnées
normalisées (-1 à 1). Avec la profondeur brute en troisième composante, on a la position du pixel
dans l'espace de découpe.

`INV_PROJECTION_MATRIX` défait la projection : on revient en espace **vue**. Comme cette matrice
introduit une composante `w`, il faut diviser par elle — c'est ce que fait `/ vue.w`.

`-vue.z` parce qu'en espace vue, la caméra regarde vers les `z` négatifs : ce qui est devant a un
`z` négatif, et on veut une distance positive.

`-VERTEX.z` donne la profondeur du pixel courant : dans le `fragment()` d'un shader `spatial`,
**`VERTEX` est la position en espace vue**, pas en espace objet comme dans `vertex()`. C'est une
des rares incohérences de Godot, et elle est exactement ce qu'il nous faut ici.

**`filter_nearest` sur la texture de profondeur.** Interpoler des profondeurs n'a aucun sens : à
la frontière entre un objet proche et le fond, la moyenne donne une distance intermédiaire où il
n'y a rien. Le résultat est un liseré d'écume parasite autour de chaque objet. Toujours en
`nearest`.

**Le shader doit être transparent.** `blend_mix` et surtout `depth_draw_never` : un objet qui
écrit dans le tampon de profondeur avant de le lire lit sa propre profondeur, et l'épaisseur vaut
zéro partout.

## L'écume

```glsl
float grain = texture(bruit_ecume, UV * carrelage_ecume + vitesse_ecume * TIME).r;
float bord = 1.0 - clamp(epaisseur / largeur_ecume, 0.0, 1.0);
float ecume = smoothstep(grain * 0.6, grain * 0.6 + 0.18, bord);
```

`bord` vaut 1 pile au contact du terrain et retombe à 0 à `largeur_ecume` de distance. C'est déjà
une ligne d'écume — mais parfaitement régulière, donc fausse.

L'astuce est dans la troisième ligne : **le seuil du `smoothstep` est lui-même le bruit**. Là où
le bruit est faible, l'écume déborde loin du bord ; là où il est fort, elle reste collée. La
frontière devient irrégulière et, comme le bruit défile, elle **bouge** — exactement comme une
ligne d'écume réelle.

C'est un patron à retenir : **pour rendre une frontière irrégulière, ne perturbe pas la valeur,
perturbe le seuil.** Le résultat est bien plus organique qu'une simple addition de bruit.

## Unity URP

```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

float2 uvEcran = GetNormalizedScreenSpaceUV(IN.positionCS);
float brut = SampleSceneDepth(uvEcran);
float profondeurFond = LinearEyeDepth(brut, _ZBufferParams);
float profondeurSurface = -TransformWorldToView(IN.positionWS).z;
float epaisseur = max(profondeurFond - profondeurSurface, 0.0);
```

Plus court que Godot, parce qu'URP fournit `LinearEyeDepth` — qui fait exactement la
dé-projection, avec les constantes déjà rangées dans `_ZBufferParams`.

**Il faut activer la texture de profondeur.** Dans l'asset URP : `Depth Texture` coché. Sans ça,
`SampleSceneDepth` renvoie zéro et l'écume couvre tout. C'est la première chose à vérifier.

**`LinearEyeDepth` et non `Linear01Depth`.** La première renvoie des mètres, la seconde une valeur
de 0 à 1 entre les plans proche et lointain. Pour soustraire deux distances, il faut des mètres.

**La profondeur du pixel courant** : on repasse par `TransformWorldToView(positionWS).z`. On
pourrait aussi la sortir du `w` de la position en espace de découpe, mais il faut alors la
transporter soi-même dans un `TEXCOORD` — la sémantique `SV_POSITION` est réécrite par le
rastériseur entre le vertex et le fragment, et son `.w` n'y contient plus la même chose.

## Les particules douces, le même calcul

Une particule est un plan translucide. Quand elle croise le sol, on voit **la ligne de coupe** :
une arête droite qui trahit immédiatement que la fumée est un rectangle.

Le remède est une ligne :

```glsl
ALPHA *= clamp(epaisseur / distance_de_fondu, 0.0, 1.0);
```

La particule devient transparente à mesure qu'elle s'approche d'une surface. Plus d'arête. C'est
ce qu'on appelle une *soft particle*, et c'est un des rapports qualité-effort les plus élevés de
tout le rendu temps réel.

Dans les moteurs : Godot, coche `Soft Particles` sur le `StandardMaterial3D`, ou écris la ligne
ci-dessus. Unity, la propriété `Soft Particles Factor` du shader Particles, ou la même ligne.
Unreal, le nœud `DepthFade`, qui ne fait que ça.

## Unreal

Voir `unreal.md` : le nœud **`DepthFade`** qui fait le fondu doux tout seul, `SceneDepth` et
`PixelDepth` pour l'épaisseur, et le rappel que tout est en **centimètres** — une profondeur
maximale de 3 mètres s'écrit `300`.

## Le banc

`banc.gdshader` simule une côte vue de dessus. Il n'y a pas de tampon de profondeur : un `fbm`
tient lieu de relief, et l'épaisseur d'eau est simplement `-hauteur`. Tout le reste du calcul est
identique au shader réel.

Trois choses à manipuler :

**`niveau_mer`** — fais-le monter et descendre. La ligne d'écume suit le contour du terrain,
qui n'a été peint nulle part. C'est **toute** l'idée de la leçon : l'effet est calculé à partir
d'une donnée qui existe déjà.

**`largeur_ecume`** — l'épaisseur de la bande. Note qu'elle est en unités de profondeur, pas en
distance à l'écran : sur une pente douce, la bande est large ; sur une falaise, elle est fine
voire absente. C'est physiquement juste et c'est parfois gênant — les jeux qui veulent une écume
d'épaisseur constante calculent la distance horizontale, ce qui est nettement plus cher.

**`profondeur_max`** — la distance sur laquelle l'eau passe du clair au sombre. Mets-la très
petite : l'eau devient une flaque opaque. Très grande : une piscine sans fond. Entre les deux, ce
paramètre à lui seul donne son caractère au plan d'eau.

## Les pièges

**L'écume couvre tout, ou rien.** La texture de profondeur n'est pas activée (Unity), ou le
matériau est opaque (les trois moteurs). Diagnostic en une ligne : affiche `epaisseur * 0.1`
directement dans la couleur. Si c'est noir uniforme, tu ne lis pas la profondeur.

**Un liseré d'écume autour de tous les objets.** Filtrage de la texture de profondeur en linéaire
au lieu de nearest.

**L'écume apparaît aussi contre les autres objets transparents.** Non : elle n'apparaît **pas**,
et c'est le problème. Un objet transparent n'écrit pas dans le tampon de profondeur, donc l'eau
ne le voit pas. Une autre nappe d'eau, une particule, une vitre : invisibles pour ce calcul.
C'est une limite structurelle, pas un réglage.

**L'effet marche en perspective et casse en vue orthographique.** La dé-projection diffère.
`LinearEyeDepth` gère les deux en URP ; en Godot il faut un cas particulier. C'est rare, mais
ça surprend sur un jeu en vue isométrique.

**Ça marche dans l'éditeur, pas sur mobile.** Certains GPU à tuiles n'exposent pas la profondeur
de la scène sans une passe supplémentaire coûteuse. Vérifie tôt sur l'appareil cible.

## Ce que ça coûte

**Un accès à la texture de profondeur, plein écran.** C'est un accès texture de plus, donc
comparable à une texture de couleur — mais avec un défaut : il se fait à la position d'écran, donc
sans cohérence avec les accès en UV du reste du shader. Le cache est moins efficace.

Le coût réel est en amont : **le moteur doit produire cette texture**. En URP, cocher
`Depth Texture` déclenche une passe de pré-profondeur sur toute la scène si le pipeline n'en avait
pas déjà. Sur une scène chargée, ce n'est pas gratuit — et c'est un coût fixe, payé même si un
seul objet s'en sert.

D'où une règle pratique : si tu actives la texture de profondeur pour un effet, **sers-t'en pour
tous ceux qui peuvent en profiter** — écume, particules douces, contours (leçon 27), brouillard
volumétrique (27). Elle est déjà payée.

## À toi

1. **Fais des particules douces.** Prends un système de particules de fumée, regarde-le traverser
   le sol, ajoute la ligne de fondu. C'est le meilleur retour sur investissement de tout ce cours.
2. **Deux bandes d'écume.** Une fine et rapide au contact, une large et lente derrière. Les
   vraies plages ont plusieurs lignes de mousse, et deux bandes suffisent à le suggérer.
3. **Un halo d'intersection.** Applique le même calcul à une sphère translucide qui traverse le
   décor : là où elle coupe, une bande lumineuse. Tu viens d'écrire le bouclier de la leçon 18.
4. **De l'absorption physiquement plausible.** Remplace le `mix` de couleurs par une exponentielle :
   `exp(-epaisseur * densite_par_couleur)`, avec une densité plus forte pour le rouge que pour le
   bleu. C'est la loi de Beer-Lambert, et c'est pourquoi l'eau profonde est bleue. Trois
   coefficients, et la teinte devient juste au lieu d'être choisie.
5. **Casse-le.** Passe la texture de profondeur en filtrage linéaire et regarde apparaître les
   liserés parasites. Puis mets le matériau en opaque et regarde l'épaisseur tomber à zéro.

**Leçon suivante : 12 — Le verre et la chaleur.** On lit non plus la profondeur, mais la
**couleur** de ce qui a déjà été rendu.
