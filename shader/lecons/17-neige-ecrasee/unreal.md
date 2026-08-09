# Leçon 17 en Unreal 5 — neige écrasée

## L'outil natif : `Render Target`

Unreal a tout ce qu'il faut sans plugin.

1. `Content Browser` → clic droit → `Materials & Textures` → **`Render Target`**. Nomme-le
   `RT_Deformation`. Dans ses `Details` : taille `512`, `Render Target Format` sur
   **`RTF_R16f`** — un seul canal en virgule flottante suffit, et c'est quatre fois moins de
   mémoire qu'un RGBA8.
2. Un matériau `M_Pinceau` en `Material Domain` : **`User Interface`** — c'est le domaine des
   matériaux qu'on dessine dans une render target.
3. Un matériau `M_Neige` pour le terrain, qui lit `RT_Deformation`.

## Dessiner dans la render target, depuis une Blueprint

Les deux nœuds à connaître :

**`Draw Material to Render Target`** — dessine un matériau plein cadre. C'est l'équivalent du
`Graphics.Blit` d'Unity : on s'en sert pour l'effacement progressif et pour les pinceaux d'un
coup.

**`Begin Draw Canvas to Render Target`** / `Draw Material` / `End Draw Canvas to Render Target` —
permet de dessiner **plusieurs** éléments dans la même passe, à des positions et tailles
différentes. C'est ce qu'il faut pour poser un pinceau par pied.

Le squelette, dans le `Tick` d'une Blueprint :

```
Begin Draw Canvas to Render Target  (RT_Deformation)
    pour chaque presseur :
        Draw Material  (M_Pinceau, position ecran, taille, MID par presseur)
End Draw Canvas to Render Target
```

**Attention : `Draw Material to Render Target` efface la cible avant de dessiner.** Pour
accumuler, il faut soit passer par le canvas, soit faire un ping-pong entre deux render targets —
comme la version Unity de cette leçon.

## L'effacement progressif

Trois façons, par ordre de préférence :

1. **Ping-pong** entre deux render targets, avec un matériau qui lit l'ancienne et écrit
   `ancienne * persistance + pinceaux`. C'est le plus propre et c'est exactement la version Unity.
2. **Un `Draw Material` plein cadre en mode translucide** avec un noir à faible opacité. Simple,
   mais l'accumulation en 8 bits se bloque : sous une certaine valeur, `x * 0.99` arrondi à `x` et
   la trace ne disparaît jamais. C'est **le** piège de cette leçon, et c'est pour ça que le format
   flottant compte.
3. **Ne pas effacer du tout**, et vider la cible quand le joueur quitte la zone.

## Le terrain : `World Position Offset` et tessellation

Le matériau du terrain lit `RT_Deformation` et branche le résultat sur **World Position Offset**,
avec un `Multiply` par `(0, 0, -1)` — l'axe vertical d'Unreal est Z, et on veut s'enfoncer.

**Le problème est le même que dans les autres moteurs** : un shader de sommets ne peut déplacer
que les sommets qui existent. Un plan de 2×2 sommets ne se creusera pas.

Unreal 5 offre une solution que les autres n'ont pas : **Nanite avec Displacement**. Sur un
maillage Nanite, active `Enable Tessellation` dans les `Details` du matériau, et branche ta
déformation sur **Displacement**. Le moteur subdivise le maillage à la volée, au niveau de détail
nécessaire, et l'empreinte devient une vraie géométrie creusée.

C'est le seul des trois moteurs où l'on n'a pas à subdiviser le terrain à la main.

## La normale

`RT_Deformation` ne contient qu'une hauteur : la normale doit être reconstruite. Le nœud
**`NormalFromHeightmap`** (fonction de matériau) le fait à partir d'une texture de hauteur, d'une
échelle et d'une taille d'échantillon.

Passe-lui la render target, et branche sa sortie sur **Normal** avec `Tangent Space Normal`
décoché — c'est une normale en espace monde.

## L'équivalent en nœud Custom

Le pinceau, entrées `UV` (Float2), `CentreMonde` (Float2), `Zone` (Float4), `Rayon`, `Durete` :

```hlsl
float2 monde = (UV - 0.5) * Zone.zw + Zone.xy;
return smoothstep(Rayon, Rayon * Durete, distance(monde, CentreMonde));
```
