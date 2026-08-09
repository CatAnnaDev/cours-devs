# 14 — Parallax et POM : creuser sans géométrie

## Ce qu'on fabrique

Un mur plat — deux triangles — qui a de vraies briques en relief. Quand on bouge la caméra, les
briques se **décalent les unes par rapport aux autres**, les joints s'enfoncent, et on voit
apparaître ce qui était caché derrière une arête. Aucun triangle en plus.

Pavés, tôle ondulée, écailles, rainures, sol de grotte : c'est l'effet qui transforme un décor
plat en décor épais, pour le prix de quelques échantillons de texture.

## L'idée

Une normal map ment sur l'**orientation** de la surface. Elle donne à l'éclairage l'aspect du
relief, mais rien ne bouge quand on se déplace : la surface reste désespérément plate dès qu'on
la regarde de biais.

Le parallax ment sur la **position**. Le principe :

> Le pixel que je suis en train de dessiner est à la surface. Mais si la surface était réellement
> creusée, le rayon qui vient de mon œil serait entré dans le creux et aurait touché un point
> **décalé**. Va donc lire la texture à cet endroit-là.

Et le décalage dépend de l'angle de vue — c'est ce qui fait tout l'effet.

## Version 1 : le parallax simple

```glsl
float hauteur = texture(carte_hauteur, uv).r;
uv -= vue_tangente.xy * (1.0 - hauteur) * profondeur;
```

Une ligne. On lit la hauteur au point d'origine, et on décale les UV proportionnellement à sa
profondeur et à l'inclinaison du regard.

C'est faux — on utilise la hauteur du **mauvais** point pour décider de combien décaler — mais
c'est bluffant pour des reliefs peu profonds, et ça ne coûte **qu'un seul échantillon
supplémentaire**. Pour du carrelage ou une texture de sol vue d'en haut, c'est souvent le bon
choix.

Ça s'écroule dès que le relief est profond ou l'angle rasant : le décalage devient énorme et la
texture se met à nager.

## Version 2 : le POM (Parallax Occlusion Mapping)

Au lieu de deviner, on **marche le long du rayon** jusqu'à toucher la surface.

```glsl
float couches = mix(couches_max, couches_min, abs(vue.z));
float pas_profondeur = 1.0 / couches;
vec2 pas_uv = vue.xy * profondeur / couches;

float profondeur_courante = 0.0;
vec2 uv = uv_depart;
float hauteur = 1.0 - texture(carte_hauteur, uv).r;

for (int i = 0; i < 128; i++) {
    if (profondeur_courante >= hauteur || float(i) >= couches) break;
    uv -= pas_uv;
    hauteur = 1.0 - texture(carte_hauteur, uv).r;
    profondeur_courante += pas_profondeur;
}
```

Lis-le comme une descente d'escalier. À chaque pas, on avance d'un cran en profondeur et d'un cran
en UV le long du rayon. On compare **où on est** (`profondeur_courante`) à **où est la surface à
cet endroit** (`hauteur`). Tant qu'on est au-dessus, on continue ; dès qu'on est passé dessous,
on s'arrête : le rayon vient de traverser la surface.

**L'interpolation finale** rattrape l'erreur de discrétisation :

```glsl
vec2 uv_precedent = uv + pas_uv;
float ecart_apres = hauteur - profondeur_courante;
float ecart_avant = (1.0 - texture(carte_hauteur, uv_precedent).r) - profondeur_courante + pas_profondeur;
float poids = ecart_apres / max(ecart_apres - ecart_avant, 0.0001);
return mix(uv, uv_precedent, clamp(poids, 0.0, 1.0));
```

On a un point juste avant l'intersection et un juste après. On cherche entre les deux le point où
les deux courbes — celle du rayon et celle de la surface — se croisent exactement. Sans cette
interpolation, le relief a des **marches d'escalier** très visibles, surtout en mouvement.

**Le nombre de couches varie avec l'angle :**

```glsl
float couches = mix(couches_max, couches_min, abs(vue.z));
```

