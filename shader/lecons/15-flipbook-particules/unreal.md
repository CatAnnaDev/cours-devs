# Leçon 15 en Unreal 5 — flipbook et particules

## Le nœud tout fait

**`Flipbook`** fait exactement le calcul de la leçon.

| Entrée | Rôle |
|---|---|
| `Animation Phase` | à quelle image on en est, en tours (0 à 1 pour un cycle complet) |
| `Number Of Rows` | lignes de la grille |
| `Number Of Columns` | colonnes |
| `Texture Coordinate` | les UV de départ, laisse vide pour `TexCoord[0]` |

Sortie : les UV à brancher sur le `TextureSample` de la feuille.

Pour un défilement automatique : `Time` → `Multiply` par (images par seconde / nombre total
d'images) → `Animation Phase`.

Il existe aussi **`SubUV_Function`**, plus complète, avec une sortie de mélange entre deux images.

## Le mélange entre images, dans Niagara

Si l'effet est une particule, la bonne façon n'est pas le matériau : c'est le module
**`SubUVAnimation`** de Niagara. Il calcule l'index d'image, gère le mode `Linear` ou
`Random`, et — surtout — expose `SubImageIndex` par particule.

Côté matériau, tu lis alors `Particle SubUV` au lieu de câbler un `Flipbook` : le nœud
**`ParticleSubUV`** renvoie directement la couleur de la bonne image, avec le mélange entre
l'image courante et la suivante si la case `Blend` est cochée sur l'émetteur.

**C'est la voie normale en Unreal**, et elle a un avantage que le matériau seul n'a pas : chaque
particule a son propre index, donc son propre décalage — le problème du « toutes les particules
jouent la même image » est réglé sans rien câbler.

## Le décalage par particule, hors Niagara

Sur un maillage classique, l'équivalent du canal rouge de la couleur de sommet utilisé par les
versions Godot et Unity est **`PerInstanceRandom`** : un nombre pseudo-aléatoire différent par
instance, disponible sur les `Instanced Static Mesh`. Multiplie-le par le nombre d'images et
ajoute-le à la phase.

## Les particules douces

Rappel de la leçon 11 : ajoute un nœud **`DepthFade`** (`FadeDistance` ≈ 30) et multiplie-le à
l'`Opacity`. Sur une fumée qui touche le sol, c'est la différence entre un effet et un rectangle.

## L'équivalent en nœud Custom

Entrées : `UV` (Float2), `Grille` (Float2), `Indice` (Float1). Sortie `CMOT Float 2`.

```hlsl
float total = Grille.x * Grille.y;
float i = fmod(floor(Indice), total);
float colonne = fmod(i, Grille.x);
float ligne = floor(i / Grille.x);
return (clamp(UV, 0.001, 0.999) + float2(colonne, ligne)) / Grille;
```

Note l'absence du `Grille.y - 1 - ...` de la version Unity : Unreal a l'origine des UV **en haut à
gauche**, comme Godot. C'est un des rares endroits où Unity est le moteur qui diffère.
