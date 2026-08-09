# 05 — La dissolution à bord incandescent

## Ce qu'on fabrique

L'objet se troue, les trous s'agrandissent, et le bord de chaque trou brûle en orange avant de
disparaître. Un ennemi qui se désintègre, une porte qui se matérialise, un téléport, une
apparition d'objet ramassable, une transition de niveau.

C'est **l'effet de shader le plus demandé de tous**, et le premier que tu montreras à quelqu'un.
Il tient en six lignes et ne contient aucune idée que tu n'aies pas déjà vue à la leçon 04 : un
masque, un seuil, un mélange.

## L'idée

Un seul curseur, `progression`, entre 0 et 1. Un champ de bruit fixe sur l'objet. On compare
l'un à l'autre :

```glsl
float grain = texture(bruit, UV * echelle_bruit).r;
float seuil = mix(-largeur_bord, 1.0 + largeur_bord, progression);
float visible = grain - seuil;

if (visible < 0.0) discard;
```

Chaque pixel possède sa valeur de bruit, comprise entre 0 et 1. On lève un seuil ; tous les
pixels dont le bruit passe sous le seuil sont **jetés**. Comme le bruit est irrégulier, la
frontière l'est aussi : des trous apparaissent, s'élargissent, se rejoignent.

**Pourquoi `mix(-largeur, 1 + largeur, progression)` et pas simplement `progression` ?** Parce
qu'il faut que le seuil parte *sous* la plus petite valeur possible du bruit et finisse *au-dessus*
de la plus grande. Sinon, à progression 0 une partie du bord brille déjà, et à progression 1 il
reste des pixels. Les deux marges valent la largeur du bord, exactement ce qu'il faut.

**Le bord.** `visible` vaut 0 pile à la frontière, et grandit quand on s'éloigne vers la zone
encore présente. Donc :

```glsl
float bord = 1.0 - smoothstep(0.0, largeur_bord, visible);
```

vaut 1 sur la frontière, retombe à 0 à `largeur_bord` de distance, et reste à 0 ensuite. C'est un
liseré, calculé sans jamais savoir où sont les pixels voisins — exactement la contrainte posée au
chapitre `00-bases/01`. On ne cherche pas le bord : **on le déduit de la distance au seuil.**

Retiens cette manière de penser, elle revient partout : *un contour, c'est une bande de valeurs
autour de la valeur critique.*

## Godot

```glsl
void fragment() {
    float grain = texture(bruit, UV * echelle_bruit).r;

    float seuil = mix(-largeur_bord, 1.0 + largeur_bord, progression);
    float visible = grain - seuil;

    if (visible < 0.0) {
        discard;
    }

    float bord = 1.0 - smoothstep(0.0, largeur_bord, visible);

    ALBEDO = texture(texture_base, UV).rgb;
    ROUGHNESS = rugosite;
    EMISSION = couleur_bord * bord * intensite_bord;
}
```

**`discard` jette le pixel** : il n'est pas écrit, il n'écrit pas de profondeur, on voit à travers.

Godot applique ce `discard` dans **toutes** les passes, y compris celle des ombres : le trou se
voit aussi dans l'ombre portée, sans que tu écrives une ligne de plus. C'est un vrai confort, et
c'est ce qui rend la version Godot deux fois plus courte que la version Unity.

**`render_mode cull_disabled`** est presque obligatoire ici : dès que l'objet est troué, on voit
son intérieur. Sans les faces arrière, l'intérieur est transparent et l'illusion tombe.

**Animer la progression** depuis un script :

```gdscript
var materiau := $MeshInstance3D.material_override as ShaderMaterial

func dissoudre(duree: float) -> void:
    var tween := create_tween()
    tween.tween_method(
        func(v: float): materiau.set_shader_parameter("progression", v),
        0.0, 1.0, duree
    )
```

Attention : `material_override` est **partagé** entre toutes les instances qui l'utilisent. Pour
dissoudre un ennemi sans dissoudre ses frères, duplique la ressource :
`material_override = material_override.duplicate()`.

## Unity URP

Le calcul est identique. La différence est structurelle : **le shader a trois passes.**

```hlsl
HLSLINCLUDE
float PartieVisible(float2 uv)
{
    float grain = SAMPLE_TEXTURE2D(_Bruit, sampler_Bruit, uv * _EchelleBruit).r;
    float seuil = lerp(-_LargeurBord, 1.0 + _LargeurBord, _Progression);
    return grain - seuil;
}
ENDHLSL
```

Le bloc `HLSLINCLUDE` au niveau du `SubShader` est partagé par toutes les passes. C'est
l'endroit où mettre les uniformes et les fonctions communes — sans lui, il faudrait recopier la
fonction trois fois, et une modification sur deux introduirait un bug.

