# 10 — L'eau : deux normal maps et l'espace tangent

## Ce qu'on fabrique

Une surface d'eau : un plan, deux normal maps qui glissent dans des directions différentes, et
des reflets qui bougent. Rien d'autre — pas de géométrie ondulée, pas de particules. C'est de
l'eau au sens où 90 % des jeux en font.

Et c'est la leçon où l'on comprend enfin **ce qu'est une normal map**, ce qui débloque le
triplanar, le parallax, les décalcomanies et la moitié du bloc 2.

## L'idée : une normale, ça se ment

Une surface a une normale, définie par sa géométrie. Un plan a la même normale partout, donc
l'éclairage y est uniforme, donc le plan a l'air plat. Logique.

Une **normal map** ment au shader d'éclairage : à chaque pixel, elle dit « en fait, ici, la
surface est inclinée comme ça ». La géométrie ne change pas d'un millimètre, mais l'éclairage se
comporte comme si elle était bosselée. Comme l'œil déduit la forme de l'éclairage, il voit des
vagues.

**Pourquoi bleu-lavande ?** Une normal map contient un vecteur, pas une couleur. Le vecteur va de
-1 à 1, la texture de 0 à 1, donc :

```glsl
vec3 normale = texture(normales, uv).xyz * 2.0 - 1.0;
```

Une surface non inclinée a pour normale `(0, 0, 1)`, ce qui se range en `(0.5, 0.5, 1.0)` :
du bleu-lavande. **La couleur dominante d'une normal map est donc le témoin de sa convention**, et
c'est ce qui permet de repérer une texture importée avec le mauvais réglage.

## L'espace tangent, enfin

Cette normale `(0, 0, 1)` n'est **pas** en espace monde. Sinon la texture ne fonctionnerait que
sur une surface tournée vers le haut, et il faudrait une texture différente par orientation.

Elle est exprimée **relativement à la surface** :

| Composante | Sens |
|---|---|
| `x` | vers la droite de la surface — la **tangente** |
| `y` | vers le haut de la surface — la **bitangente** |
| `z` | vers l'extérieur de la surface — la **normale** |

C'est l'**espace tangent** : un repère différent en chaque point, porté par la surface. Une même
normal map de briques marche donc sur un mur, un sol, un cylindre et un personnage.

La tangente vient du maillage : elle est calculée à partir du **dépliage UV**, parce que « la
droite de la surface » signifie en réalité « la direction où l'UV `u` augmente ». D'où deux
conséquences pratiques :

1. **Pas d'UV, pas de tangente, pas de normal map.** C'est pour ça que le triplanar de la
   leçon 13 devra construire ses propres repères.
2. Un dépliage miroir (l'artiste retourne la moitié gauche d'un personnage) inverse la tangente
   d'un côté, et les bosses de la normal map y apparaissent **creuses**. C'est le fameux bit de
   *handedness*, rangé dans `tangent.w`.

Pour passer d'une normale tangente à une normale monde, on la multiplie par la matrice formée par
ces trois vecteurs — la **matrice TBN**.

## Godot

Godot fait la conversion pour toi. C'est le confort le plus notable de ses shaders `spatial` :

```glsl
uniform sampler2D normales : hint_normal, filter_linear_mipmap, repeat_enable;

void fragment() {
    vec3 a = texture(normales, UV * carrelage_a + vitesse_a * TIME).xyz * 2.0 - 1.0;
    vec3 b = texture(normales, UV * carrelage_b + vitesse_b * TIME).xyz * 2.0 - 1.0;

    vec3 melange = normalize(vec3(a.xy + b.xy, a.z * b.z));

    NORMAL_MAP = melange * 0.5 + 0.5;
    NORMAL_MAP_DEPTH = force_normales;
    ...
}
```

**`NORMAL_MAP` attend une valeur encodée entre 0 et 1**, exactement comme la texture. D'où le
`* 0.5 + 0.5` pour rentrer, après le `* 2 - 1` pour sortir. C'est redondant ici — on pourrait ne
pas déballer du tout — mais on le fait parce qu'il faut travailler entre -1 et 1 pour mélanger
correctement.

Si tu écris `NORMAL` directement au lieu de `NORMAL_MAP`, Godot ne fait **aucune** conversion :
il attend alors une normale en espace vue. Les deux sorties existent, elles n'attendent pas la
même chose, et les confondre donne une surface éclairée par une lumière imaginaire.

**`NORMAL_MAP_DEPTH`** est le curseur d'intensité, sans dénormaliser quoi que ce soit.

**`hint_normal`** dit à Godot que cette texture est une normal map : il l'importe en conséquence
et n'applique pas de correction sRGB.

## Le mélange de deux normales

```glsl
vec3 melange = normalize(vec3(a.xy + b.xy, a.z * b.z));
```

C'est le mélange dit *whiteout*, et il mérite une explication parce que la version naïve est
tentante et fausse.

