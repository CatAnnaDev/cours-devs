# 13 — Le triplanar : texturer sans UV

## Ce qu'on fabrique

Un matériau qui se pose correctement sur **n'importe quelle géométrie**, sans dépliage : falaises,
terrains, rochers, grottes, géométrie générée par du bruit ou sculptée en jeu. Là où une texture
classique s'étire en traînées verticales sur une paroi, le triplanar reste net.

## L'idée

Une texture a besoin d'UV, les UV viennent d'un dépliage, et un dépliage est du travail
d'artiste — impossible sur une falaise générée à l'exécution, pénible sur un terrain, inutile sur
un rocher qu'on veut simplement couvrir de pierre.

Le triplanar contourne le problème : **on projette la texture depuis les trois axes du monde**, et
on mélange les trois selon l'orientation de la surface.

```glsl
vec3 couleur_x = texture(texture_base, p.zy).rgb;
vec3 couleur_y = texture(texture_base, p.xz).rgb;
vec3 couleur_z = texture(texture_base, p.xy).rgb;
```

Chaque ligne prend deux des trois coordonnées de la **position monde** et s'en sert comme UV.
`p.xz` projette depuis le haut : c'est la bonne projection pour un sol. `p.zy` projette depuis le
côté : la bonne pour un mur orienté est-ouest. `p.xy` depuis l'avant.

Reste à choisir laquelle utiliser — et la réponse est **les trois, pondérées** :

```glsl
vec3 poids = pow(abs(normale), vec3(nettete));
poids /= (poids.x + poids.y + poids.z);
```

Une surface horizontale a pour normale `(0, 1, 0)` : ses poids sont `(0, 1, 0)`, donc seule la
projection du haut compte. Une paroi orientée nord a `(0, 0, 1)` : seule la projection de face.
Et une pente à 45° mélange les deux.

**`abs` parce qu'une normale peut être négative** et qu'un poids négatif n'a aucun sens : un
plafond et un sol utilisent la même projection.

**`pow(..., nettete)` resserre le mélange.** Sans lui (netteté = 1), la zone de transition est
énorme et les deux textures se voient en surimpression sur toute la pente — un effet de flou sale
très reconnaissable.

| `nettete` | Transition |
|---|---|
| 1 | mélange sur toute la pente, aspect délavé |
| 4 | zone de transition raisonnable — la valeur par défaut |
| 8 à 16 | transition courte, presque une découpe |

**La division par la somme** garantit que les trois poids totalisent 1. Sans elle, la luminosité
change avec l'orientation : les pentes deviennent plus sombres que les surfaces droites.

## Godot

```glsl
varying vec3 position_monde;
varying vec3 normale_monde;

void vertex() {
    position_monde = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    normale_monde = normalize(MODEL_NORMAL_MATRIX * NORMAL);
}
```

On calcule les deux en espace **monde** dans le vertex, et on les transporte. Pourquoi le monde
plutôt que l'objet ? Parce que c'est ce qui permet à deux rochers voisins de partager un motif
continu, et à un terrain découpé en morceaux de ne pas montrer les coutures. En espace objet, le
motif tournerait avec chaque rocher — ce qui est parfois voulu, et c'est un uniforme à ajouter si
tu en as besoin.

**`MODEL_NORMAL_MATRIX`** et non `MODEL_MATRIX` pour la normale : c'est la matrice corrigée pour
les échelles non uniformes, exactement le point de `00-bases/02`.

## Le vrai morceau : mélanger trois normal maps

Mélanger trois couleurs est une moyenne pondérée. Mélanger trois **normales** ne l'est pas, et
c'est là que 90 % des triplanar trouvés sur internet sont faux.

Le problème : chaque normale échantillonnée est exprimée dans le repère de **sa** projection.
Celle de la projection du haut a son `z` qui pointe vers le haut du monde ; celle de la projection
de côté a son `z` qui pointe vers le côté. Les additionner revient à additionner des vecteurs qui
ne parlent pas de la même chose.

La solution correcte, dite *whiteout* :

```glsl
normale_x = vec3(normale_x.xy + normale.zy, abs(normale_x.z) * normale.x);
normale_y = vec3(normale_y.xy + normale.xz, abs(normale_y.z) * normale.y);
normale_z = vec3(normale_z.xy + normale.xy, abs(normale_z.z) * normale.z);

vec3 assemblee = normalize(
    normale_x.zyx * poids.x +
    normale_y.xzy * poids.y +
    normale_z.xyz * poids.z);
```

Deux opérations distinctes se cachent là :

1. **On ajoute l'inclinaison de la normal map à la normale géométrique**, comme au mélange
   whiteout de la leçon 10.
2. **On permute les composantes** (`zyx`, `xzy`, `xyz`) pour remettre chaque normale dans le
   repère du monde. C'est cette permutation qui manque partout, et son absence donne un éclairage
   qui a l'air presque juste — jusqu'à ce qu'on tourne la caméra.

Le `abs` sur le `z` évite que la normale s'inverse sur les faces orientées négativement.

En Godot, il faut ensuite revenir en espace vue, parce que `NORMAL` y est exprimée :

```glsl
NORMAL = (VIEW_MATRIX * vec4(assemblee, 0.0)).xyz;
```

Et surtout : **on écrit dans `NORMAL`, pas dans `NORMAL_MAP`**. `NORMAL_MAP` attend une normale en
espace tangent, ce que la nôtre n'est pas. Confondre les deux est l'erreur classique de cette
leçon.

