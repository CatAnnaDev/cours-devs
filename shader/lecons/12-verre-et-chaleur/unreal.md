# Leçon 12 en Unreal 5 — verre, réfraction et chaleur

Unreal est le seul des trois moteurs à avoir une entrée **Refraction** dédiée sur le nœud racine.
Il y a donc deux voies, et elles ne servent pas au même usage.

## Voie 1 — L'entrée `Refraction` (le vrai verre)

`Blend Mode` : `Translucent`. Dans `Details` → `Translucency` :

- **`Refraction Mode`** : `Index Of Refraction` pour du verre ou de l'eau physique,
  `Pixel Normal Offset` pour un effet contrôlé à la main.
- **`Screen Space Reflections`** si tu veux aussi des reflets.

Puis :

- **Refraction** = `ScalarParameter` **IndiceRefraction**. Les valeurs réelles : `1.0` = aucun
  effet, `1.33` = eau, `1.5` = verre, `2.42` = diamant.
- **Normal** = ta normal map qui défile — c'est elle qui donne le mouvement.
- **Opacity** = `0.1` environ, sinon on ne voit plus à travers.

C'est la méthode correcte pour du verre, et elle gère les cas qu'une distorsion d'UV ne gère pas :
la réfraction change avec l'angle de vue.

**`Pixel Normal Offset`** est souvent préférable en pratique : `Index Of Refraction` produit des
artefacts sur les surfaces planes vues de face, là où `Pixel Normal Offset` déplace simplement
selon la normale, ce qui est plus prévisible et se règle mieux.

## Voie 2 — `SceneColor` (la distorsion de chaleur)

Pour de l'air chaud au-dessus d'un moteur ou d'un feu, on ne veut pas de verre : on veut
simplement décaler ce qu'il y a derrière.

1. `ScreenPosition` → sortie `ViewportUV`.
2. `TextureSampleParameter2D` **Bruit** (ou deux `Panner` sur la même texture, à vitesses
   différentes) → canal `R` et `G`, ramenés en `-1..1` par un `ConstantBiasScale`
   (`Bias -0.5`, `Scale 2`).
3. `Multiply` par un `ScalarParameter` **Force** → `Add` aux UV d'écran.
4. `SceneColor` avec ces UV → **Emissive Color**.
5. `Blend Mode` : `Translucent`, `Opacity` = `1`, `Shading Model` : `Unlit`.

**`SceneColor` n'est disponible que sur un matériau `Translucent`**, et il ne contient **que les
objets opaques déjà rendus** — pas les autres translucides. Deux plans de distorsion superposés ne
se voient donc pas l'un l'autre.

**Le coût est réel** : lire `SceneColor` force Unreal à faire une copie de la cible de rendu.
Une seule copie sert à tous les matériaux qui la lisent dans la même image, mais la première
coûte une passe plein écran.

## Le flou : `SceneColor` n'a pas de mipmaps

Contrairement à Godot et Unity, la copie de scène d'Unreal n'expose pas de niveaux de mip
utilisables pour du verre dépoli. Les solutions, dans l'ordre :

1. **`Blurred Scene Color`** — sur un matériau `Post Process`, Unreal expose une version floutée.
   C'est la voie propre, mais elle impose un matériau de post-traitement (leçon 25).
2. **Plusieurs échantillons décalés** à la main : quatre à huit `SceneColor` aux UV légèrement
   différents, moyennés. Cher, et ça se voit sur le compteur.
3. **`Roughness` élevée avec la réfraction physique** : ce n'est pas un flou, mais l'aspect
   « verre dépoli » y ressemble suffisamment dans la plupart des cas.

## L'équivalent en nœud Custom

Entrées : `UVEcran` (Float2), `Perturbation` (Float2), `Force`. Sortie `CMOT Float 2`.

```hlsl
return clamp(UVEcran + Perturbation * Force, 0.001, 0.999);
```

Le `clamp` n'est pas décoratif : voir la section « Les pièges » du `README.md`.
