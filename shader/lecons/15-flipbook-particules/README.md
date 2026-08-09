# 15 — Flipbook : jouer une animation rangée dans une image

## Ce qu'on fabrique

Une explosion, une bouffée de fumée, une étincelle, un éclair : une animation de 16, 36 ou 64
images, toutes rangées dans **une seule texture**, jouée par le shader. Plus le mélange entre
images consécutives, qui transforme une animation saccadée en animation fluide.

## L'idée

Une texture de 2048 pixels de côté, découpée en 8 × 8, contient 64 images de 256 pixels. Le shader
choisit laquelle afficher selon le temps.

C'est exactement le `floor` / `fract` de la leçon 02, retourné :

```glsl
vec2 uv_image(vec2 uv, float indice) {
    float total = grille.x * grille.y;
    float i = mod(floor(indice), total);
    vec2 cellule = vec2(mod(i, grille.x), floor(i / grille.x));
    return (uv + cellule) / grille;
}
```

| Ligne | Rôle |
|---|---|
| `mod(floor(indice), total)` | l'index de l'image, qui boucle |
| `mod(i, grille.x)` | la **colonne** : le reste de la division par le nombre de colonnes |
| `floor(i / grille.x)` | la **ligne** : le quotient |
| `(uv + cellule) / grille` | on décale d'une cellule, puis on ramène à la taille d'une cellule |

À la leçon 02, on multipliait l'UV pour répéter une texture. Ici on **divise** pour n'en prendre
qu'un morceau. C'est la même arithmétique dans l'autre sens.

## Le mélange entre images

```glsl
vec4 image = texture(feuille, uv_image(UV, curseur));

if (melanger_images) {
    vec4 suivante = texture(feuille, uv_image(UV, curseur + 1.0));
    image = mix(image, suivante, fract(curseur));
}
```

`fract(curseur)` est la fraction écoulée entre l'image courante et la suivante. On lit les deux et
on interpole.

**Ce que ça change.** Une explosion à 16 images jouée à 30 images par seconde dure une demi-seconde
et saute visiblement. Avec le mélange, elle est fluide. Autrement dit : **le mélange permet de
diviser par deux ou trois le nombre d'images de la feuille**, donc sa résolution, donc sa mémoire.

Le prix : **deux accès texture au lieu d'un**, en permanence.

Ce n'est pas parfait — un mélange linéaire entre deux images très différentes produit un fondu
enchaîné, pas un mouvement. Sur une flamme qui bouge beaucoup, ça se voit. Les moteurs modernes
proposent un mélange guidé par un champ de mouvement rangé dans une seconde texture ; c'est plus
juste et plus cher.

## Godot

```glsl
render_mode blend_add, unshaded, cull_disabled, depth_draw_never;
```

Les quatre modes de la leçon 07, pour les mêmes raisons. `blend_add` parce qu'une explosion émet
de la lumière et que **l'addition n'a pas d'ordre** — c'est le remède au tri des transparents.

Pour de la fumée, qui **cache** au lieu d'éclairer, il faut `blend_mix` et accepter les problèmes
de tri.

**Le décalage par instance :**

```glsl
float depart = decalage_aleatoire * grille.x * grille.y * COLOR.r;
float curseur = TIME * images_par_seconde + depart;
```

Sans lui, dix particules affichent **la même image au même moment**, et l'œil le voit
immédiatement. Le canal rouge de la couleur de sommet sert de graine : les systèmes de particules
de Godot y écrivent une valeur aléatoire par particule si tu l'y mets, ou tu peux la peindre par
instance.

C'est le même problème et la même solution qu'au vent de la leçon 09 : **quand plusieurs copies
d'un effet jouent ensemble, il faut décorréler leurs phases.**

**Le `clamp(uv, 0.001, 0.999)`** empêche le filtrage bilinéaire d'aller mordre sur la cellule
voisine au bord d'une image. Sans lui, on voit un liseré de l'image d'à côté — et avec les
mipmaps, c'est bien pire.

## Unity URP

Une seule différence, et elle est fondamentale :

```hlsl
float ligne = _Grille.y - 1.0 - floor(i / _Grille.x);
```

**L'origine des UV d'Unity est en bas à gauche**, alors que les feuilles d'images sont rangées de
gauche à droite et de **haut** en bas. Sans ce retournement, l'animation joue les lignes à
l'envers : de la dernière ligne vers la première.

C'est le tableau des conventions de `AIDE-MEMOIRE.md`, dans sa manifestation la plus concrète.
Godot et Unreal n'ont pas ce problème, Unity si.

**`Blend One One`** pour l'additif, et la couleur est prémultipliée par l'alpha :

```hlsl
return half4(image.rgb * _Teinte.rgb * _Intensite * image.a, image.a);
```

En additif, la partie transparente d'une image doit contribuer **zéro**. Multiplier la couleur par
l'alpha le garantit, même si la texture a des pixels colorés dans ses zones transparentes — ce qui
est presque toujours le cas après compression.

## Unreal