| Passe | `LightMode` | Rôle | Sans elle |
|---|---|---|---|
| `Unlit` | `UniversalForward` | l'affichage | rien ne s'affiche |
| `ShadowCaster` | `ShadowCaster` | la shadow map | **l'ombre reste pleine** pendant que l'objet se troue |
| `DepthOnly` | `DepthOnly` | la pré-passe de profondeur | les effets d'écran (SSAO, brouillard, contours) traitent l'objet comme plein |

C'est la dette annoncée à la leçon 01, et c'est ici qu'elle devient visible. Un ennemi à moitié
dissous qui projette une ombre intacte se remarque tout de suite.

Les trois passes appellent **la même** `PartieVisible` et le même `clip()`. Si les seuils
divergent d'un cheveu, l'ombre ne coïncide plus avec l'objet.

**`clip(x)` jette le pixel quand `x < 0`** — c'est le `discard` de HLSL, en plus court.

**Le vertex de la passe d'ombre** contient deux lignes inhabituelles :

```hlsl
float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#endif
```

`ApplyShadowBias` décale légèrement le sommet le long de sa normale pour éviter l'**acné
d'ombre** — ces rayures sombres qui apparaissent quand une surface s'ombre elle-même. Le bloc
`UNITY_REVERSED_Z` empêche le sommet, une fois décalé, de passer derrière le plan proche de la
lumière. C'est de la plomberie ; recopie-la, on l'expliquera vraiment à la leçon 23.

**Les tags de rendu** comptent aussi :

```
Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
```

`AlphaTest` place l'objet **après** les opaques dans l'ordre de rendu. C'est voulu : les opaques
remplissent d'abord le tampon de profondeur, ce qui évite de calculer un shader coûteux sur des
pixels qui seront recouverts.

**Animer la progression** :

```csharp
private static readonly int Progression = Shader.PropertyToID("_Progression");
private MaterialPropertyBlock _bloc;

void Start() => _bloc = new MaterialPropertyBlock();

void Dissoudre(float valeur)
{
    var rendu = GetComponent<Renderer>();
    rendu.GetPropertyBlock(_bloc);
    _bloc.SetFloat(Progression, valeur);
    rendu.SetPropertyBlock(_bloc);
}
```

Le `MaterialPropertyBlock` change la valeur **pour ce rendu seulement**, sans dupliquer le
matériau et sans casser le regroupement des draw calls. `renderer.material.SetFloat(...)`
fonctionne aussi, mais crée une copie du matériau à la première utilisation — copie qu'il faut
détruire à la main, et qu'on oublie toujours.

`Shader.PropertyToID` convertit le nom en entier une fois pour toutes. Passer une chaîne à chaque
frame fait une recherche de hachage à chaque frame, pour rien.

## Unreal

Voir `unreal.md` : `Blend Mode` sur `Masked`, l'entrée `Opacity Mask`, `Opacity Mask Clip Value`,
et le fait qu'Unreal — comme Godot — applique le masque à toutes les passes tout seul.

## Le banc

`banc.gdshader` fait tourner la dissolution en boucle, à plat, sans texture à assigner : le bruit
est calculé dans le shader. Le `ColorRect` se troue et laisse voir ce qu'il y a derrière.

Trois réglages à manipuler, dans cet ordre :

**`echelle`** — la taille des trous. Petite valeur : deux ou trois grands trous, l'objet se
déchire. Grande valeur : une poussière de petits trous, l'objet s'évapore. **C'est le réglage qui
décide de la personnalité de l'effet**, et on le sous-estime toujours au début.

**`largeur_bord`** — l'épaisseur de la zone qui brûle. Attention : elle est en unités de bruit,
pas en pixels ni en mètres. À échelle de bruit élevée, la même largeur donne un liseré plus fin à
l'écran.

**`vitesse`** — remarque que la dissolution ne progresse pas de façon régulière : elle traîne au
début, s'emballe au milieu, traîne à la fin. C'est parce que le bruit n'est pas réparti
uniformément — il y a peu de valeurs très basses et très hautes, beaucoup autour du milieu. Le
banc corrige déjà en partie avec `smoothstep(0.25, 0.75, fbm(...))`. Enlève ce `smoothstep` et
regarde la différence : c'est le même phénomène que le curseur de couverture de la leçon 04.

## En 2D

Une seule différence, et elle est importante : **pas de `discard`.**

```glsl
float presence = step(0.0, visible);
COLOR = vec4(couleur, sprite.a * presence) * COLOR;
```

En 2D tout est déjà transparent : mettre l'alpha à zéro suffit, et c'est moins cher qu'un `discard`
— qui, lui, désactive des optimisations (leçon 05, section « Ce que ça coûte »).

Il n'y a pas non plus de passe d'ombre ni de passe de profondeur à tenir à jour : le shader 2D est
donc trois fois plus court que le 3D, pour le même résultat visuel.

