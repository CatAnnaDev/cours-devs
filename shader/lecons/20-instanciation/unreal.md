# Leçon 20 en Unreal 5 — instanciation et variation

## Les trois systèmes, et lequel choisir

| Système | Quand | Variation par instance |
|---|---|---|
| `Instanced Static Mesh` (ISM) | quelques centaines d'objets identiques | `PerInstanceRandom`, `PerInstanceCustomData` |
| `Hierarchical ISM` (HISM) | des milliers, avec LOD et culling par instance | idem |
| **Nanite** | à peu près tout, en 5.x | idem, et le culling est automatique |
| `Foliage` / `Procedural Foliage` | de la végétation peinte par un artiste | idem, plus des règles de densité |

En Unreal 5, la réponse par défaut pour un décor est **Nanite**, et pour de la végétation animée
c'est **HISM ou le système de Foliage** — Nanite n'aimait pas le `World Position Offset` avant 5.1,
et le gère depuis, mais au prix d'une boîte de déplacement à déclarer (`WPO Disable Distance`).

## La variation par instance

Deux nœuds, et c'est tout ce qu'il faut retenir :

**`PerInstanceRandom`** — un nombre pseudo-aléatoire entre 0 et 1, différent par instance,
constant dans le temps, gratuit. C'est l'équivalent direct du `INSTANCE_CUSTOM.x` de Godot ou de
la graine du `_Variation` d'Unity.

Un seul nombre suffit à faire beaucoup : passe-le dans des fonctions de hachage différentes pour en
tirer une teinte, une échelle, une phase.

```hlsl
float teinte = frac(Graine * 7.13);
float echelle = frac(Graine * 3.71);
float phase = frac(Graine * 11.37);
```

**`PerInstanceCustomData`** — jusqu'à plusieurs flottants réellement choisis par instance, écrits
depuis une Blueprint avec `Add Instance` puis `Set Custom Data Value`. À utiliser quand la variation
doit être **décidée**, pas tirée au sort : l'âge d'une plante, l'état d'un bâtiment, une couleur
d'équipe.

Le nombre de flottants se règle sur le composant : `Num Custom Data Floats`.

## L'échelle par instance

Attention à un piège spécifique : mettre à l'échelle via la matrice de l'instance change aussi la
**normale** et, sur un maillage animé par `World Position Offset`, l'amplitude du déplacement.

Si tu veux varier la taille sans toucher au vent, fais comme dans les versions Godot et Unity :
multiplie la **position locale** dans le matériau, pas la transformation de l'instance.

## Le vent, cohérent entre instances

Le point de la leçon 09 reste vrai, et il est plus visible encore ici : la phase doit dépendre de
`WorldPosition`, pas seulement de la graine.

- **La graine seule** : chaque touffe fait son propre mouvement, on voit un grouillement.
- **La position seule** : les rafales traversent le champ, mais deux touffes voisines sont
  identiques.
- **Les deux** : des rafales qui traversent, et du désordre à l'intérieur. C'est ce que fait le
  shader de cette leçon.

## Le culling et les bounds

Un HISM cull ses instances individuellement — c'est tout son intérêt par rapport à un ISM simple.
Mais le `World Position Offset` sort les instances de leur boîte : règle
**`WPO Disable Distance`** sur le composant pour couper le vent au loin, ce qui règle en même temps
le coût et l'essentiel du problème de bornes.

C'est un des rares endroits où l'optimisation et la correction vont dans le même sens.

## L'équivalent en nœud Custom

Entrée `Graine` (Float1). Sortie `CMOT Float 3` — trois variations décorrélées d'un seul nombre.

```hlsl
return float3(frac(Graine * 7.13), frac(Graine * 3.71), frac(Graine * 11.37));
```

Les multiplicateurs sont premiers entre eux à dessein : trois nombres proches donneraient trois
variations corrélées, et les touffes les plus grandes seraient toutes de la même couleur.
