# 18 — Le bouclier : impacts, hexagones et intersection

## Ce qu'on fabrique

Une bulle d'énergie autour du joueur. Elle est presque invisible au repos — juste un liseré sur la
silhouette — et **révèle sa structure hexagonale quand elle encaisse** : une onde circulaire part
du point d'impact, se propage sur la surface et s'éteint. Là où la bulle traverse le sol ou un mur,
une ligne lumineuse marque l'intersection.

C'est l'effet le plus composite du cours : fresnel (06), transparence additive (07), motif
procédural, tableau d'uniformes (17) et profondeur (11).

## L'idée : un shader qui réagit à des événements

Jusqu'ici, tout dépendait du temps ou de la géométrie. Ici il faut réagir à **ce qui vient de se
passer** — un tir a touché la bulle à tel endroit, à tel instant.

Le shader n'a aucun moyen de le savoir. Le script le lui dit, via un tableau :

```glsl
uniform vec4 impacts[8];
uniform int nombre_impacts;
```

Chaque entrée est `(x, y, z, instant)` : la position de l'impact **en espace objet**, et le moment
où il a eu lieu. Quatre nombres, un `vec4`, comme au pinceau de la leçon 17.

**Pourquoi l'espace objet ?** Parce que le bouclier bouge avec le joueur. En espace monde, l'onde
resterait accrochée à l'endroit du tir pendant que la bulle s'en va. En espace objet, elle reste
collée à la surface. C'est le tableau de décision de `00-bases/02` appliqué directement.

## L'onde

```glsl
for (int i = 0; i < 8; i++) {
    if (i >= nombre_impacts) break;

    float age = TIME - impacts[i].w;
    if (age < 0.0 || age > duree_onde) continue;

    float rayon = age * vitesse_onde;
    float anneau = smoothstep(largeur_onde, 0.0, abs(distance(position_locale, impacts[i].xyz) - rayon));
    onde += anneau * (1.0 - age / duree_onde);
}
```

Trois lignes portent tout l'effet :

**`float rayon = age * vitesse_onde`** — l'onde est un cercle dont le rayon grandit avec le temps.
C'est tout ce qu'est une onde qui se propage.

**`abs(distance(...) - rayon)`** — l'écart entre ma distance à l'impact et le rayon courant. Il
vaut zéro exactement sur le front de l'onde. C'est le même raisonnement qu'à la dissolution de la
leçon 05 : *un contour, c'est une bande de valeurs autour de la valeur critique.*

**`* (1.0 - age / duree_onde)`** — l'extinction. Sans elle, l'onde garde son intensité et disparaît
d'un coup à la fin de sa durée, ce qui se voit.

Le `continue` sur les impacts périmés n'est pas une optimisation cosmétique : sans lui, un impact
vieux de trois minutes produit un anneau de rayon 450, qui traverse la bulle bien après.

**La distance est euclidienne, donc à travers la sphère et non le long de sa surface.** L'onde
s'étale donc un peu plus vite près du point opposé. Personne ne le voit ; la distance géodésique
coûterait un `acos` par pixel.

## La grille hexagonale

```glsl
float grille_hexagonale(vec2 p) {
    vec2 maille = vec2(1.0, 1.7320508);
    vec2 a = mod(p, maille) - maille * 0.5;
    vec2 b = mod(p - maille * 0.5, maille) - maille * 0.5;
    vec2 local = mix(a, b, step(dot(b, b), dot(a, a)));
    return 0.5 - bord_hexagone(local);
}
```

Une grille hexagonale, ce sont **deux grilles rectangulaires décalées d'une demi-maille**. On
calcule la position dans chacune, on garde la plus proche, et le résultat est le centre de
l'hexagone le plus proche.

`1.7320508` est la racine de 3 : le rapport de hauteur d'une maille hexagonale.

```glsl
float bord_hexagone(vec2 p) {
    p = abs(p);
    return max(dot(p, normalize(vec2(1.0, 1.7320508))), p.x);
}
```

La distance au bord d'un hexagone régulier : le maximum entre la distance à ses faces obliques et
à sa face verticale. `abs` gère les quatre symétries d'un coup.

