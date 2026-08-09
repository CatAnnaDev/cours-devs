# 19 — Les Vertex Animation Textures

## Ce qu'on fabrique

Une créature, un drapeau, un tissu, une destruction — animés, **sans le moindre os**. Toute
l'animation est rangée dans une image : une colonne par sommet, une ligne par image. Le shader lit
la bonne case et déplace le sommet.

Ce qu'on y gagne : mille exemplaires animés indépendamment, pour le prix d'un seul appel de rendu
et zéro travail côté processeur.

## L'idée : une texture, ce sont des données

Retour au chapitre `00-bases/01` : *une texture n'est pas une image, c'est un tableau de nombres*.
Cette leçon est celle où l'idée paie le plus.

On range dans une texture, pour chaque sommet et chaque image de l'animation, **sa position**.

```
       sommet 0   sommet 1   sommet 2   ...   sommet 4095
image 0  (x,y,z)   (x,y,z)   (x,y,z)          (x,y,z)
image 1  (x,y,z)   (x,y,z)   (x,y,z)          (x,y,z)
...
image 59 (x,y,z)   (x,y,z)   (x,y,z)          (x,y,z)
```

Une texture de 4096 × 60. Le shader de sommets lit sa case :

```glsl
vec3 lire_position(int sommet, int image) {
    vec3 brut = texelFetch(positions_cuites, ivec2(sommet, image), 0).xyz;
    return mix(borne_min, borne_max, brut);
}
```

**`texelFetch` et non `texture`.** La différence est fondamentale ici : `texelFetch` lit **un
texel précis, par son index entier**, sans filtrage, sans mip, sans normalisation d'UV. C'est
exactement ce qu'on veut — un texel est un sommet, l'interpoler avec son voisin mélangerait deux
sommets sans aucun rapport et déchirerait le maillage.

**`VERTEX_ID`** est le numéro du sommet en cours, fourni par le moteur. C'est la seule fois du
cours où on s'en sert, et c'est ce qui permet à chaque sommet de retrouver sa colonne.

## L'encodage : pourquoi `borne_min` et `borne_max`

Une texture stocke des valeurs entre 0 et 1 (ou des flottants, mais on veut rester compact). Une
position, elle, va de -2 à +3 mètres. On la **normalise** à la cuisson :

```
range = (position - minimum) / (maximum - minimum)
```

et on la décode dans le shader avec un `mix`. Les deux bornes sont calculées sur **toute
l'animation**, pas image par image — sinon la créature changerait d'échelle à chaque image.

C'est la même idée que le `* 2 - 1` d'une normal map : **on utilise l'intervalle disponible au
maximum**, parce que la précision est ce qui manque toujours.

## L'interpolation entre images

```glsl
int image_a = int(mod(floor(curseur), total));
int image_b = int(mod(floor(curseur) + 1.0, total));
float melange = fract(curseur) * float(interpoler);

VERTEX = mix(lire_position(VERTEX_ID, image_a), lire_position(VERTEX_ID, image_b), melange);
```

Exactement le flipbook de la leçon 15, appliqué à des positions au lieu de couleurs. Deux lectures
au lieu d'une, et l'animation devient fluide même à 12 images par seconde cuites.

**Le gain est le même que là-bas, et il est énorme** : avec interpolation, une animation d'une
seconde peut être cuite à 15 images au lieu de 60. La texture est quatre fois plus petite.

**Et les normales aussi** doivent être cuites, dans une seconde texture. Sans elles, l'éclairage
reste celui de la pose de repos et la créature a l'air éclairée par une lumière fixe pendant
qu'elle se tord. C'est très visible, et c'est l'oubli le plus fréquent.

## La cuisson

C'est là que se fait le vrai travail, et il se fait **hors du moteur de rendu**.

**Unity** — `CuiseurVAT.cs` est un script d'éditeur. Sélectionne l'objet, menu `Cours` →
`Cuire une VAT depuis la selection`. Il échantillonne l'`AnimationClip` image par image, appelle
`SkinnedMeshRenderer.BakeMesh` — qui applique le skinning côté processeur et renvoie le maillage
figé — et écrit les deux textures.

