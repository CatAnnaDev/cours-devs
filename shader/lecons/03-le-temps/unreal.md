# Leçon 03 en Unreal 5 — défilement et pulsation

## Le graphe : le défilement

Unreal a un nœud tout fait pour ça, mais construis-le une fois à la main.

1. `TextureCoordinate` → `A` d'un `Add`.
2. `Time` → `Multiply` avec un `VectorParameter` **Vitesse** `(0, -0.35, 0, 0)` → `B` du `Add`.
3. `Add` → entrée `UVs` d'un `TextureSampleParameter2D` nommé **Motif**.

Le nœud tout fait est **`Panner`** : entrées `Coordinate` et `Time`, paramètre `Speed X` / `Speed Y`
dans `Details`. Il fait exactement ce que tu viens de câbler. Utilise-le ensuite — mais tu sais
maintenant que ce n'est pas magique.

Son cousin **`Rotator`** fait tourner les UV autour d'un point, même principe.

## Le graphe : la pulsation

1. `Time` → `Multiply` avec un `ScalarParameter` **PulsationVitesse** (`2.0`).
2. → `Sine`. **Attention** : le nœud `Sine` d'Unreal prend une **période de 1**, pas de 2π.
   Entrer `Time` directement donne un cycle par seconde, pas un cycle toutes les 6,28 secondes.
   C'est plus pratique et c'est un piège quand on traduit une formule GLSL.
3. → `Multiply` par `0.5` → `Add` `0.5`. Le nœud `ConstantBiasScale` fait les deux d'un coup,
   c'est l'idiome Unreal pour passer de `-1..1` à `0..1`.
4. → `Lerp` entre `1 - PulsationForce` et `1`, ou plus simplement un `Lerp` dont `Alpha` est la
   pulsation, `A` = `0.65`, `B` = `1.0`.

## Le résultat

`Motif` (RGB) → `Multiply` par un `VectorParameter` **Teinte** → `Multiply` par la pulsation →
`Multiply` par un `ScalarParameter` **EmissionForce** → **Emissive Color**.

`Shading Model` : `Unlit`. `Two Sided` coché dans `Details` si c'est un plan qu'on voit des deux
côtés (l'équivalent de `cull_disabled` en Godot et `Cull Off` en Unity).

## L'équivalent en nœud Custom

Entrées : `UV` (Float2), `Vitesse` (Float2), `TempsJeu` (Float, branché sur `Time`).
Sortie `CMOT Float 2`.

```hlsl
return UV + Vitesse * TempsJeu;
```

Et pour la pulsation, sortie `CMOT Float 1`, entrées `TempsJeu` et `Force` :

```hlsl
float pulsation = sin(TempsJeu * 6.28318530718) * 0.5 + 0.5;
return lerp(1.0 - Force, 1.0, pulsation);
```

Note le `6.28318530718` : dans un `Custom`, tu es en HLSL brut, donc `sin` a bien une période de
2π. La différence de convention est **entre les nœuds et le HLSL**, pas entre les moteurs.

## Le temps et la pause

Le nœud `Time` a une case **`Ignore Pause`** dans `Details`.

- **Décochée** (par défaut) : l'animation se fige quand le jeu est en pause. C'est ce qu'on veut
  pour l'eau, le feuillage, une rivière.
- **Cochée** : l'animation continue en pause. C'est ce qu'on veut pour un effet d'interface, un
  menu, un curseur de chargement.

Il a aussi un champ **`Period`** : au-delà de cette durée, le temps repart à zéro. Ça sert à
éviter la perte de précision des flottants sur une longue session (voir le `README.md` de la
leçon). Mets-y une valeur qui soit un multiple de la période de ton animation, sinon le retour à
zéro produit un saut visible.
