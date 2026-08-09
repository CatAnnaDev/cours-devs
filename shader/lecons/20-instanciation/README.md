# 20 — L'instanciation : mille objets, une passe, tous différents

## Ce qu'on fabrique

Un champ d'herbe, une forêt, une foule, un champ de débris : des milliers d'exemplaires du même
maillage, dessinés en **un seul appel de rendu**, et pourtant tous différents — teinte, taille,
orientation, phase de mouvement.

C'est la dernière leçon du bloc 3, et celle qui explique pourquoi les trois précédentes portaient
toutes le même détail : un `decalage_instance`, une graine, une couleur de sommet.

## L'idée : le coût est dans le nombre d'appels, pas dans le nombre de triangles

Rappel du chapitre `00-bases/01` : envoyer un objet au GPU s'appelle un **draw call**, et il coûte
cher **côté processeur**. Deux mille touffes d'herbe, ce sont deux mille draw calls — et le GPU
passe son temps à attendre.

L'instanciation renverse ça : **un seul appel, avec un tableau de transformations**. Le GPU
dessine le même maillage deux mille fois, en changeant de matrice à chaque fois, sans que le
processeur ait rien à dire entre deux.

| | Sans instanciation | Avec |
|---|---|---|
| appels de rendu pour 2000 touffes | 2000 | **1** (ou 2, la limite étant 1023 par lot en Unity) |
| travail processeur | proportionnel au nombre | quasi nul |
| triangles dessinés | identique | identique |

**Le nombre de triangles ne change pas.** C'est important : l'instanciation ne rend pas la
géométrie gratuite, elle supprime le coût d'organisation. Si tes deux mille touffes ont chacune
cinq mille triangles, tu auras toujours dix millions de triangles à dessiner.

## Le problème que ça crée : tout est identique

Un seul maillage, un seul matériau, donc les mêmes uniformes pour tout le monde. Deux mille
touffes rigoureusement identiques, à la position près. Ça se voit immédiatement, et c'est laid.

D'où la question centrale de cette leçon : **comment donner une valeur différente à chaque
instance, sans casser l'appel unique ?**

La réponse est un canal de données par instance, et chaque moteur a le sien :

| Moteur | Canal | Contenu |
|---|---|---|
| Godot | `INSTANCE_CUSTOM` | un `vec4` par instance |
| Godot | `INSTANCE_ID` | le numéro de l'instance |
| Unity | `UNITY_DEFINE_INSTANCED_PROP` | un tableau, une entrée par instance |
| Unreal | `PerInstanceRandom` | un flottant tiré au sort |
| Unreal | `PerInstanceCustomData` | des flottants choisis |

**Quatre nombres suffisent presque toujours.** Le shader fourni y range une graine, une maturité et
une taille — et la graine sert ensuite à décorréler tout le reste.

## Godot

```glsl
void vertex() {
    float graine = INSTANCE_CUSTOM.x;
    float maturite = INSTANCE_CUSTOM.y;
    float taille = INSTANCE_CUSTOM.z;

    teinte_instance = mix(couleur_jeune, couleur_seche, maturite);
    VERTEX *= mix(1.0 - variation_echelle, 1.0 + variation_echelle, taille);
    ...
}
```

`INSTANCE_CUSTOM` est un `vec4` fourni par le `MultiMesh`. Il faut l'activer :

```gdscript
multimesh.use_custom_data = true
multimesh.set_instance_custom_data(i, Color(graine, maturite, taille, 1.0))
```

**Le type est `Color`**, ce qui est trompeur : ce ne sont pas des couleurs, ce sont quatre
flottants. Godot réutilise le type par commodité. Rien n'oblige les valeurs à rester entre 0 et 1
— sauf si la cible est en 8 bits, ce qui n'est pas le cas ici.

**L'échelle est appliquée à `VERTEX`, pas à la transformation de l'instance.** C'est délibéré :
mettre l'instance à l'échelle changerait aussi l'amplitude du vent, qui est ajoutée après. En
multipliant la position locale, la taille et le mouvement restent indépendants.

**Le script** (`foret.gd`) fait deux choses à noter :

