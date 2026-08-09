# 08 — Toon shading et contour

## Ce qu'on fabrique

Un personnage stylisé : l'éclairage tombe par **paliers** au lieu de dégrader en continu, l'ombre
est teintée de bleu au lieu d'être noire, une tache brillante nette marque le point de lumière, un
liseré clair souligne le côté éclairé, et un trait sombre entoure la silhouette.

C'est la dernière leçon du bloc 1, et la première où tu **écris de l'éclairage** au lieu de le
contourner.

## L'idée : quantifier une lumière

L'éclairage diffus le plus simple répond à une question : à quel point cette surface fait-elle
face à la lumière ?

```glsl
float eclairement = dot(normale, direction_de_la_lumiere);
```

C'est le même produit scalaire qu'au fresnel, avec la lumière au lieu de la caméra. Il vaut 1 en
plein éclairage, 0 sur les surfaces perpendiculaires, négatif à l'ombre.

Le rendu réaliste utilise cette valeur telle quelle, ce qui donne un dégradé continu. Le toon la
fait passer par un escalier :

```glsl
float marches(float valeur, float nombre, float largeur) {
    float echelle = valeur * nombre;
    float palier = floor(echelle);
    float reste = fract(echelle);
    return (palier + smoothstep(0.5 - largeur, 0.5 + largeur, reste)) / nombre;
}
```

Décomposée :

| Ligne | Effet |
|---|---|
| `valeur * nombre` | étale `0..1` sur `0..nombre` |
| `floor` | garde le numéro de la marche |
| `fract` | garde la position dans la marche |
| `smoothstep(0.5 ± largeur, reste)` | bascule d'une marche à la suivante au **milieu** |
| `/ nombre` | ramène en `0..1` |

Le `smoothstep` mérite un mot : avec `largeur = 0`, la transition est un mur, et le bord entre
deux paliers crénèle affreusement dès que la caméra bouge. Une largeur de `0.02` suffit à le
rendre propre sans qu'on perde l'aspect « à plat ».

**Un détail qui change tout** : `dot(normale, lumiere) * 0.5 + 0.5` au lieu de
`max(0, dot(...))`. La première forme étale l'information sur toute la sphère, y compris la partie
dans l'ombre ; la seconde écrase tout le côté sombre à zéro. En rendu réaliste, la seconde est
correcte. En toon, la première te donne **des paliers dans l'ombre aussi**, et c'est ce qui rend
un personnage lisible sur son côté non éclairé au lieu d'en faire une silhouette noire.

## Godot

Godot laisse écrire une fonction `light()`, appelée **une fois par lumière** qui touche l'objet.
C'est l'accès le plus direct des trois moteurs.

```glsl
void light() {
    vec3 normale = normalize(NORMAL);
    vec3 vers_lumiere = normalize(LIGHT);
    vec3 vers_camera = normalize(VIEW);

    float eclairement = dot(normale, vers_lumiere) * 0.5 + 0.5;
    float marche = marches(eclairement, niveaux, douceur);
    vec3 teinte = mix(couleur_ombre, vec3(1.0), marche);

    vec3 demi = normalize(vers_lumiere + vers_camera);
    float brillance = pow(clamp(dot(normale, demi), 0.0, 1.0), 32.0);
    float tache = step(seuil_speculaire, brillance) * force_speculaire;

    float face = clamp(dot(normale, vers_camera), 0.0, 1.0);
    float lisere = pow(1.0 - face, puissance_lisere) * force_lisere * marche;

    DIFFUSE_LIGHT += ALBEDO * teinte * ATTENUATION * LIGHT_COLOR / PI;
    SPECULAR_LIGHT += (vec3(tache) + couleur_lisere * lisere) * ATTENUATION * LIGHT_COLOR / PI;
}
```

**Les variables de `light()` :**

| Variable | Contenu |
|---|---|
| `LIGHT` | direction **vers** la lumière, en espace vue |
| `LIGHT_COLOR` | couleur × énergie × π |
| `ATTENUATION` | 1 en pleine lumière, 0 dans l'ombre portée — inclut la shadow map |
| `DIFFUSE_LIGHT` / `SPECULAR_LIGHT` | ce que tu accumules ; le moteur additionne toutes les lumières |