**Le `mix(a, b, step(...))` remplace un `if`**, exactement pour la raison du chapitre
`00-bases/01` : ici les pixels voisins choisissent souvent des grilles différentes, donc un `if`
divergerait à chaque frontière d'hexagone.

**En pratique, une texture d'hexagones est souvent le meilleur choix** : un accès texture contre
une quinzaine d'instructions, et l'artiste peut y peindre de l'usure, des variations, des motifs.
La version procédurale reste imbattable sur un point : elle ne consomme aucune mémoire et reste
nette à toute échelle.

## L'intersection avec le décor

```glsl
float epaisseur = max(profondeur_scene + VERTEX.z, 0.0);
float intersection = 1.0 - clamp(epaisseur / largeur_intersection, 0.0, 1.0);
```

C'est le calcul de la leçon 11, **inversé**. L'écume s'estompait au contact ; ici on s'allume au
contact.

Cette ligne fait une chose que rien d'autre ne fait : elle **pose** la bulle dans la scène. Sans
elle, un bouclier transparent flotte, on ne voit pas où il touche le sol, et l'échelle devient
illisible. Avec, il devient un objet.

C'est le meilleur exemple du cours d'un effet à trois lignes dont l'absence se remarque plus que
la présence.

## Godot

```glsl
render_mode blend_add, unshaded, cull_disabled, depth_draw_never;
```

Les quatre modes de la leçon 07, pour les mêmes raisons — et l'additif règle une fois de plus le
problème de tri, ce qui compte quand la bulle entoure un personnage qui a lui aussi des effets
transparents.

**Le script** (`bouclier.gd`) tient en dix lignes, avec un détail :

```gdscript
_prochain = (_prochain + 1) % MAXIMUM_IMPACTS
```

C'est un **tampon circulaire**. Le neuvième impact écrase le premier, qui est le plus ancien —
donc celui dont l'onde s'est déjà éteinte. Aucun tri, aucune allocation, coût constant. C'est la
bonne structure pour tout ce qui est « les N derniers événements », et elle revient partout en
gameplay.

**`to_local(point_monde)`** convertit la position d'impact en espace objet. Le script fait la
conversion **une fois**, au moment de l'impact ; le shader la referait deux millions de fois.
Toujours convertir du côté le moins cher.

## Unity URP

Le tableau se déclare **hors du `CBUFFER`** :

```hlsl
float4 _Impacts[MAXIMUM_IMPACTS];
int _NombreImpacts;
```

Il est mis à jour par script, pas par matériau : le mettre dans `UnityPerMaterial` casserait le
SRP Batcher, et Unity refuse de toute façon les tableaux dans ce bloc.

**`SetVectorArray` fixe la taille au premier appel.** Passe toujours le tableau complet, jamais
une portion : la taille est verrouillée pour la vie du matériau, et un tableau plus court à un
appel suivant est silencieusement ignoré.

**Le modulo, encore.** La version Unity n'utilise pas `fmod` mais :

```hlsl
float2 ModuloPositif(float2 x, float2 y)
{
    return x - floor(x / y) * y;
}
```

Parce que `fmod` en HLSL garde le signe du dividende : `fmod(-0.2, 1.0)` vaut `-0.2` là où
`mod(-0.2, 1.0)` vaut `0.8` en GLSL. Sur des UV positives ça ne change rien ; sur des coordonnées
centrées, la grille se casse en miroir autour de zéro. C'est le piège annoncé dans
`AIDE-MEMOIRE.md`, et cette leçon est l'endroit exact où il mord.

## Unreal

Voir `unreal.md`. Unreal n'a pas de tableau de paramètres : les trois voies sont un
`Material Parameter Collection` (simple, limité à quelques impacts, mais **global** — un impact
enregistré une fois est visible par tous les matériaux), une texture de données (la voie qui monte
en nombre), ou Niagara.

## Le banc

`banc.gdshader` affiche la bulle à plat, avec des impacts qui tombent tout seuls à intervalles
réguliers, à des positions pseudo-aléatoires.

Trois manipulations :

**Coupe la grille (`force_hexagones` à 0).** L'onde toute seule est plate et un peu triste : elle
n'a rien à révéler. Le motif n'est pas décoratif, il est **ce que l'onde éclaire**. Un bouclier
sans structure n'a pas d'effet d'impact intéressant, quelle que soit la qualité de l'onde.

