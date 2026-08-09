# 07 — L'hologramme

## Ce qu'on fabrique

Un objet translucide bleu, parcouru de fines lignes horizontales qui défilent, avec une bande
lumineuse qui le balaie de bas en haut, un contour qui s'allume sur la silhouette, et de temps en
temps une tranche qui se décale d'un cran. Le projecteur de mission, le fantôme, le hologramme de
personnage, la prévisualisation de construction.

**C'est le premier effet composé du cours.** Aucune de ses quatre couches n'est nouvelle : c'est
le fresnel de la leçon 06, le défilement de la leçon 03, un masque de la leçon 04, et une
déformation de sommets. Ce que la leçon apprend, c'est **comment on les empile** — et le vrai
problème que pose la transparence.

## L'idée : quatre couches, une seule intensité

```glsl
float intensite = (opacite_base + fresnel + balayage * force_balayage) * lignes;
```

Une seule ligne, et toute la structure de l'effet est dedans. Lis-la comme une recette :

| Terme | Rôle | Opération |
|---|---|---|
| `opacite_base` | l'objet existe, même là où rien ne se passe | plancher |
| `+ fresnel` | la silhouette s'allume | **addition** : ça s'ajoute à ce qui est là |
| `+ balayage` | une bande passe | **addition** |
| `* lignes` | les rayures creusent des trous | **multiplication** : ça retire |

**La règle générale, et elle vaut pour tous les effets composés :**

> Ce qui **ajoute de la lumière** s'additionne. Ce qui **retire de la matière** se multiplie.

Se tromper d'opérateur est l'erreur la plus fréquente quand on empile des couches. Des rayures
additionnées éclaircissent l'objet au lieu de le strier ; un fresnel multiplié éteint tout le
reste dès qu'on regarde de face.

Et l'intensité sert **deux fois** : pour la couleur et pour l'opacité. C'est ce qui donne la
cohérence — les zones sombres sont aussi les zones transparentes. Un hologramme dont l'alpha est
constant a l'air d'une décalcomanie.

## Godot

```glsl
render_mode blend_add, unshaded, cull_disabled, depth_draw_never;
```

Quatre modes, quatre décisions, toutes nécessaires :

| Mode | Effet | Si tu l'enlèves |
|---|---|---|
| `blend_add` | la couleur s'**ajoute** au fond | l'hologramme cache ce qu'il y a derrière |
| `unshaded` | les lumières de la scène sont ignorées | l'hologramme s'assombrit dans le noir |
| `cull_disabled` | les faces arrière sont dessinées | l'intérieur est vide, l'objet a l'air creux |
| `depth_draw_never` | il n'écrit pas dans le tampon de profondeur | il se cache lui-même par morceaux |

Le dernier mérite un mot. Un objet transparent qui **écrit** sa profondeur empêche les parties
situées derrière lui — y compris ses propres faces arrière — d'être dessinées. On voit alors des
morceaux manquants selon l'angle. Règle générale : **un objet transparent ne doit pas écrire dans
le tampon de profondeur.**

**Les lignes.**

```glsl
float lignes = fract(position_locale.y * densite_lignes + TIME * vitesse_lignes);
lignes = mix(1.0, smoothstep(0.0, 0.45, lignes), force_lignes);
```

`fract` d'une position multipliée produit une rampe qui se répète : c'est la répétition de la
leçon 02, appliquée à une coordonnée 3D au lieu d'un UV. `smoothstep(0, 0.45, ...)` transforme la
rampe en une bande adoucie. Et le `mix(1.0, ..., force_lignes)` permet d'atténuer l'effet sans
toucher au reste : à force 0, `lignes` vaut 1 partout, donc la multiplication ne change rien.

**Cette forme, `mix(neutre, effet, force)`, est le bon patron pour tout paramètre d'intensité.**
Elle garantit qu'à force nulle, la couche disparaît vraiment.

**Pourquoi la position locale et pas l'UV ?** Parce que les rayures d'un hologramme sont
horizontales **dans le monde**, indépendamment du dépliage du modèle. En UV, elles suivraient les
coutures et partiraient dans tous les sens sur un personnage. La position locale (celle du
maillage, avant transformation) donne des rayures qui montent bien selon l'axe de l'objet, et qui
tournent avec lui.

**Le balayage.**

```glsl
float balayage = pow(fract(position_locale.y * 0.35 - TIME * vitesse_balayage), 12.0);
```

Même rampe, mais avec une densité très basse — une seule bande sur toute la hauteur — et un
`pow` de 12 qui écrase tout sauf le sommet. Il reste un pic fin qui monte lentement. **`pow` sur
une rampe est la façon la moins chère de fabriquer un pic.**

**Le glitch, dans le shader de sommets.**

```glsl
void vertex() {
    float bande = floor(VERTEX.y * hauteur_bandes);
    float pas = floor(TIME * frequence_glitch);
    float actif = step(0.93, bruit1(bande + pas * 13.37));

    VERTEX.x += actif * force_glitch * (bruit1(bande + pas) * 2.0 - 1.0);
}
```

