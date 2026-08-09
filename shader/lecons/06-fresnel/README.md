# 06 — Fresnel : le contour lumineux

## Ce qu'on fabrique

Un halo qui suit la **silhouette** de l'objet : fort sur les bords, absent au centre. Il décolle
un personnage d'un fond sombre, marque un objet sélectionné, et il est la brique de base du
bouclier, de l'hologramme, du champ de force et de la bulle de savon.

Contrairement à un contour dessiné, celui-ci **suit la forme réelle en 3D** et change quand on
tourne autour de l'objet. Personne ne l'a peint : il se déduit de la géométrie.

## L'idée

Regarde une vitre en face : tu vois à travers. Regarde-la en biais : elle devient un miroir.
C'est l'**effet Fresnel**, et il est vrai pour toutes les surfaces. Plus l'angle de vue est
rasant, plus la surface renvoie de lumière.

Traduit en shader, il faut répondre à une question : **est-ce que je regarde cette surface de
face ou de biais ?** La réponse tient dans un produit scalaire.

```glsl
float face = dot(normale, direction_de_vue);
```

| Situation | `dot` |
|---|---|
| je regarde la surface pile en face | **1** |
| je la regarde en biais | entre 0 et 1 |
| je la vois par la tranche | **0** |

Le contour est donc l'inverse :

```glsl
float fresnel = 1.0 - face;
```

Reste à contrôler son épaisseur. `pow` s'en charge :

```glsl
float fresnel = pow(1.0 - face, puissance);
```

Une valeur entre 0 et 1 élevée à la puissance `n` devient plus petite. Plus `n` est grand, plus
la zone où la valeur reste notable se resserre près du bord.

| `puissance` | Aspect |
|---|---|
| 1 | un dégradé mou sur tout l'objet, pas un contour |
| 2 | un halo large, bon pour un effet d'aura |
| 4 | un contour net et lisible, le réglage par défaut |
| 8 à 16 | un liseré fin, presque un trait |

`pow(x, n)` sur une valeur de 0 à 1 est **le réglage de courbe le moins cher qui existe** : un
seul paramètre, pas de texture, et il resservira à chaque leçon.

## Godot

```glsl
void fragment() {
    float face = clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
    float fresnel = pow(1.0 - face, puissance);
    fresnel = smoothstep(seuil_bas, 1.0, fresnel);

    ALBEDO = texture(texture_base, UV).rgb;
    EMISSION = couleur_contour * fresnel * intensite;
}
```

**Pourquoi ça marche sans convertir d'espace.** Dans le `fragment()` d'un shader `spatial`,
`NORMAL` et `VIEW` sont **tous les deux en espace vue**. Un produit scalaire ne demande rien de
plus que deux vecteurs dans le même repère : il est donc correct tel quel. C'est le cas le plus
simple, et c'est justement pour ça que la leçon 04 devait convertir, elle, alors qu'ici non.

**Les deux `normalize` ne sont pas de la superstition.** `NORMAL` a été interpolée entre trois
sommets, elle est donc plus courte que 1. Sans normalisation, `dot` renvoie une valeur trop
petite, le fresnel est trop fort au centre des grandes faces, et l'objet a l'air sale.

**Le `clamp` non plus.** Sur les faces arrière ou sur une géométrie mal orientée, `dot` peut être
négatif. `1 - (-0.3)` vaut `1.3`, et `pow(1.3, 4)` vaut `2.85` : une valeur qui explose au lieu de
se resserrer. Résultat : des taches blanches là où on ne comprend pas.

**`smoothstep(seuil_bas, 1.0, fresnel)`** est un raffinement facultatif mais très utile. Avec un
`seuil_bas` de 0.2, tout ce qui est sous 0.2 disparaît complètement, et le contour devient
franchement séparé du reste au lieu de baver.

**Le raccourci Godot :** il existe des sorties `RIM` et `RIM_TINT` dans un shader `spatial`, qui
appliquent un fresnel dans le calcul d'éclairage standard. Elles sont pratiques pour un vernis
subtil, mais elles dépendent des lumières de la scène. Pour un contour lumineux qui existe même
dans le noir, c'est bien `EMISSION` qu'il faut, comme ici.

## Unity URP

```hlsl
Varyings vert(Attributes IN)
{
    VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
    OUT.positionCS = positions.positionCS;
    OUT.positionWS = positions.positionWS;
    OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
    ...
}

half4 frag(Varyings IN) : SV_Target
{
    float3 normalWS = normalize(IN.normalWS);
    float3 vueWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);

    float face = saturate(dot(normalWS, vueWS));
    half fresnel = pow(1.0 - face, _Puissance);
    ...
}
```

**`GetVertexPositionInputs` fait quatre transformations d'un coup** et renvoie une structure
contenant `positionWS`, `positionVS`, `positionCS` et `positionNDC`. C'est l'idiome URP :
tu le préfères à `TransformObjectToHClip` dès que tu as besoin de plus d'une position.

**`GetWorldSpaceNormalizeViewDir(positionWS)`** renvoie la direction du pixel **vers** la caméra,
déjà normalisée, et gère correctement la projection orthographique — ce qu'une soustraction à la
main ne fait pas. Écrire `normalize(_WorldSpaceCameraPos - positionWS)` marche en perspective et
donne un résultat faux en vue orthographique.

