# 12 — Le verre et la distorsion de chaleur

## Ce qu'on fabrique

Un objet qui **déforme ce qu'on voit à travers lui** : une vitre irrégulière, une bulle, un
cristal, et surtout l'air brûlant au-dessus d'un moteur ou d'un feu — cet effet presque invisible
qui fait qu'une scène « respire ».

La leçon 11 lisait la **profondeur** de ce qui avait déjà été rendu. Celle-ci lit sa **couleur**.

## L'idée : décaler l'endroit où on regarde

Le moteur peut donner à un objet transparent une copie de l'image déjà rendue — tous les opaques,
dans l'état où ils sont juste avant que notre objet soit dessiné. Le pixel connaît sa propre
position à l'écran. Il peut donc aller lire la couleur **d'à côté** :

```glsl
vec3 derriere = texture(screen_texture, SCREEN_UV + decalage).rgb;
```

Et c'est tout. Un décalage nul redonne exactement ce qu'il y avait derrière : l'objet est
invisible. Un décalage non nul, et l'image se tord.

**Le décalage vient de la normale.** Une surface inclinée dévie la lumière : c'est la réfraction.
En prendre les composantes `x` et `y` — celles qui décrivent l'inclinaison dans le plan de la
surface — donne une approximation très correcte, et gratuite puisqu'on lit déjà la normal map :

```glsl
vec3 perturbation = texture(normales, uv).xyz * 2.0 - 1.0;
vec2 decalage = perturbation.xy * force_refraction;
```

Ce n'est pas la vraie loi de Snell-Descartes. Ça n'a pas d'importance : personne ne peut voir
l'écart, et le coût est de deux multiplications au lieu d'un calcul d'angle.

## Godot

```glsl
uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

void fragment() {
    vec3 perturbation = texture(normales, UV * carrelage + vitesse * TIME).xyz * 2.0 - 1.0;
    vec2 decalage = perturbation.xy * force_refraction;

    vec2 uv_ecran = clamp(SCREEN_UV + decalage, vec2(0.001), vec2(0.999));
    vec3 derriere = textureLod(screen_texture, uv_ecran, flou).rgb;

    float face = clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
    float fresnel = pow(1.0 - face, puissance_fresnel);

    ALBEDO = derriere * teinte + couleur_bord * fresnel * force_bord;
    ALPHA = 1.0;
}
```

**`hint_screen_texture`** demande à Godot de fournir la copie de l'écran. Il la crée
automatiquement dès qu'un shader la réclame : tu n'as rien à activer.

**`filter_linear_mipmap` + `textureLod` = du verre dépoli gratuit.** Godot génère les mipmaps de
la copie d'écran. Demander explicitement un niveau de mip élevé revient à lire une version
réduite, donc floutée, de l'image :

| `flou` | Résultat |
|---|---|
| 0 | verre transparent net |
| 1 à 2 | verre légèrement dépoli |
| 3 à 5 | verre translucide de salle de bain |

C'est un des rares flous **gratuits** du rendu temps réel : la chaîne de mipmaps est déjà
calculée. Godot est le moteur où c'est le plus simple des trois.

**Le `clamp` des UV d'écran** évite de lire en dehors de l'image, ce qui donne selon la
plateforme des bords étirés, noirs ou répétés. Un décalage important près du bord de l'écran
produit sinon une bande parasite très visible.

**`ALPHA = 1.0` et `blend_mix`** : l'objet est techniquement opaque en sortie — on a déjà
composé nous-même avec le fond. C'est plus simple et plus prévisible que de mélanger deux fois.
Il reste dans la file des transparents pour avoir accès à la copie d'écran.

## Unity URP

```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

float2 uvEcran = GetNormalizedScreenSpaceUV(IN.positionCS);
uvEcran = clamp(uvEcran + perturbation.xy * _ForceRefraction, 0.001, 0.999);

half3 derriere = SAMPLE_TEXTURE2D_X_LOD(
    _CameraOpaqueTexture, sampler_CameraOpaqueTexture, uvEcran, _Flou).rgb;
```

**Il faut cocher `Opaque Texture` dans l'asset URP.** Sans ça, `_CameraOpaqueTexture` est noire et
ton verre est un trou noir. C'est le symptôme numéro un.

**`Opaque Downsampling`**, juste en dessous, décide de la résolution de cette copie :

| Réglage | Effet |
|---|---|
| `None` | pleine résolution, le plus cher |
| `2x Bilinear` | moitié, presque invisible pour de la réfraction |
| `4x Box` / `4x Bilinear` | quart, déjà flou — et c'est parfois **exactement** ce qu'on veut |

Pour de la distorsion de chaleur, `4x` est gratuit en qualité perçue et divise le coût par
seize. C'est un des réglages les plus rentables d'URP.

**`SAMPLE_TEXTURE2D_X_LOD`** — le `_X` gère la VR (deux yeux dans une même texture), le `_LOD`
permet de choisir le niveau de mip. La version simple `SampleSceneColor(uv)` existe aussi et est
plus lisible, mais elle ne permet pas de choisir le mip.

**`Blend One Zero`** parce que, comme en Godot, on écrit directement le résultat composé.

## Unreal

Voir `unreal.md`. Unreal a une entrée **`Refraction`** dédiée sur les matériaux translucides, avec
un vrai indice de réfraction — c'est la bonne voie pour du verre. Pour de la chaleur, on passe par
`SceneColor` comme ici. Et le flou par mipmaps n'existe pas : c'est le seul moteur où le verre
dépoli demande du travail.

