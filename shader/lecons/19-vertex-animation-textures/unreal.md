# Leçon 19 en Unreal 5 — Vertex Animation Textures

## L'outil officiel

Unreal fournit le pipeline complet : le plugin **`Vertex Animation Tools`** côté moteur, et le
script **`SideFX Labs → Vertex Animation Textures`** côté Houdini, qui exporte le maillage et les
textures directement au format attendu.

Il gère quatre modes, et la distinction vaut d'être connue :

| Mode | Ce qui est cuit | Pour quoi |
|---|---|---|
| **Soft** | la position de chaque sommet, image par image | tissu, drapeau, personnage déformé |
| **Rigid** | la position **et la rotation** de morceaux rigides | destruction, débris |
| **Fluid** | positions + topologie qui change | liquides, fumée simulée |
| **Sprite** | des cartes de billboards | particules très nombreuses |

Le mode `Soft` correspond à ce que fait cette leçon. Les trois autres n'ont pas d'équivalent
simple dans les autres moteurs.

## Le matériau, à la main

Si tu ne veux pas passer par Houdini :

1. Nœud **`VertexID`** — Unreal l'expose directement dans le graphe, contrairement à Unity où il
   faut déclarer la sémantique.
2. `Divide` par le nombre de sommets → la coordonnée horizontale.
3. Le numéro d'image : `Time` × images par seconde, `Frac` du tout divisé par le nombre d'images
   → la coordonnée verticale.
4. `AppendVector` des deux → `TextureSample` de la texture de positions, en mode
   **`Sampler Type: Linear Color`** et **filtrage `Nearest`**.
5. `Lerp` entre `BorneMin` et `BorneMax` pour décoder.
6. `Subtract` la position d'origine (`LocalPosition`) — parce que `World Position Offset` attend un
   **déplacement**, pas une position absolue.
7. `TransformVector` de `Local` vers `World`, puis → **World Position Offset**.

L'étape 6 est la différence structurelle avec Godot et Unity : là-bas on **remplace** `VERTEX`,
ici on ne peut qu'**ajouter** un décalage. Le résultat est identique, mais il faut soustraire la
position de repos.

## Les réglages de la texture

Ils comptent plus que le graphe, et une erreur ici donne une animation qui tremble sans qu'on
sache pourquoi :

| Réglage | Valeur | Pourquoi |
|---|---|---|
| `Compression Settings` | **`HDR (RGBA16F)`** ou `VectorDisplacementmap` | une position compressée en DXT donne un maillage qui vibre |
| `Mip Gen Settings` | **`NoMipmaps`** | un mip mélange des sommets voisins, ce qui n'a aucun sens |
| `Filter` | **`Nearest`** | un texel = un sommet, pas d'interpolation |
| `sRGB` | **décoché** | ce sont des nombres, pas des couleurs |
| `Texture Group` | `UI` ou `VFX` | évite qu'un réglage de plateforme réduise la résolution |

Le plus fatal est la compression : une VAT en DXT1 est inutilisable, et le symptôme — un maillage
qui grouille légèrement — ressemble à un bug de shader.

## Les bornes

Le maillage animé sort de sa boîte englobante. Comme aux leçons 07 et 09 : `Bounds Scale` sur le
composant. Pour une VAT, il faut souvent monter à `3` ou plus, parce que l'amplitude d'animation
peut être bien supérieure à la pose de repos.

Sans ça, la créature disparaît dès qu'elle sort du champ, et son ombre est tronquée.

## L'équivalent en nœud Custom

Entrées : `SommetID` (Float1), `Image` (Float1), `NombreSommets`, `NombreImages`.
Sortie `CMOT Float 2`.

```hlsl
return float2((SommetID + 0.5) / NombreSommets, (Image + 0.5) / NombreImages);
```

Le `+ 0.5` vise **le centre** du texel. Sans lui, avec un filtrage même légèrement interpolant, on
lit à cheval sur deux sommets et le maillage se déchire. C'est le piège numéro un de toutes les
textures de données, et il vaut aussi pour la leçon 15.
