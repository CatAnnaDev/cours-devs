# 05 — Brancher un shader, concrètement

Écrire le fichier ne suffit pas : il faut le rattacher à un matériau, le matériau à un objet, et —
surtout en Unity — activer les options du pipeline dont la leçon a besoin. Ce chapitre est la
recette exacte, à garder sous la main.

## Unity URP

### Une fois par projet

Le projet doit être en **URP**. Deux cas :

**Nouveau projet** — dans Unity Hub, choisis le modèle `Universal 3D`. Tout est déjà en place.

**Projet existant en Built-in** — `Window` → `Package Manager` → `Unity Registry` → installe
`Universal RP`. Puis `Assets` → `Create` → `Rendering` → `URP Asset (with Universal Renderer)`.
Enfin `Edit` → `Project Settings` → `Graphics` → glisse l'asset dans
`Default Render Pipeline`, **et** `Project Settings` → `Quality` → le même asset dans
`Render Pipeline Asset` de chaque niveau de qualité.

Oublier le second endroit est l'erreur classique : ça marche dans l'éditeur et pas en build, ou
l'inverse.

### Les six interrupteurs du cours

Sélectionne ton **URP Asset** dans le projet. Ces réglages conditionnent des leçons entières :

| Réglage | Où | Sans lui |
|---|---|---|
| **HDR** | URP Asset → `Quality` | l'émission au-dessus de 1 est écrêtée : leçons 01, 05, 07 sans halo |
| **Depth Texture** | URP Asset → `Rendering` | `SampleSceneDepth` renvoie 0 : leçon 11 morte |
| **Opaque Texture** | URP Asset → `Rendering` | `_CameraOpaqueTexture` est noire : leçon 12 morte |
| **Opaque Downsampling** | URP Asset → `Rendering` | qualité/coût de la réfraction (leçon 12) |
| **Shadows → Cast/Soft** | URP Asset → `Lighting`/`Shadows` | pas d'ombres reçues : leçon 08 plate |
| **Post-processing** sur la caméra | inspecteur de la `Camera` | pas de bloom, même avec un `Volume` |

Et pour voir un halo (leçons 01, 05, 07) : `GameObject` → `Volume` → `Global Volume`, puis
`Add Override` → `Post-processing` → `Bloom`. Mets `Threshold` ≈ 1 et `Intensity` ≈ 0.5. Ajoute
un override `Exposure` en `Fixed` pendant que tu règles, sinon l'auto-exposition compense tout ce
que tu montes.

### Une fois par leçon

1. **Copier le fichier.** Glisse `unity.shader` dans `Assets/` (n'importe où). Renomme-le si tu
   veux : c'est la ligne `Shader "Cours/05_Dissolution"` **à l'intérieur** du fichier qui décide
   du nom affiché, pas le nom de fichier.
2. **Créer le matériau.** Clic droit dans `Assets/` → `Create` → `Material`. Nomme-le.
3. **Choisir le shader.** Sélectionne le matériau. Tout en haut de l'inspecteur, un menu déroulant
   `Shader` : ouvre-le, va dans **`Cours`**, prends la leçon. Les paramètres de la leçon
   apparaissent aussitôt en dessous.
4. **Poser le matériau.** Glisse-le depuis `Assets/` **sur l'objet dans la scène**, ou sur le
   champ `Materials → Element 0` du `Mesh Renderer` de l'objet.

   L'objet doit avoir un **`Mesh Renderer`** : `GameObject` → `3D Object` → `Sphere` fait
   l'affaire. Tous les shaders de ce cours sont des shaders de surface 3D. Ils ne sont **pas**
   faits pour un `SpriteRenderer`, un `Image` d'interface ni un `Line Renderer` — voir juste en
   dessous.
5. **Assigner les textures.** Les leçons 02 et suivantes demandent une texture. Glisse-la dans le
   champ correspondant du matériau.

### Quand ça ne marche pas

**L'objet est rose.** Le shader n'a pas compilé. Sélectionne le fichier `.shader` dans `Assets/` :
l'inspecteur affiche l'erreur en haut, avec le numéro de ligne. Le rose est *toujours* ça.

**Le menu `Cours` n'existe pas.** Le fichier n'est pas dans `Assets/`, ou il n'a pas été importé.
Clic droit dessus → `Reimport`.

**Le matériau est là mais l'objet n'a pas changé.** Tu as posé le matériau sur le prefab et pas
sur l'instance, ou l'objet a plusieurs slots de matériau et tu as rempli le mauvais.

**Ça marche, puis ça casse après une modification.** Le cache de variantes. `Reimport` sur le
shader, et si ça persiste, `Edit` → `Preferences` → `GI Cache` → `Clean Cache` n'a rien à voir :
c'est `Reimport All` qu'il faut, en dernier recours.

**`Material does not have a _MainTex or _BaseMap texture property. Having one of them is
required for SpriteRenderer.`** Tu as posé le matériau sur un **sprite 2D**, pas sur un objet 3D.
Unity impose aux `SpriteRenderer` une propriété nommée exactement `_MainTex` ou `_BaseMap`, et la
plupart des shaders du cours n'en ont pas (la leçon 01 n'a aucune texture, la 07 non plus).

Deux issues :

