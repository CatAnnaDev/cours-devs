# 06 — Les chaînes

## Il n'y a pas de type chaîne

Une chaîne en C, c'est **une suite d'octets terminée par un zéro**. Pas de longueur stockée, pas
de capacité, pas de type. Juste une convention.

```c
char mot[4] = {'a', 'b', 'c', '\0'};
char autre[] = "abc";                  // exactement la même chose
```

`"abc"` occupe **quatre** octets. Oublier ce quatrième est la cause numéro un des dépassements de
tampon en C.

Cette convention a un nom, on l'appelle parfois une « chaîne NUL-terminée », et elle a été
qualifiée par un de ses contemporains de « plus coûteuse erreur de conception à un octet de
l'histoire ». Elle explique tout ce qui suit.

## Ce que la convention coûte

**`strlen` est en temps linéaire.** Il n'y a pas de longueur : il faut parcourir jusqu'au zéro.
Écrire `for (size_t i = 0; i < strlen(texte); i++)` relit toute la chaîne à chaque tour, ce qui
transforme une boucle linéaire en boucle quadratique. Calcule la longueur **une fois**, avant.

**Aucune fonction ne connaît la taille de la destination.** `strcpy` copie jusqu'au zéro de la
source, sans jamais savoir si ça rentre. C'est le mécanisme de la faille de sécurité la plus
classique de l'informatique.

**Une chaîne ne peut pas contenir d'octet nul.** Donc pas de données binaires. Pour ça il faut un
pointeur **et** une longueur, comme `memcpy`.

## Littéral contre tableau

```c
char *pointeur = "abc";      // pointe sur une zone en LECTURE SEULE
char tableau[] = "abc";      // une copie modifiable, sur la pile

pointeur[0] = 'A';           // plante à l'exécution
tableau[0] = 'A';            // parfaitement légal
```

Un littéral vit dans un segment en lecture seule du programme. Y écrire est un comportement
indéfini, qui se manifeste par un plantage brutal.

Écris donc toujours `const char *` pour un littéral :

```c
const char *message = "abc";
```

Le compilateur t'empêchera alors d'écrire dedans, à la compilation plutôt qu'à l'exécution.

## Les fonctions, et laquelle choisir

| Fonction | Ce qu'elle fait | Verdict |
|---|---|---|
| `strlen` | longueur, sans le zéro | correcte, mais linéaire |
| `strcpy` | copie jusqu'au zéro | **jamais** : aucune borne |
| `strcat` | concatène | **jamais** : aucune borne, et relit tout |
| `strncpy` | copie n octets | piégeuse, voir plus bas |
| `strncat` | concatène n octets | le `n` n'est pas la taille du tampon |
| `snprintf` | formate avec une borne | **c'est celle-là** |
| `strcmp` | compare, 0 si égal | correcte |
| `strchr` / `strstr` | cherche un caractère / une sous-chaîne | correctes |
| `memcpy` / `memmove` | copie n octets, sans zéro | pour du binaire |

### Le piège de `strncpy`

```c
char tampon[6];
strncpy(tampon, "abcdefgh", sizeof tampon);
```

`strncpy` écrit exactement six octets : `a b c d e f`. **Il n'y a pas de zéro terminal.** Le
« tampon » n'est plus une chaîne, et le prochain `strlen` partira dans la mémoire jusqu'à tomber
sur un zéro par hasard.

Et dans l'autre sens, si la source est plus courte, `strncpy` remplit **tout le reste de zéros** —
ce qui peut coûter cher sur un gros tampon.

Cette fonction n'a jamais été conçue pour les chaînes : elle servait à remplir des champs de taille
fixe dans les tables du système. Si tu t'en sers quand même, la seule forme correcte est :

```c
strncpy(destination, source, taille - 1);
destination[taille - 1] = '\0';
```

### Pourquoi `snprintf`

```c
int voulu = snprintf(destination, taille, "%s : %d", nom, score);
```

Trois qualités qu'aucune autre n'a :

1. **Elle ne dépasse jamais** la taille donnée.
2. **Elle termine toujours** par un zéro (sauf si `taille` vaut 0).
3. **Elle renvoie la longueur qu'elle aurait écrite** — donc si le retour est supérieur ou égal à
   `taille`, tu sais que le texte a été tronqué.

Ce troisième point est ce qui la rend supérieure à tout le reste : la troncature est **détectable**.

```c
if (voulu < 0 || (size_t)voulu >= taille) {
    // le texte ne tenait pas
}
```

## Comparer

```c
if (a == b)              // compare deux ADRESSES
if (strcmp(a, b) == 0)   // compare le contenu
```

C'est l'erreur de débutant classique, et elle est sournoise : avec des littéraux identiques, le
compilateur peut les fusionner, donc `"abc" == "abc"` peut être vrai. Le code marche en test et
casse en vrai.

`strcmp` renvoie 0 si égal, un négatif si le premier est « avant », un positif s'il est « après ».
Le sens de « avant » est celui des valeurs d'octets, pas l'ordre alphabétique d'une langue : les
majuscules passent avant les minuscules, et les accents partent n'importe où.

## Un `char` n'est pas un caractère

```c
const char *texte = "héllo";
strlen(texte);      // 6, pas 5
```

En UTF-8, « é » occupe deux octets. `strlen` compte des **octets**. Conséquences :

- couper une chaîne à un octet arbitraire peut couper un caractère en deux ;
- `texte[1]` ne donne pas le deuxième caractère ;
- passer une chaîne en majuscules avec une simple soustraction ne marche que sur l'ASCII.

En pratique, la bonne stratégie est presque toujours : **traiter le texte comme des octets et ne
jamais l'interpréter**. Copier, comparer, concaténer, transmettre : tout ça marche en octets. Dès
qu'il faut vraiment compter des caractères, découper ou changer la casse, il faut une bibliothèque.

Le point important : `char` veut dire « octet ». Le C n'a jamais eu de type caractère.

## Le tampon, et à qui il appartient

Toute manipulation de chaîne en C revient à une question : **qui possède la mémoire ?**

Trois conventions, et il faut choisir explicitement :

**1. L'appelant fournit le tampon.** C'est celle de la bibliothèque standard.

```c
void construire(char *destination, size_t taille, int score);
```

Aucune allocation, aucune fuite possible, et l'appelant décide. C'est la meilleure quand la taille
maximale est connue.

**2. La fonction alloue, l'appelant libère.**

```c
char *construire(int score);    // à libérer avec free()
```

Souple, mais il faut le dire — dans le nom ou juste au-dessus. Une fonction sur deux qui fuit dans
un vrai projet vient d'une convention non écrite.

**3. La fonction renvoie un pointeur vers sa propre mémoire.** À éviter : ça marche jusqu'au
deuxième appel, ou jusqu'au premier appel depuis deux threads.

## À retenir

1. Une chaîne, c'est des octets plus un zéro. `"abc"` en occupe quatre.
2. `strlen` est linéaire : ne l'appelle pas dans une condition de boucle.
3. `strcpy` et `strcat` n'ont aucune borne. `snprintf` en a une, et signale la troncature.
4. `strncpy` ne termine pas forcément par un zéro.
5. `==` compare des adresses, `strcmp` compare du texte.
6. Un `char` est un octet ; en UTF-8, un caractère peut en occuper quatre.
7. Décide et écris qui possède le tampon.

**Exercices : `06_chaines`.**