Trois idées, chacune réutilisable ailleurs :

1. **`floor(position * n)` découpe l'objet en tranches.** Tous les sommets d'une même tranche
   reçoivent le même numéro, donc le même décalage : ils se déplacent ensemble, ce qui donne une
   franche cassure au lieu d'une bouillie.
2. **`floor(TIME * f)` fait avancer le temps par crans.** Sans lui, le décalage varierait en
   continu et l'objet tremblerait. Avec lui, il saute `f` fois par seconde et se tient immobile
   entre deux. **La discontinuité est ce qui fait « numérique ».**
3. **`step(0.93, hasard)` ne déclenche qu'une fois sur quinze.** Un glitch permanent n'est plus un
   glitch, c'est une texture. La rareté fait l'effet.

## Unity URP

```hlsl
Blend One One
ZWrite Off
Cull Off
```

`Blend One One` est l'additif : `resultat = source * 1 + destination * 1`. Les autres mélanges
courants :

| Écriture | Nom | Usage |
|---|---|---|
| `Blend One One` | additif | lumière, feu, hologramme, magie |
| `Blend SrcAlpha OneMinusSrcAlpha` | alpha classique | vitre, fumée, décalcomanie |
| `Blend One OneMinusSrcAlpha` | prémultiplié | le plus correct pour composer, mais demande des textures préparées |
| `Blend DstColor Zero` | multiplicatif | assombrir, teinter |

Et les tags :

```
Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
```

`Queue = Transparent` (valeur 3000) place l'objet **après tous les opaques**, et le fait trier de
l'arrière vers l'avant. C'est indispensable et insuffisant — voir la section suivante.

**Le glitch est fait dans `vert`, sur `positionOS`**, avant `GetVertexPositionInputs`. Note qu'on
garde aussi la position d'origine dans `OUT.positionOS` pour les rayures : sinon les rayures
sauteraient avec le glitch, ce qui annule l'effet de tranche décalée.

C'est un détail à cinq caractères, et c'est le genre de chose qui distingue un effet qui marche
d'un effet qui a l'air juste.

## Unreal

Voir `unreal.md` : `Blend Mode: Additive`, `World Position Offset` pour le glitch, et surtout
**les unités** — `ForceGlitch` vaut `4` en Unreal là où elle vaut `0.04` ailleurs, parce
qu'Unreal compte en centimètres.

## Le banc

`banc.gdshader` monte l'hologramme sur la sphère analytique de la leçon 06. Aucun maillage, aucune
transparence : c'est la maths des couches, isolée.

L'exercice à faire dans le banc, et il vaut plus qu'une lecture : **mets `force_lignes` à 0, puis
`force_balayage` à 0, puis `opacite_base` à 0.** Regarde chaque couche seule, puis rallume-les une
par une. C'est le réflexe 5 du chapitre `00-bases/04`, et c'est comme ça qu'on règle un effet
composé — jamais en bougeant dix curseurs à la fois.

## Le vrai sujet : pourquoi la transparence est un problème

Un objet opaque, le GPU sait quoi en faire : il compare sa profondeur à ce qui est déjà écrit, et
il garde le plus proche. L'ordre de dessin n'a aucune importance.

Un objet transparent doit être **mélangé** avec ce qu'il y a derrière. Donc ce qu'il y a derrière
doit avoir été dessiné avant. Donc **l'ordre compte**. Et le moteur ne peut trier que les objets
entiers, par la distance de leur centre à la caméra.

Ce qui casse, en pratique :

**Deux transparents qui s'interpénètrent** clignotent ou passent l'un devant l'autre quand la
caméra bouge : leurs centres changent d'ordre alors que leur géométrie, elle, s'entremêle.

**Un objet transparent concave se cache lui-même.** Ses propres faces arrière et avant ne sont
pas triées entre elles. Une sphère creuse transparente montre son intérieur par plaques.

**Le tri se fait au centre.** Un long objet transparent qui traverse la scène est classé par son
milieu, ce qui est faux pour ses deux extrémités.

Les remèdes, du plus simple au plus lourd :

| Remède | Coût | Quand |
|---|---|---|
| **utiliser l'additif** | gratuit | tout ce qui émet de la lumière : l'addition est commutative, donc l'ordre ne compte plus |
| priorité de tri manuelle | gratuit | quand deux objets précis sont mal classés (`Render Queue` Unity, `Render Priority` Godot, `Translucency Sort Priority` Unreal) |
| découper le modèle | temps d'artiste | un objet convexe se trie tout seul |
| passer en `Masked` | perte de finesse | un contour dur mais un tri parfait — c'est la leçon 05 |
| tri par pixel (OIT) | très cher | rarement dans un jeu |

