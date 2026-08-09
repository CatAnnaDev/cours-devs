# Leçon 10 en Unreal 5 — eau et normal maps

## Le graphe

`Shading Model` : `Default Lit`. `Blend Mode` : `Opaque` pour l'instant (la transparence et
l'écume arrivent à la leçon 11).

**Les deux couches qui défilent.**

1. Deux nœuds `Panner`. Le premier : `Speed X = 0.03`, `Speed Y = 0.018`. Le second :
   `Speed X = -0.021`, `Speed Y = 0.035`.
2. Devant chacun, un `TextureCoordinate` avec des tilings différents (`3` et `7`).
3. Chacun alimente les `UVs` d'un `TextureSampleParameter2D` **Normales**.
   **Le `Sampler Type` doit être `Normal`** — c'est ce réglage, dans le panneau `Details` du
   nœud, qui déballe correctement la texture. S'il reste sur `Color`, l'eau est plate et un peu
   bleutée sans qu'aucune erreur ne s'affiche.

**Le mélange.** Unreal fournit la bonne fonction : cherche **`BlendAngleCorrectedNormals`** dans
la palette. Entrées `BaseNormal` et `AdditionalNormal`, et elle fait le mélange correct — pas une
moyenne.

Si tu veux le faire à la main, c'est le mélange dit *whiteout* :

```hlsl
return normalize(float3(A.xy + B.xy, A.z * B.z));
```

4. Le résultat → **Normal**.

**Le reste de la surface.**

- `VectorParameter` **CouleurEau** → **Base Color**
- `ScalarParameter` **Rugosite** (`0.05`) → **Roughness**
- `0` → **Metallic**
- `ScalarParameter` **Specular** (`0.6`) → **Specular**

**La force des normales.** `FlattenNormal` prend une normale et une force, et l'aplatit vers
`(0,0,1)`. C'est la façon propre d'exposer un curseur d'intensité — multiplier les composantes X
et Y à la main marche aussi mais peut dénormaliser le vecteur.

## Le vrai outil d'Unreal pour l'eau

Unreal 5 a un **plugin Water** complet : `Water Body Ocean`, `Water Body River`, `Water Body Lake`,
avec des ondes de Gerstner, la profondeur, l'écume, les rives et le rendu sous l'eau.

Sers-t'en pour un projet réel. Mais monte quand même le matériau de cette leçon une fois : le
plugin fait exactement ça, en plus complet, et tu ne sauras pas le régler si tu n'as jamais vu ce
qu'il y a dedans.

## L'espace tangent, côté Unreal

Le nœud racine attend une normale **en espace tangent** sur son entrée `Normal`. Si tu calcules
une normale en espace monde (par exemple pour du triplanar, leçon 13), il faut soit la convertir,
soit cocher **`Tangent Space Normal`** à *décoché* dans `Details` — le nœud racine attend alors
une normale monde.

C'est une case unique, elle change complètement l'interprétation de l'entrée, et c'est une source
classique d'eau qui a l'air éclairée par une lumière imaginaire.

## L'équivalent en nœud Custom

Entrées : `A` (Float3), `B` (Float3). Sortie `CMOT Float 3`.

```hlsl
return normalize(float3(A.xy + B.xy, A.z * B.z));
```

Attention : les nœuds `TextureSample` en mode `Normal` renvoient déjà la normale **déballée**
(entre -1 et 1). Ne refais pas le `* 2 - 1` que font les versions Godot et Unity — tu le ferais
deux fois.