**Naïf :** `normalize(a + b)`. Deux normales presque plates donnent `(0,0,1) + (0,0,1) = (0,0,2)`,
soit `(0,0,1)` après normalisation : correct. Mais dès qu'une des deux est inclinée, la moyenne
**aplatit** le résultat. Empile deux normal maps de cette façon et le relief disparaît au lieu de
s'additionner.

**Whiteout :** on additionne les inclinaisons (`xy`), et on multiplie les composantes
« verticales » (`z`). Deux surfaces plates restent plates ; deux inclinaisons s'ajoutent
franchement. C'est ce qui donne l'aspect « deux trains de vagues qui se croisent » plutôt que
« une bouillie moyenne ».

**Et les deux couches doivent différer en trois points** : carrelage, vitesse, et direction. Si
les deux se ressemblent, on voit qu'il n'y en a qu'une. La recette qui marche : un rapport de
carrelage non entier (3 et 7), des vitesses de sens contraire, et une des deux nettement plus
lente.

## Unity URP

Unity ne cache rien : tu construis la matrice TBN et tu convertis toi-même.

```hlsl
VertexNormalInputs normales = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
OUT.normalWS = normales.normalWS;
OUT.tangentWS = normales.tangentWS;
OUT.bitangentWS = normales.bitangentWS;
```

`GetVertexNormalInputs` calcule les trois vecteurs, y compris le signe de la bitangente à partir
de `tangentOS.w`. Écrire le produit vectoriel à la main sans ce signe est la cause du dépliage
miroir creusé.

```hlsl
float3x3 versMonde = float3x3(
    normalize(IN.tangentWS),
    normalize(IN.bitangentWS),
    normalize(IN.normalWS));

donnees.normalWS = normalize(mul(normalTS, versMonde));
```

**L'ordre de `mul` compte.** `mul(vecteur, matrice)` traite la matrice comme des lignes,
`mul(matrice, vecteur)` comme des colonnes. Ici la matrice est construite ligne par ligne, donc
c'est `mul(normalTS, versMonde)`. Inverser les deux donne une normale plausible mais fausse, et
c'est le genre de bug qu'on ne voit qu'en tournant la caméra.

**`UnpackNormal`** fait le `* 2 - 1` **et** gère les formats compressés : sur beaucoup de
plateformes, une normal map est stockée en deux canaux seulement (le troisième est reconstruit),
et un `* 2 - 1` à la main donnerait alors un résultat faux. Utilise toujours `UnpackNormal`.

**`[Normal]` devant la propriété** fait afficher par Unity un avertissement si tu assignes une
texture qui n'a pas été importée comme normal map. Accepte la correction proposée.

**Le squelette éclairé promis à la leçon 02**, le voici :

```hlsl
InputData donnees = (InputData)0;
donnees.positionWS = IN.positionWS;
donnees.normalWS = ...;
donnees.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
donnees.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
donnees.bakedGI = SampleSH(donnees.normalWS);
donnees.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
donnees.shadowMask = half4(1, 1, 1, 1);

SurfaceData surface = (SurfaceData)0;
surface.albedo = _CouleurEau.rgb;
surface.metallic = 0.0;
surface.smoothness = 1.0 - _Rugosite;
surface.occlusion = 1.0;
surface.alpha = 1.0;

half4 couleur = UniversalFragmentPBR(donnees, surface);
```

`UniversalFragmentPBR` est **la** fonction d'éclairage d'URP : lumière principale, lumières
additionnelles, ombres, sondes de réflexion, éclairage global. Tu remplis deux structures, elle
fait le reste.

Les `(InputData)0` et `(SurfaceData)0` mettent tout à zéro d'abord : ces structures gagnent des
champs à chaque version d'URP, et un champ non initialisé donne des résultats aléatoires — ou un
plantage du compilateur.

**Les `#pragma multi_compile` sont obligatoires.** Chacun correspond à une fonctionnalité :
sans `_MAIN_LIGHT_SHADOWS`, pas d'ombres ; sans `_ADDITIONAL_LIGHTS`, seule la lumière
principale compte. Ils multiplient le nombre de variantes compilées — c'est pour ça qu'un shader
URP complet met plus longtemps à compiler qu'un unlit, et pourquoi on ne les met pas partout.

## Unreal

Voir `unreal.md` : le `Sampler Type: Normal` qui déballe la texture, la fonction
`BlendAngleCorrectedNormals`, la case `Tangent Space Normal`, et le plugin Water — qu'il faut
utiliser en production, mais après avoir monté celui-ci une fois.

## Le banc

`banc.gdshader` ne triche pas avec une texture : il calcule **quatre vagues sinusoïdales
directionnelles**, en additionne les hauteurs, et en déduit la normale analytiquement.

```glsl
vec3 vague(vec2 p, vec2 direction, float longueur, float amplitude, float vitesse) {
    float k = 6.28318530718 / longueur;
    float phase = dot(direction, p) * k + TIME * vitesse * k;
    return vec3(amplitude * sin(phase), direction * k * amplitude * cos(phase));
}
```

