# Leçon 02 en Unreal 5 — texture, carrelage et teinte

## Le graphe

Matériau `M_Sol`, `Shading Model` : `Default Lit`.

1. Clic droit → `TextureSampleParameter2D`. Nomme-le **BaseMap**, assigne-lui une texture dans
   `Details`. (Un `TextureSample` simple marche aussi, mais un *Parameter* est réglable depuis
   une instance — prends l'habitude.)
2. Clic droit → `TextureCoordinate`. C'est l'UV. Dans `Details`, `UTiling` et `VTiling` te donnent
   déjà le carrelage — mais fais-le une fois à la main pour comprendre :
3. Clic droit → `VectorParameter` nommé **Carrelage**, valeur `(4, 4, 0, 0)`.
4. Clic droit → `Multiply` : `TextureCoordinate` sur `A`, `Carrelage` sur `B` (le nœud n'utilisera
   que les composantes `RG`).
5. Branche le `Multiply` sur l'entrée `UVs` du `TextureSampleParameter2D`.
6. Clic droit → `VectorParameter` nommé **Teinte**, valeur `(1, 1, 1)`.
7. Clic droit → `Multiply` : sortie `RGB` de la texture sur `A`, `Teinte` sur `B`.
8. Résultat → **Base Color**.
9. Clic droit → `ScalarParameter` nommé **Rugosite**, valeur `0.85` → **Roughness**.

`Apply`, `Save`, puis `Create Material Instance` pour régler carrelage et teinte sans recompiler.

## Le décalage, en prime

Ajoute un `VectorParameter` **Decalage** et un `Add` entre le `Multiply` du carrelage et l'entrée
`UVs`. Ordre imposé : **on multiplie d'abord, on additionne ensuite**. L'inverse déplace la
texture d'une distance qui dépend du carrelage, ce qui est presque toujours faux.

Il existe un nœud tout fait : `Panner` (décalage animé) et `TexCoord` avec ses champs de tiling.
Écris la version manuelle une fois, puis utilise les nœuds tout faits — mais tu sauras ce qu'ils
contiennent.

## L'équivalent en nœud Custom

Entrées : `UV` (Float2), `Carrelage` (Float2), `Decalage` (Float2). Sortie `CMOT Float 2`.

```hlsl
return UV * Carrelage + Decalage;
```

Une texture ne s'échantillonne pas facilement dans un `Custom` (il faut passer l'objet texture et
son sampler), c'est pourquoi on garde le `TextureSample` en nœud et on ne calcule que l'UV en
HLSL. C'est le partage habituel : **la maths en Custom, les ressources en nœuds.**

## Les réglages de la texture elle-même

Double-clic sur la texture dans le `Content Browser` :

| Réglage | Ce que ça change |
|---|---|
| `sRGB` | **coché pour une couleur, décoché pour un masque ou une hauteur** |
| `Compression Settings` | `Default (DXT1/5)` pour une couleur, `Masks` pour un masque, `Normalmap` pour une normale |
| `Texture Group` | décide de la résolution effective par plateforme |
| `X-axis / Y-axis Tiling Method` | `Wrap` pour carreler, `Clamp` pour étirer le bord |
| `Filter` | `Nearest` pour du pixel art, sinon laisse `Default` |

Le premier est celui qui produit des bugs muets : un masque importé en `sRGB` a ses valeurs
tordues par la courbe gamma, et ton seuil à 0.5 ne tombe plus au milieu. On y revient à la
leçon 05.