**Retiens le premier**, c'est celui qui explique pourquoi tant d'effets de jeu sont additifs :
non pas pour l'esthétique, mais parce que **l'addition n'a pas d'ordre**. `a + b = b + a`. Le
mélange alpha, lui, n'est pas commutatif, et c'est toute la difficulté.

## En 2D

Les rayures et le balayage se calculent directement sur `UV.y` au lieu de la position locale : sur
un sprite, l'UV **est** la coordonnée verticale de l'objet.

Le glitch, lui, ne déplace pas les sommets — il décale **les UV par bande** :

```glsl
vec2 uv = vec2(clamp(UV.x + decalage, 0.0, 1.0), UV.y);
```

Résultat visuel identique, et deux avantages : aucun problème de boîte englobante (leçon 07,
« Les pièges »), et ça marche sur un sprite qui n'a que quatre sommets.

Le `clamp` n'est pas décoratif : sans lui, une bande décalée va lire le sprite voisin dans
l'atlas.

Le tri des transparents reste exactement le même problème qu'en 3D — sauf qu'en 2D il se règle
avec l'ordre des calques, ce qui est autrement plus simple.

## Les pièges

**L'hologramme disparaît quand la caméra tourne.** Il est trié derrière un autre transparent.
Priorité de tri, ou additif.

**On voit des morceaux manquants sur l'objet lui-même.** `depth_draw_never` / `ZWrite Off` oublié.

**L'hologramme est invisible sur un fond clair.** L'additif ne peut qu'éclaircir. Sur un ciel
blanc, il n'y a plus rien à éclaircir. Si ton jeu a des fonds clairs, il faut un mélange alpha —
et accepter les problèmes de tri.

**Le glitch fait disparaître l'objet quand il sort de l'écran.** Le déplacement de sommets sort
de la boîte englobante que le moteur utilise pour décider s'il faut dessiner l'objet. Godot :
augmente le `Custom AABB` du `MeshInstance3D`. Unity : `Bounds` du mesh, ou coche
`Skinned Motion Vectors` / élargis via un script. Unreal : `Bounds Scale`.

**Les rayures ondulent et scintillent à distance.** Une rayure fine devient plus petite qu'un
pixel : c'est de l'aliasing pur. Réduis `densite_lignes` avec la distance, ou adoucis avec
`fwidth` comme au chapitre `00-bases/04`.

**Les rayures suivent les coutures du modèle.** Tu les as calculées en UV. Passe en position
locale.

## Ce que ça coûte

Le calcul est négligeable : un `dot`, deux `pow`, deux `fract`. **Le coût est ailleurs, et il est
réel.**

Un objet transparent :

- ne bénéficie d'**aucun** test de profondeur anticipé : chaque pixel est calculé, même caché ;
- se paie autant de fois qu'il y a de couches empilées — c'est l'*overdraw*, mesuré par la vue
  correspondante dans chaque moteur ;
- avec `cull_disabled`, coûte **deux fois** : faces avant et faces arrière.

Un hologramme de personnage en plein écran, avec deux faces, c'est quatre millions d'exécutions
du shader de fragments en 1080p. Ce n'est pas grave pour un objet ; c'est un mur pour dix.

**La mesure à connaître dans chaque moteur** : Godot, `Debug` → `View Overdraw`. Unity, mode de
dessin `Overdraw` dans la vue Scene. Unreal, `Optimization Viewmodes` → `Quad Overdraw`. Regarde
ton hologramme dedans une fois : tu ne verras plus jamais la transparence de la même façon.

## À toi

1. **Ajoute une couche.** Un bruit lent qui module l'opacité — l'hologramme « respire » et perd
   par moments de la stabilité. Choisis toi-même l'opérateur, addition ou multiplication, et
   justifie-le avant d'essayer.
2. **Fais monter l'hologramme.** Combine avec la dissolution de la leçon 05 : un seuil sur la
   position locale en Y fait apparaître l'objet de bas en haut avec un bord lumineux, comme un
   projecteur qui s'allume. Deux effets déjà écrits, un résultat que personne ne devine composé.
3. **Passe-le en mélange alpha** et provoque le bug de tri : deux hologrammes qui se croisent,
   et tourne autour. Note à quel angle exact ça saute. Puis reviens en additif. Un bug qu'on a
   vu une fois se diagnostique en trois secondes la fois suivante.
4. **Rends le glitch dépendant d'un paramètre.** Un uniforme `sante` entre 0 et 1 qui augmente la
   fréquence et la force du glitch quand il baisse. C'est exactement ce que fait un jeu quand le
   hologramme est « instable » — et ça montre qu'un shader bien paramétré raconte quelque chose.
5. **Mesure l'overdraw.** Mets dix hologrammes qui se recouvrent, ouvre la vue d'overdraw de ton
   moteur, et note les images par seconde. Puis enlève `cull_disabled` et recommence. Tu viens de
   mesurer le prix exact d'une décision de deux mots.

**Leçon suivante : 08 — Toon shading et contour.** On arrête de contourner l'éclairage et on
commence à en écrire.