`vue.z` proche de 1 signifie qu'on regarde la surface de face : le rayon traverse peu de matière,
huit pas suffisent. `vue.z` proche de 0, on rase la surface : le rayon parcourt une longue
distance, il en faut trente-deux. **Adapter l'effort à l'angle est ce qui rend le POM utilisable**
— à couches fixes, il faut prendre le pire cas partout.

## Godot

```glsl
mat3 tbn = mat3(TANGENT, BINORMAL, NORMAL);
vec3 vue_tangente = normalize(VIEW * tbn);
```

Toute la leçon 10 sert ici. On a besoin de la direction de vue **en espace tangent**, parce que le
décalage se fait en UV et que les UV sont les axes de cet espace.

`VIEW * tbn` — et non `tbn * VIEW`. En GLSL, multiplier un vecteur **à gauche** d'une matrice
revient à multiplier par sa transposée, ce qui est exactement l'opération inverse : passer de
l'espace vue vers l'espace tangent. C'est une astuce d'écriture classique, et c'est le genre de
détail où inverser donne un résultat plausible mais faux.

**Les trois vecteurs `TANGENT`, `BINORMAL`, `NORMAL` sont en espace vue** dans le `fragment()` de
Godot, et `VIEW` aussi : ils sont cohérents, le calcul est correct.

**Un seul `discard` optionnel** pour le bord :

```glsl
if (couper_les_bords && (uv.x < 0.0 || uv.x > 1.0 || ...)) discard;
```

À angle rasant, le rayon peut sortir des UV du modèle et lire n'importe quoi. Le couper évite la
bavure — mais laisse un trou. C'est un choix, pas une correction ; voir « Les pièges ».

## Unity URP

```hlsl
float3x3 versTangente = float3x3(
    normalize(IN.tangentWS),
    normalize(IN.bitangentWS),
    normalize(IN.normalWS));

float3 vueTS = normalize(mul(versTangente, vueWS));
```

Ici l'ordre est `mul(matrice, vecteur)` : la matrice a `T`, `B`, `N` en **lignes**, donc le
produit donne les trois produits scalaires — les composantes de la vue dans le repère tangent.
C'est l'inverse du `mul(normalTS, versMonde)` de la leçon 10, et c'est cohérent : **une matrice
orthonormée transposée est son inverse.**

**`[loop]` devant la boucle** demande explicitement au compilateur HLSL de faire une vraie boucle
plutôt que de la dérouler. Sans cette annotation, il tente de dérouler 128 itérations, ce qui
produit un shader énorme et une compilation qui prend plusieurs secondes — quand elle n'échoue
pas.

**`clip(float4(uv, 1.0 - uv))`** est une astuce compacte : `clip` jette le pixel si **n'importe
laquelle** des composantes est négative. Les quatre tests de bord tiennent en un appel.

## Unreal

Voir `unreal.md` : le nœud **`BumpOffset`** pour la version simple, la fonction
**`ParallaxOcclusionMapping`** pour la complète, et surtout la sortie **`Pixel Depth Offset`** —
qu'aucun des deux autres moteurs n'expose aussi simplement, et qui règle la moitié des limites de
cette leçon.

## Le banc

`banc.gdshader` est le seul banc du cours à ne montrer aucun effet : il montre **la coupe**.

Vu de côté, avec la surface en haut et la matière en dessous. Le trait bleu est le rayon qui entre
depuis l'œil. Les points sont les pas de la marche : **orange** tant qu'on est au-dessus de la
surface, **vert** au premier pas passé dessous, gris ensuite.

Trois choses à observer :

**Le point vert n'est pas sur la surface.** Il est *sous* elle, d'autant plus que les pas sont
grands. Cet écart, c'est exactement ce que l'interpolation finale corrige. Descends
`nombre_pas` à 3 et regarde le point vert s'enfoncer.

**Quand le rayon est vertical, un seul pas suffit ; quand il rase, il en faut dix fois plus.**
Regarde l'angle osciller et compte les points orange avant le vert. C'est la justification directe
du `mix(couches_max, couches_min, abs(vue.z))`.

