# Leçon 21 en Unreal 5 — peinture de sommets

Unreal est le seul des trois moteurs à avoir un **outil de peinture de sommets intégré**. C'est un
avantage décisif, et il vaut la peine d'être connu même si tu travailles ailleurs.

## Peindre

1. Sélectionne le maillage dans le niveau.
2. Barre d'outils → **`Mesh Paint`** (ou `Shift + 4` selon la version).
3. Onglet **`Colors`**, mode `Paint`.
4. Choisis les canaux à peindre : les cases **`R`, `G`, `B`, `A`** en bas du panneau. Décoche
   ceux que tu ne veux pas toucher — c'est ce qui permet de peindre le rouge sans effacer
   l'occlusion rangée dans l'alpha.
5. Règle `Paint Color` et `Erase Color`, la taille et la dureté du pinceau, puis peins dans la vue.

Trois choses qui sauvent du temps :

**`Fill`** remplit tout le maillage d'un coup. À faire en premier, pour partir d'une base propre —
typiquement rouge à 1, le reste à 0.

**La peinture est stockée par instance**, pas dans l'asset. Deux copies du même rocher peuvent être
peintes différemment, et le fichier source n'est jamais modifié.

**`Copy` / `Paste` et `Import from TGA`** permettent de transférer une peinture d'une instance à
l'autre, ou de la générer hors ligne.

## La contrainte à connaître

**La résolution de la peinture est celle du maillage.** Un mur de deux triangles n'a que quatre
sommets : tu ne peux y peindre que quatre valeurs, interpolées entre elles.

Unreal propose `Subdivide` dans l'outil, mais c'est un pis-aller : subdiviser pour peindre alourdit
la géométrie partout. La bonne pratique est de **modéliser avec la peinture en tête** — un mur
destiné à recevoir de la mousse au sol a besoin de quelques boucles d'arêtes en bas.

C'est la même limite dans les trois moteurs, et c'est la raison pour laquelle les terrains
utilisent des **cartes de mélange** (textures) plutôt que des couleurs de sommet.

## Le graphe

Le nœud est **`VertexColor`**, avec ses quatre sorties `R`, `G`, `B`, `A`.

**Le mélange à trois couches :**

1. Trois `TextureSampleParameter2D`, une par couche.
2. `VertexColor` → masques `R`, `G`, `B`.
3. Deux `Lerp` en cascade : `Lerp(Lerp(couche0, couche1, R), couche2, G)`.

Cette cascade est la forme la plus courante, et elle a un défaut : les canaux ne sont pas
symétriques — `G` écrase `R`. Pour un vrai mélange pondéré, normalise d'abord :

```hlsl
float somme = Peinture.r + Peinture.g + Peinture.b;
return somme > 0.0001 ? Peinture.rgb / somme : float3(1, 0, 0);
```

**L'occlusion dans l'alpha** : `VertexColor` → `A` → **Ambient Occlusion** du nœud racine.

## Le mélange par hauteur, en nœud Custom

C'est la technique qui fait toute la différence visuelle. Entrées : `Peinture` (Float3),
`Hauteur` (Float3), `Durete` (Float1). Sortie `CMOT Float 3`.

```hlsl
float3 combines = Peinture * (Hauteur + 0.0001);
float maximum = max(combines.r, max(combines.g, combines.b));
float3 retenus = max(combines - (maximum - Durete), 0.0);
float somme = retenus.r + retenus.g + retenus.b;
return somme > 0.0001 ? retenus / somme : float3(1, 0, 0);
```

Voir le `README.md` de la leçon pour ce que ça change — et c'est spectaculaire.

## L'autre outil : `Texture Paint`

Le même mode `Mesh Paint` propose un onglet **`Textures`**, qui peint dans une texture au lieu des
sommets. Résolution indépendante du maillage, au prix d'une texture par objet peint.

La règle de choix :

| Besoin | Outil |
|---|---|
| variation large, gratuite en mémoire | couleurs de sommet |
| détail fin, motif précis | texture peinte |
| terrain, grandes surfaces | cartes de mélange (`Landscape Layer Blend`) |

## Le piège de l'import

Un maillage importé depuis Blender ou Maya perd ses couleurs de sommet si l'option n'est pas
cochée à l'import : `Vertex Color Import Option` → **`Replace`** (et non `Ignore`, qui est parfois
le défaut).

Symptôme : tout est peint en blanc, donc la première couche recouvre tout. Vérifie-le avant de
chercher un bug dans ton matériau — c'est presque toujours ça.
