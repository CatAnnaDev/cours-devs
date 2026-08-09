# 17 — La neige qu'on écrase

## Ce qu'on fabrique

Un sol de neige qui garde la trace de ce qui passe dessus : le joueur s'enfonce, laisse des
empreintes avec un bourrelet sur les bords, et la trace s'efface lentement derrière lui. Le même
système donne l'herbe couchée, le sable, la boue, la mousse écrasée, l'eau qui ondule au passage.

**C'est la première leçon où le shader ne suffit pas.** Il faut un script, une texture de rendu,
et une boucle qui se souvient d'une image à l'autre. Le shader ne fait que lire le résultat.

## L'idée : donner une mémoire au shader

Rappel du chapitre `00-bases/01` : un shader ne se souvient de rien. Il reçoit des nombres, il
produit une couleur, et à l'image suivante il recommence de zéro.

Pour qu'une empreinte persiste, il faut donc la ranger quelque part. Ce quelque part est une
**texture de rendu** : une image dans laquelle on dessine, et qu'on relit à l'image suivante.

Le cycle, à chaque image :

```
nouvelle_texture = ancienne_texture * persistance  +  pinceaux aux positions des pieds
```

Un terme qui **efface** (multiplication par un nombre légèrement inférieur à 1) et un terme qui
**ajoute** (les pinceaux). C'est la règle de la leçon 07, appliquée au temps : ce qui ajoute
s'additionne, ce qui retire se multiplie.

Cette texture couvre une **zone du monde**. Sa correspondance avec les coordonnées monde est
l'unique chose que le shader et le script doivent partager :

```glsl
vec2 uv_zone(vec3 monde) {
    return (monde.xz - zone.xy) / zone.zw + 0.5;
}
```

`zone` est un `vec4` : le centre en XZ, puis la taille en XZ. Écris cette fonction **une fois**,
et utilise-la partout — dans le shader du terrain et dans celui du pinceau. Si les deux divergent
d'un demi-texel, les empreintes n'apparaissent pas sous les pieds.

## Le terrain

```glsl
void vertex() {
    position_monde = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    float enfoncement = textureLod(texture_deformation, uv_zone(position_monde), 0.0).r;
    VERTEX.y -= enfoncement * profondeur_max;
}
```

**Lire une texture dans le shader de sommets** est nouveau, et il y a une règle : utilise
`textureLod` avec un niveau explicite, pas `texture`. Un shader de sommets n'a pas de dérivées
d'écran, donc pas de moyen de choisir un niveau de mip tout seul. Certains pilotes l'acceptent,
d'autres non ; `textureLod` est portable.

**Et la contrainte qui domine tout** : un shader de sommets ne peut déplacer que les sommets **qui
existent**. Un plan de deux triangles ne se creusera jamais.

| Résolution du plan | Résultat |
|---|---|
| 2 × 2 sommets | rien du tout |
| 32 × 32 sur 20 m | 60 cm entre deux sommets : une empreinte de 30 cm passe entre les mailles |
| 200 × 200 sur 20 m | 10 cm : ça marche, 40 000 sommets |
| tessellation adaptative | ce que fait Unreal avec Nanite |

C'est le vrai coût de cette technique, et il est en **géométrie**, pas en pixels.

## La normale, reconstruite

Déplacer les sommets ne suffit pas : sans normale correcte, l'empreinte est un creux **qui ne se
voit pas**, puisque l'éclairage ne change pas.

```glsl
float dx = texture(texture_deformation, uv + vec2(pas.x, 0.0)).r
         - texture(texture_deformation, uv - vec2(pas.x, 0.0)).r;
float dz = ...

vec3 normale_monde = normalize(vec3(
    dx * profondeur_max / (2.0 * taille_texel_x),
    1.0,
    dz * profondeur_max / (2.0 * taille_texel_z)));
```

C'est la même formule qu'au banc de la leçon 10 : **la normale d'une surface de hauteur, c'est sa
pente**. Ici on l'obtient par différence finie — deux échantillons de part et d'autre, divisés par
la distance qui les sépare **en mètres**.

Cette division par la taille du texel en mètres est ce qui rend le résultat correct quelle que
soit la résolution de la texture ou la taille de la zone. Sans elle, changer la résolution change
la profondeur apparente des creux, et on passe une heure à se demander pourquoi.

Quatre accès texture pour la normale, plus un pour la couleur : c'est le coût principal du
shader de fragments.

## Le bourrelet

```glsl
float pente = length(vec2(dx, dz));
float bourrelet = smoothstep(0.0, largeur_bourrelet, pente) * (1.0 - centre);
```

La neige poussée sur les côtés d'une empreinte forme un bourrelet plus clair. On le trouve là où
la **pente est forte** — donc là où le gradient est grand — et là où on n'est **pas** au fond du
trou, d'où le `* (1.0 - centre)`.

On aurait pu le déplacer géométriquement aussi ; en pratique, un simple éclaircissement suffit,
parce que la normale fait déjà le travail de relief.

