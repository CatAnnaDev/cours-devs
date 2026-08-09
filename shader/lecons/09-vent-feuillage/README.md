# 09 — Le vent dans le feuillage

## Ce qu'on fabrique

Un arbre dont les feuilles ondulent : des rafales qui **traversent** la forêt au lieu de la faire
bouger d'un bloc, un frisson rapide sur les feuilles, un tronc qui reste immobile. Aucune
animation, aucun script — que du shader de sommets.

C'est la première leçon du bloc 2, et la première où on écrit vraiment dans `vertex()`.

## L'idée

Trois problèmes, trois solutions, et c'est tout l'effet.

**Problème 1 : tout l'arbre bouge, tronc compris.** Il faut savoir quelle partie du modèle est
souple. Cette information n'est ni dans la position, ni dans l'UV : elle est **peinte par
l'artiste dans la couleur de sommet**.

```glsl
float souplesse = COLOR.r;
```

Tronc en noir, branches en gris, bout des feuilles en blanc. Le déplacement est multiplié par
cette valeur : à zéro, rien ne bouge. C'est le premier usage sérieux de la couleur de sommet, et
c'est un canal de données extrêmement pratique — quatre nombres par sommet, gratuits, que
l'artiste peint au pinceau.

**Problème 2 : tous les arbres ondulent en même temps.** C'est le défaut le plus visible d'un
feuillage raté, et il saute aux yeux dès qu'il y a trois arbres. La cause : le déplacement ne
dépend que du temps, qui est le même pour tout le monde.

La solution est de **décaler la phase selon la position dans le monde** :

```glsl
vec3 position_monde = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
float phase = dot(position_monde.xz, direction) / longueur_rafale;
float rafale = sin(TIME * frequence_vent - phase);
```

`dot(position.xz, direction)` mesure **jusqu'où on est allé dans le sens du vent**. Deux arbres
alignés avec le vent ont des phases différentes, donc ondulent en décalé. Mieux : le décalage est
cohérent, et l'œil voit une **vague qui traverse la forêt**. C'est exactement ce qu'on observe
dans un vrai champ, et ça ne coûte qu'un produit scalaire.

`longueur_rafale` règle la distance entre deux crêtes de la vague, en mètres. Petite valeur :
chaque arbre fait ce qu'il veut, ça a l'air d'un tremblement. Grande valeur : la forêt bouge
presque ensemble, avec juste ce qu'il faut de retard. **Autour de 8 mètres, c'est crédible.**

**Problème 3 : une vague sinusoïdale est trop régulière.** On ajoute un second mouvement, plus
rapide et plus petit, sur un autre axe :

```glsl
float flottement = sin(TIME * frequence_feuille + position_monde.x * 3.1 + position_monde.z * 2.3);
```

Les coefficients `3.1` et `2.3` n'ont rien de magique — ce sont juste deux nombres qui ne sont
pas dans un rapport simple, pour que le motif ne se répète pas visiblement. **Éviter les rapports
entiers** est la même règle qu'à la leçon 03.

## Godot

```glsl
void vertex() {
    vec3 position_monde = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    vec2 direction = normalize(direction_vent);

    float souplesse = COLOR.r;
    float phase = dot(position_monde.xz, direction) / longueur_rafale;

    float rafale = sin(TIME * frequence_vent - phase);
    vec3 poussee = vec3(direction.x, 0.0, direction.y) * rafale * force_vent;

    float flottement = sin(TIME * frequence_feuille + position_monde.x * 3.1 + position_monde.z * 2.3);
    vec3 tremblement = vec3(0.0, flottement * force_feuille, 0.0);

    VERTEX += (poussee + tremblement) * souplesse;
}
```

**Attention à un détail qui a l'air anodin.** On calcule la position monde pour obtenir la phase,
mais on ajoute le déplacement à `VERTEX`, qui est en **espace objet**. Ça marche tant que l'arbre
n'est pas tourné ni mis à l'échelle. Dès qu'il l'est, le vent souffle dans une direction qui
tourne avec lui.

La version correcte transforme le déplacement dans l'espace de l'objet :

```glsl
vec3 poussee_objet = (inverse(MODEL_MATRIX) * vec4(poussee, 0.0)).xyz;
```

