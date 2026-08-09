# Leçon 14 en Unreal 5 — parallax et POM

## Le nœud tout fait

Unreal fournit **`BumpOffset`** : c'est le parallax simple, en un nœud.

| Entrée | Rôle |
|---|---|
| `Coordinate` | les UV de départ |
| `Height` | la carte de hauteur, canal `R` |
| `HeightRatio` | la profondeur, typiquement `0.02` à `0.08` |
| `ReferencePlane` | à quelle hauteur se trouve le « niveau zéro », `0.5` par défaut |

Branche sa sortie sur les `UVs` de **toutes** tes textures — couleur, normale, rugosité. C'est le
point le plus souvent raté : décaler la couleur mais pas la normale donne un relief qui glisse par
rapport à son éclairage.

`BumpOffset` est très bon marché — un seul échantillon supplémentaire — et suffit pour du
carrelage, du pavé, des rainures peu profondes.

## Le POM tout fait

Unreal fournit aussi la version complète : cherche la fonction de matériau
**`ParallaxOcclusionMapping`**.

| Entrée | Rôle |
|---|---|
| `Heightmap Texture` | un `TextureObjectParameter`, pas un `TextureSample` |
| `Height Ratio` | la profondeur |
| `Min Steps` / `Max Steps` | les couches, `8` et `32` sont de bonnes valeurs |
| `Temporal Dither` | anti-bandes, à activer si tu vois des marches |

Sorties : `Parallax UVs` (à brancher sur toutes tes textures), `Pixel Depth Offset` et
`Shadow Ray Steps`.

**`Pixel Depth Offset` est la sortie qui fait la différence avec Godot et Unity.** Elle indique au
moteur que le pixel est en réalité **plus loin** que la surface géométrique. Conséquences :

- l'objet s'intersecte correctement avec les autres : un objet posé dans un creux y entre
  vraiment ;
- les ombres portées suivent le relief ;
- le brouillard et les effets d'écran voient la bonne profondeur.

Branche-la sur l'entrée **Pixel Depth Offset** du nœud racine. C'est le seul des trois moteurs qui
expose ça proprement, et ça règle une bonne partie des limites décrites dans le `README.md`.

## L'équivalent en nœud Custom

Entrées : `UV` (Float2), `VueTangente` (Float3), `Profondeur`, `Couches`, plus la texture et son
sampler passés par `Texture2D` et `SamplerState` (à déclarer dans le champ `Additional Defines` ou
en branchant un `TextureObject`).

```hlsl
float pasProfondeur = 1.0 / Couches;
float2 pasUV = VueTangente.xy * Profondeur / Couches;
float2 uv = UV;
float profondeurCourante = 0.0;
float hauteur = 1.0 - Texture2DSample(Tex, TexSampler, uv).r;

for (int i = 0; i < 128; i++)
{
    if (profondeurCourante >= hauteur || (float)i >= Couches) break;
    uv -= pasUV;
    hauteur = 1.0 - Texture2DSample(Tex, TexSampler, uv).r;
    profondeurCourante += pasProfondeur;
}

float2 uvPrecedent = uv + pasUV;
float apres = hauteur - profondeurCourante;
float avant = (1.0 - Texture2DSample(Tex, TexSampler, uvPrecedent).r) - profondeurCourante + pasProfondeur;
return lerp(uv, uvPrecedent, saturate(apres / max(apres - avant, 0.0001)));
```

Pour obtenir la direction de vue en espace tangent, le nœud est **`CameraVectorTS`** — ou
`TransformVector` du `CameraVector` de `World Space` vers `Tangent Space`.

## Le réglage qui casse tout

Dans `Details` du matériau, **`Tangent Space Normal`** doit rester **coché** pour cette leçon : la
normal map lue aux UV décalées est bien en espace tangent.

Et le maillage doit avoir des **tangentes**. Un plan créé par un Blueprint ou une géométrie
procédurale peut ne pas en avoir : la parallaxe part alors dans une direction arbitraire, et
l'effet a l'air de tourner avec la caméra au lieu de creuser.
