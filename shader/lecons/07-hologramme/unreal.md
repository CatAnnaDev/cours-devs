# Leçon 07 en Unreal 5 — hologramme

## Les réglages du matériau

Sur le nœud racine, `Details` :

| Réglage | Valeur | Pourquoi |
|---|---|---|
| `Material Domain` | `Surface` | |
| `Blend Mode` | `Additive` | un hologramme ajoute de la lumière, il n'en cache pas |
| `Shading Model` | `Unlit` | il ne reçoit pas les lumières de la scène |
| `Two Sided` | coché | on voit l'intérieur à travers |
| `Translucency Sort Priority` | à régler si l'objet se mélange mal avec d'autres translucides | voir le `README.md` |

Dans la section `Translucency` : `Disable Depth Test` **décoché** en général. Coché, l'hologramme
se dessine par-dessus tout, y compris les murs — utile pour un marqueur d'interface, faux pour un
objet posé dans la scène.

## Le graphe

**Le fresnel** — comme à la leçon 06 : nœud `Fresnel`, `ExponentIn` = `ScalarParameter`
**PuissanceFresnel**, `BaseReflectFractionIn` = `0`.

**Les lignes de balayage.**

1. `ObjectLocalPosition` (ou `LocalPosition`) → `ComponentMask` sur `B` (l'axe vertical d'Unreal
   est Z).
2. `Multiply` par `ScalarParameter` **DensiteLignes**.
3. `Add` avec (`Time` × **VitesseLignes**).
4. `Frac`.
5. `SmoothStep` `Min 0`, `Max 0.45`.
6. `Lerp` : `A` = `1`, `B` = le résultat, `Alpha` = **ForceLignes**.

**Le balayage large.** Même chaîne, mais avec une densité très faible (`0.35`) et un `Power` de
`12` au lieu du `SmoothStep`. Le `Power` élevé écrase tout sauf le sommet de la rampe : il ne
reste qu'une bande fine qui monte.

**Le total.** `OpaciteBase` + `Fresnel` + (`Balayage` × **ForceBalayage**), le tout `Multiply` par
les lignes. Ce résultat va :

- `Multiply` par un `VectorParameter` **Couleur** → **Emissive Color** ;
- directement → **Opacity**.

## Le glitch : `World Position Offset`

C'est l'entrée du nœud racine qui correspond au shader de sommets. Elle attend un **déplacement
en espace monde**, en centimètres.

1. `ObjectLocalPosition` → masque `B` → `Multiply` par **HauteurBandes** → `Floor`.
   C'est le numéro de la bande horizontale.
2. `Time` → `Multiply` par **FrequenceGlitch** → `Floor`. C'est le numéro du « pas » temporel :
   le glitch change d'aspect ce nombre de fois par seconde, au lieu de trembler en continu.
3. Combine les deux (`Multiply` le pas par `13.37`, puis `Add`) et passe le résultat dans une
   fonction de hachage. Unreal n'a pas de nœud `hash` : utilise un `Custom`.

```hlsl
float bruit = frac(sin(Graine * 91.7) * 43758.5453);
float actif = step(0.93, bruit);
return actif * Force * (frac(sin(Graine * 27.3) * 21031.1) * 2.0 - 1.0);
```

4. Le résultat, multiplié par un vecteur `(1, 0, 0)`, va dans **World Position Offset**.

**Attention aux unités** : `ForceGlitch` valait `0.04` en Godot et Unity, c'est-à-dire 4
centimètres. En Unreal, écris `4`, pas `0.04`.

**Attention aussi aux ombres et au culling** : un objet déplacé par `World Position Offset` sort
de sa boîte englobante calculée par le moteur, ce qui peut le faire disparaître au bord de
l'écran ou tronquer son ombre. Le remède est le champ `Bounds Scale` du composant de maillage
statique : monte-le à `1.2` pour un décalage de quelques centimètres.

## Le nœud `Fresnel` et la translucidité

En `Additive`, `Opacity` ne contrôle pas une transparence mais **combien on ajoute**. Le noir est
donc invisible et il n'y a pas de tri à faire — c'est ce qui rend l'additif si commode pour les
effets. Le prix : un hologramme additif ne peut jamais être plus sombre que ce qu'il y a derrière.
Pour un hologramme qui assombrit (un fantôme, une ombre projetée), passe en `Translucent` et
accepte les problèmes de tri décrits dans le `README.md`.