**Le rayon peut passer au-dessus d'un pic sans le voir.** Mets `nombre_pas` à 4, laisse tourner et
attends : à certains angles, la marche saute par-dessus une brique et touche la suivante. Le
relief se met alors à sauter. **C'est le défaut fondamental du POM**, et le seul remède est de
raffiner — ou de passer à une recherche binaire après le premier contact (le *relief mapping*).

## Les pièges

**Le relief part dans le mauvais sens.** Signe du décalage inversé, ou carte de hauteur inversée.
Certaines cartes ont 1 = haut, d'autres 1 = creux. D'où le `1.0 - texture(...)` du shader :
enlève-le si ta carte suit l'autre convention.

**Le relief nage à angle rasant.** Profondeur trop grande, ou couches insuffisantes. La profondeur
utile est petite : `0.03` à `0.08` en unités d'UV. Au-delà de `0.1`, ça se voit toujours.

**La silhouette reste plate.** Structurel et incontournable : le POM décale des UV, il ne déplace
pas la géométrie. Le bord de l'objet reste la ligne droite du maillage, même si le relief semble
sortir. C'est pourquoi on l'utilise sur des grandes surfaces (murs, sols) et jamais sur un petit
objet vu de profil.

**Le relief ne projette pas d'ombre et ne s'auto-occulte pas.** On peut ajouter les deux — un
second parcours vers la lumière donne les ombres propres — mais le coût double. Unreal l'expose
via `Shadow Ray Steps`.

**Les objets posés dessus flottent.** La profondeur écrite dans le tampon est celle du plan. En
Unreal, `Pixel Depth Offset` corrige. Ailleurs, il faut écrire soi-même dans la profondeur du
fragment, ce qui désactive le test de profondeur anticipé et coûte très cher.

**La compilation prend dix secondes.** La boucle est déroulée. Ajoute `[loop]` en HLSL, et garde
une borne constante avec un `break`.

**Le maillage n'a pas de tangentes.** L'effet part dans une direction arbitraire. Sur un plan
créé par code, génère-les.

## Ce que ça coûte

**C'est le premier effet du cours dont le coût est variable par pixel** — et ça change tout.

De 8 à 32 échantillons de la carte de hauteur, plus les textures habituelles. Un mur en parallax
peut coûter dix fois un mur normal.

Trois conséquences pratiques :

**La divergence est ici, enfin, un vrai problème.** Deux pixels voisins peuvent sortir de la
boucle à des pas très différents ; le groupe entier attend le plus lent. Le coût réel est celui du
**pire pixel du groupe**, pas de la moyenne. C'est le meilleur exemple concret du chapitre
`00-bases/01`.

**Le LOD est obligatoire.** À dix mètres, personne ne voit la parallaxe. Passe à la normal map
seule : un uniforme de distance, un `mix` entre les UV décalées et les UV d'origine, et le coût
s'effondre.

**Sur mobile, oublie le POM.** Le parallax simple à un échantillon, oui. La marche, non.

## À toi

1. **Compare les deux versions.** Mets `couches_max` à 1 pour retrouver le parallax simple, puis
   remonte. Regarde surtout **à angle rasant** : c'est là que l'écart est énorme, et de face on ne
   voit presque rien. Ça t'apprendra où l'effort en vaut la peine.
2. **Fabrique la carte de hauteur depuis la couleur.** Beaucoup de textures n'en ont pas : prends
   la luminance de la couleur de base comme approximation. C'est faux, et souvent suffisant.
3. **Ajoute l'auto-ombrage.** Après le point de contact, marche une seconde fois vers la lumière :
   si tu retouches la surface, le point est à l'ombre. Le relief devient franchement plus solide,
   pour le double du coût.
4. **Un LOD par distance.** Interpole entre les UV parallaxées et les UV d'origine selon la
   distance à la caméra, et mesure. Trouve la distance à partir de laquelle tu ne vois plus la
   différence : c'est ton seuil.
5. **Provoque le saut.** Baisse `couches_max` à 4 sur un relief très contrasté et bouge la caméra
   lentement. Le moment où le relief saute d'une brique à l'autre est exactement ce que montre le
   banc. Un défaut qu'on sait reconnaître est un défaut qu'on sait régler.

**Leçon suivante : 15 — Flipbook et particules.** Faire jouer une animation rangée dans une seule
image.
