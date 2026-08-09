# shader

Écrire des shaders pour un vrai moteur — **Godot 4**, **Unity URP** et **Unreal 5**.

Pas de théorie hors-sol : chaque leçon fabrique **un effet précis** que tu peux coller dans
un projet le soir même. Et chaque effet est écrit **trois fois**, une par moteur, parce que
c'est en voyant le même effet en GLSL et en HLSL qu'on arrête d'apprendre une syntaxe pour
commencer à apprendre le métier.

## Ce que contient une leçon

```
lecons/05-dissolution/
├── README.md        le tuto : le problème, la maths, la construction pas à pas, les pièges, le coût
├── godot.gdshader   fichier prêt à déposer dans un projet Godot 4
├── unity.shader     fichier prêt à déposer dans un projet Unity URP
├── unreal.md        le graphe Material node par node + l'équivalent HLSL en nœud Custom
└── banc.gdshader    quand la maths se comprend mieux à plat : un ColorRect 2D dans Godot
```

À partir de la leçon 17, certaines fournissent aussi un **script** (`.gd` et `.cs`) : au-delà d'un
certain point, un effet n'est plus un fichier de shader, c'est un système. Le shader ne fait que
lire ce que le script lui prépare.

Les fichiers de shader ne contiennent **aucun commentaire** : tout est expliqué dans le
`README.md` de la leçon. Le code que tu copies est le code propre, pas le code annoté.

## Comment lire ce cours

1. **Lis `00-bases/` en entier avant la première leçon.** Cinq chapitres courts, mais sans eux tu
   vas copier des lignes sans savoir pourquoi elles marchent. Le cinquième,
   `05-brancher-un-shader.md`, est la recette exacte pour rattacher un fichier à un objet dans
   chaque moteur, et la liste des réglages de pipeline que chaque leçon exige — garde-le ouvert.
2. **Fais les leçons dans l'ordre.** Chacune réutilise la précédente : la dissolution (05) est
   le masque (04) avec du bruit, l'hologramme (07) est le fresnel (06) plus des rayures.
3. **Ouvre le banc** quand une leçon en fournit un. Un shader se comprend en bougeant un chiffre
   et en regardant ce qui change — pas en le lisant.
4. **Fais la section « À toi »** à la fin de chaque leçon. C'est là que ça rentre.

## Prérequis

- Savoir ce qu'est une fonction et une variable. C'est tout côté programmation.
- Un moteur installé : Godot 4.3+, Unity 6 (ou 2022 LTS) avec URP, ou Unreal 5.3+.
  **Un seul suffit** — les leçons se lisent avec la colonne qui te concerne.
- Godot en plus, si tu veux les bancs 2D : il pèse 100 Mo, s'ouvre en deux secondes et recharge
  un shader à la sauvegarde. Même pour un dev Unity ou Unreal, c'est le meilleur bloc-notes à
  formules qui existe. Facultatif.

Tu n'as **pas** besoin de maths avancées. Un produit scalaire et une interpolation linéaire,
et on t'explique les deux.

## Le banc

Certaines leçons fournissent un `banc.gdshader` : un shader 2D à poser sur un `ColorRect` dans
Godot, où `UV` va simplement de 0 à 1. Pas de maillage, pas de lumière, pas de caméra — juste la
formule, à plat, rechargée à chaque sauvegarde.

C'est là qu'on comprend une courbe, un bruit ou un masque. Le vrai shader, lui, se règle dans le
moteur, sur une sphère. Le chapitre `00-bases/04-la-boucle-d-iteration.md` monte les deux scènes.

## Le programme

### Bloc 1 — Les bases

| # | Dossier | Ce qu'on fabrique | Ce que tu apprends au passage |
|---|---|---|---|
| 01 | `01-couleur-et-parametres` | un néon réglable | la structure d'un shader, exposer un réglage, HDR et bloom |
| 02 | `02-texture-et-uv` | un sol carrelé et teintable | échantillonner, `_ST`, répétition, sRGB contre linéaire |
| 03 | `03-le-temps` | une chute d'énergie qui défile | animer sans script, `TIME`/`_Time`, `fract`, `sin` |
| 04 | `04-masques-et-melanges` | de la neige sur les faces horizontales | `step`, `smoothstep`, `mix`, d'où vient un masque |
| 05 | `05-dissolution` | une désintégration à bord incandescent | bruit, seuil, `discard`, passes d'ombre et de profondeur |
| 06 | `06-fresnel` | un contour lumineux de silhouette | normale, direction de vue, produit scalaire, `pow` |
| 07 | `07-hologramme` | un hologramme rayé qui glitche | empiler des couches, transparence, tri, overdraw |
| 08 | `08-toon-et-contour` | un rendu cel avec trait noir | écrire son éclairage, quantifier, coque inversée |

### Bloc 2 — Surfaces

