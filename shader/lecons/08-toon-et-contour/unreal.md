# Leçon 08 en Unreal 5 — toon et contour

Unreal est le moteur où cette leçon demande le plus de contournements, pour une raison précise :
**son pipeline d'éclairage n'est pas ouvert au matériau**. Un matériau `Default Lit` ne peut pas
décider comment il réagit à la lumière — c'est le moteur qui applique sa BRDF. Il faut donc
sortir du chemin balisé.

## Option 1 — `Unlit` et lumière fournie à la main

C'est la voie la plus utilisée en stylisé, et de loin la plus contrôlable.

`Shading Model` : `Unlit`.

**Obtenir la direction de la lumière.** Trois façons, par ordre de préférence :

| Méthode | Comment | Quand |
|---|---|---|
| `VectorParameter` **DirectionLumiere** mis à jour par Blueprint | `Get Forward Vector` de la `Directional Light` → `Set Vector Parameter Value` | le plus fiable, une seule ligne de Blueprint |
| nœud `Atmospheric Light Vector` | direct dans le graphe | seulement si la scène a une `SkyAtmosphere` |
| `Material Parameter Collection` | une valeur globale lue par tous les matériaux toon | dès que plusieurs matériaux la partagent — **la bonne solution en production** |

**Le graphe.**

1. `VertexNormalWS` et `DirectionLumiere` (inversée : `Multiply` par `-1`, car la *forward* d'une
   lumière pointe **dans** la direction où elle éclaire).
2. `DotProduct` → `ConstantBiasScale` (`Bias 1`, `Scale 0.5`) pour passer en `0..1`.
3. La quantification, en `Custom` (voir plus bas) ou en nœuds : `Multiply` par **Niveaux**,
   `Floor`, `Divide` par **Niveaux**.
4. `Lerp` : `A` = **CouleurOmbre**, `B` = `1`, `Alpha` = le résultat → `Multiply` par la texture
   de base → **Emissive Color**.

En `Unlit`, tout passe par `Emissive Color`. C'est déroutant au début, mais c'est cohérent : tu
calcules toi-même la couleur finale, le moteur n'y touche plus.

**Ce que tu perds** : les ombres portées reçues, les lumières additionnelles, le brouillard, les
réflexions. Pour un jeu entièrement stylisé, c'est souvent un bon marché. Pour un objet toon dans
une scène réaliste, c'est rédhibitoire — et il faut alors l'option 3.

## Option 2 — Le contour par coque inversée

Unreal ne permet pas d'ajouter une seconde passe à un matériau. Deux voies :

**A. Un second maillage.** Duplique le `Static Mesh Component`, applique-lui un matériau
d'extrusion, et coche `Reverse Culling` — ou, plus simple, mets le matériau en `Two Sided` et
extrude vers l'intérieur.

Le matériau d'extrusion :
- `Shading Model` : `Unlit`
- `Blend Mode` : `Opaque`
- **World Position Offset** = `VertexNormalWS` × `ScalarParameter` **Epaisseur**
- **Emissive Color** = `VectorParameter` **CouleurContour**
- `Details` → `Two Sided` coché, et `Opacity Mask`... non : pour ne garder que la coque, coche
  plutôt **`Is Two Sided`** et laisse la géométrie avant recouvrir la coque.

Pour une épaisseur constante à l'écran, multiplie l'extrusion par la distance à la caméra :
`CameraPositionWS` − `WorldPosition` → `Length` → `Multiply`. Et rappelle-toi : **en
centimètres**. Une épaisseur de `1` en Unreal correspond à `0.01` en Godot.

**B. Le contour en post-traitement.** C'est ce que fait la majorité des productions Unreal, parce
que ça donne aussi les contours **intérieurs** (les plis d'un vêtement, la séparation d'un bras et
du torse) que la coque inversée ne peut pas produire. Ça demande un matériau `Post Process` qui
lit `SceneTexture:SceneDepth` et `SceneTexture:WorldNormal` et applique un opérateur de Sobel.

**C'est la leçon 26 du cours**, et elle vaut pour les trois moteurs. Si tu vises Unreal
sérieusement, saute la coque inversée et attends-la.

## Option 3 — Un vrai Shading Model toon

Unreal expose `Preintegrated Skin`, `Subsurface`, `Clear Coat`… mais pas de modèle toon. En
ajouter un demande de **modifier le code du moteur** (`ShadingModels.ush`, `MaterialShared.ush`,
recompilation complète). C'est faisable, plusieurs studios le font, et c'est hors de portée d'une
leçon d'introduction.

Le compromis utilisé en pratique : `Default Lit` avec une `Roughness` très basse et une
`Base Color` déjà quantifiée, plus un post-traitement qui étage la luminosité finale. Moins
contrôlable, mais aucune modification du moteur.

## L'équivalent en nœud Custom

La quantification adoucie, en un nœud. Entrées : `Valeur`, `Nombre`, `Largeur`.
Sortie `CMOT Float 1`.

```hlsl
float echelle = Valeur * Nombre;
float palier = floor(echelle);
float reste = frac(echelle);
return (palier + smoothstep(0.5 - Largeur, 0.5 + Largeur, reste)) / Nombre;
```

Et la tache spéculaire toon, entrées `Normale`, `Lumiere`, `Vue`, `Seuil` :

```hlsl
float3 demi = normalize(Lumiere + Vue);
float brillance = pow(saturate(dot(normalize(Normale), demi)), 32.0);
return step(Seuil, brillance);
```