`inverse()` par sommet est cher. En pratique, on utilise plutôt `MODEL_NORMAL_MATRIX` transposée,
ou — bien plus simple — **on n'applique jamais de rotation aux arbres autour d'un axe horizontal**,
ce qui est le cas dans 99 % des scènes. La version Unity, elle, déplace directement en espace
monde et n'a pas ce problème du tout : lis-la, la comparaison est instructive.

**Le découpage alpha :**

```glsl
ALPHA = echantillon.a;
ALPHA_SCISSOR_THRESHOLD = seuil_alpha;
```

`ALPHA_SCISSOR_THRESHOLD` fait le `discard` pour toi, et Godot l'applique aussi dans la passe
d'ombre. C'est ce qu'il faut pour du feuillage : jamais de vraie transparence, qui coûterait
l'*overdraw* de la leçon 07 sur des dizaines de milliers de feuilles.

**`cull_disabled`** parce qu'une feuille est un plan qu'on voit des deux côtés.

## Unity URP

Le déplacement est fait **en espace monde**, ce qui est structurellement plus propre :

```hlsl
float3 AppliquerVent(float3 positionWS, float souplesse)
{
    float2 direction = normalize(_DirectionVent.xy);
    float phase = dot(positionWS.xz, direction) / _LongueurRafale;

    float rafale = sin(_Time.y * _FrequenceVent - phase);
    float3 poussee = float3(direction.x, 0.0, direction.y) * rafale * _ForceVent;

    float flottement = sin(_Time.y * _FrequenceFeuille + positionWS.x * 3.1 + positionWS.z * 2.3);
    float3 tremblement = float3(0.0, flottement * _ForceFeuille, 0.0);

    return positionWS + (poussee + tremblement) * souplesse;
}

Varyings vert(Attributes IN)
{
    float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
    positionWS = AppliquerVent(positionWS, IN.color.r);
    OUT.positionCS = TransformWorldToHClip(positionWS);
    ...
}
```

On transforme en monde, on déplace, on projette. La rotation de l'objet n'a plus aucune
importance.

**La couleur de sommet se lit avec la sémantique `COLOR`** :

```hlsl
float4 color : COLOR;
```

Elle vaut `(1,1,1,1)` sur un maillage qui n'en a pas — donc le shader appliqué à un modèle sans
couleurs de sommet fait bouger **tout**, tronc compris. C'est le premier symptôme à reconnaître.

**Et surtout : la passe d'ombre applique exactement le même déplacement.**

```hlsl
positionWS = AppliquerVent(positionWS, IN.color.r);
```

Sans ça, l'arbre ondule et son ombre reste figée. C'est le même piège qu'à la leçon 05, sous une
autre forme, et c'est pour ça que le calcul est dans une fonction du bloc `HLSLINCLUDE` : une
seule définition, impossible de faire diverger les deux passes.

## Unreal

Voir `unreal.md` : `World Position Offset`, le `Bounds Scale` sans lequel l'arbre disparaît au
bord de l'écran, la fonction toute faite `SimpleGrassWind`, et surtout **Pivot Painter**, qui
range le pivot de chaque branche dans le modèle et permet de faire **plier** les branches au lieu
de translater les sommets.

## Le banc

`banc.gdshader` affiche une grille de disques verts — une forêt vue de dessus. Chacun a sa
position, donc sa phase, et sa propre souplesse tirée au hasard.

Trois réglages, à manipuler dans cet ordre :

**`longueur_rafale`** — mets-la à `20`, tout bouge presque ensemble. Mets-la à `1`, c'est un
grouillement sans direction. Cherche la valeur où l'œil voit **une vague traverser l'image** :
c'est le réglage qui donne la sensation de vent.

**`melange_rafales`** — à 0, l'ondulation est continue et régulière ; à 1, elle est modulée par
un bruit qui se déplace, donc le vent souffle par bouffées. La différence entre « ça bouge » et
« il y a du vent » est presque entièrement là.

**`force`** — regarde à partir de quelle amplitude ça devient caoutchouteux. Le vent crédible est
toujours plus discret qu'on ne le croit au moment de régler.

## Les pièges

**Le tronc bouge.** Le modèle n'a pas de couleurs de sommet, ou tu lis le mauvais canal. Vérifie
en affichant `COLOR.rgb` directement dans `ALBEDO` : tu dois voir le dégradé peint par l'artiste.

**Tous les arbres sont synchronisés.** La phase n'utilise pas la position monde. C'est le bug le
plus fréquent et le plus visible.

