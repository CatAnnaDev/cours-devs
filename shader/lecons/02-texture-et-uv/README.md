# 02 — Texture, UV et carrelage : un sol qui se répète

## Ce qu'on fabrique

Un matériau de sol ou de mur : une texture posée sur la surface, **répétée** autant de fois qu'on
veut, **décalable**, et **teintable** sans toucher au fichier image. C'est le matériau le plus
banal d'un jeu, et il contient déjà trois pièges qui coûtent des heures quand on ne les connaît
pas.

## L'idée

Une texture n'a pas de position dans le monde. Elle a **des coordonnées à elle**, appelées UV,
qui vont de 0 à 1 en largeur et en hauteur. Le maillage porte, pour chaque sommet, un couple UV
qui dit : « à cet endroit de ma surface, va chercher ce point de l'image ».

Trois conséquences, et elles expliquent tout le reste :

**1. Les UV sont dans le maillage, pas dans le shader.** C'est le modeleur qui les a posées (le
« dépliage »). Ton shader les reçoit, il ne les invente pas — sauf si tu décides de t'en passer,
ce qui est exactement l'objet de la leçon 13.

**2. Un UV en dehors de 0..1 n'est pas une erreur.** Le mode de répétition de la texture décide
de ce qui se passe : `repeat` recommence l'image (`1.3` donne le même point que `0.3`), `clamp`
étire indéfiniment le pixel du bord. **C'est le mécanisme du carrelage** : multiplier l'UV par 4
donne des valeurs jusqu'à 4, donc quatre répétitions.

**3. Multiplier avant d'additionner.** `uv * carrelage + decalage` déplace la texture d'une
distance constante. `(uv + decalage) * carrelage` déplace d'une distance multipliée par le
carrelage — c'est-à-dire d'un montant qui change dès qu'on touche au carrelage. L'ordre n'est
pas une préférence, c'est une correction.

## Godot

```glsl
shader_type spatial;

uniform sampler2D texture_base : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform vec3 teinte : source_color = vec3(1.0);
uniform vec2 carrelage = vec2(4.0, 4.0);
uniform vec2 decalage = vec2(0.0);
uniform float rugosite : hint_range(0.0, 1.0) = 0.85;

void fragment() {
    vec2 uv = UV * carrelage + decalage;
    vec4 echantillon = texture(texture_base, uv);

    ALBEDO = echantillon.rgb * teinte;
    ROUGHNESS = rugosite;
    SPECULAR = 0.3;
}
```

**Les indices du `sampler2D` ne sont pas décoratifs.** Ce sont eux qui décident du
comportement, et ils sont **ignorés silencieusement** si tu les oublies :

| Indice | Ce qu'il fait | Si tu l'oublies |
|---|---|---|
| `source_color` | décode le sRGB vers le linéaire | les couleurs sont délavées |
| `repeat_enable` | autorise le carrelage | la texture est étirée au lieu de se répéter |
| `filter_linear_mipmap` | active les mipmaps | ça scintille horriblement de loin |
| `filter_linear_mipmap_anisotropic` | mipmaps + filtrage en biais | les sols vus de loin restent nets |
| `filter_nearest` | pas de lissage | à utiliser pour du pixel art, et seulement là |

Note que ce shader **est éclairé** : un `spatial` sans `render_mode unshaded` passe par le
pipeline PBR de Godot. `ALBEDO` est la couleur de base, les lumières font le reste. C'est
pourquoi la version Godot de cette leçon est plus courte que la version Unity : Godot fournit
l'éclairage.

## Unity URP

```hlsl
Properties
{
    _BaseMap ("Texture", 2D) = "white" {}
    _Teinte ("Teinte", Color) = (1, 1, 1, 1)
}
```

```hlsl
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _Teinte;
CBUFFER_END

Varyings vert(Attributes IN)
{
    Varyings OUT;
    OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
    return OUT;
}

half4 frag(Varyings IN) : SV_Target
{
    half4 echantillon = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
    return half4(echantillon.rgb * _Teinte.rgb, 1.0);
}
```

**Là où Unity fait mieux que Godot.** Tu n'as pas déclaré de `carrelage` ni de `decalage` :
déclarer une propriété `2D` fait apparaître automatiquement les champs **Tiling** et **Offset**
dans l'inspecteur, et Unity les range dans une variable nommée `_BaseMap_ST` (`S` pour scale,
`T` pour translate). `TRANSFORM_TEX` fait exactement le calcul de la section précédente :

```hlsl
uv * _BaseMap_ST.xy + _BaseMap_ST.zw
```

D'où deux règles à retenir : le `float4 _NomDeLaTexture_ST` doit être dans le `CBUFFER` (sans
lui, `TRANSFORM_TEX` ne compile pas), et **le `TRANSFORM_TEX` se met dans le vertex, pas dans le
fragment**. Deux multiplications par sommet plutôt que par pixel, gratuitement.

**Le trio `TEXTURE2D` / `SAMPLER` / `SAMPLE_TEXTURE2D`** sépare l'image (les données) du sampler
(comment on la lit : filtrage, répétition). Cette séparation existe dans le matériel, et elle
permet un truc utile : lire deux textures avec un seul sampler, ce qui économise une ressource
rare sur mobile.

```hlsl
half4 a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
half4 b = SAMPLE_TEXTURE2D(_Detail, sampler_BaseMap, IN.uv);
```

**Le filtrage et la répétition** ne se règlent pas dans le shader ici, mais sur la texture :
sélectionne-la dans le projet, et règle `Wrap Mode` (`Repeat` / `Clamp`) et `Filter Mode`
(`Bilinear` / `Point`) dans son inspecteur.

