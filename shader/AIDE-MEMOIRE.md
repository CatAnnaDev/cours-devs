# Aide-mémoire — GLSL, HLSL, Godot, Unity, Unreal

À garder ouvert pendant les leçons. Rien à apprendre par cœur : on revient ici quand ça coince.

## 1. Le même langage, deux orthographes

GLSL (Godot) et HLSL (Unity, Unreal) sont **le même langage à 90 %**. Les 10 % qui changent :

| Idée | GLSL / Godot | HLSL / Unity / Unreal |
|---|---|---|
| vecteur 3 flottants | `vec3` | `float3` |
| vecteur 3 entiers | `ivec3` | `int3` |
| demi-précision | `mediump float` | `half` |
| matrice 4×4 | `mat4` | `float4x4` |
| interpolation linéaire | `mix(a, b, t)` | `lerp(a, b, t)` |
| partie fractionnaire | `fract(x)` | `frac(x)` |
| modulo | `mod(x, y)` | `fmod(x, y)` — **résultat différent sur les négatifs** |
| inverse de racine | `inversesqrt(x)` | `rsqrt(x)` |
| arc-tangente à deux arguments | `atan(y, x)` | `atan2(y, x)` |
| dérivée d'écran | `dFdx` / `dFdy` | `ddx` / `ddy` |
| échantillonner | `texture(tex, uv)` | `tex.Sample(sampler, uv)` |
| id. en URP | — | `SAMPLE_TEXTURE2D(_Tex, sampler_Tex, uv)` |
| multiplier matrice × vecteur | `M * v` | `mul(M, v)` |
| jeter le pixel | `discard;` | `discard;` ou `clip(x)` si `x < 0` |
| saturer entre 0 et 1 | `clamp(x, 0.0, 1.0)` | `saturate(x)` |
| constructeur implicite | `vec3(1.0)` | `float3(1, 1, 1)` ou `(float3)1` |

Le piège de `mod` / `fmod` : `mod(-1.0, 3.0)` vaut **2** en GLSL et **-1** en HLSL. Dès que tu
fais du carrelage ou de la répétition avec des coordonnées qui peuvent devenir négatives, ça
se voit immédiatement.

## 2. Les conventions qui font apparaître l'effet à l'envers

| Sujet | Godot | Unity | Unreal |
|---|---|---|---|
| Origine des UV | en haut à gauche, V vers le bas | en bas à gauche, V vers le haut | en haut à gauche, V vers le bas |
| Sens de l'axe Z | -Z devant | +Z devant | +X devant, Z vers le haut |
| Unité | 1 = 1 mètre | 1 = 1 mètre | 1 = 1 **centimètre** |
| Espace de couleur | linéaire, sRGB décodé à l'échantillonnage | idem | idem |

Conséquence pratique : un effet copié de Unity vers Godot apparaît souvent **retourné
verticalement**. Correction : `uv.y = 1.0 - uv.y`.

Et en Unreal, une distance de 100 n'est pas 100 mètres mais 1 mètre. Toutes les constantes de
rayon, de vitesse et d'épaisseur sont à multiplier par 100.

## 3. Les variables du moteur

| Ce que tu veux | Godot 4 | Unity URP | Unreal |
|---|---|---|---|
| le temps | `TIME` | `_Time.y` | nœud `Time` |
| les UV | `UV` | `IN.uv` (que tu passes toi-même) | nœud `TexCoord` |
| position en espace objet | `VERTEX` (dans `vertex()`) | `IN.positionOS` | nœud `ObjectPosition` / `LocalPosition` |
| position en espace monde | `(MODEL_MATRIX * vec4(VERTEX,1)).xyz` | `TransformObjectToWorld(...)` | nœud `WorldPosition` |
| la normale | `NORMAL` (**espace vue** en fragment) | `normalWS` (que tu calcules) | nœud `VertexNormalWS` |
| direction vers la caméra | `VIEW` (espace vue) | `GetWorldSpaceNormalizeViewDir(posWS)` | nœud `CameraVector` |
| position de la caméra | `CAMERA_POSITION_WORLD` | `_WorldSpaceCameraPos` | nœud `CameraPositionWS` |
| UV de l'écran | `SCREEN_UV` | `GetNormalizedScreenSpaceUV(IN.positionCS)` | nœud `ScreenPosition` |
| la couleur déjà rendue | `hint_screen_texture` | `SampleSceneColor(uv)` | `SceneTexture:PostProcessInput0` |
| la profondeur de la scène | `hint_depth_texture` | `SampleSceneDepth(uv)` | nœud `SceneDepth` |
| la couleur de sommet | `COLOR` | `IN.color` | nœud `VertexColor` |

**Attention à `NORMAL` en Godot** : dans `fragment()`, elle est en **espace vue**, pas en espace
monde. `VIEW` aussi. Les deux étant dans le même espace, `dot(NORMAL, VIEW)` marche — mais si tu
veux comparer la normale à une direction du monde (le vent, le haut), il faut la convertir :
`(INV_VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz`.

