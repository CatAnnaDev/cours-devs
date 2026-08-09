# Leçon 04 en Unreal 5 — masques et mélanges

## Le graphe

Matériau `M_Neige`, `Shading Model` : `Default Lit`.

**La source du masque : l'orientation de la surface.**

1. Clic droit → `VertexNormalWS`. C'est la normale en espace monde.
2. Clic droit → `ComponentMask`, coche **uniquement `B`**. En Unreal, l'axe vertical est **Z**,
   pas `Y` comme en Godot et Unity. C'est la conversion la plus fréquemment ratée.
3. → `ConstantBiasScale` (`Bias 1`, `Scale 0.5`) pour passer de `-1..1` à `0..1`.

**L'irrégularité.**

4. `TextureSampleParameter2D` **Bruit** (une texture de bruit en niveaux de gris, `sRGB` décoché)
   → canal `R`.
5. `OneMinus` → `Multiply` par un `ScalarParameter` **Irregularite** → `Subtract` du résultat de
   l'étape 3.

**Le seuil et la netteté.**

6. `ScalarParameter` **Couverture** → `OneMinus` : c'est le seuil.
7. `ScalarParameter` **Nettete**.
8. Clic droit → `SmoothStep`. Entrées : `Min` = seuil − netteté (`Subtract`), `Max` = seuil +
   netteté (`Add`), `Value` = le résultat de l'étape 5.

**Le mélange.**

9. `Lerp` : `A` = la texture de base, `B` = un `VectorParameter` **CouleurNeige**, `Alpha` = le
   masque → **Base Color**.
10. Un second `Lerp` avec deux `ScalarParameter` de rugosité, même `Alpha` → **Roughness**.

Le nœud **`Blend_Overlay`** et toute la famille `Blend_*` (dans `Material Functions`) font des
mélanges plus élaborés que `Lerp`. Ils sont utiles, mais commence par comprendre `Lerp` : les
autres sont des combinaisons de `Lerp`, `Multiply` et `Add`.

## L'équivalent en nœud Custom

Entrées : `NormaleMonde` (Float3), `Grain` (Float1), `Couverture`, `Nettete`, `Irregularite`.
Sortie `CMOT Float 1`.

```hlsl
float versLeHaut = NormaleMonde.z * 0.5 + 0.5;
float valeur = versLeHaut - Irregularite * (1.0 - Grain);
float seuil = 1.0 - Couverture;
return smoothstep(seuil - Nettete, seuil + Nettete, valeur);
```

## Le nœud à connaître : `WorldAlignedBlend`

Unreal fournit une fonction de matériau toute faite qui produit exactement ce masque
« vers le haut », avec des réglages de netteté et de bruit intégrés : cherche
**`WorldAlignedBlend`** dans la palette. En production, c'est ce que tu utiliseras.

Construis quand même la version manuelle une fois. Le jour où `WorldAlignedBlend` ne fait pas
tout à fait ce qu'il te faut — et ce jour arrive — tu sauras quoi remplacer.

## Rappel d'unités

Rien à convertir dans cette leçon : tout est en `0..1`. Mais garde en tête pour la suite que
l'axe vertical d'Unreal est **Z**, et que ses distances sont en centimètres.
