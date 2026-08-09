# 03 — Le temps : une chute d'énergie qui défile

## Ce qu'on fabrique

Une bande lumineuse qui **défile** et **pulse** : chute d'eau magique, tapis roulant, flux
d'énergie sur un mur de vaisseau, coulée de lave, barre de chargement. Trois lignes de shader
remplacent une animation et un script.

## L'idée

Le shader reçoit une variable qui augmente : le temps depuis le lancement, en secondes. À partir
de là, **tout ce qui bouge est une addition**.

| Ce que tu veux | Ce que tu écris |
|---|---|
| ça défile dans une direction | `uv + vitesse * TIME` |
| ça va et vient doucement | `sin(TIME * k)` |
| ça monte puis retombe d'un coup | `fract(TIME * k)` |
| ça tourne | passer `TIME * k` dans une matrice de rotation |

Deux détails changent tout.

**Le défilement s'écrit dans l'UV, pas dans la couleur.** On ne fait pas bouger l'image, on fait
bouger l'endroit où on va la chercher. Le résultat est le même à l'écran, mais le premier est
gratuit et le second est impossible.

**`sin` va de -1 à 1, et une couleur va de 0 à 1.** D'où le `* 0.5 + 0.5` qu'on voit partout :
c'est la remise à l'échelle de l'intervalle. Si tu l'oublies, la moitié de ton animation est
sous zéro, donc noire, donc invisible — et l'effet a l'air de clignoter au lieu de respirer.
C'est le réflexe 2 du chapitre `00-bases/04`.

## Godot

```glsl
uniform vec2 vitesse = vec2(0.0, -0.35);
uniform float pulsation_vitesse : hint_range(0.0, 12.0) = 2.0;
uniform float pulsation_force : hint_range(0.0, 1.0) = 0.35;

void fragment() {
    vec2 uv = UV * carrelage + vitesse * TIME;
    float bande = texture(motif, uv).r;

    float pulsation = sin(TIME * pulsation_vitesse) * 0.5 + 0.5;
    float intensite = mix(1.0 - pulsation_force, 1.0, pulsation);

    ALBEDO = vec3(0.0);
    EMISSION = teinte * bande * emission_force * intensite;
}
```

`vitesse` est un `vec2` : `(0, -0.35)` fait descendre la texture d'un tiers d'image par seconde,
`(0.2, 0)` la fait glisser vers la droite. Le signe est contre-intuitif — **soustraire à l'UV
fait avancer l'image dans le sens positif**, parce qu'on déplace le point de lecture, pas
l'image.

`mix(1.0 - pulsation_force, 1.0, pulsation)` mérite qu'on s'y arrête : avec une force de 0.35,
l'intensité oscille entre 0.65 et 1. On ne module jamais entre 0 et 1 pour une pulsation, sinon
l'objet s'éteint complètement à chaque cycle et ça a l'air cassé. **Une bonne pulsation reste
dans le dernier tiers.**

`render_mode cull_disabled` affiche les faces arrière : indispensable pour une bande, un rideau
d'eau ou une flamme faite d'un simple plan.

## Unity URP

```hlsl
float2 uv = IN.uv + _Vitesse.xy * _Time.y;
half bande = SAMPLE_TEXTURE2D(_Motif, sampler_Motif, uv).r;

half pulsation = sin(_Time.y * _PulsationVitesse) * 0.5 + 0.5;
half intensite = lerp(1.0 - _PulsationForce, 1.0, pulsation);

return half4(_Teinte.rgb * bande * _EmissionForce * intensite, 1.0);
```

**`_Time` est un `float4`, pas un flottant.** Il contient quatre versions du même temps :

| Composante | Valeur |
|---|---|
| `_Time.x` | `t / 20` |
| `_Time.y` | `t` — celle que tu veux dans 95 % des cas |
| `_Time.z` | `t * 2` |
| `_Time.w` | `t * 3` |

Écrire `_Time` sans composante donne `_Time.x`, soit vingt fois trop lent. C'est une erreur
classique, et silencieuse : l'effet marche, il est juste bizarrement mou.

Il existe aussi `_SinTime` et `_CosTime`, construits sur le même modèle. Ils économisent un
`sin`, ce qui est négligeable — mais ils sont pratiques.

**Le défilement est fait ici dans le fragment**, alors que la leçon 02 mettait `TRANSFORM_TEX`
dans le vertex. Pourquoi ? Parce qu'une addition constante peut aller dans le vertex sans
problème, et c'est même mieux. Déplace-la si tu veux, le résultat est identique et moins cher.
La règle : **tout ce qui varie linéairement sur la surface peut monter dans le vertex.**

`Cull Off` dans le bloc `SubShader` ou `Pass` est l'équivalent de `cull_disabled`.

## Unreal

Voir `unreal.md` — les nœuds `Panner` et `Rotator`, la période du nœud `Sine` qui vaut **1 et non
2π**, et la case `Ignore Pause`.

## Le banc

`banc.gdshader` affiche cinq bandes, chacune animée par une fonction différente. De haut en bas :

