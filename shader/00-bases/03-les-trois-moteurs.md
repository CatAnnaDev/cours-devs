# 03 — Le même shader dans les trois moteurs

Un seul objectif ici : afficher une couleur unie et comprendre **la structure** du fichier dans
chaque moteur. C'est le squelette qu'on remplira pendant trente-deux leçons.

## Godot 4

Fichier `simple.gdshader` :

```glsl
shader_type spatial;
render_mode unshaded;

uniform vec3 couleur : source_color = vec3(0.9, 0.3, 0.4);

void fragment() {
    ALBEDO = couleur;
}
```

**Comment le brancher.** Dans l'inspecteur d'un `MeshInstance3D` : `Material Override` →
`New ShaderMaterial` → `Shader` → `New Shader` → choisir `spatial` → coller le code. Ou :
`Load` pour pointer un `.gdshader` du disque, ce qui est mieux car le fichier reste versionnable.

**Ce qu'il faut voir.**

`shader_type` doit être la **toute première ligne**. Les cinq valeurs possibles :

| Type | Pour quoi |
|---|---|
| `spatial` | tout ce qui est en 3D |
| `canvas_item` | 2D, interface, sprites |
| `particles` | mouvement des particules |
| `sky` | le ciel |
| `fog` | le brouillard volumétrique |

`render_mode` modifie le comportement du pipeline : `unshaded` (ignorer les lumières),
`cull_disabled` (afficher les faces arrière — indispensable pour du feuillage), `blend_add`,
`depth_draw_opaque`, `depth_test_disabled`. On en ajoutera au fil des leçons.

Tu n'écris **ni la fonction de sommets, ni la transformation en espace de découpe**. Godot
fournit un shader complet et tes fonctions viennent s'y greffer : tu ne remplis que les trous.
Tu écris `void vertex()` seulement si tu veux déplacer les sommets, et `void fragment()`
seulement pour changer l'aspect. Non écrites, elles gardent le comportement par défaut.

`ALBEDO` n'est pas la couleur finale : c'est la couleur **de base**, celle que les lumières vont
ensuite éclairer. Avec `unshaded`, elle part telle quelle à l'écran. Les autres sorties d'un
shader `spatial` : `EMISSION`, `ALPHA`, `NORMAL_MAP`, `ROUGHNESS`, `METALLIC`, `AO`, `RIM`.

## Unity URP

Fichier `Simple.shader` :

```hlsl
Shader "Cours/Simple"
{
    Properties
    {
        _Couleur ("Couleur", Color) = (0.9, 0.3, 0.4, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Couleur;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(_Couleur.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
```

Trente lignes contre six. Ce n'est pas de la lourdeur gratuite : Unity ne cache aucune plomberie,
tu vois tout, et tu peux tout changer.

**Comment le brancher.** `Create` → `Shader` → `Unlit Shader`, remplacer le contenu. Puis
`Create` → `Material`, et dans son inspecteur choisir `Cours/Simple` en haut. Le chemin du
`Shader "..."` est ce qui apparaît dans ce menu.

**Ce qu'il faut voir.**

`Properties` déclare ce que l'inspecteur affiche. Le `CBUFFER` déclare la même chose côté HLSL.
**Les deux doivent correspondre**, sinon la valeur réglée dans l'inspecteur n'arrive pas. Une
propriété `Color` correspond à un `float4`, `Range`/`Float` à un `float`, `Vector` à un `float4`.

`Attributes` = ce que je lis du maillage. `Varyings` = ce que le vertex passe au fragment, en
étant interpolé au passage. Les mots après le `:` sont des **sémantiques** — ils disent au GPU à
quoi sert le champ. `POSITION`, `NORMAL`, `TEXCOORD0` en entrée ; `SV_POSITION` (obligatoire, la
position finale) et `TEXCOORD0..n` (tes données à toi) en sortie.

