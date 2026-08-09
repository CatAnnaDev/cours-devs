# 01 — Couleur et paramètres : un néon réglable

## Ce qu'on fabrique

Un matériau qui **émet** une couleur choisie, avec une intensité réglable — l'enseigne au néon,
le noyau de lave, la lame magique, la LED d'un vaisseau. Le shader le plus court du cours, et
pourtant celui qu'on utilise le plus souvent en production.

Le vrai sujet de la leçon n'est pas la couleur : c'est **comment un réglage voyage** de
l'inspecteur jusqu'au GPU, et pourquoi ce trajet est piégé.

## L'idée

Trois choses seulement :

1. Un **uniforme** de type couleur, exposé dans l'inspecteur.
2. Un **uniforme** flottant pour l'intensité, avec un curseur borné.
3. La couleur écrite dans la sortie **émissive**, pas dans la couleur de base.

Le point 3 est celui qui fait la différence entre « un objet bleu » et « un objet qui éclaire ».
Un matériau a deux sorties de couleur qui n'ont rien à voir :

| Sortie | Sens | Réagit à la lumière |
|---|---|---|
| `ALBEDO` / `Base Color` | « de quelle couleur est cette matière » | oui : dans le noir, elle est noire |
| `EMISSION` / `Emissive Color` | « quelle lumière cette surface produit » | non : elle brille toute seule |

Un néon dans une pièce sombre doit rester lumineux. Donc : albédo **noir**, émission colorée.

## Godot

Fichier : `godot.gdshader`.

```glsl
shader_type spatial;

uniform vec3 couleur_neon : source_color = vec3(0.10, 0.70, 1.0);
uniform float intensite : hint_range(0.0, 20.0, 0.1) = 4.0;

void fragment() {
    ALBEDO = vec3(0.0);
    SPECULAR = 0.0;
    ROUGHNESS = 1.0;
    EMISSION = couleur_neon * intensite;
}
```

**Le branchement.** `MeshInstance3D` → `Material Override` → `New ShaderMaterial` → dans
`Shader`, `Load` → `godot.gdshader`. Les deux réglages apparaissent alors sous `Shader
Parameters`.

**Ligne par ligne.**

`: source_color` prévient Godot que cet uniforme est une couleur, et non trois nombres. Godot
travaille en espace linéaire, alors que le sélecteur de couleur te montre du sRGB. Sans
`source_color`, la valeur passe brute : le bleu que tu as choisi arrive plus clair et plus délavé
qu'à l'écran. **C'est l'erreur la plus fréquente en Godot, et elle est silencieuse.**

`: hint_range(0.0, 20.0, 0.1)` transforme le champ de saisie en curseur, avec un pas de 0.1.
Ce n'est pas cosmétique : un curseur borné empêche de taper `2000` par accident et de se
demander pourquoi tout l'écran est blanc.

`SPECULAR = 0.0` et `ROUGHNESS = 1.0` empêchent une lumière de la scène de laisser un reflet
brillant sur un objet censé être une source lumineuse. Sans ça, un néon dans une pièce éclairée
a un petit point blanc de plastique.

**Pour voir le halo**, il faut activer le glow : sélectionne le `WorldEnvironment`, section
`Glow`, coche `Enabled`. Le halo n'apparaît que si l'émission **dépasse 1.0** — d'où l'intensité.

## Unity URP

Fichier : `unity.shader`.

```hlsl
Properties
{
    [HDR] _Couleur ("Couleur", Color) = (0.10, 0.70, 1.0, 1.0)
    _Intensite ("Intensite", Range(0, 20)) = 4.0
}
```

```hlsl
CBUFFER_START(UnityPerMaterial)
    float4 _Couleur;
    float _Intensite;
CBUFFER_END

half4 frag(Varyings IN) : SV_Target
{
    return half4(_Couleur.rgb * _Intensite, 1.0);
}
```

**Le branchement.** `Create` → `Material`, puis en haut de son inspecteur choisir le shader
`Cours/01_Neon`. Glisser le matériau sur l'objet.

**Ce qu'il faut voir.**

`[HDR]` devant `_Couleur` change le sélecteur : Unity ajoute un champ `Intensity` en stops
d'exposition, et autorise des composantes au-dessus de 1. Tu as donc **deux façons** de régler
la puissance — celle du sélecteur HDR et notre `_Intensite`. Garde-en une seule, sinon tu
chercheras longtemps pourquoi le résultat ne correspond pas au chiffre affiché. Ici on garde
`_Intensite` et on laisse le sélecteur HDR à 0 stop.

Il n'y a pas de sortie « émissive » : ce shader est **unlit**, il écrit directement sa couleur
finale. C'est le bon choix pour un néon. Si tu voulais un objet éclairé *et* émissif, il faudrait
un shader `Lit` et ajouter l'émission au résultat de l'éclairage — leçon 22.