Attention à l'atlas : `UV * echelle_bruit` sur le bruit ne pose pas de problème (c'est une texture
séparée, hors atlas), mais si tu échantillonnes le sprite lui-même à des UV modifiées, tu
déborderas.

## Les pièges

**L'ombre reste pleine (Unity).** Il manque la passe `ShadowCaster`, ou elle n'applique pas le
même `clip`.

**On voit à travers l'objet, pas seulement par les trous.** Les faces arrière ne sont pas
dessinées : `cull_disabled` en Godot, `Cull Off` en Unity, `Two Sided` en Unreal.

**Le bord ne brille pas.** L'émission a besoin de HDR et de bloom pour produire un halo — leçon 01.
Sans eux, tu obtiens un liseré orange plat, pas une braise.

**La dissolution part de partout à la fois de façon plate et ennuyeuse.** Elle est purement
guidée par le bruit. Ajoute une direction : voir l'exercice 1.

**Le motif de dissolution est étiré ou tordu sur le modèle.** Le bruit est échantillonné en UV, et
les UV d'un personnage sont découpées en morceaux. Deux remèdes : échantillonner le bruit en
espace objet ou en espace monde plutôt qu'en UV (leçon 13), ou accepter les coutures si l'effet
est rapide — à un dixième de seconde, personne ne les voit.

**Le seuil ne couvre pas tout le modèle.** Ton bruit ne descend jamais sous 0.2 ni au-dessus de
0.8, par exemple à cause d'un `fbm` mal normalisé. Étale-le : `smoothstep(min, max, grain)`.

**En Unreal, rien ne se passe.** `Blend Mode` est resté sur `Opaque` : `Opacity Mask` est ignorée.

## Ce que ça coûte

Deux accès texture et un `smoothstep` : négligeable. **Le vrai coût est le `discard` lui-même.**

Un GPU moderne teste la profondeur *avant* d'exécuter le shader de fragments — c'est l'*early-Z*,
et c'est ce qui évite de calculer des pixels cachés. Mais si le shader peut jeter le pixel, le GPU
ne sait plus à l'avance ce qui sera écrit dans le tampon de profondeur : **il doit désactiver
l'early-Z pour cet objet**.

Conséquences concrètes :

- un objet dissous coûte plus cher qu'un objet opaque, même quand `progression` vaut 0 ;
- il coûte cher **même** quand il est entièrement caché derrière un mur ;
- sur mobile, où les GPU à tuiles s'appuient énormément sur ce mécanisme, le surcoût est bien
  plus lourd que sur bureau.

La bonne pratique, en production : **deux matériaux**. Un opaque normal pendant la vie de
l'objet, et on ne bascule sur le matériau de dissolution qu'au moment où l'effet démarre. Ça
paraît petit ; sur cent ennemis à l'écran, ça ne l'est pas.

## À toi

1. **Une dissolution directionnelle.** Combine le bruit avec un dégradé, pour que l'objet se
   dissolve du bas vers le haut :
   ```glsl
   float hauteur = VERTEX.y * 0.5 + 0.5;
   float grain = mix(texture(bruit, UV * echelle_bruit).r, hauteur, 0.5);
   ```
   Cette seule ligne transforme un effet correct en un effet qui a l'air voulu. Essaie aussi avec
   la distance à un point (une onde qui part de l'impact).
2. **Une rampe de couleur au lieu d'une couleur unie.** Le bord brûle en blanc au plus près de la
   découpe, puis orange, puis rouge sombre :
   ```glsl
   vec3 rampe = mix(couleur_bord, vec3(1.0), pow(bord, 3.0));
   ```
   Compare avec la version à couleur unie. La différence est celle entre « ça marche » et « c'est
   joli ».
3. **Reconstruis l'effet inverse.** Fais apparaître l'objet au lieu de le faire disparaître.
   Une ligne à changer. Puis fais-le boucler : apparition, pause, disparition.
4. **Mesure le prix du `discard`.** Mets cent objets dissous à l'écran, note les images par
   seconde. Remplace le shader par un opaque simple, note à nouveau. Sur un GPU de bureau tu
   verras peu de différence ; teste sur mobile ou sur une carte intégrée et l'écart devient net.
   C'est la première mesure de ce cours, et c'est une habitude à prendre.
5. **Le bord en tant que masque réutilisable.** Sers-toi de `bord` pour autre chose que
   l'émission : décaler la normale, changer la rugosité, teinter l'albédo. Un masque bien calculé
   sert plusieurs fois — c'est ce qui fait la différence entre un shader empilé et un shader
   construit.

**Leçon suivante : 06 — Fresnel.** L'effet qui rend un objet lisible sur n'importe quel fond, et
la brique de l'hologramme, du bouclier et du sous-sol lumineux.
