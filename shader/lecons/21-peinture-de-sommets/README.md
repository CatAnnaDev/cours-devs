# 21 — La peinture de sommets

## Ce qu'on fabrique

Un rocher couvert de mousse au pied et de neige au sommet, une falaise qui passe de la roche au
sable, un mur sale dans les coins — **sans aucun masque en texture, sans dépliage supplémentaire, et
sans un octet de mémoire en plus.**

L'artiste peint directement sur le maillage, et le shader lit ce qu'il a peint.

## L'idée : quatre nombres gratuits par sommet

Un maillage porte, pour chaque sommet, une **couleur** : quatre flottants, `R`, `G`, `B`, `A`. Le
matériel les transporte et les interpole depuis toujours, et presque personne ne s'en sert pour
afficher une couleur.

Ce sont donc **quatre canaux de données libres**, interpolés gratuitement du vertex au fragment.

Les leçons 09 et 20 en utilisaient déjà un — la souplesse du feuillage. Cette leçon les utilise
tous les quatre.

| Canal | Usage typique |
|---|---|
| `R`, `G`, `B` | les poids de trois couches de matière |
| `A` | l'occlusion ambiante, l'usure, la souplesse au vent |

Et le budget se négocie : rien n'oblige à trois couches plus un masque. Quatre masques
indépendants marchent aussi bien.

## Ce que ça remplace, et ce que ça ne remplace pas

| | Couleurs de sommet | Texture de masque |
|---|---|---|
| mémoire | **zéro** | une texture de plus par objet |
| résolution | celle du **maillage** | celle de la texture |
| dépliage UV | pas nécessaire | nécessaire |
| variation par instance | **oui, gratuite** | non, sauf à dupliquer la texture |
| détail fin | non | oui |
| coût de rendu | nul | un accès texture |

La ligne qui décide : **la résolution est celle du maillage**.

Un mur de deux triangles a quatre sommets. Tu peux y peindre exactement quatre valeurs, interpolées
en dégradé. Aucun détail fin n'est possible.

C'est pour ça que les couleurs de sommet servent aux **variations larges** — la mousse au pied
d'un rocher, le sable qui monte sur une falaise, la saleté dans un coin — et jamais aux motifs
précis, qui restent en texture.

Et la conséquence de conception : **on modélise en pensant à la peinture**. Un mur qui doit
recevoir de la mousse au sol a besoin de quelques boucles d'arêtes en bas. Ça se décide au
moment du modèle, pas après.

## Le mélange, et pourquoi la version naïve est laide

La version évidente est une moyenne pondérée :

```glsl
vec3 poids = COLOR.rgb / (COLOR.r + COLOR.g + COLOR.b);
vec3 melange = rouge * poids.r + verte * poids.g + bleue * poids.b;
```

Elle marche, et elle produit une **transition boueuse** : dans la zone de mélange, on voit les deux
textures en surimpression, à 50 % chacune. Ça ne ressemble à rien de réel — dans la nature, le
sable ne devient pas translucide au bord de l'herbe : **il se glisse entre les brins**.

## Le mélange par hauteur

La technique qui change tout. On ajoute une **carte de hauteur** par couche — les creux et les
bosses de la matière — et on laisse la plus haute gagner :

```glsl
vec3 poids_par_hauteur(vec3 peinture, vec3 hauteur) {
    vec3 combines = peinture * (hauteur + 0.0001);
    float maximum = max(combines.r, max(combines.g, combines.b));
    vec3 retenus = max(combines - (maximum - durete), 0.0);
    float somme = retenus.r + retenus.g + retenus.b;
    return somme > 0.0001 ? retenus / somme : poids_lineaires(peinture);
}
```

Lis-le en trois temps :

**`peinture * hauteur`** — chaque couche est d'autant plus forte que l'artiste l'a peinte **et** que
sa matière est haute à cet endroit.

**`max(combines - (maximum - durete), 0.0)`** — on ne garde que ce qui est à moins de `durete` du
maximum. Une `durete` faible ne garde presque que le gagnant : la transition devient une découpe
irrégulière qui suit le relief. Une `durete` élevée redonne un fondu classique.

**La normalisation finale** garantit que la somme des poids vaut 1, donc pas de changement de
luminosité dans la zone de transition.

