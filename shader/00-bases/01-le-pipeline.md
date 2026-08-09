# 01 — Ce qu'est vraiment un shader

## Le malentendu à dissiper tout de suite

Un shader n'est pas « un effet visuel ». Un shader est **un petit programme que le GPU exécute
des millions de fois par image**, une fois par sommet puis une fois par pixel. Tout ce que tu
vois à l'écran dans un jeu — absolument tout, y compris une surface mate parfaitement banale —
est sorti d'un shader.

La conséquence est la règle numéro un du métier :

> Ton shader ne sait rien. Il ne sait pas ce qu'il y a à côté, il ne sait pas ce qui a été
> dessiné avant, il ne se souvient pas de l'image précédente. Il reçoit quelques nombres et il
> doit produire une couleur.

Toute la difficulté des shaders vient de là. Un contour autour d'un objet est facile à décrire
(« si le pixel d'à côté est le fond, dessine du noir ») et pénible à faire, parce qu'un pixel
n'a pas accès à son voisin. Chaque leçon de ce cours est, au fond, une astuce pour contourner
cette ignorance.

## Le trajet d'un triangle

Tu poses un cube dans ta scène. Voilà ce qui se passe, à chaque image, pour chacun de ses
triangles.

**1. Le CPU envoie.** Le moteur dit au GPU : « dessine ces sommets, avec ce shader, ces textures
et ces réglages ». Cet envoi s'appelle un **draw call**. Il coûte cher côté CPU — c'est pour ça
que les moteurs regroupent les objets qui partagent un matériau.

**2. Le shader de sommets (vertex shader).** Il tourne **une fois par sommet**. Un cube en a 24,
un personnage en a 30 000. Son travail obligatoire : transformer la position du sommet, écrite
dans le repère de l'objet, vers le repère de l'écran. Son travail facultatif — et c'est là qu'on
s'amuse : **bouger** ce sommet avant de le transformer. Le vent dans le feuillage (leçon 09),
un drapeau, une déformation à l'impact : tout ça vit ici.

**3. La rastérisation.** Le GPU regarde le triangle projeté et détermine quels pixels de l'écran
il recouvre. Ce n'est pas programmable, mais il se passe une chose essentielle : **tout ce que
le shader de sommets a produit est interpolé** entre les trois sommets. Si le sommet A a l'UV
`(0,0)` et le sommet B l'UV `(1,0)`, un pixel à mi-chemin reçoit `(0.5, 0)`. Gratuitement. Cette
interpolation automatique est le tuyau par lequel les données passent du vertex au fragment.

**4. Le shader de fragments (pixel shader).** Il tourne **une fois par pixel couvert**. Un objet
plein écran en 1080p, c'est deux millions d'exécutions. Il reçoit les valeurs interpolées et
produit une couleur. C'est ici que se passent 80 % de ce cours.

**5. Les tests et le mélange.** Le pixel produit est comparé au tampon de profondeur (est-il
derrière quelque chose ?), éventuellement mélangé avec ce qui est déjà là (transparence), puis
écrit.

## Pourquoi l'ordre coûte cher

Retiens les ordres de grandeur. Pour un personnage de 30 000 sommets qui occupe un quart de
l'écran en 1080p :

| Étape | Nombre d'exécutions |
|---|---|
| shader de sommets | 30 000 |
| shader de fragments | ~500 000 |

Le fragment tourne **seize fois plus souvent**. D'où l'astuce la plus rentable des shaders :
**calcule dans le vertex ce qui peut y être calculé**, et laisse l'interpolation faire le
travail. Un calcul qui varie doucement sur la surface (une direction de vent, une phase, une
teinte de fond) n'a aucune raison d'être refait par pixel.

Attention quand même : l'interpolation est **linéaire**. Une normale interpolée n'est plus
unitaire, un vecteur normalisé dans le vertex ne l'est plus dans le fragment. D'où les
`normalize()` en début de shader de fragments que tu verras partout.

## Le parallélisme, et pourquoi `if` est un piège

Un GPU n'exécute pas un pixel à la fois. Il en traite **32 ensemble** (un *warp* chez NVIDIA, un
*wavefront* de 32 ou 64 chez AMD), et ces 32 pixels exécutent **la même instruction en même
temps**. Toujours.

Alors que se passe-t-il si ton shader contient ceci, et que dans un groupe de 32 pixels, 20 sont
dans le `if` et 12 dans le `else` ?

```glsl
if (masque > 0.5) {
    couleur = effet_cher();
} else {
    couleur = effet_pas_cher();
}
```

Le GPU exécute **les deux branches** pour les 32 pixels, et jette le résultat inutile. Le coût
est celui des deux branches additionnées. C'est ce qu'on appelle la **divergence**, et c'est la
raison pour laquelle tu verras si peu de `if` dans ce cours et tant de `mix` et de `step`.

Un `if` n'est gratuit que quand les 32 pixels prennent la **même** branche — ce qui arrive
souvent en pratique (un `if` sur un uniforme, ou sur une zone bien contiguë de l'écran).

## Ce que le shader reçoit, et de qui

Trois canaux, à ne pas confondre :

| Canal | D'où ça vient | Change à quelle fréquence |
|---|---|---|
| **attributs** de sommet | le maillage : position, normale, UV, couleur de sommet, tangente | par sommet |
| **uniformes** | toi, via le matériau ou un script | une fois par draw call |
| **textures** | des images en mémoire GPU | lues à la demande, par pixel |

Un **uniforme** est constant pendant tout le dessin de l'objet : les 500 000 pixels lisent la
même valeur. C'est ce qui les rend bon marché, et c'est pourquoi tout paramètre de matériau en
est un.

Une **texture** n'est pas qu'une image. C'est un tableau de nombres qu'on peut interroger avec
des coordonnées continues. On y range des couleurs, mais aussi des normales (leçon 10), des
hauteurs (14), des masques (04), du bruit (05), des positions d'animation (19). Dès qu'un calcul
est trop cher pour être refait par pixel, on le pré-calcule dans une texture.

## Ce qui est possible et ce qui ne l'est pas

Ce que le shader **peut** faire :

- décider de la couleur d'un pixel selon sa position, son UV, sa normale, le temps ;
- déplacer des sommets ;
- lire autant de textures qu'il veut, y compris ce que la caméra a déjà rendu ;
- refuser d'écrire un pixel (`discard`).

Ce qu'il **ne peut pas** faire :

- créer de la géométrie qui n'existait pas (sauf en compute, leçon 30) ;
- lire le pixel voisin dans la même passe — d'où les passes multiples ;
- se souvenir de l'image précédente sans qu'on la lui donne explicitement ;
- écrire ailleurs qu'à sa propre position.

Chaque fois qu'un effet te paraît impossible, la question est : **quelle information me manque,
et dans quelle texture puis-je la ranger ?**

## À retenir

1. Vertex = une fois par sommet, fragment = une fois par pixel, souvent 16 fois plus.
2. Ce que le vertex produit est interpolé gratuitement jusqu'au fragment.
3. Un pixel ne sait rien de ses voisins.
4. Un `if` divergent coûte les deux branches.
5. Une texture, ce sont des données, pas seulement une image.

Chapitre suivant : **02 — Les espaces**, c'est-à-dire la seule vraie difficulté conceptuelle du
métier.