| # | Dossier | Ce qu'on fabrique | Ce que tu apprends au passage |
|---|---|---|---|
| 09 | `09-vent-feuillage` | un arbre qui ondule par rafales | shader de sommets, couleur de sommet, phase par position monde |
| 10 | `10-eau-normal-maps` | une surface d'eau à deux couches | l'espace tangent, la matrice TBN, le mélange whiteout |
| 11 | `11-profondeur-ecume` | l'écume au bord de l'eau | texture de profondeur, linéarisation, particules douces |
| 12 | `12-verre-et-chaleur` | du verre et de l'air brûlant | texture d'écran, décalage d'UV, flou par mipmap |
| 13 | `13-triplanar` | une falaise texturée sans UV | projection sur trois axes, mélange de trois normales |
| 14 | `14-parallax-pom` | un mur de briques en relief | marche dans une carte de hauteur, divergence réelle |
| 15 | `15-flipbook-particules` | une explosion depuis un atlas | index d'image, mélange entre images, overdraw |
| 16 | `16-decalcomanies` | un impact de balle qui épouse le décor | remonter de la profondeur à l'espace local |

### Bloc 3 — Interaction et données

| # | Dossier | Ce qu'on fabrique | Ce que tu apprends au passage |
|---|---|---|---|
| 17 | `17-neige-ecrasee` | de la neige qui garde les empreintes | texture de rendu persistante, lecture en vertex, lissage indépendant du framerate |
| 18 | `18-bouclier-impacts` | un bouclier qui encaisse | tableaux d'uniformes, tampon circulaire, grille hexagonale, intersection |
| 19 | `19-vertex-animation-textures` | une créature animée sans os | `texelFetch`, `VERTEX_ID`, cuisson d'animation, et les cuiseurs qui vont avec |
| 20 | `20-instanciation` | mille touffes en une passe | données par instance, ce que l'instanciation supprime et ce qu'elle ne supprime pas |

### Bloc 4 — Éclairage

| # | Leçon | Ce que tu apprends au passage |
|---|---|---|
| 21 | La BRDF à la main | pourquoi GGX, Smith, Fresnel-Schlick, conservation de l'énergie |
| 22 | Éclairage personnalisé dans le moteur | `light()` en Godot, `LightingData` en URP, Shading Model en Unreal |
| 23 | Ombres | shadow map, biais, acné et Peter Panning, PCF, cascades |
| 24 | Réflexions et IBL | sondes, roughness et mips, plan de réflexion |

### Bloc 5 — Effets d'écran

| # | Leçon | Ce que tu apprends au passage |
|---|---|---|
| 25 | Post-traitement : bloom et tonemap | passes de réduction, ACES, l'ordre des opérations |
| 26 | Contours par profondeur et normales | opérateur de Sobel, seuils qui tiennent à toutes distances |
| 27 | Volumétrique : brouillard et rais de lumière | marche dans un volume, bruit bleu, sous-échantillonnage |
| 28 | Raymarching intégré au moteur | rendre une SDF avec la bonne profondeur, se mêler à la scène |

### Bloc 6 — GPU

| # | Leçon | Ce que tu apprends au passage |
|---|---|---|
| 29 | Compute : particules GPU | groupes de travail, buffers, barrières |
| 30 | Compute : herbe et rendu indirect | générer la géométrie sur le GPU, `DrawIndirect` |
| 31 | Optimiser : mesurer puis corriger | divergence, occupancy, ALU contre bande passante, RenderDoc |
| 32 | Précision et pièges | `half` sur mobile, mips et dérivées, coutures, NaN |

## Vérification

Aucun shader de ce cours n'est publié sans avoir été **compilé pour de vrai**, par Godot et par
Unity, en ligne de commande :

```bash
./verif/verifier.sh
```

Voir `verif/README.md` pour ce que ça attrape et ce que ça n'attrape pas.

## État

**Blocs 1, 2 et 3 écrits et complets** — les cinq chapitres de `00-bases/`, l'aide-mémoire, et
vingt leçons avec leurs fichiers Godot, Unity et Unreal.

À la dernière vérification : **43 shaders Godot et 21 shaders Unity compilent, zéro erreur, zéro
avertissement.**

Quelques leçons fournissent plus que les cinq fichiers habituels :

| Leçon | En plus |
|---|---|
| 08 | `godot-contour.gdshader` — le contour est une seconde passe, donc un second matériau |
| 17 | `godot-pinceau` et `godot-effacement`, plus `neige.gd` / `Neige.cs` et `unity-empreinte.shader` |
| 18 | `bouclier.gd` / `Bouclier.cs` — enregistrement des impacts en tampon circulaire |
| 19 | `cuire_vat.gd` / `CuiseurVAT.cs` — les cuiseurs d'animation |
| 20 | `foret.gd` / `Foret.cs` — peuplement et données par instance |

Les blocs 4 à 6 arrivent dans l'ordre du sommaire.
