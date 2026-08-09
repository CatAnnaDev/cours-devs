# Leçon 09 en Unreal 5 — vent sur le feuillage

## L'entrée à connaître : `World Position Offset`

C'est le shader de sommets d'Unreal, et la seule entrée du nœud racine qui déplace la géométrie.
Elle attend un **déplacement en espace monde, en centimètres**.

Tous les nombres de cette leçon sont donc à multiplier par 100 par rapport aux versions Godot et
Unity : une `ForceVent` de `0.15` devient `15`.

## Le graphe

**La phase, pour que les arbres ne soient pas synchronisés.**

1. `WorldPosition` → `ComponentMask` sur `RG` (en Unreal, le plan horizontal est XY, l'axe
   vertical est Z).
2. `VectorParameter` **DirectionVent** `(1, 0.3, 0, 0)` → `Normalize`.
3. `DotProduct` des deux → `Divide` par un `ScalarParameter` **LongueurRafale**.

C'est la ligne la plus importante de la leçon : la phase dépend de **où l'arbre se trouve dans le
monde**, donc deux arbres identiques posés à dix mètres l'un de l'autre ondulent en décalé.

**L'ondulation.**

4. `Time` → `Multiply` par **FrequenceVent** → `Subtract` la phase → `Sine`.
   (Rappel : le nœud `Sine` d'Unreal a une période de **1**, pas de 2π.)
5. `Multiply` par **ForceVent**, puis par la direction du vent étendue en 3D
   (`AppendVector` de la direction XY et d'un `0` en Z).

**La souplesse, peinte par l'artiste.**

6. `VertexColor` → canal `R` → `Multiply` sur le résultat.

Le tronc a une couleur de sommet noire (souplesse 0, il ne bouge pas), les branches un gris, le
bout des feuilles un blanc. **C'est du travail d'artiste, pas de programmeur** — et c'est ce qui
sépare un feuillage crédible d'une masse qui glisse en bloc.

7. Le total → **World Position Offset**.

## Le réglage qui casse tout si on l'oublie : `Bounds Scale`

Un maillage déplacé par `World Position Offset` **sort de sa boîte englobante**. Unreal utilise
cette boîte pour décider si l'objet est visible et pour calculer son ombre. Résultat sans
correction : l'arbre disparaît quand il touche le bord de l'écran, et son ombre est tronquée.

Sur le `Static Mesh Component`, `Details` → `Rendering` → **`Bounds Scale`** : mets `1.5` pour un
déplacement de quelques dizaines de centimètres. C'est un réglage par composant, pas par
matériau — pense à le mettre sur la Blueprint de l'arbre, pas sur chaque instance.

## Ce qu'Unreal fait mieux que les autres

**`SimpleGrassWind`** — une fonction de matériau fournie, qui fait exactement le contenu de cette
leçon avec quatre entrées : `WindIntensity`, `WindWeight`, `WindSpeed`, `AdditionalWPO`. Branche-la
directement sur `World Position Offset`.

**`Pivot Painter`** — un outil qui range, dans les UV et les couleurs de sommet du modèle, le
**pivot et l'axe de chaque branche**. Le matériau peut alors faire tourner chaque branche autour
de son vrai pivot au lieu de translater les sommets. C'est la différence entre un feuillage qui
ondule et un feuillage qui **plie**.

C'est la bonne façon de faire du feuillage en production, et ça n'a pas d'équivalent aussi
intégré dans Godot ou Unity — où il faut ranger les mêmes données à la main dans un second jeu
d'UV.

## L'équivalent en nœud Custom

Entrées : `PositionMonde` (Float3), `Direction` (Float2), `Souplesse`, `Force`, `Frequence`,
`Longueur`, `TempsJeu`. Sortie `CMOT Float 3`.

```hlsl
float2 direction = normalize(Direction);
float phase = dot(PositionMonde.xy, direction) / Longueur;
float rafale = sin(TempsJeu * Frequence * 6.28318530718 - phase);
return float3(direction.x, direction.y, 0.0) * rafale * Force * Souplesse;
```

Note le `.xy` et le `float3(..., 0.0)` : l'axe vertical d'Unreal est Z, pas Y.

## Le découpage alpha

`Blend Mode` : `Masked`, `Opacity Mask Clip Value` autour de `0.33`, et **`Two Sided`** coché —
une feuille est un plan, on la voit des deux côtés.

Un piège spécifique : par défaut, une face arrière garde la normale de la face avant, donc les
feuilles vues de dos sont éclairées à l'envers. Coche **`Two Sided Foliage`** comme
`Shading Model` : Unreal gère alors la transmission de la lumière à travers la feuille, ce qui
donne l'aspect « feuille traversée par le soleil » sans rien câbler.