**Le `/ PI` n'est pas décoratif.** La documentation de Godot précise que `LIGHT_COLOR` contient
déjà un facteur π, hérité de la normalisation de la BRDF diffuse. Quand tu écris ton propre
éclairage, tu dois le retirer, sinon tout est environ trois fois trop clair et tu passeras la
soirée à baisser l'énergie de tes lumières.

**Le `+=` non plus.** Tu accumules : trois lumières appellent `light()` trois fois. Écrire `=`
ferait gagner la dernière lumière traitée, ce qui produit un scintillement quand la caméra bouge
et que l'ordre change.

**Le vecteur demi.** `normalize(vers_lumiere + vers_camera)` est le vecteur exactement entre la
lumière et l'œil. Quand la normale lui est parallèle, la surface renvoie la lumière pile vers la
caméra : c'est le point brillant. C'est le modèle de Blinn-Phong, plus ancien et bien plus rapide
que le calcul physiquement correct — qu'on écrira à la leçon 21. En toon, il est *meilleur* que le
modèle correct, parce qu'un `step` dessus donne une tache aux bords nets, exactement ce qu'on veut.

## Le contour, ou la coque inversée

Le contour est un **second rendu du même objet**, gonflé le long de ses normales, en ne dessinant
que les faces **arrière**.

```glsl
render_mode cull_front, unshaded, shadows_disabled;

void vertex() {
    VERTEX += normalize(NORMAL) * epaisseur * echelle;
}

void fragment() {
    ALBEDO = couleur_contour;
}
```

Le mécanisme, en trois phrases. On grossit l'objet de quelques millimètres. On ne dessine que ses
faces arrière — donc, vu de l'extérieur, on ne voit que la doublure qui dépasse tout autour. Le
vrai objet, dessiné par-dessus, cache tout le reste. Il ne subsiste qu'un liseré : le contour.

**Pour le brancher en Godot** : sur le `ShaderMaterial` de l'objet, propriété **`Next Pass`** →
`New ShaderMaterial` → charge `godot-contour.gdshader`. Deux matériaux, un seul objet.

**L'épaisseur constante à l'écran.**

```glsl
echelle = length((MODELVIEW_MATRIX * vec4(VERTEX, 1.0)).xyz);
```

Sans cette ligne, l'épaisseur est en mètres : le contour est énorme de près et invisible de loin.
En la multipliant par la distance à la caméra, l'extrusion grandit exactement comme la perspective
rétrécit, et le trait garde la même épaisseur en pixels. C'est le même raisonnement que le
`fwidth` du chapitre `00-bases/04` : **ce qu'on veut constant, c'est le nombre de pixels.**

**Ce que la coque inversée ne sait pas faire :**

| Limite | Pourquoi |
|---|---|
| pas de contours **intérieurs** | il n'y a pas de face arrière au milieu d'un torse |
| casse sur les arêtes vives | un cube a trois normales par coin : la coque s'ouvre aux angles |
| épaisseur inégale sur les zones plates | l'extrusion suit la normale, pas la silhouette |
| double le coût géométrique | l'objet est dessiné deux fois |

Les deux premières sont sérieuses. La parade classique pour les arêtes vives : faire faire au
modeleur une version du maillage aux normales **lissées**, rangée dans les couleurs de sommet ou
dans un second jeu d'UV, et extruder selon celles-là. C'est un travail d'artiste autant que de
programmeur.

Et pour les contours intérieurs, il n'y a pas de solution en coque : il faut détecter les
discontinuités de profondeur et de normale en post-traitement. **C'est la leçon 26.**

## Unity URP

Trois passes dans un seul fichier.

| Passe | `LightMode` | Rôle |
|---|---|---|
| `Contour` | `SRPDefaultUnlit` | la coque inversée |
| `Toon` | `UniversalForward` | l'éclairage |
| `ShadowCaster` | `ShadowCaster` | l'ombre portée |

URP dessine les passes `SRPDefaultUnlit` **en plus** de `UniversalForward` : c'est le mécanisme
standard pour ajouter un contour sans script ni Renderer Feature.

**L'éclairage :**

```hlsl
float4 coordOmbre = TransformWorldToShadowCoord(IN.positionWS);
Light lumiere = GetMainLight(coordOmbre);

float eclairement = dot(normalWS, lumiere.direction) * 0.5 + 0.5;
eclairement *= lumiere.shadowAttenuation;
float marche = Marches(eclairement, _Niveaux, _Douceur);
```

