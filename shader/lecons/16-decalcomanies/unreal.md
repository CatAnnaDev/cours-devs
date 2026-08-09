# Leçon 16 en Unreal 5 — décalcomanies

Unreal est le seul des trois moteurs où la décalcomanie est un **type de matériau natif**. Tout ce
que les versions Godot et Unity écrivent à la main, le moteur le fait.

## Le matériau

1. `Content Browser` → clic droit → `Material`.
2. Nœud racine, `Details` → **`Material Domain` : `Deferred Decal`**.
3. **`Decal Blend Mode`** — c'est le réglage qui décide de ce que le décal remplace :

| Mode | Ce qui est écrit |
|---|---|
| `Translucent` | couleur, normale, rugosité, métal — le plus complet |
| `Stain` | multiplie la couleur, garde le reste : idéal pour de la saleté |
| `Normal` | uniquement la normale : des rayures en relief sur une surface intacte |
| `Emissive` | uniquement de l'émission : des runes lumineuses, un marquage |
| `DBuffer` | version compatible avec l'éclairage précalculé, plus coûteuse |

4. Branche ta texture sur `Base Color` et son alpha sur `Opacity`.

## Le poser

`Place Actors` → **`Decal Actor`**, ou ajoute un `Decal Component` à une Blueprint. Assigne le
matériau, et redimensionne la boîte : sa face **-X** est la direction de projection.

Attention, c'est une différence de convention : Godot et Unity projettent selon l'axe **Y** dans
les shaders de cette leçon, Unreal selon **-X**. Le décal apparaît donc tourné de 90° si tu portes
un matériau d'un moteur à l'autre.

## Les réglages qui comptent

| Réglage | Où | Rôle |
|---|---|---|
| `Sort Order` | sur le composant | l'ordre entre décals qui se recouvrent |
| `Fade Screen Size` | sur le composant | à quelle taille à l'écran le décal disparaît |
| `Decal Response` | sur les **matériaux du décor** | un matériau peut refuser les décals |
| `Opacity` | dans le matériau | multiplié par le `Fade` du composant |

**`Decal Response`** vaut d'être connu : sur le matériau d'un mur, `Details` →
`Decal Response` → `None` empêche les décals de s'y afficher. C'est ce qui évite les impacts de
balle sur les vitres et l'eau.

## L'angle de projection

Unreal ne fournit pas de rejet par angle prêt à l'emploi, mais l'équivalent est faisable :

1. `PixelNormalWS` — la normale de la surface **sous** le décal.
2. `Multiply` avec un vecteur constant représentant l'axe du décal, obtenu par `TransformVector`
   d'un `(−1, 0, 0)` de `Local Space` vers `World Space`.
3. `DotProduct` → `Abs` → `SmoothStep` entre le cosinus de l'angle limite et 1 → multiplié à
   l'`Opacity`.

Le résultat est meilleur que le `discard` des versions Godot et Unity : au lieu d'une coupure
franche, le décal **s'estompe** sur les surfaces trop inclinées.

## Et si tu n'es pas en rendu différé

Sur mobile ou en Forward, `Deferred Decal` n'est pas disponible. Deux voies :

- **`DBuffer Decals`** — à activer dans les `Project Settings` → `Rendering`. Compatible Forward
  au prix d'un tampon supplémentaire.
- **Un décal en géométrie** : un maillage plaqué sur la surface, en `Translucent` avec un léger
  décalage de profondeur. Simple, sans coût de tampon, mais il faut connaître la géométrie à
  l'avance — ça marche pour un graffiti sur un mur plat, pas pour un impact sur une forme
  quelconque.

## L'équivalent en nœud Custom

Rien à écrire : c'est le seul cas du cours où le graphe natif fait strictement mieux que le HLSL
à la main. Le calcul de reconstruction de position décrit dans le `README.md` est fait par le
moteur, avant même que ton matériau s'exécute.

Lis quand même la version Godot ou Unity : c'est **exactement** ce qu'Unreal fait dans son
`Deferred Decal`, et le savoir change la façon dont on règle les décals.