## Godot : trois shaders et un script

Godot n'a pas besoin de ping-pong, grâce à un réglage du `SubViewport` :

```gdscript
vue_deformation.render_target_clear_mode = SubViewport.CLEAR_MODE_NEVER
```

La cible **n'est jamais vidée** : son contenu persiste d'une image à l'autre. Il suffit alors de
dessiner par-dessus, dans l'ordre de l'arbre :

| Nœud | Shader | `render_mode` | Rôle |
|---|---|---|---|
| `ColorRect` effacement | `godot-effacement.gdshader` | `blend_mul` | multiplie tout par 0.999 |
| `ColorRect` pinceau | `godot-pinceau.gdshader` | `blend_add` | ajoute les empreintes |

Deux shaders de trois lignes, et l'accumulation est gratuite.

**Le tableau d'uniformes** est la nouveauté :

```glsl
uniform vec4 presseurs[16];
uniform int nombre_presseurs : hint_range(0, 16) = 0;
```

Chaque `vec4` est `(x_monde, z_monde, rayon, force)`. Ranger quatre valeurs dans un vecteur plutôt
que de déclarer quatre tableaux est l'idiome universel : les uniformes se transmettent par blocs
de quatre flottants de toute façon, autant les remplir.

**La boucle bornée par une constante avec un `break`** :

```glsl
for (int i = 0; i < 16; i++) {
    if (i >= nombre_presseurs) break;
    ...
}
```

La borne doit être une constante — c'est ce qui permet au compilateur de dérouler. Le `break`
évite le travail inutile quand il n'y a que deux pieds à l'écran.

**Le script** (`neige.gd`) fait trois choses, et rien d'autre : remplir le tableau depuis les
positions monde des presseurs, passer la texture du `SubViewport` au terrain, et calculer la
persistance.

```gdscript
var persistance := pow(persistance_par_seconde, delta)
```

Cette ligne mérite une explication. On veut « il reste 15 % de la trace au bout d'une seconde »,
indépendamment du nombre d'images par seconde. Multiplier par une constante à chaque image donne
un effacement deux fois plus rapide à 120 images qu'à 60. `pow(taux, delta)` donne exactement le
bon facteur pour l'intervalle écoulé. **C'est la formule d'un lissage exponentiel indépendant du
framerate**, et elle sert partout : caméras qui suivent, lissage de valeurs, inertie.

## Unity : ping-pong et un seul shader

Unity n'a pas d'équivalent au `CLEAR_MODE_NEVER` : une `RenderTexture` est réutilisable, mais on
ne peut pas y lire et y écrire dans la même passe. D'où le **ping-pong** :

```csharp
Graphics.Blit(_precedente, _courante, _materiauEmpreinte);
(_courante, _precedente) = (_precedente, _courante);
```

On lit l'une, on écrit l'autre, on échange. Le shader d'empreinte fait donc les deux opérations en
une passe, ce qui est plus simple à lire que la version Godot :

```hlsl
float resultat = saturate(precedent * _Persistance + ajout);
```

**Le format de la texture compte** :

```csharp
new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RHalf)
```

Un seul canal (`R`), en demi-flottant. Deux raisons :

1. **Un canal suffit** — on ne range qu'une hauteur. Quatre fois moins de mémoire et de bande
   passante qu'un RGBA.
2. **Le flottant est indispensable** pour l'effacement. En 8 bits, une valeur de 3 multipliée par
   0.999 vaut toujours 3 après arrondi : **la trace ne disparaît jamais**. C'est le bug le plus
   déroutant de cette leçon, et il ne se manifeste que sur les valeurs faibles, longtemps après.

**`SetVectorArray`** transmet le tableau. Attention : la taille du tableau est fixée à la
**première** transmission, pour toute la durée de vie du matériau. Passe toujours un tableau de
taille maximale, avec un compteur séparé — c'est ce que fait `Neige.cs`.

## Unreal

Voir `unreal.md` : les nœuds `Draw Material to Render Target` et le canvas, le format `RTF_R16f`
pour la même raison qu'en Unity, `NormalFromHeightmap` pour la normale, et surtout **Nanite avec
Displacement** — le seul des trois moteurs qui subdivise le terrain tout seul, ce qui supprime la
contrainte de résolution du maillage.

## Le banc

`banc.gdshader` triche, et il le dit : un shader ne peut pas se souvenir, donc il **rejoue** les
quarante dernières positions d'un chemin paramétrique et les dessine avec une force décroissante.
Le résultat est visuellement identique à une vraie accumulation.

Ce qu'on y observe :

**Le bourrelet fait tout le travail.** Mets `largeur_bourrelet` à zéro : il reste un creux gris,
plat et sans intérêt. Remets-le : la trace prend du volume. Dans un vrai rendu, c'est la normale
reconstruite qui joue ce rôle, et le banc simule les deux.

