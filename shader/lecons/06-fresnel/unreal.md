# Leçon 06 en Unreal 5 — Fresnel et contour lumineux

## Le nœud tout fait

Unreal a un nœud `Fresnel`. Il fait exactement le calcul de la leçon.

1. Clic droit → `Fresnel`.
2. `ExponentIn` : branche un `ScalarParameter` **Puissance** (`4.0`).
3. `BaseReflectFractionIn` : la valeur du fresnel quand on regarde la surface **de face**.
   `0` donne un contour qui disparaît complètement au centre — c'est ce qu'on veut ici.
   La valeur physiquement correcte pour un diélectrique est `0.04`, et c'est ce qu'on utilisera à
   la leçon 21 pour la vraie BRDF.
4. `Normal` : laisse vide pour utiliser la normale du pixel. Branche-y une normal map si tu veux
   que le contour suive les détails de la surface — voir « Les pièges » dans le `README.md`.

Puis : `Fresnel` → `Multiply` par un `VectorParameter` **CouleurContour** → `Multiply` par un
`ScalarParameter` **Intensite** → **Emissive Color**.

Pour un objet éclairé normalement avec un contour en plus, garde `Shading Model` à `Default Lit`,
branche ta texture sur `Base Color`, et le fresnel sur `Emissive Color` uniquement.

## La version manuelle

Construis-la une fois — c'est trois nœuds, et ça enlève toute magie au nœud `Fresnel`.

1. `VertexNormalWS`.
2. `CameraVector` — la direction du pixel **vers** la caméra, déjà normalisée.
3. `DotProduct` des deux.
4. `Clamp` entre 0 et 1.
5. `OneMinus`.
6. `Power` avec `Exp` = **Puissance**.

C'est exactement `pow(1 - saturate(dot(N, V)), puissance)`.

Compare les deux branches avec `Start Previewing Node` : elles donnent la même image. Le nœud
`Fresnel` ajoute juste le `BaseReflectFraction` et un `Clamp` de sécurité.

## L'équivalent en nœud Custom

Entrées : `Normale` (Float3), `Vue` (Float3), `Puissance` (Float1). Sortie `CMOT Float 1`.

```hlsl
float face = saturate(dot(normalize(Normale), normalize(Vue)));
return pow(1.0 - face, Puissance);
```

## Le piège spécifique à Unreal

`CameraVector` pointe **du pixel vers la caméra**. Certains tutoriels utilisent `PixelDepth` ou
un `Subtract` entre `CameraPositionWS` et `WorldPosition` — c'est le même vecteur, mais **non
normalisé**. Un `dot` avec un vecteur non normalisé donne n'importe quoi : le contour devient
dépendant de la distance, et grossit quand on s'éloigne.

Si tu construis la direction de vue à la main, le `Normalize` n'est pas optionnel.

## Où ça sert vraiment, en Unreal

- **Contour de sélection** : matériau appliqué en `Overlay Material` sur le maillage, fresnel pur
  en émissif, `Blend Mode` à `Translucent`.
- **Bouclier / champ de force** : `Blend Mode` `Translucent`, `Shading Model` `Unlit`, fresnel
  branché à la fois sur `Emissive Color` et sur `Opacity`. C'est la leçon 07.
- **Adoucir un bord de particule** : combiné avec `DepthFade`, leçon 11.
- **Faire ressortir un personnage sur un fond sombre** : la lumière de contour, ci-dessous.
