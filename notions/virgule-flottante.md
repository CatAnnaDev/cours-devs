# La virgule flottante

Pourquoi `0.1 + 0.2` ne fait pas `0.3`, et les quatre endroits où ça mord vraiment.

## Ce qu'est un flottant

Un nombre à virgule flottante est stocké comme une notation scientifique en base 2 :

```
valeur = signe × mantisse × 2^exposant
```

| Type | Bits | Mantisse | Chiffres décimaux fiables | Portée |
|---|---|---|---|---|
| `half` (GPU) | 16 | 10 bits | ~3 | ±65 504 |
| `float` | 32 | 23 bits | **~7** | ±3.4 × 10³⁸ |
| `double` | 64 | 52 bits | **~16** | ±1.8 × 10³⁰⁸ |

Retiens les deux chiffres du milieu : **sept chiffres significatifs pour un `float`, seize pour un
`double`**. Tout le reste en découle.

## Pourquoi 0.1 n'existe pas

En base 10, `1/3` s'écrit `0.333...` sans jamais finir. En base 2, c'est `1/10` qui ne finit pas :

```
0.1 en binaire = 0.0001100110011001100110011...
```

La machine garde 52 bits et arrondit. `0.1` vaut en réalité `0.1000000000000000055511151231257827`.
Additionne deux approximations, tu obtiens une troisième approximation, qui n'est pas celle de
`0.3`.

**Ce n'est pas un bug du langage.** C'est vrai en C, Python, JavaScript, Rust, Java, dans un
tableur, et sur ta calculatrice si elle avait assez de chiffres.

Les nombres exactement représentables sont ceux dont la partie fractionnaire est une somme de
puissances de 2 : `0.5`, `0.25`, `0.75`, `0.125`. C'est tout.

## Ne compare jamais avec `==`

```c
if (a == b)                          // faux
if (fabs(a - b) < 1e-9)              // mieux, mais pas suffisant
```

La tolérance absolue marche pour des nombres autour de 1. Autour d'un milliard, `1e-9` est bien
plus petit que l'écart entre deux flottants représentables : la comparaison devient un `==`
déguisé.

La forme robuste combine absolu et relatif :

```c
int proches(double a, double b) {
    double ecart = fabs(a - b);
    if (ecart < 1e-12) return 1;                      // près de zéro
    return ecart <= 1e-9 * fmax(fabs(a), fabs(b));    // ailleurs
}
```

La tolérance absolue est indispensable près de zéro, où la tolérance relative n'a plus de sens.

## Les quatre endroits où ça mord

### 1. L'accumulation

```c
float total = 0.0f;
for (int i = 0; i < 10000000; i++) {
    total += 0.1f;
}
```

Le résultat n'est pas `1000000`. À force d'ajouter un petit nombre à un grand, le petit finit par
**disparaître dans l'arrondi** : quand `total` dépasse 2²⁴, ajouter `0.1f` ne change plus rien.

Les remèdes : accumuler en `double` même si les données sont en `float`, ou sommer par blocs, ou
utiliser la sommation de Kahan qui garde la trace de l'erreur.

### 2. Le temps qui grandit

Un compteur de temps en `float` vaut 10 000 après trois heures de jeu. À cette échelle, l'écart
entre deux `float` consécutifs dépasse le millième de seconde : une animation basée dessus se met
à saccader visiblement.

C'est un problème **réel** en shader, où `TIME` est souvent un `float` — les moteurs le font
d'ailleurs repartir à zéro périodiquement. Toute valeur qui doit boucler passe par `fract` ou
`sin`, jamais par une accumulation nue.

### 3. La soustraction de deux nombres proches

```c
double a = 1000000.1;
double b = 1000000.0;
double d = a - b;        // 0.10000000009313226
```

Chaque opérande a seize chiffres fiables, mais leur différence n'en a plus que sept : les chiffres
de tête, identiques, se sont annulés et il ne reste que le bruit. C'est l'**annulation
catastrophique**.

Ça touche les formules de distance, les résolutions d'équations du second degré, et surtout les
comparaisons de profondeur en 3D — d'où le *z-fighting*, ces surfaces qui clignotent quand deux
polygones sont presque coplanaires.

### 4. Les entiers déguisés en flottants

Un `double` représente exactement tous les entiers jusqu'à 2⁵³, un `float` jusqu'à 2²⁴, soit
seulement **16 millions**. Un identifiant, un compteur d'objets, un nombre d'octets stockés en
`float` deviennent faux sans prévenir.

**Un identifiant n'est jamais un flottant.**

## Les valeurs spéciales

| Valeur | D'où elle vient | Comportement |
|---|---|---|
| `+inf` / `-inf` | division par zéro, dépassement | se propage |
| `nan` | `0.0/0.0`, `sqrt(-1)`, `inf - inf` | **contamine tout** |
| `-0.0` | égal à `+0.0`, mais `1/-0.0` vaut `-inf` | rarement gênant |

`nan` a une propriété unique : **il n'est égal à rien, pas même à lui-même**. D'où le test
standard :

```c
if (x != x) { /* c'est un nan */ }
```

Et sa conséquence pratique : un seul `nan` dans un tableau contamine toute somme, tout minimum,
tout tri — et un tri avec des `nan` peut même sortir des bornes du tableau, parce que le
comparateur cesse d'être un ordre valide.

**En shader, un `nan` produit un pixel noir ou blanc, souvent isolé.** Quand tu vois des pixels
qui clignotent au hasard, cherche une division par zéro ou un `sqrt` d'un nombre négatif — souvent
un `dot` légèrement inférieur à zéro à cause de l'arrondi.

D'où l'habitude : `sqrt(max(x, 0.0))`, `acos(clamp(x, -1.0, 1.0))`.

## Quand ne pas utiliser de flottants

**L'argent.** Jamais. Compte en centimes, dans un entier, ou utilise un type décimal
(`decimal` en C#, `BigDecimal` en Java).

**Les identifiants et les compteurs.** Des entiers.

**Le déterminisme entre machines.** Deux processeurs peuvent donner des résultats différents sur
la même formule, selon l'ordre des optimisations, la présence de FMA ou de SIMD. Un jeu en réseau
qui simule chez chaque joueur en attendant le même résultat ne peut pas utiliser de flottants : il
faut de la **virgule fixe** (des entiers avec un facteur d'échelle).

## À retenir

1. `float` : 7 chiffres fiables. `double` : 16.
2. `0.1` n'est pas représentable en binaire. Aucun langage n'y échappe.
3. Compare avec une tolérance, absolue près de zéro et relative ailleurs.
4. Accumuler beaucoup de petites valeurs les perd : accumule en `double`.
5. Soustraire deux nombres proches détruit la précision.
6. `nan != nan`, et un seul contamine tout.
7. Ni l'argent, ni les identifiants, ni le déterminisme réseau.
