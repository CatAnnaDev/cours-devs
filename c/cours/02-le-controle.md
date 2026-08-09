# 02 — Le contrôle

Le chapitre le plus court du cours : les structures de contrôle du C n'ont presque rien
d'original. Ce qui mérite d'être dit tient dans quatre pièges et une section sur les bits.

## Il n'y a pas de booléen (ou presque)

Historiquement, le C n'en a pas. **Zéro est faux, tout le reste est vrai.**

```c
if (compteur) { ... }        // vrai si compteur n'est pas nul
if (pointeur) { ... }        // vrai si le pointeur n'est pas NULL
```

C99 a ajouté `bool` via `<stdbool.h>`, et il vaut mieux l'utiliser quand on exprime une vérité.
Mais la règle sous-jacente reste : la condition est un entier.

Conséquence directe, le piège classique :

```c
if (x = 5) { ... }     // affecte 5 à x, puis teste 5, donc toujours vrai
if (x == 5) { ... }    // compare
```

Le premier compile, et `-Wall` te signale « suggest parentheses around assignment used as truth
value ». Prends l'avertissement au sérieux : c'est un vrai bug dans neuf cas sur dix.

## Le `switch` et la chute

```c
switch (touche) {
    case 'a':
    case 'q':
        aller_a_gauche();
        break;
    case 'd':
        aller_a_droite();
    case 'e':
        interagir();
        break;
}
```

Un `case` sans `break` **tombe dans le suivant**. Ici c'est voulu pour `'a'` et `'q'`, et c'est un
bug pour `'d'` : appuyer sur `d` va à droite **et** interagit.

C'est la source de bugs numéro un du `switch`, au point que les compilateurs modernes savent la
signaler (`-Wimplicit-fallthrough`) et que le C23 fournit `[[fallthrough]]` pour dire « c'est
voulu ».

Deux autres points :

**Le `default` n'est pas obligatoire, mais mets-le.** Sans lui, une valeur non prévue ne fait rien
du tout, silencieusement.

**On ne peut pas déclarer une variable juste après un `case`** sans ouvrir un bloc :

```c
case 'a': {
    int local = 3;
    ...
    break;
}
```

## Les boucles

`for` quand tu connais le nombre de tours, `while` quand tu attends une condition. `do ... while`
quand le corps doit s'exécuter au moins une fois — c'est rare, et souvent le signe qu'on peut
réécrire plus clairement.

Les deux erreurs de bornes, et elles reviennent partout :

```c
for (int i = 0; i <= taille; i++)    // un tour de trop
for (int i = 1; i < taille; i++)     // un tour de moins, et on saute l'élément 0
```

La forme juste pour parcourir `taille` éléments à partir de zéro est
`for (size_t i = 0; i < taille; i++)`. Écris-la mécaniquement, sans réfléchir, et réserve ton
attention aux cas particuliers.

Et le piège de la boucle descendante avec un type non signé :

```c
for (size_t i = taille - 1; i >= 0; i--)     // infinie
for (size_t i = taille; i-- > 0; )            // correcte
```

La seconde forme est un idiome à connaître : `i-- > 0` teste puis décrémente, donc le corps voit
`taille - 1` jusqu'à `0`, et la boucle s'arrête proprement.

## `break`, `continue`, et le `goto` qu'on ne dit pas

`break` sort de la boucle **la plus interne**. Il n'y a pas de `break` étiqueté en C. Pour sortir
de deux boucles imbriquées, il y a trois solutions, et la troisième est la bonne :

1. un drapeau `int fini = 0;` — verbeux et facile à rater ;
2. extraire les boucles dans une fonction et faire `return` — souvent la plus propre ;
3. `goto sortie;` — et c'est **légitime**.

```c
for (int y = 0; y < hauteur; y++) {
    for (int x = 0; x < largeur; x++) {
        if (grille[y][x] == cible) {
            goto trouve;
        }
    }
}
trouve:
```

Le `goto` a mauvaise réputation à cause d'un article de 1968 sur des langages qui n'avaient pas de
fonctions. En C moderne, il a deux usages parfaitement acceptés : **sortir de boucles imbriquées**
et **gérer les erreurs avec nettoyage** :

```c
int traiter(void) {
    char *tampon = malloc(1024);
    if (tampon == NULL) return -1;

    FILE *fichier = fopen("donnees", "rb");
    if (fichier == NULL) goto erreur_fichier;

    ...

    fclose(fichier);
    free(tampon);
    return 0;

erreur_fichier:
    free(tampon);
    return -1;
}
```

C'est le motif utilisé dans le noyau Linux. La règle : **on ne saute que vers l'avant, et jamais
dans un bloc**.

## Les opérateurs bit à bit

Un entier est un paquet de bits, et le C donne les outils pour les manipuler directement. Ce n'est
pas de la micro-optimisation : c'est ce qui permet de ranger seize drapeaux dans un entier, de
lire un format binaire, ou de piloter du matériel.

| Opérateur | Nom | Effet |
|---|---|---|
| `&` | et | 1 si les deux bits sont à 1 |
| `\|` | ou | 1 si au moins un bit est à 1 |
| `^` | ou exclusif | 1 si les deux bits diffèrent |
| `~` | non | inverse tous les bits |
| `<<` | décalage gauche | multiplie par 2 à la puissance n |
| `>>` | décalage droit | divise par 2 à la puissance n |

Les trois gestes qu'on refait sans arrêt :

```c
int lire     = (valeur >> position) & 1u;
unsigned mis = valeur | (1u << position);
unsigned oté = valeur & ~(1u << position);
```

Et quelques règles de sécurité :

**Travaille en `unsigned`.** Le décalage à droite d'un nombre signé négatif est défini par
l'implémentation, et le décalage à gauche qui déborde est indéfini.

**Ne décale jamais d'un nombre supérieur ou égal à la largeur du type.** `1u << 32` sur un
`unsigned` de 32 bits est un comportement indéfini — pas « ça donne zéro ». UBSan l'attrape.

**Attention aux priorités.** `&` et `|` sont **moins** prioritaires que `==`. `a & 1 == 0` est lu
`a & (1 == 0)`, c'est-à-dire `a & 0`. Mets des parenthèses ; les compilateurs le suggèrent.

## À retenir

1. Zéro est faux, tout le reste est vrai. `=` n'est pas `==`.
2. Un `case` sans `break` tombe dans le suivant.
3. `for (size_t i = 0; i < taille; i++)` s'écrit sans réfléchir.
4. Une boucle descendante en non signé s'écrit `for (size_t i = taille; i-- > 0; )`.
5. `goto` est acceptable pour sortir de boucles imbriquées et pour nettoyer sur erreur.
6. Les opérations sur les bits se font en `unsigned`, avec des parenthèses.

**Exercices : `02_controle`.**