Le résultat : les cailloux dépassent du sable, l'herbe pousse entre les pavés, la neige se pose
dans les creux et pas sur les crêtes. **Une seule ligne de plus qu'un `mix`, pour la différence
entre « ça marche » et « c'est réel ».**

## Godot

```glsl
void fragment() {
    vec3 poids = melange_par_hauteur
        ? poids_par_hauteur(COLOR.rgb, hauteur)
        : poids_lineaires(COLOR.rgb);

    ALBEDO = rouge * poids.r + verte * poids.g + bleue * poids.b;
    AO = mix(1.0, COLOR.a, force_occlusion);
    AO_LIGHT_AFFECT = 1.0;
}
```

**`COLOR` est disponible directement dans `fragment()`** d'un shader `spatial`, déjà interpolée.
Pas de `varying` à déclarer.

**`AO` et `AO_LIGHT_AFFECT`** sont les sorties d'occlusion ambiante de Godot. La seconde décide si
l'occlusion touche seulement la lumière indirecte (`0`) ou aussi la directe (`1`).

**Godot n'a pas d'outil de peinture de sommets.** Tu peins dans Blender, tu exportes en glTF, et
Godot les importe. Vérifie que `Vertex Color / Use as Albedo` est bien réglé sur le matériau
importé — sinon Godot peut multiplier ton albédo par la peinture, ce qui donne un objet très
sombre et très déroutant.

## Unity URP

```hlsl
struct Attributes
{
    float4 color : COLOR;
    ...
};
```

La sémantique `COLOR` lit la couleur de sommet. Deux avertissements :

**Sur un maillage sans couleurs de sommet, elle vaut `(1,1,1,1)`.** Donc les trois poids valent 1,
la normalisation en donne un tiers chacun, et tu obtiens une bouillie uniforme des trois couches.
C'est le symptôme à reconnaître : *tout est mélangé partout, uniformément*.

**Unity n'a pas d'outil de peinture intégré non plus.** Il faut Polybrush (gratuit, via le Package
Manager), un outil de l'Asset Store, ou peindre dans Blender.

Attention aussi à l'espace de couleur : selon le pipeline et l'import, les couleurs de sommet
peuvent subir une conversion sRGB. Si tes transitions sont décalées par rapport à ce que tu as
peint, c'est presque toujours ça. Peins des valeurs franches — 0 ou 1 — plutôt que des demi-teintes,
et le problème disparaît.

## Unreal

Voir `unreal.md`. Unreal est le seul moteur avec un **`Mesh Paint` intégré**, avec les canaux
sélectionnables individuellement, la peinture stockée **par instance**, et un mode texture en
complément. Plus le piège d'import à connaître : `Vertex Color Import Option` doit être sur
`Replace`.

## En 2D

Les couleurs de sommet existent aussi en 2D, et on les oublie complètement.

Un `Polygon2D` en Godot, un `Line2D`, un `MeshInstance2D`, un maillage de `SpriteShape` en Unity :
tous portent des couleurs par sommet. `godot-2d.gdshader` et `unity-2d.shader` s'en servent pour
mélanger deux textures le long d'une forme — de la terre qui devient de l'herbe le long d'une
plateforme, par exemple.

Et le cas le plus courant, qu'on ne reconnaît pas comme de la peinture de sommets : **le `modulate`
d'un sprite est une couleur de sommet**. C'est pour ça qu'on multiplie par `COLOR` à la fin de tout
shader 2D correct (chapitre `00-bases/06-le-2d.md`).

## Peindre dans Blender

C'est le chemin commun aux trois moteurs.