`Tags { "LightMode" = "UniversalForward" }` désigne **quand** cette passe est utilisée. Un
matériau a souvent plusieurs passes : une pour le rendu principal, une pour les ombres
(`ShadowCaster`), une pour la pré-passe de profondeur (`DepthOnly`). **Si ton objet ne projette
plus d'ombre après que tu as écrit ton shader, c'est qu'il te manque la passe `ShadowCaster`.**
On l'ajoutera à la leçon 05, où ça devient visible.

`half4` plutôt que `float4` : de la demi-précision, deux fois moins chère sur mobile, sans
différence visible pour une couleur. On y revient à la leçon 32.

## Unreal 5

Unreal ne se pilote pas au fichier texte, mais au **graphe de nœuds**. C'est un vrai compilateur
HLSL derrière : les nœuds produisent du code, et tu peux le lire (`Window` → `Shader Code` →
`HLSL Code`).

**Le même effet :**

1. `Content Browser` → clic droit → `Material`. Nomme-le `M_Simple`.
2. Double-clic. Dans le panneau `Details` du nœud racine, mets `Shading Model` à `Unlit`.
   Le nœud racine perd alors toutes ses entrées sauf `Emissive Color` et `Opacity`.
3. Clic droit dans le graphe → `VectorParameter`. Nomme-le `Couleur`, donne-lui `(0.9, 0.3, 0.4)`.
4. Relie sa sortie principale à `Emissive Color`.
5. `Apply`, puis `Save`.

**Ce qu'il faut voir.**

En `Unlit`, la couleur passe par `Emissive Color` : c'est la sortie « je m'affiche tel quel ».
En `Default Lit`, tu remplis `Base Color`, `Metallic`, `Roughness`, `Normal` — les mêmes entrées
qu'un shader PBR classique.

Le `Material Domain` (dans `Details`) décide de la nature de l'objet : `Surface` pour un
matériau normal, `Post Process` pour un effet plein écran (leçon 25), `Deferred Decal` pour une
décalcomanie (16), `Volume` pour du volumétrique (27).

**Et le HLSL, alors ?** Le nœud `Custom` accepte du HLSL brut : tu lui ajoutes des entrées
nommées dans `Details`, tu écris le corps de la fonction, tu déclares le type de sortie.

```hlsl
return Couleur * Force;
```

Un nœud `Custom` casse certaines optimisations du compilateur de matériaux et n'est pas
prévisualisable dans le graphe. La bonne pratique : construire en nœuds, et réserver `Custom`
aux calculs qui deviennent illisibles en nœuds — une boucle, un raymarch, une formule longue.
C'est exactement ce que font les `unreal.md` des leçons : le graphe d'abord, le `Custom`
équivalent ensuite, pour que tu puisses lire la maths.

**Instances de matériau.** Ne duplique jamais un matériau pour changer une couleur : clic droit
sur `M_Simple` → `Create Material Instance`. L'instance expose les paramètres nommés, se change
en jeu, et ne recompile rien. C'est l'équivalent Unreal de ce qu'un `.material` est à un
`.gdshader`.

## Ce qui diffère vraiment

| | Godot | Unity URP | Unreal |
|---|---|---|---|
| forme | fichier texte | fichier texte | graphe (+ HLSL en `Custom`) |
| langage | GLSL modifié | HLSL | HLSL généré |
| plomberie | cachée, tu remplis les trous | visible, tu écris tout | cachée, très |
| paramètres | `uniform` avec indice | `Properties` **et** `CBUFFER` | nœuds `Parameter` |
| itération | instantanée | recompilation courte | recompilation parfois longue |
| lisibilité d'un effet complexe | très bonne | très bonne | mauvaise (le graphe explose) |
| découverte quand on débute | très bonne | moyenne | excellente |

Aucun n'est meilleur. Godot est le plus rapide pour apprendre, Unity le plus explicite, Unreal le
plus productif en équipe et le plus pénible à lire quand l'effet grossit.

**Le conseil pratique** : apprends l'effet dans le moteur que tu utilises, mais lis toujours les
trois versions de la leçon. Cinq minutes de plus, et tu vois ce qui relève de la vraie idée et ce
qui n'est que de la syntaxe.