**`saturate` est le `clamp(x, 0, 1)` de HLSL**, en un mot et gratuitement (le matériel le fait au
passage sur beaucoup d'instructions).

Ici on travaille en espace **monde**, alors que Godot travaillait en espace **vue**. Le résultat
est identique : le produit scalaire ne demande que la cohérence entre les deux vecteurs. C'est
une bonne illustration de `00-bases/02` — l'espace n'a d'importance que par rapport à ce à quoi
on compare.

## Unreal

Voir `unreal.md` : le nœud `Fresnel`, sa version manuelle en trois nœuds, et le piège du
`CameraVector` non normalisé.

## Le banc

`banc.gdshader` affiche **cinq sphères** côte à côte, avec des puissances de 1, 2, 4, 8 et 16.
Aucune géométrie n'est en jeu : la normale de la sphère est calculée analytiquement.

```glsl
vec3 normale = vec3(p, sqrt(1.0 - dot(p, p)));
```

C'est une petite astuce qui vaut le détour. Sur une sphère unité centrée en zéro, si tu connais
`x` et `y`, alors `z = sqrt(1 - x² - y²)`. Deux coordonnées d'écran suffisent donc à reconstituer
une normale 3D — et tu obtiens un objet 3D éclairable dans un shader 2D. C'est aussi le principe
des *impostors* et des particules sphériques.

Ce que le banc rend visible d'un coup d'œil :

- **puissance 1** : ce n'est pas un contour, c'est un dégradé sur toute la sphère ;
- **puissance 4** : le contour est net et l'intérieur est propre ;
- **puissance 16** : un liseré très fin, qui **disparaît** presque sur une surface peu courbée.

Cette dernière observation est importante : la largeur du contour à l'écran dépend de la
**courbure** de la surface, pas d'une distance choisie. Sur une sphère il est large, sur un
cylindre il est fin dans un sens et absent dans l'autre, sur un cube **il n'existe que sur les
faces vues en biais**. Le fresnel n'est pas un contour dessiné : c'est une propriété de la forme.

## Les pièges

**Sur un cube, ça ne ressemble à rien.** Normal. Un cube n'a que six normales : chaque face est
uniformément « de face » ou « de biais ». Le fresnel colore des faces entières au lieu de suivre
la silhouette. **Le fresnel exige des normales qui varient** — donc une surface courbe, ou un
maillage aux normales lissées.

**L'objet a des facettes visibles.** Ses normales sont dures (*flat shading*). Dans le logiciel
de modélisation, passe en ombrage lissé, ou augmente l'angle de lissage à l'import.

**Le contour bave à l'intérieur.** Augmente la puissance, ou relève `seuil_bas`.

**Le contour clignote sur les faces arrière.** Tu affiches les deux faces (`cull_disabled`) et
les normales arrière pointent à l'opposé. Soit tu réactives le culling, soit tu retournes la
normale quand on regarde une face arrière (Godot fournit `FRONT_FACING`, Unity la sémantique
`VFACE` / `SV_IsFrontFace`).

**Le contour suit les bosses de la normal map au lieu de la silhouette.** C'est un choix, pas un
bug : si tu appliques le fresnel après avoir modifié la normale, il épouse le détail. Pour un
contour de silhouette propre, utilise la normale **géométrique**, avant la normal map.

**Le halo ne brille pas.** HDR et bloom, encore. Leçon 01.

## Ce que ça coûte

Un `dot`, un `pow`, deux `normalize`. Quelques cycles. C'est un des effets les plus rentables du
métier : très visible, presque gratuit.

Une optimisation possible et rarement utile : la partie `dot` peut se calculer dans le shader de
**sommets** et être interpolée. Ça marche mal sur les objets peu denses — l'interpolation
linéaire d'une valeur qui varie en `pow` produit des bandes. Garde-le dans le fragment sauf
mesure contraire.

## À toi

1. **Colore l'objet entier, pas seulement le contour.** Remplace `EMISSION` par un `mix` sur
   `ALBEDO` : `mix(couleur_base, couleur_contour, fresnel)`. Tu obtiens une teinte de bord au
   lieu d'un halo — l'aspect « porcelaine » ou « velours ».
2. **Fais-le pulser.** Multiplie l'intensité par la pulsation de la leçon 03. Tu tiens le
   surlignage d'objet ramassable de n'importe quel jeu.
3. **Un contour de sélection propre.** Combine avec la leçon 04 : `max` du fresnel et d'un masque,
   pour que seule une partie de l'objet soit surlignée.
4. **Inverse-le.** `pow(face, puissance)` au lieu de `pow(1 - face, puissance)` éclaire le
   **centre** au lieu du bord. C'est ce qu'il faut pour un effet de « point chaud » ou pour
   simuler une sphère éclairée par-derrière.
5. **Cherche le point de rupture.** Applique le shader à un plan, puis à un cylindre, puis à une
   sphère. Note à partir de quelle courbure l'effet devient lisible. C'est le genre d'observation
   qui t'évite de choisir cet effet pour un objet où il ne marchera pas.

**Leçon suivante : 07 — L'hologramme.** Fresnel + transparence + rayures + un glitch : le premier
effet composé du cours, et le moment où l'on découvre le vrai problème de la transparence.