| Bande | Fonction | Aspect |
|---|---|---|
| 1 | `fract(x - t)` | dent de scie : monte puis coupe net |
| 2 | `sin(x - t)` remis en 0..1 | vague douce |
| 3 | `abs(fract(x - t) * 2 - 1)` | triangle : monte et descend, angles nets |
| 4 | le triangle passé dans `smoothstep` | triangle adouci, sans angles |
| 5 | `step(0.5, fract(x - t))` | créneau : allumé/éteint |

Ces cinq courbes couvrent presque tout ce que tu animeras. **La 1 pour un défilement, la 2 pour
une respiration, la 3 pour un aller-retour, la 4 pour un aller-retour sans à-coups, la 5 pour un
clignotement.**

Note que la bande 3 a des **angles** au sommet, et pas la 4 : c'est visible à l'œil, et c'est la
différence entre une animation qui a l'air mécanique et une qui a l'air vivante. Le passage par
`smoothstep` est le plus petit lissage possible ; on appelle ça de l'*easing*, et c'est la même
idée qu'en animation d'interface.

Ce banc contient une chaîne de `if / else if` — exactement ce que la leçon `00-bases/01`
déconseille, puisque les cinq branches divergent entre bandes voisines. C'est assumé : ici on
compare des formules, on ne rend pas un jeu. Dans un vrai shader, on choisirait une seule formule.

## Les pièges

**Après une heure de jeu, l'animation saccade.** C'est réel, et déroutant la première fois. Un
`float` a environ sept chiffres significatifs : à `t = 100000`, l'écart entre deux valeurs
représentables dépasse le centième de seconde, et `sin(t * k)` devient granuleux.

Les parades, par moteur :

- **Godot** : le paramètre de projet `rendering/limits/time/time_rollover_secs` (3600 par défaut)
  fait repartir `TIME` à zéro périodiquement. Ça règle le problème, **et ça en crée un autre** :
  si ton animation n'a pas une période qui divise 3600, elle saute au moment du retour à zéro.
- **Unity** : `_Time` repart à zéro au chargement de scène. Pour une session très longue, passe
  ton propre temps via `Shader.SetGlobalFloat`, calculé en `Mathf.Repeat(Time.time, periode)`.
- **Unreal** : le champ `Period` du nœud `Time`, avec la même précaution.

**L'animation s'arrête quand le jeu est en pause.** Souvent voulu (l'eau doit se figer), parfois
non (l'interface doit continuer). Unreal : `Ignore Pause`. Unity : `Time.timeScale` affecte
`_Time`, donc passe un temps global à toi si tu veux qu'un effet d'interface survive. Godot :
`TIME` suit `Engine.time_scale` — même solution, un uniforme alimenté depuis un script.

**Ça défile dans le mauvais sens.** Inverse le signe de la vitesse. Et rappelle-toi que l'axe V
descend en Godot et Unreal, monte en Unity : le même `(0, -0.35)` ne va pas dans le même sens
selon le moteur.

**Ça défile mais l'image se casse à la jointure.** La texture n'est pas répétable sans couture,
ou son mode de répétition est en `Clamp`. Retour à la leçon 02.

**Deux objets identiques sont parfaitement synchronisés.** Visible et laid dès qu'il y en a
trois. La correction est à la leçon 09 : décaler la phase avec la position monde de l'objet.

## Ce que ça coûte

Rien, ou presque. Une addition, un `sin`. Le `sin` coûte quelques cycles, ce qui est invisible
sauf dans une boucle.

Le vrai coût du défilement est ailleurs : **il empêche certaines optimisations de mise en
cache**, et sur une surface qui occupe tout l'écran, le nombre d'accès texture reste le poste
dominant. Deux couches qui défilent, c'est deux accès texture, donc deux fois le prix.

## À toi

1. **Deux couches, deux vitesses.** Échantillonne le motif deux fois, avec des carrelages et des
   vitesses différentes, et multiplie les résultats. C'est *la* recette de toute chute d'eau et
   de tout nuage de fumée : deux couches lentes produisent un mouvement bien plus riche qu'une
   seule rapide. Essaie `(0, -0.2)` et `(0, -0.35)` avec des carrelages `1` et `1.7`.
2. **Un rapport non entier.** Donne aux deux couches des vitesses de rapport 2 exactement, puis
   de rapport 1.7. Le premier boucle visiblement, le second a l'air aléatoire. **Éviter les
   rapports entiers** est une règle générale des effets en boucle.
3. **Remplace la pulsation par un battement de cœur.** `pow(sin(TIME * k) * 0.5 + 0.5, 4.0)`
   passe l'essentiel du temps près de zéro avec des pics brefs. Change l'exposant et regarde la
   forme changer : `pow` sur une valeur de 0 à 1 est le réglage de courbe le moins cher qui
   existe.
4. **Fais tourner l'UV.** Autour de son centre :
   ```glsl
   vec2 c = UV - 0.5;
   float a = TIME * 0.5;
   vec2 uv = vec2(c.x * cos(a) - c.y * sin(a), c.x * sin(a) + c.y * cos(a)) + 0.5;
   ```
   Un portail, un cercle de magie, un radar. Note qu'il faut **recentrer avant de tourner**,
   sinon la rotation se fait autour du coin.

**Leçon suivante : 04 — Masques et mélanges.** Là où le métier commence vraiment.