**Pour voir le halo** : dans l'asset URP, coche `HDR`. Puis dans la scène, ajoute un `Volume`
global avec un override `Bloom` (`Threshold` ≈ 1, `Intensity` ≈ 0.5). Sans `HDR` coché, ta
couleur est écrêtée à 1 avant même le bloom, et monter `_Intensite` au-delà de 1 ne fait
strictement rien.

## Unreal

Voir `unreal.md` — graphe, nœud `Custom` équivalent, instances de matériau, et le réglage
d'exposition qui masque l'effet tant qu'on ne l'a pas trouvé.

## Le banc

`banc.gdshader` est un `shader_type canvas_item` : charge-le sur un `ColorRect` en plein écran
(voir `00-bases/04-la-boucle-d-iteration.md`). Il affiche la même couleur avec une intensité qui
monte de 0 à 20 de gauche à droite, traitée de deux façons :

- **en haut** : la couleur écrêtée entre 0 et 1, ce que fait un écran sans HDR ;
- **en bas** : la même passée dans un tonemap simple, `c / (1 + c)`.

Regarde la bande du haut : au-delà du premier tiers, **c'est du blanc uni**. Toute l'information
au-dessus de 1 est perdue, et la teinte bleue avec. La bande du bas continue à évoluer jusqu'au
bout : elle monte en luminosité **en gardant sa couleur**.

C'est exactement ce qui se passe dans ton moteur selon que le HDR est activé ou non, et ça tient
en une image : **une intensité au-dessus de 1 ne sert à rien tant que le pipeline n'est pas en
HDR**, et elle devient indispensable dès qu'il l'est.

## Les pièges

**L'auto-exposition annule ton réglage.** Les trois moteurs ont une exposition automatique
activée par défaut ou presque. Tu montes l'intensité, le moteur assombrit toute la scène pour
compenser, et le néon ne change pas. Mets l'exposition en manuel pendant que tu règles.
(Godot : `WorldEnvironment` → `Tonemap` / `Auto Exposure`. Unity : override `Exposure` du Volume.
Unreal : `Post Process Volume` → `Exposure` → `Metering Mode: Manual`.)

**La couleur ne correspond pas à celle choisie.** En Godot, `source_color` oublié. En Unity, tu
as réglé l'intensité à la fois dans le sélecteur `[HDR]` et dans `_Intensite`.

**L'objet est rose (Unity) ou violet (Godot).** Le shader n'a pas compilé. Ouvre l'inspecteur du
shader (Unity) ou le panneau du bas (Godot) et lis l'erreur : elle est presque toujours un
point-virgule ou une propriété déclarée dans `Properties` mais absente du `CBUFFER`.

**L'objet ne projette plus d'ombre (Unity).** Normal : ce shader n'a qu'une passe. Il manque une
passe `ShadowCaster`. Pour un néon c'est souvent ce qu'on veut. On l'ajoutera à la leçon 05, où
son absence devient un vrai bug.

**Un paramètre hors du `CBUFFER` (Unity).** Le shader marche, mais le SRP Batcher se désactive
pour ce matériau et tes performances plongent sans message d'erreur. Règle : tout ce qui est dans
`Properties`, sauf les textures et les samplers, doit être dans `CBUFFER_START(UnityPerMaterial)`,
**dans le même ordre de préférence**.

## Ce que ça coûte

Une multiplication par pixel. C'est le shader le moins cher possible.

En revanche le **bloom**, lui, coûte cher : il lit l'image, la réduit plusieurs fois, la floute,
la remonte. Sur mobile, c'est souvent le premier post-traitement qu'on coupe. Retiens que le
halo n'est pas produit par ton shader mais par un effet plein écran qui, lui, se paie.

## À toi

1. **Sépare la teinte de la puissance.** Remplace l'unique intensité par deux uniformes :
   `intensite_min` et `intensite_max`, et affiche leur moyenne. Ça n'a aucun intérêt visuel —
   l'exercice est de refaire tout le trajet inspecteur → uniforme → sortie sans copier-coller.
2. **Deux couleurs mélangées.** Ajoute `couleur_secondaire` et un `melange` de 0 à 1, et sors
   `mix(couleur_neon, couleur_secondaire, melange)`. Tu viens d'écrire le cœur de 80 % des
   shaders de ce cours.
3. **Trouve le seuil.** Avec le bloom activé, descends l'intensité jusqu'à ce que le halo
   disparaisse. Note la valeur. Compare-la au `Threshold` du bloom. Tu tiens la relation entre
   les deux réglages, et tu ne la chercheras plus jamais.
4. **Casse-le exprès.** Enlève `source_color` en Godot, ou sors `_Intensite` du `CBUFFER` en
   Unity. Regarde ce qui se passe. Un bug qu'on a provoqué une fois se reconnaît en trois
   secondes la fois suivante.

**Leçon suivante : 02 — Texture, UV et tiling.** On arrête de peindre en uni.
