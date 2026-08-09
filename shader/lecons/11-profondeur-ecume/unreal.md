# Leçon 11 en Unreal 5 — profondeur, écume et fondu doux

## Le nœud qui fait tout : `DepthFade`

Unreal fournit exactement cet effet en un nœud.

`DepthFade` a deux entrées :

- **`Opacity`** — l'opacité de base, avant le fondu ;
- **`FadeDistance`** — sur quelle distance, en centimètres, l'objet s'efface à l'approche d'une
  autre surface.

Il renvoie l'opacité corrigée : 0 pile au contact du décor, `Opacity` à `FadeDistance` de
distance. Branché sur **Opacity**, il supprime la ligne de coupe nette qu'un plan translucide
laisse quand il traverse le sol.

**Le matériau minimal :**

- `Blend Mode` : `Translucent`
- `Shading Model` : `Unlit` (ou `Default Lit` si tu veux l'éclairage)
- `DepthFade` (`FadeDistance` ≈ `30`, soit 30 cm) → **Opacity**

C'est le premier réglage à faire sur **toute** particule translucide d'Unreal. Sans lui, un nuage
de fumée traverse le sol avec une arête franche, et ça se voit dans tous les projets où personne
n'y a pensé.

## L'épaisseur d'eau, à la main

`DepthFade` donne un fondu, pas l'épaisseur. Pour teinter l'eau selon sa profondeur et poser une
ligne d'écume, il faut le calcul complet :

1. `SceneDepth` — la profondeur de ce qui est **derrière** le pixel, en unités monde.
   Ce nœud n'est disponible que sur un matériau `Translucent`.
2. `PixelDepth` — la profondeur **du pixel courant**.
3. `Subtract` : `SceneDepth` − `PixelDepth` = l'épaisseur d'eau traversée, en centimètres.

À partir de là :

- **Le teintage** : `Divide` par un `ScalarParameter` **ProfondeurMax** (en centimètres, donc
  `300` pour 3 mètres) → `Saturate` → `Lerp` entre la couleur peu profonde et la couleur
  profonde → **Base Color** ou **Emissive Color**.
- **L'écume** : `Divide` par **LargeurEcume** → `Saturate` → `OneMinus` → comparé à une texture
  de bruit qui défile via un `SmoothStep`.

## L'équivalent en nœud Custom

Entrées : `ProfondeurFond`, `ProfondeurPixel`, `ProfondeurMax`, `LargeurEcume`, `Grain`.
Sortie `CMOT Float 2`.

```hlsl
float epaisseur = max(ProfondeurFond - ProfondeurPixel, 0.0);
float melange = saturate(epaisseur / ProfondeurMax);
float bord = 1.0 - saturate(epaisseur / LargeurEcume);
float ecume = smoothstep(Grain * 0.6, Grain * 0.6 + 0.18, bord);
return float2(melange, ecume);
```

## Les pièges spécifiques à Unreal

**`SceneDepth` renvoie du noir.** Le matériau n'est pas en `Translucent`. Un matériau `Opaque` ne
peut pas lire la profondeur de la scène : elle est en train d'être écrite au moment où il
s'exécute.

**Tout est plat et l'épaisseur vaut zéro partout.** Tu as branché `SceneDepth` sans lui donner
d'UV : par défaut il échantillonne à la position du pixel, ce qui est correct. Vérifie plutôt que
le décor sous l'eau est bien rendu **avant** — un autre objet translucide n'écrit pas dans le
tampon de profondeur, donc l'eau ne le « voit » pas.

**Les unités.** Tout est en centimètres. Une `ProfondeurMax` de 3 mètres s'écrit `300`. C'est la
source d'erreur numéro un quand on porte un shader depuis Godot ou Unity.

**Le brouillard translucide.** Dans `Details` → `Translucency`, le champ `Apply Fogging` décide si
l'eau reçoit le brouillard atmosphérique. Décoché, une grande étendue d'eau reste bleu vif jusqu'à
l'horizon pendant que le reste de la scène s'estompe.