`GetMainLight(coordOmbre)` renvoie une structure `Light` contenant `direction`, `color`,
`distanceAttenuation` et `shadowAttenuation`. Sans argument, elle ne renvoie pas les ombres.

**Multiplier l'ombre portée avant la quantification, et pas après**, est un vrai choix
esthétique : ainsi l'ombre reçue tombe elle aussi par paliers, avec un bord franc, et se marie
avec l'ombre propre. Après quantification, elle resterait un dégradé continu — et le mélange des
deux styles se voit immédiatement.

Les deux `#pragma multi_compile` sont indispensables : sans eux, `TransformWorldToShadowCoord`
compile mais ne renvoie jamais d'ombre, et tu chercheras longtemps.

**Les lumières additionnelles** ne sont pas gérées ici. Les ajouter demande
`#pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS` et une boucle sur `GetAdditionalLight` —
c'est la leçon 22.

## Unreal

Voir `unreal.md`. Résumé : Unreal ne laisse pas le matériau intervenir dans l'éclairage, donc on
passe en `Unlit` et on fournit soi-même la direction de la lumière (idéalement par un
`Material Parameter Collection`). Et pour le contour, la voie réellement utilisée en production
est le **post-traitement**, pas la coque inversée — leçon 26.

## Le banc

`banc.gdshader` affiche cinq sphères avec 1, 2, 3, 4 et 5 paliers, éclairées par une direction
réglable.

Ce qu'il faut y voir :

**Un palier** donne deux zones : lumière et ombre. C'est le style le plus graphique, celui des
jeux très stylisés. Il ne pardonne rien : la forme doit être lisible par sa silhouette seule.

**Deux ou trois paliers** est la zone utile pour un personnage. Le troisième palier sert
généralement à la zone la plus sombre, et c'est lui qui donne du volume.

**Cinq paliers et plus**, on est en train de réinventer un dégradé, en plus laid. Si tu as besoin
de cinq paliers, c'est probablement un rendu continu qu'il te faut.

Fais aussi ceci : mets `douceur` à `0`, regarde le bord entre deux paliers, puis remonte à `0.02`.
Le crénelage disparaît sans que l'aspect change. **Trois centièmes suffisent** — c'est le genre de
réglage qu'on oublie et qui fait toute la différence entre un rendu propre et un rendu amateur.

## En 2D — la posterisation

Quantifier un **éclairage** n'a pas de sens sans lumières. L'équivalent 2D du toon quantifie
directement **la couleur** :

```glsl
vec3 quantifiee = floor(sprite.rgb * niveaux + 0.5) / niveaux;
```

C'est la posterisation, et c'est ce qui donne le rendu « peu de couleurs, aplats francs » d'un jeu
stylisé. Le `+ 0.5` arrondit au lieu de tronquer, sinon l'image s'assombrit franchement.

Le shader fournit aussi le second mode, plus puissant : **l'échange de palette**. On calcule la
luminance du pixel et on s'en sert comme indice dans une rampe de couleurs peinte par un artiste :

```glsl
float indice = luminance(sprite.rgb);
vec3 finale = texture(palette, vec2(indice, 0.5)).rgb;
```

Une texture de 32 × 1 pixels suffit. Changer la rampe change entièrement l'ambiance sans toucher
aux sprites — c'est comme ça qu'on fait un mode nuit, un effet de poison, une équipe rouge et une
bleue, ou un flash de dégât.

**Le contour**, lui, n'est pas ici : en 2D c'est celui de la leçon 06, calculé sur l'alpha.

Pour du pixel art : mets la palette en filtrage `Nearest`, et méfie-toi de la posterisation, qui
peut créer des teintes absentes de ta palette d'origine. L'échange de palette, lui, garantit que
seules tes couleurs sortent.

## Les pièges

**Le contour n'apparaît pas.** L'objet a des normales dures (facettes) : la coque s'ouvre aux
arêtes. Ou l'épaisseur est trop faible. Ou tu as oublié `cull_front` / `Cull Front` — sans lui,
la coque recouvre entièrement l'objet, qui devient uniformément noir.

