# Leçon 05 en Unreal 5 — dissolution à bord incandescent

## Le réglage qui conditionne tout

Sur le nœud racine du matériau, dans `Details` :

- **`Blend Mode` : `Masked`**. C'est ce qui active l'entrée `Opacity Mask`. En `Opaque`, l'entrée
  est grisée et rien de cette leçon ne marchera.
- **`Opacity Mask Clip Value` : `0.333`** par défaut. Un pixel dont le masque est sous cette
  valeur est jeté. On peut le laisser tel quel et calibrer notre masque autour, mais c'est plus
  clair de le mettre à `0.5` et de produire un masque franchement en dessous ou au-dessus.
- **`Two Sided`** coché si l'objet doit rester visible de l'intérieur pendant qu'il se troue.
  C'est presque toujours souhaitable : un objet troué laisse voir son intérieur.

## Le graphe

**Le masque de dissolution.**

1. `TextureSampleParameter2D` **Bruit** — une texture de bruit, `sRGB` **décoché**, compression
   `Masks` ou `Grayscale`. Canal `R`.
2. `ScalarParameter` **Progression** (0 à 1). Dans `Details`, `Slider Min 0` / `Slider Max 1`.
3. `ScalarParameter` **LargeurBord** (`0.08`).
4. Le seuil : `Lerp` avec `A` = `LargeurBord` négatif (passe-le dans un `Multiply` par `-1`),
   `B` = `1 + LargeurBord` (un `Add`), `Alpha` = `Progression`.
5. `Subtract` : `A` = le bruit, `B` = le seuil. Ce résultat, appelons-le **Visible**.
6. `Visible` → **Opacity Mask**.

Comme `Opacity Mask Clip Value` vaut `0.333`, remonte `Visible` de `0.333` avec un `Add`, ou
descends la valeur de clip à `0`. La deuxième solution est plus simple à raisonner : le pixel
disparaît exactement quand `Visible` passe sous zéro, comme dans les versions Godot et Unity.

**Le bord incandescent.**

7. `SmoothStep` : `Min` = `0`, `Max` = `LargeurBord`, `Value` = `Visible`.
8. `OneMinus` → c'est le masque du bord : 1 juste au bord de la découpe, 0 ailleurs.
9. `Multiply` par un `VectorParameter` **CouleurBord** puis par un `ScalarParameter`
   **IntensiteBord** → **Emissive Color**.

`Apply`, puis crée une `Material Instance` : c'est sur elle que tu animeras `Progression`.

## Animer la progression

Trois façons, de la plus simple à la plus propre :

**Dans le matériau** — pour un test : `Time` → `Frac` → `Progression`. La dissolution boucle
toute seule. Pratique pour régler, inutilisable en jeu (tous les objets seraient synchronisés).

**Depuis un Blueprint** — la bonne façon :

```
Create Dynamic Material Instance  →  variable MID
Set Scalar Parameter Value  (Parameter Name: "Progression", Value: alpha)
```

Fais-le une seule fois au `BeginPlay` pour créer l'instance dynamique, puis mets à jour la valeur
dans un `Timeline`. Créer l'instance dynamique à chaque frame est une erreur coûteuse et
fréquente.

**Avec un `Material Parameter Collection`** — quand *tous* les objets doivent se dissoudre
ensemble (une transition de niveau). Une seule valeur globale, lue par tous les matériaux, mise à
jour une fois par frame.

## L'équivalent en nœud Custom

Entrées : `Grain` (Float1), `Progression`, `LargeurBord`. Sortie `CMOT Float 2` — on renvoie les
deux valeurs d'un coup, le masque de découpe et le masque de bord.

```hlsl
float seuil = lerp(-LargeurBord, 1.0 + LargeurBord, Progression);
float visible = Grain - seuil;
float bord = 1.0 - smoothstep(0.0, LargeurBord, visible);
return float2(visible, bord);
```

Puis un `ComponentMask` sur `R` vers `Opacity Mask`, et sur `G` vers la chaîne d'émission.
Renvoyer plusieurs valeurs dans un vecteur est l'idiome habituel pour sortir d'un `Custom` : il
n'accepte qu'une sortie.

## Ce qui se passe côté ombres et profondeur

Bonne nouvelle : Unreal gère ça tout seul. Un matériau `Masked` applique son `Opacity Mask` dans
**toutes** les passes, y compris la shadow map et la pré-passe de profondeur. Tu n'as rien à
écrire — contrairement à Unity, où il faut une passe `ShadowCaster` explicite.

Mauvaise nouvelle : ça se paie. Un matériau `Masked` désactive plusieurs optimisations, dont le
test de profondeur anticipé. La console affiche le surcoût avec `r.ShaderComplexity 1` ou la vue
`Optimization Viewmodes` → `Shader Complexity` : ton objet dissous y apparaît plus chaud qu'un
objet opaque équivalent. Voir la section « Ce que ça coûte » du `README.md`.