**La durée de vie change la lecture de la scène.** `duree_trace` à 1 : on voit à peine où le
joueur est passé. À 20 : le sol devient un enchevêtrement. Autour de 5 à 8 secondes, on lit une
trajectoire — c'est un réglage de game design, pas de rendu.

**Le rayon et la dureté font la forme.** `durete` proche de 1 donne un bord net comme une découpe
à l'emporte-pièce, proche de 0 un creux mou. La neige est plutôt nette, la boue plutôt molle.

## Les pièges

**Rien ne s'enfonce.** Le maillage n'a pas assez de sommets. C'est la cause dans neuf cas sur dix.
Diagnostic : affiche l'enfoncement en couleur dans le fragment — si la couleur bouge et pas la
géométrie, c'est bien ça.

**La trace ne disparaît jamais.** Texture en 8 bits. Passe en flottant.

**La trace disparaît trop vite au ralenti, ou pas assez en 120 images par seconde.** L'effacement
n'est pas indépendant du framerate : `pow(taux, delta)`.

**Les empreintes ne sont pas sous les pieds.** Les deux fonctions `uv_zone` divergent, ou tu passes
la position locale au lieu de la position monde. Diagnostic : affiche `uv_zone` en couleur sur le
terrain, tu dois voir un dégradé rouge-vert propre, centré sur la zone.

**Le terrain est éclairé comme s'il était plat.** La normale n'est pas reconstruite.

**Tout se déforme quand le joueur s'éloigne.** La zone est fixe dans le code fourni. C'est
volontaire : faire suivre la zone au joueur demande de **faire défiler le contenu de la texture**
en même temps, sinon les anciennes traces restent aux mauvaises coordonnées et l'écran devient une
traînée. La solution en production : arrondir le centre de la zone à un texel exact, et décaler
le contenu du même nombre de texels au moment du blit. C'est l'exercice 4.

**Un pic apparaît là où deux pieds se croisent.** Les pinceaux s'additionnent au-delà de 1.
`saturate` le règle en Unity ; en Godot, l'additif sature à 1 sur une cible en 8 bits, et pas sur
une cible flottante — utilise `max` au lieu de `+` si tu veux la garantie.

## Ce que ça coûte

Trois postes distincts, et ce n'est pas celui qu'on croit qui domine :

**La passe de déformation** : une texture de 512 × 512 redessinée chaque image, avec une boucle
sur quelques presseurs. Environ 260 000 pixels très bon marché. Négligeable.

**Le shader de fragments du terrain** : cinq accès texture par pixel, dont quatre pour la normale.
Sur un sol qui couvre l'écran, c'est notable — et c'est le poste optimisable : on peut
pré-calculer la normale dans une seconde texture pendant la passe de déformation, et n'en lire
qu'une seule au moment du rendu.

**La géométrie** : c'est le vrai coût. 200 × 200 sommets pour 20 mètres, c'est 40 000 sommets
redessinés chaque image, et il en faut plus pour une zone plus grande. Les parades : une
subdivision plus fine seulement près du joueur, un LOD géométrique, ou la tessellation adaptative
quand le moteur la propose.

**Un dernier point** : la texture de déformation est lue dans le shader de **sommets**. Sur
certains GPU mobiles, l'accès texture en vertex est nettement plus lent qu'en fragment, voire
limité en nombre d'unités. À vérifier tôt sur la cible.

## À toi

1. **De l'herbe au lieu de la neige.** Le même buffer, mais lu par le shader d'herbe de la
   leçon 09 : au lieu d'enfoncer le sol, il couche les brins dans la direction du gradient
   `(dx, dz)`. Tu as déjà tout : le gradient est calculé, il suffit de l'utiliser comme direction
   au lieu d'une normale.
2. **Une trace qui a une direction.** Range deux valeurs de plus dans la texture — le canal vert
   pour la direction X, le bleu pour Z — et fais que les brins se couchent dans le sens du
   déplacement, pas juste vers l'extérieur.
3. **De l'eau qui ondule.** Remplace l'effacement exponentiel par une simulation d'onde à deux
   tampons : `nouveau = 2 * courant - ancien + laplacien * vitesse`. Trois textures au lieu d'une,
   et tu as des rides qui se propagent et se réfléchissent. C'est la même architecture, avec une
   physique dedans.
4. **Fais suivre la zone.** Arrondis le centre à un texel, décale le contenu au moment du blit,
   et vérifie qu'aucune traînée n'apparaît. C'est l'exercice le plus difficile de cette leçon et
   celui qui sépare une démo d'un système utilisable.
5. **Mesure les deux coûts.** Passe la texture de 512 à 2048 : note l'écart. Puis passe le
   maillage de 100 × 100 à 400 × 400 : note l'écart. L'un des deux compte beaucoup plus que
   l'autre, et savoir lequel décide de toutes tes optimisations futures.

**Leçon suivante : 18 — Le bouclier.** Des impacts qui se propagent, un tableau d'uniformes bien
rempli, et un halo à l'intersection du décor.
