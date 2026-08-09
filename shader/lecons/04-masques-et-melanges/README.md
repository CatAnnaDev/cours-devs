# 04 — Masques et mélanges : de la neige sur les faces tournées vers le ciel

## Ce qu'on fabrique

Un matériau qui mélange **deux aspects** — pierre et neige — selon un masque calculé, pas peint.
La neige se dépose sur les surfaces horizontales, avec un bord irrégulier, et un unique curseur
fait passer de « trois flocons » à « tout est blanc ».

Le même shader, en changeant deux valeurs, donne de la mousse sur des rochers, de la rouille dans
les creux, du givre sur du métal, de la poussière sur des meubles.

## L'idée

**C'est la leçon la plus importante du bloc 1.** Presque tout effet de shader se ramène à ceci :

> produire un **masque** — un nombre entre 0 et 1 par pixel — puis s'en servir pour mélanger deux
> choses.

```glsl
resultat = mix(aspect_a, aspect_b, masque);
```

Toute la difficulté est dans le masque. Écrire `mix` est trivial ; **savoir d'où vient le
nombre** est le métier.

### Les sources de masque

| Source | Ce qu'elle donne | Typique de |
|---|---|---|
| un canal de texture | ce que l'artiste a peint | usure, saleté, zones d'un personnage |
| une **couleur de sommet** | ce que l'artiste a peint sur le maillage, sans texture | végétation, variation par instance |
| une coordonnée UV | un dégradé | fondu vertical, barre de progression |
| la **normale** | l'orientation de la surface | neige, mousse, projection triplanar |
| la position monde | l'altitude, la distance à un point | brume par hauteur, onde de choc |
| la **profondeur** | la distance à la caméra ou à une autre surface | écume, particules douces (leçon 11) |
| du **bruit** | de l'irrégularité | tout, absolument tout |

Cette leçon en combine trois : normale (l'orientation), bruit (l'irrégularité), et un seuil
réglable (la couverture).

### Les trois opérations sur les masques

Un masque est un nombre entre 0 et 1, donc la logique booléenne s'écrit en arithmétique :

| Idée | Écriture | Résultat |
|---|---|---|
| A **et** B | `a * b` | 1 seulement si les deux valent 1 |
| A **ou** B | `max(a, b)` | 1 dès que l'un vaut 1 |
| **non** A | `1.0 - a` | inversion |
| A **sauf** B | `a * (1.0 - b)` | A moins la zone B |
| plus contrasté | `pow(a, 3.0)` | resserre vers 0 |
| plus étalé | `pow(a, 0.4)` | pousse vers 1 |

`a + b` existe aussi, mais dépasse 1 dès que les deux masques se recouvrent, ce qui produit des
surbrillances involontaires. Utilise `max` sauf si tu veux justement l'accumulation.

## Godot

```glsl
vec3 normale_monde = normalize((INV_VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz);
float vers_le_haut = normale_monde.y * 0.5 + 0.5;

float grain = texture(bruit, uv * 1.7).r;
float valeur = vers_le_haut - irregularite * (1.0 - grain);

float seuil = 1.0 - couverture;
float masque = smoothstep(seuil - nettete, seuil + nettete, valeur);

ALBEDO = mix(base, couleur_neige, masque);
ROUGHNESS = mix(rugosite_base, rugosite_neige, masque);
```

**La ligne à comprendre est la première.** Dans le `fragment()` d'un shader `spatial`, `NORMAL`
est en **espace vue** : elle change quand la caméra tourne. Utilisée telle quelle, la neige
resterait collée à l'écran au lieu de rester en haut du rocher. `INV_VIEW_MATRIX` la ramène en
espace monde, et le `0.0` en quatrième composante dit « c'est une direction, pas une position »
— relis `00-bases/02-les-espaces.md` si ce zéro te surprend encore.

**Le `smoothstep` est le cœur de l'affaire.** Ses deux bornes sont `seuil ± nettete` :

- `nettete` petit → transition franche, un bord de neige net comme une découpe ;
- `nettete` grand → transition longue, la neige se fond dans la pierre.

Et `seuil = 1.0 - couverture` renverse le curseur pour qu'il se lise dans le bon sens : à 0, rien
n'est couvert ; à 1, tout l'est. **Un curseur qui va dans le sens de l'intuition vaut mieux qu'un
curseur techniquement direct** — c'est de l'ergonomie, et c'est toi qui l'utiliseras cent fois.

