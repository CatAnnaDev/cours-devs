# Leçon 01 en Unreal 5 — un matériau émissif réglable

## Le graphe

1. `Content Browser` → clic droit → `Material`, nomme-le `M_Neon`.
2. Sélectionne le nœud racine. Dans `Details` :
   - `Material Domain` : `Surface`
   - `Shading Model` : `Unlit`
   - `Blend Mode` : `Opaque`
3. Clic droit dans le graphe → `VectorParameter`. Nomme-le **Couleur**.
   Double-clic sur sa pastille pour ouvrir le sélecteur de couleur, coche `Use Legacy...` non,
   et laisse les valeurs entre 0 et 1 : l'intensité viendra du multiplicateur.
4. Clic droit → `ScalarParameter`. Nomme-le **Intensite**, valeur par défaut `4`.
   Dans `Details`, renseigne `Slider Min = 0` et `Slider Max = 20` : les instances afficheront
   un curseur au lieu d'un champ libre.
5. Clic droit → `Multiply`. Branche `Couleur` sur `A`, `Intensite` sur `B`.
6. Relie la sortie du `Multiply` à **Emissive Color** du nœud racine.
7. `Apply`, `Save`.

## L'équivalent en nœud Custom

Clic droit → `Custom`. Dans `Details`, ajoute deux entrées nommées `Couleur` et `Intensite`,
mets `Output Type` à `CMOT Float 3`, et écris :

```hlsl
return Couleur * Intensite;
```

Branche les mêmes paramètres dessus. Le résultat est identique — c'est l'intérêt de voir les
deux : le graphe *est* du HLSL, juste dessiné.

## L'instance, et pourquoi c'est le vrai geste Unreal

Ne modifie jamais `M_Neon` pour changer la couleur d'un néon particulier. Clic droit sur le
matériau → `Create Material Instance` → `MI_NeonBleu`.

L'instance affiche `Couleur` et `Intensite` avec une case à cocher devant chacun : coche pour
surcharger. Tu peux en créer trente, elles partagent **un seul shader compilé**. Modifier une
instance ne déclenche aucune recompilation — modifier le matériau parent recompile tout ce qui
en dérive, et ça peut prendre plusieurs minutes sur un gros projet.

## Voir le bloom

L'émissif ne brille que si le post-traitement le veut bien.

1. Pose un `Post Process Volume` dans le niveau, coche `Infinite Extent (Unbound)`.
2. Section `Lens` → `Bloom` : `Method` sur `Standard` ou `Convolution`, `Intensity` autour de
   `0.7`.
3. Section `Lens` → `Exposure` : passe `Metering Mode` à `Manual` pendant que tu règles ton
   néon. Sinon l'auto-exposition compense ton intensité en assombrissant toute la scène, et tu
   passes vingt minutes à te demander pourquoi monter `Intensite` ne change rien.

Ce dernier point vaut aussi pour Unity et Godot, sous d'autres noms. C'est le piège numéro un
de tout ce qui est émissif.

## Unités

Rappel : en Unreal, **1 unité = 1 centimètre**. Ça ne change rien à cette leçon, mais dès la
leçon 05 les constantes de distance devront être multipliées par 100 par rapport aux versions
Godot et Unity.