## 4. Déclarer un paramètre réglable

**Godot** — dans le shader, l'inspecteur le trouve tout seul :

```glsl
uniform vec3 base_color : source_color = vec3(1.0);
uniform float force : hint_range(0.0, 1.0) = 0.5;
uniform sampler2D bruit : source_color, filter_linear_mipmap, repeat_enable;
```

`source_color` signifie « cette valeur est une couleur, décode-la en linéaire ». **Oublier
`source_color` sur une couleur est l'erreur numéro un en Godot** : la couleur choisie dans
l'inspecteur ne correspond pas à celle affichée.

**Unity URP** — deux endroits, et ils doivent correspondre :

```hlsl
Properties
{
    _BaseColor ("Couleur", Color) = (1,1,1,1)
    _Force ("Force", Range(0,1)) = 0.5
    _Bruit ("Bruit", 2D) = "white" {}
}
```

```hlsl
TEXTURE2D(_Bruit);
SAMPLER(sampler_Bruit);

CBUFFER_START(UnityPerMaterial)
    float4 _Bruit_ST;
    float4 _BaseColor;
    float _Force;
CBUFFER_END
```

Tout ce qui est réglable par matériau **doit** être dans le `CBUFFER_START(UnityPerMaterial)`,
sinon l'instanciation GPU casse (et Unity le signale par un avertissement qu'on ignore trop
souvent). Les textures et samplers restent **dehors**.

**Unreal** — clic droit dans le graphe, `ScalarParameter` / `VectorParameter` / `TextureParameter`,
et tu nommes le paramètre. C'est ce nom qui apparaît dans les Material Instances.

## 5. Les fonctions à connaître par cœur

| Fonction | Ce qu'elle fait | Là où tu t'en sers |
|---|---|---|
| `mix(a, b, t)` | `a` quand `t=0`, `b` quand `t=1` | tout mélange |
| `step(seuil, x)` | 0 ou 1, franchement | découpe nette |
| `smoothstep(a, b, x)` | 0 → 1 avec des bords adoucis | 90 % des masques |
| `clamp` / `saturate` | borne | sécuriser avant un `pow` |
| `dot(a, b)` | 1 si même sens, 0 si perpendiculaire, -1 si opposé | éclairage, fresnel |
| `normalize(v)` | longueur ramenée à 1 | **avant chaque `dot`** |
| `length(v)` | longueur | distances, masques ronds |
| `pow(x, n)` | resserre une rampe vers 0 | contraste d'un fresnel |
| `fract(x)` | garde la partie après la virgule | répétition, défilement |
| `abs` / `sign` | valeur absolue, signe | symétries |
| `reflect(i, n)` | rebond d'un rayon | réflexions, spéculaire |

`smoothstep` est l'outil le plus rentable du métier. `smoothstep(0.4, 0.6, x)` vaut 0 sous 0.4,
1 au-dessus de 0.6, et fait une transition douce entre les deux. Presque tous les effets de ce
cours sont un `smoothstep` bien placé.

## 6. Erreurs de compilation, traduites

| Message | Ce que ça veut dire |
|---|---|
| `no matching function for call to 'mix(float, vec3, float)'` | tes types ne concordent pas — GLSL ne convertit pas tout seul |
| `implicit truncation of vector type` (HLSL) | tu ranges un `float3` dans un `float` : mets `.rgb` ou `.x` explicitement |
| `Shader is not supported on this GPU` (Unity) | souvent trop de textures échantillonnées, ou un `#pragma target` trop bas |
| `Expected ';'` en Godot ligne 1 | `shader_type` manquant ou mal orthographié — il doit être la **première** ligne |
| l'objet est rose (Unity) | le shader n'a pas compilé du tout : ouvre l'inspecteur du shader pour lire l'erreur |
| l'objet est blanc-violet (Godot) | même chose : le panneau du bas affiche l'erreur |

## 7. Le prix des choses

Ordre de grandeur, sur une carte de bureau, pour un pixel :

| Opération | Coût relatif |
|---|---|
| addition, multiplication, `mix` | 1 |
| `dot`, `normalize` | 1 à 3 |
| `pow`, `exp`, `log`, `sin`, `cos` | 4 à 8 |
| division, `sqrt` | 4 à 8 |
| **échantillonner une texture** | 20 à 100+ (et bien plus si le cache rate) |
| `if` dont les deux branches divergent dans le groupe | le coût des **deux** branches |
| `discard` | modeste en soi, mais **désactive le test de profondeur anticipé** |

Retiens deux choses. Un accès texture coûte bien plus qu'un calcul : remplacer une texture de
dégradé par une formule est presque toujours gagnant. Et un `if` sur le GPU n'économise rien
si, dans un même groupe de 32 pixels, certains prennent une branche et d'autres l'autre.