**La texture de bruit, en un clic.** Dans l'inspecteur, sur le paramètre `bruit` :
`New NoiseTexture2D` → dans `Noise`, `New FastNoiseLite`. Règle `Frequency` autour de `0.02`.
Coche `Seamless` pour qu'elle se répète sans couture. Godot te fabrique la texture, tu n'as rien
à importer.

**Ne mélange pas que la couleur.** La ligne `ROUGHNESS = mix(...)` est ce qui fait la différence
entre « du blanc peint » et « de la neige » : la neige est plus lisse que la pierre, et l'œil le
voit sur les reflets bien avant de voir la couleur. Chaque fois que tu mélanges deux matières,
demande-toi quelles **autres** propriétés changent aussi.

## Unity URP

Le calcul est le même, avec deux différences.

```hlsl
Varyings vert(Attributes IN)
{
    OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
    ...
}

half4 frag(Varyings IN) : SV_Target
{
    float3 normalWS = normalize(IN.normalWS);
    float versLeHaut = normalWS.y * 0.5 + 0.5;
    ...
}
```

**La normale est calculée dans le vertex, normalisée dans le fragment.** C'est le schéma standard,
et le `normalize` du fragment n'est pas facultatif : l'interpolation entre trois sommets produit
un vecteur plus court que 1. Sans lui, l'éclairage s'assombrit au milieu des grandes faces.

**`TransformObjectToWorldNormal` plutôt qu'une multiplication à la main**, parce qu'elle gère les
échelles non uniformes. Un objet mis à l'échelle `(2, 1, 1)` a des normales fausses si on utilise
la matrice du modèle telle quelle.

**La texture de bruit.** Unity n'a pas d'équivalent au `NoiseTexture2D` de Godot. Trois options :

1. n'importe quelle image de bruit en niveaux de gris, importée avec **`sRGB` décoché** ;
2. un Shader Graph avec un nœud `Simple Noise`, dont tu récupères le résultat ;
3. du bruit procédural directement dans le shader — c'est la leçon 05 qui le construit.

Le point important est le `sRGB` **décoché** : un masque n'est pas une couleur. Coché, ses valeurs
sont tordues par la courbe gamma, ton seuil à 0.5 ne tombe plus au milieu, et la couverture réagit
de travers sans que rien ne signale l'erreur.

## Unreal

Voir `unreal.md`. Attention : **l'axe vertical d'Unreal est `Z`**, pas `Y`. Le `ComponentMask`
doit cocher `B`, pas `G`. Et la fonction toute faite `WorldAlignedBlend` fait tout ça en un nœud
une fois que tu as compris ce qu'elle contient.

## Le banc

`banc.gdshader` affiche le même champ de bruit traité de quatre façons, avec un seuil qui monte
et descend tout seul. Aucune texture à assigner : le bruit est calculé dans le shader.

| Quadrant | Traitement | Ce que tu observes |
|---|---|---|
| haut gauche | rien, la valeur brute | un nuage gris continu |
| haut droit | `step(seuil, valeur)` | des taches noires ou blanches, bord en escalier |
| bas gauche | `smoothstep(seuil ± nettete, valeur)` | des taches à bord adouci, largeur constante |
| bas droit | `smoothstep` avec `fwidth` | des taches à bord d'exactement un pixel |

Trois choses à voir, dans cet ordre :

**1. Le quadrant haut droit crénèle.** `step` produit une frontière parfaitement dure, donc des
escaliers de pixels. C'est visible en mouvement, encore plus après une réduction d'image.

**2. La netteté du bas gauche est en unités de valeur, pas de pixels.** Là où le bruit change
lentement, la transition est large ; là où il change vite, elle est étroite. Le bord n'a pas une
épaisseur constante à l'écran.

**3. Le bas droit est constant.** `fwidth(valeur)` mesure la variation d'un pixel au suivant : en
l'utilisant comme largeur, la transition fait toujours un pixel. C'est la recette du bord propre,
et elle vaut aussi pour un contour de personnage (leçon 08) ou un contour de scène (leçon 27).

Change `echelle` dans l'inspecteur : le bas droit reste net à toutes les échelles, le bas gauche
non.

## En 2D

