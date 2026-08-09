# Leçon 13 en Unreal 5 — triplanar

## Le nœud tout fait

Unreal a la fonction : **`WorldAlignedTexture`**.

Entrées :

| Entrée | Rôle |
|---|---|
| `TextureObject` | branche un `TextureObjectParameter`, pas un `TextureSample` |
| `TextureSize` | la taille en centimètres que doit couvrir une répétition |
| `WorldPosition` | laisse vide pour la position du pixel, ou branche autre chose |
| `bUseWorldNormalToBlend` | à cocher |
| `ProjectionVectorScale` | l'équivalent de la netteté du mélange |

Sorties : `XYTexture`, `XZTexture`, `YZTexture` et surtout **`Texture`**, déjà mélangée. C'est
celle-là qu'on utilise.

Il existe aussi **`WorldAlignedNormal`**, qui fait le même travail pour une normal map en gérant
le mélange correct des trois normales — le morceau pénible de cette leçon.

**Sers-t'en en production.** Mais monte la version manuelle une fois, parce que `WorldAlignedTexture`
coûte trois échantillons sans le dire, et que beaucoup de projets Unreal en collent quatre sur un
même matériau sans comprendre pourquoi le rendu s'écroule.

## La version manuelle

1. `WorldPosition` → `Multiply` par un `ScalarParameter` **Echelle**.
2. Trois `ComponentMask` : `RG` (le plan XY), `RB` (XZ), `GB` (YZ).
3. Trois `TextureSample` sur la même texture, un par plan.
4. `VertexNormalWS` → `Abs` → `Power` avec un `ScalarParameter` **Nettete**.
5. Normaliser les poids : `Dot` du vecteur de poids avec `(1,1,1)`, puis `Divide`.
6. Trois `Multiply` par la composante correspondante du poids, deux `Add`.

Attention à la convention d'axes : en Unreal, **Z est vertical**. Le plan « horizontal », celui
qui couvre le sol, est donc XY — pas XZ comme en Godot et Unity.

## Le cas du terrain

Le `Landscape` d'Unreal a ses propres nœuds : `LandscapeLayerBlend`, `LandscapeLayerCoords`,
`LandscapeLayerSwitch`. Ils permettent au level designer de **peindre** les couches, ce qui est
plus contrôlable qu'un mélange automatique par normale.

En pratique, un bon matériau de terrain Unreal combine les deux : les couches peintes pour le
choix des matériaux, et le triplanar **à l'intérieur de chaque couche** pour éviter l'étirement
sur les falaises.

## L'équivalent en nœud Custom

Entrées : `Normale` (Float3), `Nettete` (Float1). Sortie `CMOT Float 3`.

```hlsl
float3 poids = pow(abs(Normale), Nettete);
return poids / (poids.x + poids.y + poids.z);
```

Et le mélange des trois normales, entrées `NX`, `NY`, `NZ`, `Normale`, `Poids` :

```hlsl
float3 x = float3(NX.xy + Normale.yz, abs(NX.z) * Normale.x);
float3 y = float3(NY.xy + Normale.xz, abs(NY.z) * Normale.y);
float3 z = float3(NZ.xy + Normale.xy, abs(NZ.z) * Normale.z);
return normalize(x.zxy * Poids.x + y.xzy * Poids.y + z.xyz * Poids.z);
```

Les permutations diffèrent de la version Godot/Unity parce que l'axe vertical n'est pas le même.
C'est exactement le genre de détail où il faut **regarder** plutôt que faire confiance : affiche
la normale en couleur et compare avec une normale géométrique.

## Le réglage `Tangent Space Normal`

Comme à la leçon 10 : si tu produis une normale en **espace monde** — ce que fait le triplanar —
il faut décocher `Tangent Space Normal` dans les `Details` du nœud racine. C'est la case la plus
oubliée de cette leçon, et le symptôme est un éclairage qui a l'air « presque juste ».