**L'arbre disparaît quand il touche le bord de l'écran.** Sa boîte englobante n'inclut pas le
déplacement. Godot : `Custom AABB` sur le `MeshInstance3D`. Unity : agrandis les `Bounds` du mesh
ou utilise `Renderer.bounds`. Unreal : `Bounds Scale`.

**L'ombre ne bouge pas.** La passe `ShadowCaster` n'applique pas le vent.

**Les feuilles s'étirent.** La force est trop grande, ou la souplesse est peinte trop haut à la
base des branches. Souviens-toi qu'on **translate** les sommets : au-delà de quelques
centimètres, une feuille se déforme au lieu de se déplacer. Pour de grandes amplitudes, il faut
une vraie rotation autour d'un pivot — c'est ce que fait Pivot Painter côté Unreal, et c'est
faisable ailleurs en rangeant le pivot dans un second jeu d'UV.

**Ça scintille au loin.** L'alpha découpé sur des feuilles fines produit un aliasing sévère.
Trois remèdes : des mipmaps correctes, un LOD qui remplace le feuillage détaillé par des cartes
de billboard, et l'*alpha to coverage* si ton pipeline le propose.

**Le feuillage est éclairé à l'envers de dos.** Les faces arrière gardent la normale de la face
avant. Godot : retourne la normale avec `FRONT_FACING`. Unity : sémantique `SV_IsFrontFace`.
Unreal : `Shading Model: Two Sided Foliage`, qui gère en plus la lumière qui traverse la feuille.

## Ce que ça coûte

**C'est le premier effet du cours dont le coût est dans le shader de sommets**, et c'est une
bonne nouvelle : un arbre de 30 000 sommets, c'est 30 000 exécutions, contre des centaines de
milliers pour le fragment.

Deux `sin` et quelques multiplications par sommet : négligeable pour un arbre, notable pour une
forêt de deux cents arbres instanciés — soit six millions de sommets. Les optimisations
habituelles, dans l'ordre de rentabilité :

1. **Un LOD qui coupe le vent au loin.** Personne ne voit onduler un arbre à cent mètres.
2. **Un seul `sin` au lieu de deux** sur les LOD éloignés.
3. **Le vent calculé une fois par instance** au lieu d'une fois par sommet, pour les herbes
   lointaines : chaque touffe bouge d'un bloc, ce qui suffit largement à distance.

Le vrai coût du feuillage n'est d'ailleurs presque jamais le vent : c'est l'*overdraw* des
feuilles alpha-testées qui se recouvrent, et le fait que le découpage alpha désactive le test de
profondeur anticipé — exactement ce qu'on a mesuré à la leçon 05.

## À toi

1. **Fais plier au lieu de translater.** Au lieu d'ajouter un décalage, fais tourner le sommet
   autour de la base de l'arbre :
   ```glsl
   float angle = rafale * force_vent * souplesse;
   float hauteur = VERTEX.y;
   VERTEX.xz += vec2(direction.x, direction.y) * sin(angle) * hauteur;
   VERTEX.y = hauteur * cos(angle);
   ```
   Compare avec la version par translation sur une grande amplitude. La différence est frappante :
   la longueur de la branche est préservée.
2. **Un vent qui répond au jeu.** Passe `force_vent` depuis un script, et fais-la monter pendant
   un orage. Un shader bien paramétré est un shader qu'un designer peut piloter.
3. **Une onde de choc.** Remplace la direction du vent par une direction qui **part d'un point** :
   `direction = normalize(position_monde.xz - centre_explosion)`, et la phase par la distance à ce
   point. Tu obtiens un souffle circulaire qui couche l'herbe en s'éloignant. Même shader, une
   ligne changée.
4. **Peins les couleurs de sommet.** Dans Blender, mode Vertex Paint, peins le rouge du tronc vers
   les feuilles. C'est le geste d'artiste que ce shader attend, et le faire une fois change ta
   façon d'écrire des shaders de feuillage.
5. **Mesure la boîte englobante.** Enlève le `Bounds Scale`, regarde l'arbre disparaître au bord
   de l'écran, remets-le. Un bug qu'on a provoqué se reconnaît.

**Leçon suivante : 10 — L'eau.** Deux normal maps qui se croisent, et l'espace tangent enfin
expliqué.