Le masque ne peut pas venir de la normale — il n'y en a pas. Les versions 2D le prennent d'où il
vient toujours en 2D :

| Source | Ce que ça donne |
|---|---|
| une texture de masque | ce que l'artiste a peint : usure, saleté, zones |
| `UV.y` | un dégradé vertical : remplissage, jauge, niveau de liquide |
| la couleur de sommet | une variation par sprite, sans texture |
| la distance au centre | un halo, une vignette |

Le shader fourni combine les deux premières : un dégradé vertical perturbé par un bruit, pour
faire monter une usure ou un remplissage avec un bord irrégulier.

Toute la logique de la leçon — `smoothstep(seuil ± nettete)`, `mix`, et les opérations `*`, `max`,
`1 - x` — est identique. **Seule la source du masque change.**

## Les pièges

**La neige tourne avec la caméra.** Tu as utilisé la normale en espace vue sans la convertir.
C'est le bug numéro un de cette leçon, en Godot.

**La neige tourne avec l'objet.** Tu as utilisé la normale en espace objet. Correct pour une
décoration peinte sur un modèle, faux pour de la neige.

**En Unreal, la neige se dépose sur le côté.** `ComponentMask` sur `G` au lieu de `B` : l'axe
vertical d'Unreal est `Z`.

**Le curseur de couverture ne fait presque rien sur une partie de sa course.** Ton masque n'est
pas réparti uniformément — le bruit se concentre autour de 0.5. Étale-le avant le seuil :
`valeur = (valeur - 0.5) * contraste + 0.5`. Cette remise à l'échelle autour du milieu est un
geste que tu referas souvent.

**La transition scintille au loin.** Le bruit est échantillonné plus finement que le pixel.
Solution : réduire le carrelage du bruit, ou activer les mipmaps sur la texture de bruit.

**Le masque à 0 laisse quand même voir un peu de neige.** `smoothstep(seuil - nettete, seuil +
nettete, ...)` avec un `seuil` de 1 et une `nettete` de 0.12 borne haute à 1.12 — inatteignable,
donc masque nul, c'est bon. Mais si tu écris les bornes dans l'autre sens ou si `nettete` dépasse
le seuil, la borne basse passe sous zéro et il reste toujours un peu de masque. Vérifie
`seuil - nettete >= 0` quand la couverture est à zéro, ou `clamp` le seuil.

## Ce que ça coûte

Deux accès texture, un `smoothstep`, deux `mix`. Le poste dominant reste les textures.

Une remarque qui vaut pour tout le cours : **mélanger deux matériaux complets coûte le double
d'un seul**. Si ta neige avait sa propre texture de couleur, sa propre normale et sa propre
rugosité, tu passerais de 2 à 6 accès texture. C'est pour ça que les shaders de terrain
sérieux utilisent un mélange par hauteur qui **choisit** plutôt que d'échantillonner les deux —
ou un tableau de textures. On y reviendra au bloc 6.

## À toi

1. **Change la source du masque sans toucher au reste.** Remplace `vers_le_haut` par :
   - `UV.y` — la neige monte du bas, comme un remplissage de barre ;
   - la position monde en Y, normalisée — la neige apparaît au-dessus d'une altitude ;
   - `1.0 - vers_le_haut` — de la mousse sous les surplombs.
   Trois lignes changées, trois effets complètement différents. C'est la démonstration que le
   masque *est* l'effet.
2. **Ajoute un liseré.** Entre pierre et neige, une bande de couleur : calcule un second masque
   avec un seuil légèrement décalé, soustrais l'un de l'autre, et tu as un contour. C'est
   exactement le mécanisme du bord incandescent de la leçon 05 — essaie de le trouver seul avant
   de la lire.
3. **Contrôle le contraste du bruit.** Ajoute un uniforme `contraste` et applique
   `grain = (grain - 0.5) * contraste + 0.5` avant de t'en servir. Observe l'effet sur la forme
   du bord : contraste faible, bord mou et régulier ; contraste fort, bord découpé et anguleux.
4. **Combine deux masques.** Neige **et** au-dessus d'une certaine altitude : multiplie les deux.
   Puis neige **ou** givre près d'un point : `max`. Tu écris de la logique, en arithmétique.

**Leçon suivante : 05 — La dissolution.** Le masque de cette leçon, animé, avec un bord qui brûle
— et le premier vrai effet que tu montreras à quelqu'un.