```gdscript
var distance := sqrt(hasard.randf()) * rayon_zone
```

La racine carrée n'est pas décorative : sans elle, les instances se concentrent au centre du
disque. La surface d'un anneau croît avec le rayon, donc pour une densité uniforme il faut
distribuer selon la racine. C'est un classique qu'on redécouvre toujours trop tard.

```gdscript
hasard.seed = graine_aleatoire
```

Un générateur **à graine fixe**. La forêt est donc identique à chaque lancement, ce qui est
indispensable : sans ça, impossible de reproduire un bug d'affichage, et le level designer voit
une scène différente à chaque ouverture.

## Unity URP

```hlsl
#pragma multi_compile_instancing

UNITY_INSTANCING_BUFFER_START(Variation)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Variation)
UNITY_INSTANCING_BUFFER_END(Variation)
```

Les macros font trois choses invisibles : elles créent un tableau côté GPU, elles ajoutent l'index
d'instance aux structures d'entrée, et elles génèrent une variante non instanciée du shader pour
les cas où l'instanciation est désactivée.

Il faut **quatre marqueurs**, et en oublier un donne des symptômes déroutants :

| Marqueur | Où | Sans lui |
|---|---|---|
| `UNITY_VERTEX_INPUT_INSTANCE_ID` | dans `Attributes` | erreur de compilation |
| `UNITY_SETUP_INSTANCE_ID(IN)` | début de `vert` **et** de `frag` | toutes les instances lisent l'entrée 0 |
| `UNITY_TRANSFER_INSTANCE_ID(IN, OUT)` | dans `vert` | le fragment ne sait plus quelle instance |
| `UNITY_VERTEX_INPUT_INSTANCE_ID` | dans `Varyings` | idem |

**Le symptôme classique** : toutes les instances ont la couleur de la première. C'est
`UNITY_SETUP_INSTANCE_ID` qui manque.

**`Graphics.RenderMeshInstanced`** dessine par lots de 1023 au maximum — une limite de taille de
buffer d'uniformes, pas un choix de Unity. `Foret.cs` découpe donc en lots et garde un
`MaterialPropertyBlock` par lot.

**`worldBounds` n'est pas optionnel** : c'est ce que le moteur utilise pour le culling. Trop
petit, la forêt disparaît quand on regarde ailleurs ; absent, rien ne s'affiche.

## Unreal

Voir `unreal.md` : `Instanced Static Mesh`, `HISM`, Nanite, et les deux nœuds à connaître —
**`PerInstanceRandom`** (un nombre gratuit par instance) et **`PerInstanceCustomData`** (des
valeurs choisies). Plus `WPO Disable Distance`, qui règle en même temps le coût du vent au loin et
le problème de boîte englobante.

## Le banc

`banc.gdshader` montre quatre colonnes, et c'est une démonstration en quatre étapes de ce que la
variation apporte.

| Colonne | Ce qui varie |
|---|---|
| 1 | rien : toutes identiques, toutes synchrones |
| 2 | + la **teinte** |
| 3 | + la **taille** |
| 4 | + la **phase** du mouvement |

Regarde-les de gauche à droite, puis reviens à la première. Ce qu'on constate :

**La colonne 1 ne ressemble à rien.** Un motif régulier, un mouvement de bloc. L'œil détecte
immédiatement la répétition.

**La teinte à elle seule fait la moitié du travail.** C'est la variation la moins chère — un
`mix` sur une couleur — et la plus payante.

**La phase est ce qui tue le motif en mouvement.** Colonne 3, à l'arrêt, ça a l'air varié ; dès
que ça bouge, la synchronisation trahit tout. C'est exactement ce que disait la leçon 09, et c'est
le défaut qu'on voit dans beaucoup de jeux.

**Aucune de ces variations ne coûte un appel de rendu.** C'est tout l'intérêt : quatre nombres par
instance, et le champ passe de « copié-collé » à « vivant ».

## Les pièges

**Toutes les instances sont identiques.** Le canal de données n'est pas activé
(`use_custom_data` en Godot), les marqueurs Unity manquent, ou le tableau n'est pas transmis.