**Fais varier `largeur_onde`.** Fine, c'est un choc électrique ; large, une déformation
d'énergie. Combinée à `vitesse_onde`, elle donne le caractère : rapide et fine pour un tir
d'énergie, lente et large pour une explosion.

**Regarde deux ondes se croiser.** Baisse `intervalle_impacts` à 0.3 : les anneaux s'additionnent
et produisent des surbrillances aux croisements. C'est physiquement juste — deux ondes
s'additionnent — et c'est joli. Note qu'avec `max` au lieu de `+`, on perdrait ça.

## Les pièges

**L'onde reste accrochée dans le monde pendant que le bouclier bouge.** Position d'impact stockée
en espace monde au lieu d'objet.

**Un anneau géant traverse la bulle sans raison.** Un impact périmé dont l'âge n'est pas testé.

**Rien ne se passe quand j'appelle la fonction d'impact.** Trois causes fréquentes : le tableau
n'est pas retransmis au shader après modification ; `nombre_impacts` est resté à zéro ; ou en
Unity, tu as modifié `sharedMaterial` au lieu de `material`.

**La grille est déformée aux pôles.** Elle est calculée en UV, et l'UV d'une sphère se resserre
aux pôles. Deux remèdes : un maillage à meilleur dépliage (une icosphère), ou un motif calculé en
triplanar depuis la position objet (leçon 13).

**Les hexagones scintillent au loin.** Motif procédural plus fin qu'un pixel. C'est le point où
la texture avec mipmaps l'emporte franchement sur le calcul.

**Le halo d'intersection clignote sur les bords des objets.** La texture de profondeur en filtrage
linéaire — même piège qu'à la leçon 11.

**Le bouclier est invisible sur fond clair.** L'additif ne peut qu'éclaircir. Leçon 07.

## Ce que ça coûte

Le shader est **cher** : fresnel, grille procédurale, boucle sur huit impacts, accès profondeur, le
tout sur une surface qui entoure le joueur et occupe souvent une grande partie de l'écran, avec
`cull_disabled` donc **deux fois**.

Les optimisations qui comptent :

**Ne pas boucler sur les impacts éteints.** Le script peut trier et ne transmettre que les impacts
actifs, en mettant `nombre_impacts` à jour. Au repos, la boucle ne s'exécute pas du tout.

**La grille en texture** plutôt qu'en calcul, dès que l'effet est confirmé.

**Couper les faces arrière** quand le joueur n'est pas dedans. `cull_disabled` n'est utile que si
la caméra est à l'intérieur de la bulle : un test dans le script, deux matériaux, et le coût est
divisé par deux la plupart du temps.

**L'accès profondeur** est le poste fixe. Si tu as déjà activé la texture de profondeur pour
l'écume ou les particules douces, elle est gratuite ; sinon, ce seul effet la fait payer à toute
la scène.

## À toi

1. **Un impact qui déforme la géométrie.** Dans `vertex()`, pousse le sommet le long de sa normale
   selon la même formule d'anneau. La bulle se creuse au point d'impact et l'onde devient une vraie
   vague. Attention à la boîte englobante, comme aux leçons 07 et 09.
2. **Une jauge de santé visible.** Un uniforme `energie` de 0 à 1 qui pilote la couleur (bleu →
   rouge), la force du fresnel et la densité des hexagones. Le bouclier **raconte** son état sans
   interface. C'est ce que font tous les bons jeux.
3. **Un bouclier qui casse.** Combine avec la dissolution de la leçon 05, pilotée par la même
   énergie : à zéro, la bulle se troue et disparaît, bords incandescents compris.
4. **Passe à trente-deux impacts.** Le tableau d'uniformes atteint sa limite : essaie la texture
   de données décrite dans `unreal.md` — huit pixels d'une texture flottante lue avec
   `texelFetch`. Tu découvriras la structure que tous les gros systèmes utilisent.
5. **Mesure la boucle.** Compare huit impacts actifs et zéro, en plein écran. Puis avec et sans
   `cull_disabled`. Deux mesures, deux décisions d'architecture.

**Leçon suivante : 19 — Les Vertex Animation Textures.** Cuire une animation entière dans une
image, et la rejouer sans le moindre os.