Le point à comprendre : **on cuit le résultat du skinning**, pas les os. Peu importe ensuite la
complexité du squelette, le nombre de contraintes ou de blend shapes : à l'arrivée, ce sont des
positions.

**Godot** — `cuire_vat.gd` est un `EditorScript` (ouvre-le dans l'éditeur de script, `Ctrl+Maj+X`
pour l'exécuter). Il cuit depuis une **séquence de maillages** rangée dans `res://vat/mailles` :
un fichier par image, exporté depuis Blender par un export de séquence. C'est plus manuel qu'en
Unity, et c'est la voie la plus fiable, parce qu'elle ne dépend pas de la façon dont Godot importe
les squelettes.

Le script affiche à la fin les trois valeurs à recopier dans le matériau :
`nombre_images`, `borne_min`, `borne_max`.

**Unreal** — voir `unreal.md` : le plugin `Vertex Animation Tools` et le script Houdini officiel
font tout, y compris les modes `Rigid` et `Fluid` qui n'ont pas d'équivalent simple ailleurs.

## Les réglages de texture, qui décident de tout

| Réglage | Valeur | Sans ça |
|---|---|---|
| format | **flottant** (`RGBAHalf`, `RGBF`, `HDR`) | le maillage grouille |
| compression | **aucune** | le maillage grouille beaucoup plus |
| filtrage | **Nearest / Point** | les sommets se mélangent, le maillage se déchire |
| mipmaps | **désactivées** | de loin, tous les sommets fusionnent |
| sRGB | **décoché** | les positions sont tordues par la courbe gamma |

Ces cinq lignes sont plus importantes que le shader. Une VAT qui « tremble légèrement » est
presque toujours une VAT compressée en DXT.

## Ce que ça permet, et qui n'a pas d'équivalent

**Mille créatures animées, une passe de rendu.** Chaque instance reçoit un `decalage_instance`
différent — le même mécanisme qu'aux leçons 09, 15 et 17 — et joue l'animation à sa propre phase.
Comme tout est dans le shader de sommets, le processeur n'a strictement rien à faire.

Un squelette animé, lui, coûte : la mise à jour de la hiérarchie d'os côté processeur, une matrice
par os transmise au GPU, et un appel de rendu par personnage sauf instanciation savante. Au-delà
de quelques dizaines de personnages, la VAT gagne d'un ordre de grandeur.

**Ce qu'on perd** :

| Perdu | Conséquence |
|---|---|
| le mélange entre animations | on ne peut pas fondre une marche vers une course |
| l'animation procédurale | pas d'IK, pas de regard qui suit, pas de ragdoll |
| l'attachement d'objets | on ne peut pas mettre une arme dans la main : il n'y a plus de main |
| la mémoire | 4096 sommets × 60 images × 8 octets ≈ 2 Mo par animation |

C'est un échange, et il est très clair : **la VAT sert aux foules, aux décors animés et aux
effets, pas aux personnages qu'on contrôle**.

## Le banc

`banc.gdshader` montre les deux moitiés du système en même temps.

**À gauche, la texture cuite.** Chaque colonne est un sommet, chaque ligne une image. Le rouge
code la position en X, le vert la position en Y. La ligne encadrée en vert est l'image en cours de
lecture, et elle descend au rythme de l'animation.

**À droite, le maillage reconstruit** — trente-deux points qui forment une forme qui se tord.

Ce qu'il faut y voir :

**Les colonnes sont lisses, les lignes le sont aussi.** Une VAT bien cuite ressemble toujours à un
dégradé doux : les sommets voisins ont des positions voisines, et une image ressemble à la
précédente. **Si ta texture cuite a l'air d'un bruit, c'est que l'ordre des sommets a changé entre
deux images**, et l'animation sera un chaos. C'est le diagnostic le plus rapide qui soit : regarde
la texture.

**Coupe `interpoler`.** À 12 images par seconde, l'animation devient saccadée ; avec, elle est
fluide. Le coût est un `texelFetch` de plus.

**Descends `images_par_seconde` à 2.** Tu vois clairement chaque pose. C'est ce qu'on regarde
quand on cherche à savoir si une animation a été cuite avec assez d'images.

## Les pièges

**Le maillage explose en confettis.** L'ordre des sommets ne correspond pas entre la cuisson et le
rendu. Causes : un import qui réordonne, une soudure de sommets, un LOD différent, ou un maillage
modifié après la cuisson. **C'est le bug numéro un**, et il est brutal : soit ça marche, soit c'est
méconnaissable.

**Le maillage grouille.** Texture compressée, ou en 8 bits.

**Le maillage se déchire par endroits.** Filtrage interpolant au lieu de `Nearest`, ou UV mal
centrées sur le texel (le `+ 0.5` de `unreal.md`).

**L'éclairage ne suit pas.** Les normales ne sont pas cuites, ou pas lues.

**La créature disparaît au bord de l'écran.** Boîte englobante, comme aux leçons 07, 09 et 17. Une
VAT sort souvent **beaucoup** plus de sa boîte de repos que les autres effets.

**L'animation saute au bouclage.** La dernière image cuite est identique à la première, donc on la
voit deux fois. Cuis `N` images pour un cycle de `N`, en échantillonnant à `image * duree / N` — et
non `image * duree / (N-1)`. C'est ce que fait le cuiseur fourni.

**La texture fait 4096 × 60 et le maillage 12 000 sommets.** La largeur maximale d'une texture est
souvent 16384, mais beaucoup de plateformes s'arrêtent à 8192. Au-delà, il faut découper sur
plusieurs lignes — et le shader doit alors calculer sa case en deux dimensions.

## Ce que ça coûte

**Deux `texelFetch` par sommet** (quatre avec les normales interpolées). C'est plus cher qu'un
skinning classique **par sommet** — mais le skinning classique coûte aussi côté processeur, et
c'est là que se joue la comparaison.

| | VAT | Squelette |
|---|---|---|
| par sommet, GPU | 2 à 4 accès texture | 4 multiplications de matrices |
| par personnage, CPU | **rien** | mise à jour de la hiérarchie d'os |
| par personnage, transfert | **rien** | une matrice par os |
| appels de rendu pour 1000 | **1** | 1000, ou moins avec instanciation |
| mémoire | 2 Mo par animation | quelques kilo-octets |

**Le point de bascule est autour de quelques dizaines d'instances.** En dessous, le squelette est
plus souple et moins coûteux en mémoire. Au-dessus, la VAT écrase tout.

**L'accès texture en vertex** est le point de vigilance sur mobile, comme à la leçon 17 : certaines
architectures ont moins d'unités de texture disponibles côté sommets.

## À toi

1. **Cuis un drapeau.** Une simulation de tissu de 60 images sur une grille de 32 × 32, exportée
   en séquence. C'est le cas le plus simple et le plus démonstratif : aucun squelette n'aurait pu
   le faire.
2. **Mille instances, mille phases.** Un `decalage_instance` tiré au hasard par instance. Regarde
   le compteur : il ne bouge presque pas. C'est le moment où l'on comprend pourquoi cette
   technique existe.
3. **Compare avec un squelette.** Mets cent personnages animés classiquement, note les images par
   seconde et le temps processeur. Refais avec des VAT. Note le point de bascule sur **ta**
   machine — c'est un chiffre qui décide d'architectures entières.
4. **Cuis une destruction.** Une simulation de fracture donne des morceaux rigides. Cuite en VAT,
   elle se rejoue à volonté, à coût nul, et se déclenche par un simple uniforme de temps.
5. **Regarde tes textures.** Ouvre une VAT cuite dans un visualiseur d'image. Si elle est douce,
   elle est bonne ; si elle est bruitée, l'ordre des sommets a changé. Prends l'habitude de ce
   coup d'œil avant de déboguer quoi que ce soit.

**Leçon suivante : 20 — L'instanciation.** Mille objets, une passe, et chacun différent.