Ce shader est **unlit** : il écrit sa couleur telle quelle. Pour un vrai sol tu voudras
l'éclairage — c'est la leçon 23, qui fournit le squelette URP éclairé complet. Toutes les leçons
d'ici là s'y reportent en trois lignes.

## Unreal

Voir `unreal.md` — nœuds `TextureCoordinate`, `TextureSampleParameter2D`, l'ordre
multiplication/addition, et surtout les réglages d'import de la texture, qui sont la source de
bugs numéro un.

## Le banc

`banc.gdshader` sur un `ColorRect` affiche `UV * carrelage` décomposé :

- chaque **cellule** a une couleur tirée de son indice — c'est `floor(uv)` ;
- à l'intérieur de chaque cellule, un dégradé de gauche à droite — c'est `fract(uv)` ;
- une grille blanche marque les frontières.

Change `carrelage` dans l'inspecteur et regarde. Tu tiens les deux fonctions qui servent partout
dès qu'on répète quelque chose :

| Fonction | Question à laquelle elle répond |
|---|---|
| `floor(uv)` | **dans quelle** case suis-je ? |
| `fract(uv)` | **où dans** la case suis-je ? |

C'est le mécanisme des atlas (leçon 15), des flipbooks, des motifs procéduraux et de la
variation par cellule. Le connaître ici évite de le redécouvrir trois fois.

## En 2D

C'est la leçon où le 2D pose un vrai problème : **un sprite dans un atlas ne peut pas être
carrelé.**

Multiplier l'UV par 4 sort de la région du sprite et va lire ses voisins dans la planche. Il n'y a
pas de contournement dans le shader.

D'où le choix des versions 2D : elles carrèlent une **seconde texture**, `texture_carrelee` en
Godot et `_Carrelee` en Unity, qui doit être **hors atlas** et en mode répétition. Le sprite garde
son rôle de forme, la texture carrelée fournit la matière.

C'est exactement ce qu'on fait en vrai pour un sol, un mur ou une barre de progression texturée.
Voir `00-bases/06-le-2d.md` pour les trois parades à l'atlas.

## Les pièges

**Le sol scintille quand la caméra bouge.** Les mipmaps sont désactivées, ou la texture est en
filtrage `Nearest`. Un sol carrelé vu en biais est le pire cas possible : active le filtrage
anisotrope.

**La texture est étirée au lieu de se répéter.** Mode de répétition en `Clamp` : dans le shader
Godot (`repeat_enable`), sur l'import en Unity (`Wrap Mode: Repeat`), sur la texture en Unreal
(`Tiling Method: Wrap`).

**Les couleurs sont fades.** `source_color` manquant en Godot, ou `sRGB` décoché à l'import en
Unity/Unreal. **Et la règle inverse, tout aussi importante** : une texture qui n'est *pas* une
couleur — un masque, une carte de hauteur, une carte de rugosité, du bruit — doit avoir `sRGB`
**décoché**. Sinon ses valeurs sont tordues par la correction gamma, et un seuil à 0.5 ne tombe
plus au milieu. C'est le bug qui rend une dissolution asymétrique à la leçon 05.

**La texture bouge quand je change le carrelage.** Tu as additionné le décalage avant de
multiplier.

**Les bords de la texture se répètent visiblement.** Ce n'est pas un bug de shader : la texture
n'est pas conçue pour se répéter sans couture. Les remèdes commencent à la leçon 13.

## Ce que ça coûte

**Un accès texture coûte entre vingt et cent fois une multiplication**, et bien plus quand le
cache rate. C'est presque toujours le poste le plus lourd d'un shader de surface.

Trois conséquences pratiques :

1. **Ne lis jamais deux fois la même texture aux mêmes UV.** Range le résultat dans une variable.
   Le compilateur le fait souvent tout seul, mais pas quand les UV passent par une fonction.
2. **Une texture de dégradé est presque toujours remplaçable par une formule.** Une rampe de
   couleur en `mix`, une courbe en `pow` : c'est du calcul, donc quasi gratuit, et ça n'occupe
   pas de mémoire.
3. **Le carrelage est gratuit, la résolution ne l'est pas.** Répéter une texture de 512 pixels
   quatre fois coûte moins cher, et est plus net, qu'une texture de 2048 posée une fois.

## À toi

1. **Deux carrelages différents.** Sépare la couleur en deux échantillons de la même texture,
   l'un carrelé 1 fois, l'autre 8 fois, et mélange-les à 50 %. C'est la base du *detail mapping* :
   le premier donne la forme générale, le second empêche le flou quand on s'approche.
2. **Un décalage animé.** Remplace `decalage` par `vec2(TIME * 0.1, 0.0)`. Tu viens d'écrire la
   leçon 03 avant l'heure — et tu comprendras pourquoi elle tient en trois lignes.
3. **Casse le gamma exprès.** Importe une texture de masque en `sRGB` coché, affiche
   `step(0.5, echantillon.r)` et observe où tombe la frontière. Décoche `sRGB`, regarde-la
   bouger. Ce décalage, c'est la correction gamma que tu viens de voir de tes yeux.
4. **Ajoute une seconde texture.** Un `sampler2D` supplémentaire, multiplié par le premier.
   Regarde le compteur d'images par seconde de ton moteur avec la sphère en plein écran : tu
   mesures le prix d'un accès texture sur *ta* carte.

**Leçon suivante : 03 — Le temps.** Trois lignes, et tout se met à bouger.