**Le contour est énorme de près et invisible de loin.** L'échelle par distance n'est pas activée.

**Le contour clignote au bord de l'écran.** L'extrusion sort de la boîte englobante. Même remède
qu'à la leçon 07.

**Les paliers ondulent le long des arêtes.** `douceur` à zéro. Remonte-la.

**Tout est trop clair en Godot.** Le `/ PI` manque.

**L'objet devient noir dès qu'une seconde lumière l'éclaire (Godot).** Tu as écrit `=` au lieu de
`+=` dans `light()`.

**Les paliers bougent quand l'objet se déplace.** Normal et correct : l'éclairage dépend de
l'orientation par rapport à la lumière. Si tu veux des paliers fixes sur le modèle, ce n'est plus
de l'éclairage, c'est une texture de rampe peinte par un artiste — et c'est ce que font beaucoup
de jeux anime, avec une texture 1D indexée par l'éclairement.

**Le rendu est plat et sans intérêt.** C'est le vrai piège du toon, et il n'est pas technique. Un
rendu toon sans liseré, sans variation d'ombre colorée et sans contour a l'air d'un modèle non
fini. Ce sont les trois couches secondaires — la teinte d'ombre, le liseré, le contour — qui font
le style, pas la quantification elle-même.

## Ce que ça coûte

L'éclairage toon coûte **moins** qu'un éclairage PBR : un `dot`, un `floor`, un `pow`, contre une
BRDF complète. C'est un des rares cas où le style choisi est aussi le moins cher.

Le contour, lui, se paie : **l'objet est dessiné deux fois**. Pour un personnage de 30 000
sommets, ce sont 30 000 sommets de plus, et la coque couvre à peu près la même surface d'écran.
Compte grossièrement le double.

Sur une foule, ça devient le poste dominant. Les parades habituelles : n'appliquer le contour
qu'aux personnages proches (un niveau de détail sur le matériau), ou basculer sur un contour en
post-traitement, dont le coût est **fixe** — une passe plein écran, quel que soit le nombre
d'objets. C'est un bon exemple d'un arbitrage qu'on retrouve partout : *par objet* contre *par
écran*.

## À toi

1. **Une rampe peinte au lieu d'un escalier calculé.** Remplace `marches()` par un accès à une
   texture 1D : `texture(rampe, vec2(eclairement, 0.5)).rgb`. L'artiste dessine alors la courbe
   d'éclairage à la main, y compris des transitions colorées. C'est ce qu'utilise la majorité des
   jeux au rendu anime, et c'est un accès texture contre trois instructions.
2. **Une ombre colorée qui n'est pas une teinte.** Au lieu de `mix(couleur_ombre, blanc, marche)`,
   prends deux textures différentes — une de jour, une d'ombre — et mélange-les. Les artistes
   peuvent alors dessiner ce qui se passe à l'ombre, ce qui est bien plus expressif qu'un
   assombrissement.
3. **Combine avec la leçon 06.** Le liseré est déjà un fresnel ; multiplie-le par `marche` pour
   qu'il n'apparaisse **que** du côté éclairé. C'est fait dans le shader fourni — enlève ce
   facteur et regarde : le liseré des deux côtés a l'air faux, et on ne sait pas dire pourquoi
   avant de l'avoir vu.
4. **Mesure le contour.** Cent personnages avec contour, cent sans. Note l'écart. Puis essaie de
   n'activer le contour que sous dix mètres. C'est le genre de décision qu'on prend une fois et
   qui tient tout le projet.
5. **Fais le cube.** Applique le shader à un cube et regarde le contour s'ouvrir aux coins. Puis
   lisse les normales du cube et regarde le contour se refermer — au prix d'un éclairage devenu
   faux. Tu tiens là, en trente secondes, le compromis exact que la coque inversée impose, et tu
   sauras pourquoi la leçon 26 existe.

---

**Fin du bloc 1.** Tu sais maintenant : structurer un shader dans les trois moteurs, poser une
texture, animer, construire un masque, découper, lire la géométrie, et écrire de l'éclairage.
Les huit effets ci-dessus couvrent une part étonnante de ce qu'on demande à un programmeur de
shaders au quotidien.

Le bloc 2 attaque les surfaces : le vent dans le feuillage, l'eau, la profondeur, le verre,
le triplanar.