- **La bonne, dans 99 % des cas** : `GameObject` → `3D Object` → `Sphere`, et pose le matériau
  dessus. Ces shaders décrivent une surface éclairée dans l'espace : normales, direction de vue,
  profondeur. Sur un rectangle plat vu de face, la moitié n'a aucun sens — un fresnel sur un
  sprite ne montre rien.
- **Si tu veux vraiment du 2D** : renomme la propriété principale du shader en `_BaseMap`, ou
  ajoute-en une factice. Mais c'est le signe qu'il faut un shader `canvas_item` / `Sprite-Unlit`,
  pas celui de la leçon. Le `banc.gdshader` de chaque leçon est justement la version à plat.

Si tu es dans un **projet 2D** (modèle `Universal 2D`), la scène n'a ni caméra en perspective ni
lumière 3D : crée plutôt un projet `Universal 3D` pour suivre le cours.

**Rien ne bouge alors que le shader utilise `_Time`.** Hors du mode Play, la vue Game ne se
rafraîchit pas en continu. Coche le petit bouton **`Always Refresh`** dans la barre de la vue
Scene, ou entre en Play.

## Godot 4

### Une fois par projet

Rien. Godot n'a pas de pipeline à choisir pour ce cours.

Deux réglages seulement, quand une leçon en a besoin :

| Réglage | Où | Pour quoi |
|---|---|---|
| **Glow** | `WorldEnvironment` → `Environment` → `Glow` → `Enabled` | le halo des leçons 01, 05, 07 |
| **Background: Sky** | `WorldEnvironment` → `Environment` → `Background` | sans ciel, tout est noir et les reflets sont vides |

Ajoute donc un `WorldEnvironment` avec un `New Environment` dès ta première scène de test, et
mets son `Background Mode` sur `Sky`. C'est le manque le plus fréquent quand « rien ne se voit ».

### Une fois par leçon

1. Sélectionne ton `MeshInstance3D`.
2. Dans l'inspecteur, `Geometry` → **`Material Override`** → `New ShaderMaterial`.
3. Déplie-le, champ `Shader` → **`Load`** → choisis le `.gdshader` de la leçon.
   (`New Shader` crée un shader vide inclus dans la scène : pratique pour bidouiller, mauvais pour
   suivre un cours, puisque le fichier du disque n'est plus la référence.)
4. Les paramètres apparaissent sous **`Shader Parameters`**.

**Pour un `banc.gdshader`** : scène avec un `Control` racine, un `ColorRect` en `Full Rect`,
inspecteur → `CanvasItem` → `Material` → `New ShaderMaterial` → `Load`.

**Pour la leçon 08**, le contour est un **second** matériau : sur le `ShaderMaterial` du toon,
propriété `Next Pass` → `New ShaderMaterial` → `Load` → `godot-contour.gdshader`.

### Quand ça ne marche pas

**Le panneau du bas affiche l'erreur**, avec la ligne. C'est le meilleur retour des trois moteurs :
il apparaît à la sauvegarde, sans lancer le jeu.

**L'objet est blanc-violet quadrillé** : le shader n'a pas compilé.

**Tout est noir** : pas de lumière, pas d'environnement, ou la leçon utilise `EMISSION` sans que
le glow soit activé.

## Unreal 5

Il n'y a pas de fichier à copier : chaque leçon fournit `unreal.md`, qui décrit le graphe nœud par
nœud, plus l'équivalent HLSL en nœud `Custom`.

La marche à suivre est toujours la même :

1. `Content Browser` → clic droit → `Material`.
2. Régler `Material Domain`, `Blend Mode` et `Shading Model` **d'abord** — la moitié des entrées
   du nœud racine n'apparaissent qu'après.
3. Monter le graphe, `Apply`, `Save`.
4. Clic droit sur le matériau → **`Create Material Instance`**, et c'est l'instance qu'on pose sur
   les objets et qu'on règle.

Et le rappel qui vaut pour toutes les leçons : **1 unité Unreal = 1 centimètre**. Une distance de
`0.06` en Godot ou Unity s'écrit `6` en Unreal.

## Les réglages exigés, leçon par leçon

| Leçon | Unity | Godot | Unreal |
|---|---|---|---|
| 01 néon | HDR + Bloom | Glow | Bloom + Exposure manuelle |
| 02 texture | — | — | — |
| 03 temps | `Always Refresh` hors Play | — | — |
| 04 masques | une texture de bruit | `NoiseTexture2D` intégrée | une texture de bruit, `sRGB` décoché |
| 05 dissolution | — | — | `Blend Mode: Masked` |
| 06 fresnel | — | — | — |
| 07 hologramme | HDR + Bloom | Glow | `Blend Mode: Additive` |
| 08 toon | Shadows activées | — | `Shading Model: Unlit` |
| 09 feuillage | couleurs de sommet sur le modèle | idem | idem + `Bounds Scale` |
| 10 eau | texture importée en `Normal map` | `hint_normal` | `Sampler Type: Normal` |
| 11 profondeur | **Depth Texture** | matériau transparent | `Blend Mode: Translucent` |
| 12 verre | **Opaque Texture** | rien | `Blend Mode: Translucent` |
| 13 triplanar | — | — | décocher `Tangent Space Normal` |
| 14 parallax | tangentes sur le modèle | idem | idem |
