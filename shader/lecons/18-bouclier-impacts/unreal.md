# Leçon 18 en Unreal 5 — bouclier et impacts

## Les réglages

`Blend Mode` : `Additive`. `Shading Model` : `Unlit`. `Two Sided` coché.

Dans `Details` → `Translucency` : `Disable Depth Test` **décoché** — le bouclier doit être caché
par les murs.

## Le problème des tableaux

Unreal ne permet pas de déclarer un tableau de paramètres exposé dans une `Material Instance`.
Trois voies, par ordre de préférence :

### 1. Un `Material Parameter Collection` (jusqu'à 8 impacts)

`Content Browser` → clic droit → `Materials & Textures` → `Material Parameter Collection`. Ajoute
huit `Vector Parameters` nommés `Impact0` à `Impact7`. Chaque vecteur porte `(x, y, z, instant)`.

Dans le matériau, un nœud `CollectionParameter` par impact. C'est verbeux, et c'est la solution la
plus simple qui marche.

Depuis une Blueprint : `Set Vector Parameter Value` sur la collection.

**L'avantage sur les autres moteurs** : la collection est **globale**. Un impact enregistré une
fois est vu par tous les matériaux qui la lisent — le bouclier, un effet d'écran, une décalcomanie.

### 2. Une texture de données

Range les impacts dans une petite texture (8 × 1 en `RGBA32f`), mise à jour par Blueprint via
`Draw Material to Render Target`. Le matériau la lit avec un `TextureSample` en `Nearest`.

C'est la voie qui monte en nombre : soixante-quatre impacts coûtent le même travail de câblage que
huit. C'est ce qu'utilisent les vrais systèmes.

### 3. Niagara

Si les impacts viennent déjà d'un système de particules, laisse Niagara écrire dans un
`Grid2D Collection` ou un `Render Target`, et lis-le depuis le matériau. C'est la voie la plus
intégrée, et la plus lourde à mettre en place.

## Le graphe

**Le fresnel** — nœud `Fresnel`, comme à la leçon 06.

**La grille hexagonale** — deux options :

- une **texture** d'hexagones, carrelée : le plus simple, et l'artiste contrôle le motif ;
- le calcul procédural du `README.md`, dans un nœud `Custom`.

En production, la texture gagne presque toujours : un accès texture contre une dizaine
d'instructions, et on peut y peindre de l'usure et des variations.

**L'onde** — pour chaque impact :

1. `Distance` entre `ObjectLocalPosition` (ou `WorldPosition`) et la position de l'impact.
2. `Time` − l'instant de l'impact = l'âge.
3. `Multiply` l'âge par la vitesse = le rayon courant.
4. `Subtract`, `Abs`, `SmoothStep` inversé = l'anneau.
5. `Multiply` par `1 − âge / durée` pour l'extinction.

Fais-en une **fonction de matériau** (`MF_OndeImpact`) avec les entrées qui vont bien, et
instancie-la huit fois. Sinon le graphe devient illisible au troisième impact.

## L'intersection avec le décor

C'est le nœud **`DepthFade`** de la leçon 11, **inversé** : au lieu d'estomper au contact, on veut
s'allumer au contact.

```
1 - DepthFade(Opacity = 1, FadeDistance = 35)
```

Multiplie le résultat par une couleur vive et ajoute-le à l'`Emissive Color`. Le bouclier
s'illumine là où il traverse un mur ou le sol — c'est ce qui le pose dans la scène au lieu de le
faire flotter.

Rappel : `FadeDistance` est en centimètres. `0.35` en Godot ou Unity s'écrit `35`.

## L'équivalent en nœud Custom

La grille hexagonale, entrée `P` (Float2). Sortie `CMOT Float 1` — la distance au bord de
l'hexagone.

```hlsl
float2 maille = float2(1.0, 1.7320508);
float2 a = fmod(P, maille) - maille * 0.5;
float2 b = fmod(P - maille * 0.5, maille) - maille * 0.5;
float2 local = lerp(a, b, step(dot(b, b), dot(a, a)));
float2 q = abs(local);
return 0.5 - max(dot(q, normalize(float2(1.0, 1.7320508))), q.x);
```

Attention : `fmod` en HLSL garde le signe du dividende, contrairement à `mod` en GLSL. Sur des
coordonnées qui deviennent négatives — ce qui arrive dès qu'on centre la grille — le motif se
casse en miroir autour de l'origine. Ajoute un grand décalage positif avant le `fmod`, ou utilise
`P - floor(P / maille) * maille`.

C'est exactement le piège annoncé dans `AIDE-MEMOIRE.md`, et c'est la leçon où il mord.