## Unity URP

Même calcul, avec `donnees.normalWS = assemblee` directement — URP travaille en espace monde, il
n'y a pas de conversion finale.

Le shader n'a **pas besoin des UV ni des tangentes du maillage** : `Attributes` ne contient que
`positionOS` et `normalOS`. C'est le signe qu'on a bien compris la leçon — un triplanar qui lit
`IN.uv` quelque part n'en est pas un.

Conséquence agréable : ce shader marche sur un maillage sans dépliage, sans tangentes, généré par
du code. C'est exactement le cas d'usage.

## Unreal

Voir `unreal.md` : la fonction **`WorldAlignedTexture`**, sa cousine `WorldAlignedNormal` qui gère
le mélange de normales, l'axe vertical qui est **Z** et non Y, et la case `Tangent Space Normal`
à décocher.

## Le banc

`banc.gdshader` affiche une sphère qui tourne. Deux modes, avec le booléen `montrer_poids` :

**Poids en couleur** — rouge, vert et bleu montrent la contribution de chacune des trois
projections. Tu vois trois taches qui se rejoignent, et les zones de mélange sont les zones
grisâtres entre elles. Fais varier `nettete` : à 1, presque toute la sphère est grise, c'est-à-dire
mélangée ; à 16, il ne reste que trois taches franches séparées par un liseré.

**Damier projeté** — le résultat, avec un damier procédural. Cherche les zones de transition à
45° : c'est là que le motif se croise et devient visuellement plus dense. **C'est le défaut
inhérent du triplanar**, et aucune netteté ne l'élimine : à la transition, deux motifs se
superposent forcément.

Les remèdes connus, si ça se voit trop : une texture peu contrastée, un mélange par hauteur au
lieu d'un mélange linéaire, ou une répétition stochastique. Aucun n'est gratuit.

## Les pièges

**La texture est trois fois trop grande ou trop petite.** L'échelle du triplanar est en unités
monde, pas en répétitions par surface. Une échelle de `0.25` signifie qu'une répétition couvre
4 mètres. C'est plus prévisible que les UV, une fois qu'on y est habitué.

**Les faces opposées ont un motif en miroir.** C'est le `abs` sur la normale : les deux côtés
d'un mur reçoivent la même projection, donc l'un est retourné. Souvent invisible, gênant sur un
motif directionnel comme du bois. Le remède : multiplier la coordonnée par `sign(normale)` pour la
projection concernée.

**L'éclairage est faux sur les pentes.** Les normal maps sont mélangées sans permutation.

**Ça coûte trois fois plus cher.** Ce n'est pas un piège, c'est le prix — voir plus bas.

**Ça marche mal sur un objet qui bouge.** Le motif est ancré au monde : si le rocher se déplace,
la texture glisse dessus. Pour un objet mobile, il faut le triplanar en espace **objet**.

## Ce que ça coûte

**Trois accès texture au lieu d'un.** Avec une normal map, **six**. Sur un terrain qui couvre
l'écran, c'est le poste dominant et de loin.

Les optimisations, par ordre de rentabilité :

**1. Sauter les projections dont le poids est nul.** Un `if (poids.x > 0.01)` — mais attention à
la divergence de `00-bases/01` : ça n'aide que si les pixels voisins prennent la même branche, ce
qui est vrai en pratique sur de grandes surfaces plates.

**2. Le biplanar.** Ne garder que les **deux** projections les plus fortes, en ignorant la
troisième dont le poids est presque toujours négligeable. Quatre accès au lieu de six, pour une
différence quasi invisible.

**3. Un triplanar uniquement là où c'est nécessaire.** Beaucoup de terrains utilisent des UV
classiques sur les zones plates et ne basculent en triplanar que sur les pentes fortes, via un
masque. Le mélange des deux méthodes coûte moins cher que le triplanar partout.

**4. Réduire au loin.** Une seule projection au-delà de vingt mètres : personne ne voit
l'étirement à cette distance.

## À toi

1. **Trois textures différentes au lieu d'une.** De l'herbe sur la projection du haut, de la roche
   sur les deux latérales. Une ligne à changer, et tu as un matériau de falaise complet — le motif
   le plus utilisé de tous les jeux en extérieur.
2. **Ajoute un seuil d'altitude.** Combine avec le masque de la leçon 04 : de la neige au-dessus
   d'une hauteur, du triplanar de roche en dessous. Deux leçons superposées, et tu as un vrai
   shader de montagne.
3. **Fais le biplanar.** Trouve l'axe dominant, prends les deux autres, ignore le plus faible.
   Mesure la différence de performance avec ton moteur, puis regarde si tu vois la différence
   visuelle. Ce genre de comparaison est le cœur du métier.
4. **Casse-le exprès.** Enlève la division par la somme des poids et regarde les pentes
   s'assombrir. Puis enlève les permutations de normales et tourne la caméra jusqu'à voir
   l'éclairage partir de travers. Les deux bugs sont subtils, et les avoir vus une fois te fera
   gagner des heures.
5. **Passe en espace objet.** Un uniforme booléen, et le rocher garde son motif quand il roule.
   Compare les deux comportements sur un objet en mouvement.

**Leçon suivante : 14 — Parallax et POM.** Creuser un mur de briques sans ajouter un seul
triangle.