1. Mode **`Vertex Paint`**.
2. `Object Data Properties` → `Color Attributes` → ajoute un attribut de type **`Byte Color`** sur
   le domaine **`Vertex`** (et non `Face Corner`, qui donne des couleurs par coin de face et
   s'exporte moins bien).
3. Peins. `Shift + K` remplit avec la couleur courante.
4. Pour peindre **un seul canal**, ouvre le panneau `Brush` et décoche les autres dans
   `Advanced → Affect Alpha` / les masques de canaux, ou passe par le mode `Blend: Mix` avec une
   couleur pure.
5. Exporte en **glTF** avec `Materials → Vertex Colors` activé, ou en FBX avec `Vertex Colors`
   coché.

Le piège Blender le plus courant : **Blender travaille en sRGB pour les couleurs d'octets**. Un gris
à 50 % dans Blender n'arrive pas à 0.5 dans le shader. Peins du **noir pur et du blanc pur** quand
c'est possible, et fais les demi-teintes par dégradé du pinceau plutôt que par une couleur grise
choisie à la main.

## Le banc

`banc.gdshader` compare les deux mélanges sur la même peinture, avec la même carte de hauteur :

- **en haut** : le mélange linéaire — une bande boueuse où les deux matières se superposent ;
- **en bas** : le mélange par hauteur — une frontière irrégulière qui suit le grain.

Fais varier `durete`. À `1`, les deux moitiés se ressemblent. À `0.05`, la frontière du bas devient
une découpe dentelée qui suit exactement le relief.

C'est le genre d'effet qu'on ne remarque pas quand il est là, et qui rend une scène plate quand il
manque.

## Les pièges

**Tout est mélangé uniformément.** Le maillage n'a pas de couleurs de sommet : elles valent
`(1,1,1,1)` partout. Diagnostic en une ligne : `ALBEDO = COLOR.rgb;` — tu dois voir ta peinture.

**L'objet est très sombre.** Godot multiplie l'albédo par la couleur de sommet quand le matériau
importé a `Vertex Color / Use as Albedo` coché. Décoche-le.

**Les transitions sont décalées par rapport à ce que j'ai peint.** Conversion sRGB quelque part —
Blender, l'import, ou le pipeline. Peins des valeurs franches.

**La peinture disparaît après un ré-export.** L'option de couleurs de sommet n'est pas cochée à
l'export ou à l'import.

**La transition est trop grossière.** Pas assez de sommets. Ça ne se corrige pas dans le shader :
il faut subdiviser le maillage à cet endroit, ou passer à une texture de masque.

**Le mélange par hauteur clignote au loin.** Les cartes de hauteur sont mipmapées, donc elles
s'aplatissent avec la distance, et le mélange redevient linéaire. C'est physiquement inévitable et
généralement acceptable ; si ça se voit, augmente `durete` avec la distance.

## Ce que ça coûte

**Les couleurs de sommet elles-mêmes sont gratuites** : quatre octets par sommet déjà transportés
par le matériel, interpolés par le rastériseur sans coût.

Ce qui coûte, c'est le nombre de couches. Trois couches, c'est **trois accès texture** — six avec
les hauteurs, neuf avec les normales. Sur un terrain qui remplit l'écran, c'est le poste dominant.

Les optimisations, dans l'ordre :

1. **Empaqueter les hauteurs dans un seul accès.** Les trois hauteurs dans les canaux `R`, `G`, `B`
   d'une seule texture : c'est ce que fait le shader fourni.
2. **Sauter les couches dont le poids est nul.** Un `if` par couche, avec la réserve habituelle sur
   la divergence (chapitre `00-bases/01`) — mais sur un terrain, de grandes zones sont
   monochromes, donc les branches convergent souvent.
3. **Réduire à deux couches au loin**, ou une seule.

## À toi

1. **Passe à quatre couches.** Utilise l'alpha comme quatrième poids au lieu de l'occlusion, et
   normalise sur quatre canaux. C'est ce que font la plupart des shaders de terrain.
2. **Mélange aussi les normales.** Trois normal maps pondérées par les mêmes poids, avec le mélange
   whiteout de la leçon 10. C'est ce qui fait vraiment croire aux matières différentes — la couleur
   seule ne suffit pas.
3. **Combine avec la leçon 04.** Multiplie le poids d'une couche par le masque « vers le haut » :
   la neige n'apparaît que là où l'artiste l'a peinte **et** où la surface est horizontale.
   L'artiste garde le contrôle, la géométrie fait le reste.
4. **Range de la souplesse dans l'alpha.** Reprends le shader de vent de la leçon 09 et fais-lui
   lire `COLOR.a` peint dans Blender. Tu tiens le pipeline complet du feuillage.
5. **Mesure le mélange par hauteur.** Compare linéaire et par hauteur sur ta machine : le surcoût
   est de quelques instructions et d'un accès texture. Regarde ensuite les deux images côte à côte,
   et décide si tu peux t'en passer.

**Leçon suivante : 22 — La BRDF à la main.** On arrête d'utiliser l'éclairage du moteur et on
l'écrit.