Voir `unreal.md` : le nœud **`Flipbook`**, et surtout le fait qu'en Unreal on ne fait
généralement pas ça dans le matériau — **Niagara** gère l'index d'image par particule via
`SubUVAnimation`, ce qui règle le décalage par instance sans rien câbler.

## Le banc

`banc.gdshader` fabrique une feuille d'images **procédurale** : chaque cellule contient un anneau
qui grandit et pâlit. Aucune texture à assigner.

À gauche, la feuille entière, avec la cellule courante encadrée en vert. À droite, le résultat
joué, avec une barre de progression qui montre `fract(curseur)`.

Trois observations :

**L'ordre de parcours.** Regarde le cadre vert : il va de gauche à droite, puis passe à la ligne
du dessous. C'est la convention universelle des feuilles d'images, et c'est ce que code
`colonne = mod(i, colonnes)` / `ligne = floor(i / colonnes)`.

**Le mélange, en direct.** Coupe `melanger_images` et remets-le, avec `images_par_seconde` à 4.
Sans mélange, l'animation avance par à-coups. Avec, elle glisse. À 4 images par seconde, la
différence est frappante ; à 30, elle est subtile — d'où la règle : **le mélange sert surtout aux
animations lentes ou aux feuilles peu fournies**.

**La barre de progression** est `fract(curseur)`. Elle montre que le facteur de mélange n'est pas
un réglage : c'est la partie fractionnaire du curseur, gratuite.

## Les pièges

**L'animation joue à l'envers, ou les lignes sont inversées.** L'origine des UV. Unity a besoin du
retournement, pas Godot ni Unreal.

**On voit un liseré de l'image voisine.** Le filtrage déborde. Trois remèdes cumulables : le
`clamp` du shader, une marge vide autour de chaque image dans la feuille, et **désactiver les
mipmaps** sur la feuille — c'est le seul remède complet, parce qu'un mip élevé mélange les
cellules entre elles quoi qu'il arrive.

**Toutes les particules sont synchronisées.** Le décalage par instance manque.

**L'animation ne boucle pas proprement.** La dernière image ne se raccorde pas à la première.
C'est un problème d'animation, pas de shader : soit la feuille est faite pour boucler, soit
l'effet doit mourir avant la fin (une explosion), soit il faut fondre vers la transparence.

**Ça rame avec beaucoup de particules.** Voir plus bas : ce n'est presque jamais le flipbook.

**La feuille pèse 8 mégaoctets.** 64 images en 512 pixels, ça fait 4096 de côté. C'est très
courant et très lourd. Le mélange entre images permet de descendre à 16 images, soit un
quart de la surface.

## Ce que ça coûte

Un ou deux accès texture. Le calcul d'index est négligeable.

**Le coût réel des particules n'est pas là**, et c'est le point important de cette leçon :

**L'overdraw.** Vingt particules de fumée qui se recouvrent, plein écran, c'est vingt fois le
shader sur chaque pixel. C'est le poste dominant, toujours. La mesure : la vue Overdraw de ton
moteur (leçon 07).

**Le remède principal n'est pas dans le shader** : c'est de découper les quads de particules au
plus près de la forme visible. Un quad dont 70 % de la surface est transparente coûte quand même
100 %. Unity et Unreal proposent tous deux une géométrie ajustée automatiquement à l'alpha de la
texture, et le gain est considérable.

**La bande passante texture.** Une feuille de 4096 lue à des UV qui sautent d'une cellule à
l'autre est très mauvaise pour le cache. Ne mets pas plus d'images qu'il n'en faut.

## À toi

1. **Fabrique une feuille.** Blender, EmberGen, ou même une suite de rendus assemblés à la main.
   Seize images en 4 × 4 suffisent pour comprendre. Joue-la, puis coupe le mélange : tu sentiras
   à quel point seize images est peu.
2. **Un flipbook qui ne boucle pas.** Ajoute un uniforme `vie` de 0 à 1 piloté par un script, et
   remplace `TIME * images_par_seconde` par `vie * total`. L'animation joue exactement une fois,
   du début à la fin, en suivant la durée de vie de la particule. **C'est comme ça qu'on fait en
   vrai** — le temps absolu ne sert que pour les boucles.
3. **Une variation par instance sans couleur de sommet.** Utilise la position monde de l'objet
   comme graine : `hash(position.xz)`. Chaque copie démarre à une image différente sans qu'aucune
   donnée n'ait été peinte.
4. **Ajoute le fondu doux de la leçon 11.** Sur de la fumée qui touche le sol. Une ligne, et
   l'effet passe de « rectangle » à « fumée ».
5. **Mesure l'overdraw.** Cinquante particules plein écran, vue Overdraw, note les images par
   seconde. Puis réduis la taille des quads de moitié en agrandissant l'image dans la feuille.
   Le gain te dira où est vraiment le coût.

**Leçon suivante : 16 — Les décalcomanies.** Projeter une texture sur une géométrie qu'on ne
connaît pas : impacts de balle, flaques, graffitis.