**Toutes les instances ont la valeur de la première (Unity).** `UNITY_SETUP_INSTANCE_ID` oublié
dans `frag`.

**Les instances disparaissent quand on regarde ailleurs.** `worldBounds` trop petit en Unity,
`Custom AABB` en Godot, `Bounds Scale` en Unreal.

**Les instances se concentrent au centre.** La distribution radiale sans racine carrée.

**La forêt change à chaque lancement.** Générateur sans graine fixe.

**Ça ne va pas plus vite qu'avant.** Deux causes fréquentes : l'instanciation n'est pas active
(vérifie le compteur d'appels de rendu, pas le compteur d'images par seconde), ou le goulot n'était
pas là. **Mesure avant d'optimiser** — si tu es limité par le nombre de triangles ou par le
remplissage de pixels, l'instanciation ne change rien.

**Les ombres coûtent le double.** Chaque instance est aussi dessinée dans la shadow map. Un champ
d'herbe qui projette des ombres coûte deux fois le prix. C'est souvent le premier réglage à couper.

## Ce que ça coûte

**L'instanciation supprime un coût processeur, pas un coût GPU.** À retenir avant tout.

Ce qui reste, par ordre d'importance sur un champ d'herbe :

**Le remplissage.** Deux mille touffes alpha-testées qui se recouvrent, c'est de l'overdraw, et le
découpage alpha désactive le test de profondeur anticipé (leçon 05). C'est presque toujours le
poste dominant.

**La géométrie.** Deux mille touffes de deux cents triangles, c'est quatre cent mille triangles.
Le vent s'y ajoute, par sommet.

**Les ombres.** Une seconde passe complète.

Les optimisations qui marchent, dans l'ordre :

1. **Un LOD par distance** — moins de triangles, pas de vent, et à la fin une simple carte de
   billboard. C'est le levier numéro un.
2. **Couper les ombres** au-delà de quelques mètres.
3. **Réduire la densité avec la distance**, ce qui est aussi plus joli.
4. **Regrouper plusieurs brins dans un seul maillage** : dix brins par instance divisent le nombre
   d'instances par dix, et l'overdraw reste identique.

## À toi

1. **Ajoute la quatrième composante.** `INSTANCE_CUSTOM.w` est libre : sers-t'en pour une
   inclinaison, ou pour un état (fraîche, fanée, brûlée) piloté par le jeu. Une valeur, et le champ
   raconte quelque chose.
2. **Une seule graine, quatre variations.** Remplace les trois nombres tirés au sort par un seul,
   passé dans trois fonctions de hachage différentes — comme dans `unreal.md`. Tu libères trois
   composantes pour autre chose, et tu comprends pourquoi `PerInstanceRandom` suffit à Unreal.
3. **Un LOD.** Deux maillages, deux `MultiMesh`/lots, un seuil de distance. Mesure avant et après
   sur deux mille instances : c'est l'optimisation la plus rentable de la leçon.
4. **Compare les compteurs.** Deux mille objets séparés contre deux mille instances : regarde le
   nombre d'appels de rendu **et** les images par seconde. Sur une machine de bureau, l'écart de
   framerate peut être faible alors que l'écart d'appels est de 2000 à 1. Comprendre pourquoi —
   où était vraiment le goulot — vaut plus que le gain lui-même.
5. **Combine avec la leçon 17.** Le champ d'herbe qui se couche là où le joueur passe : lis la
   texture de déformation dans ce shader, et incline les brins selon son gradient. Deux leçons du
   bloc, un système complet.

---

**Fin du bloc 3.** Les quatre leçons partageaient une même idée : **faire entrer dans le shader
des informations qu'il n'a pas** — une mémoire (17), des événements (18), une animation cuite (19),
une identité (20).

Et toutes les quatre ont demandé du code hors du shader. C'est normal, et c'est le message du
bloc : au-delà d'un certain point, un effet n'est plus un fichier de shader, c'est un **système**.

Le bloc 4 attaque l'éclairage pour de bon : la BRDF écrite à la main, l'intégration dans le
pipeline du moteur, les ombres, et les réflexions.