## Le banc

`banc.gdshader` fabrique un faux décor — damier, rayures, ligne d'horizon — et fait passer devant
une lentille de chaleur.

Ce que le banc rend visible :

**La distorsion est un décalage d'UV, rien d'autre.** Mets `force` à zéro : l'objet disparaît
complètement. C'est le test qui prouve qu'on ne dessine rien, on ne fait que déplacer un point de
lecture.

**Deux composantes de décalage font la lentille, le bruit fait la chaleur.** `courbure` contrôle
la partie « lentille » (un décalage radial qui grandit vers le bord), la turbulence `fbm` la
partie « air chaud ». Sépare-les en mettant l'une à zéro : la lentille seule ressemble à une
bille de verre, la turbulence seule à de la chaleur. Ensemble, à une bulle de savon.

**Le bord se trahit.** À force élevée, on voit exactement où finit l'objet, parce que la
discontinuité de décalage crée une arête. C'est le défaut fondamental de la méthode, et le remède
est toujours le même : **faire tendre la force vers zéro sur les bords**, ce que fait le `masque`
du banc.

## Les pièges

**Le verre est noir.** La copie d'écran n'est pas activée : `Opaque Texture` en Unity,
`Blend Mode: Translucent` en Unreal. En Godot, ça marche toujours.

**Le verre se voit lui-même, en boucle.** Non — et c'est important : la copie est prise **avant**
que l'objet soit dessiné, donc il ne peut pas s'auto-lire. En revanche, deux objets réfractants
superposés ne se voient pas l'un l'autre : le second lit la même copie, prise avant les deux.
C'est une limite structurelle, la même qu'à la leçon 11.

**Les objets transparents disparaissent derrière le verre.** Même raison : la copie ne contient
que les opaques. Une flamme, une particule, une autre vitre : absentes. C'est le défaut le plus
visible en pratique, et il n'a pas de remède simple.

**Une bande étirée apparaît sur les bords de l'écran.** Le `clamp` manque, ou le décalage est
trop fort.

**Le décor visible à travers ne correspond pas à la géométrie.** Normal : on décale une image
plate, on ne calcule pas de vrais rayons. Un verre épais devrait montrer les objets déplacés
**selon leur distance** ; ici tout est déplacé pareil. Pour aller plus loin il faut du
raymarching (leçon 28), et ce n'est presque jamais rentable.

**L'effet est trop visible.** C'est le piège esthétique de cette leçon. Une bonne distorsion de
chaleur se remarque **quand on l'enlève**, pas quand elle est là. Règle-la, puis divise par deux.

## Ce que ça coûte

Un accès texture plein écran, comme à la leçon 11. Mais le coût réel est en amont, et il est plus
lourd :

**Le moteur doit copier la cible de rendu.** C'est une passe de copie de tout l'écran, déclenchée
dès qu'un seul matériau réclame la couleur de scène. En 1080p, c'est huit mégaoctets déplacés
par image, à quoi s'ajoute la génération des mipmaps si tu utilises le flou.

Les conséquences pratiques :

- **Le premier objet réfractant coûte cher, les suivants sont presque gratuits.** Une seule copie
  sert à tous. Si tu as un verre, autant en avoir dix.
- **Sur mobile, c'est souvent rédhibitoire.** Les GPU à tuiles doivent vider leur tuile pour faire
  la copie, ce qui casse tout leur avantage. Beaucoup de jeux mobiles remplacent la réfraction par
  une texture de fond pré-rendue.
- **Baisser la résolution de la copie est la meilleure optimisation.** `Opaque Downsampling` en
  `4x` sur une distorsion de chaleur : invisible à l'œil, seize fois moins cher.

## À toi

1. **Fais un effet de chaleur crédible.** Un plan invisible au-dessus d'un feu, teinte à
   `(1,1,1)`, fresnel à zéro, force à `0.01`, deux couches de bruit qui montent à des vitesses
   différentes. Puis coupe-le et remets-le : c'est en le coupant qu'on voit ce qu'il apportait.
2. **Une aberration chromatique.** Échantillonne trois fois la copie d'écran, avec des décalages
   légèrement différents pour le rouge, le vert et le bleu, et recompose. C'est ce que fait un
   vrai prisme, et ça rend n'importe quel verre plus riche. Trois accès texture au lieu d'un :
   mesure la différence.
3. **Un verre dépoli progressif.** Fais dépendre le niveau de mip de la distance ou d'un masque
   peint. Une vitre nette au centre et dépolie sur les bords.
4. **Une onde de choc.** Remplace la normal map par un anneau qui grandit :
   `decalage = normalize(direction_depuis_le_centre) * anneau(distance, rayon_courant)`. Un plan
   invisible, un uniforme animé de 0 à 1, et tu as l'explosion de n'importe quel jeu d'action.
5. **Mesure la copie d'écran.** Note tes images par seconde, ajoute un objet réfractant, note
   à nouveau. Puis ajoute-en dix. La courbe n'est pas linéaire, et comprendre pourquoi est plus
   utile que le chiffre.

**Leçon suivante : 13 — Le triplanar.** Texturer un objet qui n'a pas d'UV — falaises, terrains,
géométrie générée.