La fonction renvoie la hauteur **et sa pente** — la dérivée de `sin` étant `cos`, on l'obtient
gratuitement. La normale s'en déduit :

```glsl
vec3 normale = normalize(vec3(-pente.x, 1.0, -pente.y));
```

C'est la formule de la normale d'une surface donnée par une hauteur, et elle resservira partout :
**pour connaître l'orientation d'une surface, il faut sa pente.**

Ce que le banc rend visible :

**Quatre vagues suffisent.** Enlève-en une, l'eau devient répétitive ; ajoute-en huit, ça ne
change presque plus rien. Les longueurs d'onde choisies (3.1, 1.7, 0.9, 0.5) sont dans des
rapports non entiers : c'est ce qui empêche le motif de se répéter visiblement.

**Le fresnel fait plus de la moitié du travail.** Mets sa contribution à zéro dans le `mix` : il
reste une surface bleue bosselée qui ne ressemble pas du tout à de l'eau. L'eau est bleue de
près et **couleur de ciel** de loin, et c'est cette variation qui la rend crédible.

**Le spéculaire est un `pow` à 128.** Descends-le à 8 : l'eau devient du plastique mouillé. Une
surface lisse a une tache spéculaire minuscule et intense ; c'est la définition même du lisse.

## Les pièges

**L'eau est plate malgré la normal map.** Trois causes, dans l'ordre de fréquence : la texture
n'a pas été importée comme normal map (Unity : `Texture Type: Normal map` ; Unreal :
`Sampler Type: Normal` ; Godot : `hint_normal`), le maillage n'a pas de tangentes, ou tu as écrit
dans `NORMAL` au lieu de `NORMAL_MAP`.

**L'eau a l'air éclairée par une lumière qui n'existe pas.** La normale est dans le mauvais
espace. Vérifie l'ordre du `mul`, ou la case `Tangent Space Normal` en Unreal.

**Les bosses sont creuses.** Le canal vert est inversé — c'est la guerre DirectX contre OpenGL.
Correction : `normalTS.y = -normalTS.y`. Symptôme typique : l'eau a l'air « à l'envers » sans
qu'on sache dire pourquoi.

**On voit clairement une seule texture qui glisse.** Les deux couches sont trop semblables.
Change le rapport de carrelage, mets des directions opposées.

**Le plan d'eau scintille au loin.** Une normal map ne peut pas être mipmapée correctement : la
moyenne de deux normales opposées est une normale plate, donc l'eau lointaine devient miroir et
scintille. Remèdes : réduire la force des normales avec la distance, ou augmenter la rugosité au
loin — ce qui est en plus physiquement juste.

**L'eau est opaque.** C'est le sujet de la leçon suivante.

## Ce que ça coûte

**Deux accès texture**, et c'est le poste dominant. Le mélange whiteout coûte trois
multiplications.

Mais le vrai coût est ailleurs : `UniversalFragmentPBR` avec toutes ses variantes est un shader
**lourd**, et une surface d'eau couvre souvent la moitié de l'écran. Sur mobile, l'eau de cette
leçon est déjà trop chère et l'on descend à une seule couche de normales, une rugosité constante,
et un ciel en cubemap au lieu des sondes de réflexion.

Le compte à retenir : **deux textures + PBR complet + plein écran** est un budget sérieux. La
leçon 32 mesurera tout ça.

## À toi

1. **Une troisième couche, très lente et très large.** Carrelage `0.7`, vitesse `0.004`. Elle ne
   se voit pas, et pourtant l'eau devient nettement plus vivante : elle casse la régularité des
   deux autres.
2. **Fais varier la rugosité.** Rends-la dépendante de la pente de la normale : les crêtes plus
   lisses, les creux plus mats. Deux lignes, et l'eau gagne en profondeur.
3. **Le vent, encore.** Fais dépendre les vitesses de défilement d'un uniforme `force_vent`
   partagé avec la leçon 09. Une seule valeur qui pilote le feuillage et l'eau : c'est comme ça
   qu'on fait une scène cohérente.
4. **Casse le mélange exprès.** Remplace le whiteout par `normalize(a + b)` et regarde le relief
   s'aplatir. Puis par `normalize(a * b)`, qui n'a aucun sens, et regarde ce que ça donne. Voir
   les mauvaises versions rend la bonne mémorable.
5. **Trouve la limite du plan.** Regarde ton eau presque à l'horizontale, très rasante. Elle
   devient plate et scintillante : les normal maps ne peuvent rien contre le fait qu'il n'y a
   aucune géométrie. C'est là qu'il faut de vraies ondes de Gerstner dans le shader de sommets —
   la même maths que le banc, appliquée à `VERTEX`. Essaie : tu as tout ce qu'il faut.

**Leçon suivante : 11 — La profondeur.** L'écume au bord de l'eau, les particules qui ne coupent
plus le décor, et la première fois qu'on lit ce que la caméra a déjà rendu.
