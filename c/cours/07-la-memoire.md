# 07 — La mémoire

Le chapitre qui donne son titre au cours. Tout ce qui précède y converge.

## Trois endroits où vivent les données

| Zone | Qui décide | Quand ça meurt | Taille |
|---|---|---|---|
| **statique** | le compilateur | à la fin du programme | fixe, connue à la compilation |
| **pile** | le compilateur | à la sortie de la fonction | fixe, quelques mégaoctets en tout |
| **tas** | toi | quand tu appelles `free` | ce que tu veux |

```c
int global = 1;                    // statique

void fonction(void) {
    int local = 2;                 // pile
    int *bloc = malloc(4);         // le pointeur est sur la pile,
                                   // les 4 octets sont sur le tas
}
```

**La pile est rapide et automatique** : réserver une variable locale coûte une addition sur un
registre. Elle est aussi **petite** — 8 Mo par défaut sur macOS et Linux, souvent 1 Mo pour un
thread secondaire — et une récursion trop profonde ou un tableau local énorme la fait déborder.

**Le tas est souple et manuel.** C'est le seul endroit où l'on peut décider d'une taille à
l'exécution, et le seul dont la durée de vie n'est pas dictée par la structure du code. C'est aussi
le seul où l'on peut se tromper.

## Les quatre fonctions

```c
void *malloc(size_t taille);                  // réserve, contenu indéterminé
void *calloc(size_t nombre, size_t taille);   // réserve et met à zéro
void *realloc(void *bloc, size_t taille);     // redimensionne
void free(void *bloc);                        // rend
```

Quatre remarques, une par fonction.

**`malloc` ne met rien à zéro.** Le bloc contient ce qui traînait là — souvent les restes d'un
bloc libéré. Lire un octet non initialisé est un comportement indéfini.

**`calloc` met à zéro, et vérifie le produit** `nombre * taille` contre le débordement. C'est un
vrai argument : `malloc(nombre * taille)` avec des valeurs venant d'une entrée peut déborder et
allouer beaucoup trop peu, ce qui donne un dépassement de tampon derrière.

**`realloc` peut déplacer le bloc**, et c'est le piège de tout le chapitre — voir plus bas.

**`free(NULL)` ne fait rien**, et c'est garanti par la norme. Donc pas besoin de tester avant, et
mettre un pointeur à `NULL` après libération rend un double `free` inoffensif.

## Toujours tester le retour

```c
int *bloc = malloc(taille * sizeof(int));
if (bloc == NULL) {
    return -1;
}
```

`malloc` renvoie `NULL` quand il ne peut pas. Sur un système de bureau moderne, ça n'arrive
presque jamais — et « presque jamais » est exactement le genre de chemin qu'on n'a jamais testé le
jour où il se produit.

Note le `taille * sizeof(int)` : `malloc` compte en **octets**, pas en éléments. `malloc(5)` pour
cinq entiers réserve cinq octets, et l'écriture suivante déborde. ASan l'attrape immédiatement.

## Le motif correct de `realloc`

```c
int *agrandi = realloc(bloc, nouvelle_taille * sizeof(int));
if (agrandi == NULL) {
    // bloc est TOUJOURS valide : ne le perds pas
    return -1;
}
bloc = agrandi;
```

Et le motif faux, qu'on voit partout :

```c
bloc = realloc(bloc, nouvelle_taille);    // FUITE si realloc échoue
```

Si `realloc` échoue, il renvoie `NULL` **sans toucher au bloc d'origine**. En écrasant `bloc`, on
perd la seule adresse qui permettait de le libérer.

Deuxième point, plus subtil et plus fréquent : **`realloc` peut déplacer le bloc**. Tout pointeur
qui désignait l'intérieur de l'ancien bloc devient pendouillant.

```c
int *element = &vecteur[0];
agrandir(vecteur);        // realloc à l'intérieur
*element = 1;             // use-after-free, parfois
```

« Parfois », parce que `realloc` n'agrandit sur place que s'il y a la place derrière. Le programme
marche pendant des mois, puis casse quand la mémoire se fragmente. C'est le pire type de bug qui
soit, et la raison pour laquelle on stocke des **indices** plutôt que des pointeurs dans un tableau
qui peut grandir.

## Les quatre fautes, et ce qu'ASan en dit

### La fuite

```c
int *bloc = malloc(100);
return;                   // personne ne libérera jamais
```

Rien ne plante. La mémoire du processus grandit, jusqu'à ce que le système le tue — au bout de
trois heures de jeu, chez quelqu'un d'autre.

Sur Linux, `valgrind --leak-check=full` les liste. **Sur macOS ARM, LeakSanitizer n'existe pas** :
`clings` compte donc les blocs à la main, avec `suivi_malloc` / `suivi_free` et
`VERIFIE_PAS_DE_FUITE()`.

Ce n'est pas un pis-aller. Compter ses allocations est ce que font les vrais moteurs de jeu, et
pour la même raison : on veut savoir **combien** de blocs vivent, pas seulement qu'il n'en reste
aucun à la sortie.

### Le double `free`

```c
free(bloc);
free(bloc);               // AddressSanitizer: attempting double-free
```

L'allocateur maintient des métadonnées à côté de chaque bloc. Le libérer deux fois les corrompt,
et le désordre se manifeste beaucoup plus tard, dans une allocation sans rapport.

Le remède mécanique : `free(bloc); bloc = NULL;` — un `free(NULL)` supplémentaire ne fait rien.

### L'utilisation après libération

```c
free(bloc);
printf("%d\n", bloc[0]);  // AddressSanitizer: heap-use-after-free
```

Le bloc peut avoir été réattribué à autre chose entre-temps. Tu lis ou écris chez quelqu'un
d'autre. C'est le mécanisme d'une grande partie des exploits modernes.

ASan garde les blocs libérés en quarantaine précisément pour attraper ça, et son rapport indique
**où le bloc avait été alloué et où il avait été libéré**. Deux traces de pile pour le prix d'une.

### Le dépassement

```c
int *bloc = malloc(4 * sizeof(int));
bloc[4] = 1;              // AddressSanitizer: heap-buffer-overflow
```

Chapitre 05. ASan place des zones interdites autour de chaque bloc et te dit de combien tu
dépasses.

## Qui possède quoi

Le C n'a **aucune** notion de propriété. Il ne sait pas qui doit libérer un bloc, et ne le saura
jamais. C'est une décision de conception que **tu** dois prendre, écrire, et tenir.

Trois conventions qui marchent :

**1. Celui qui alloue libère.** La fonction alloue un tampon de travail et le libère avant de
sortir. Le plus simple, et le plus sûr. Applicable dès que la donnée ne sort pas.

**2. La fonction alloue, l'appelant libère.** À dire dans le nom (`creer_`, `nouvelle_`) ou juste
au-dessus, et à accompagner d'une fonction de destruction :

```c
Image *image_charger(const char *chemin);
void image_detruire(Image *image);
```

**Fournis toujours la fonction de destruction**, même si elle ne fait qu'un `free`. Le jour où la
structure gagne un second bloc, tu n'auras qu'un endroit à changer.

**3. Une arène.** On alloue tout dans un gros bloc, et on libère **tout d'un coup** à la fin d'une
image, d'un niveau, d'une requête. Pas de `free` individuel, donc pas de fuite possible, pas de
double `free`, pas d'utilisation après libération à l'intérieur du cycle.

C'est ce qu'utilisent la plupart des moteurs de jeu et des compilateurs, et c'est le sujet de la
section `12_allocateurs`.

## Le tableau qui grandit

C'est la structure de données la plus utile du C, et elle tient en dix lignes. Deux écritures y
apparaissent pour la première fois : `vecteur->donnees` est exactement `(*vecteur).donnees`, une
abréviation que le chapitre 08 détaille ; et `a ? b : c` est le ternaire du chapitre 01, un `if`
en une expression.

```c
typedef struct {
    int *donnees;
    size_t taille;
    size_t capacite;
} Vecteur;

int ajouter(Vecteur *vecteur, int valeur) {
    if (vecteur->taille == vecteur->capacite) {
        size_t nouvelle = vecteur->capacite == 0 ? 1 : vecteur->capacite * 2;
        int *agrandi = realloc(vecteur->donnees, nouvelle * sizeof(int));
        if (agrandi == NULL) {
            return 0;
        }
        vecteur->donnees = agrandi;
        vecteur->capacite = nouvelle;
    }
    vecteur->donnees[vecteur->taille++] = valeur;
    return 1;
}
```

**Trois champs, et pas deux.** `taille` est le nombre d'éléments utilisés, `capacite` le nombre de
places réservées. Les confondre force une réallocation à chaque ajout.

**Pourquoi doubler ?** Parce que le coût total des copies pour `n` ajouts reste proportionnel à
`n` : chaque élément est déplacé en moyenne une fois. Agrandir de un à chaque fois donnerait un
coût total proportionnel à `n²` — sur cent mille ajouts, la différence est entre instantané et
plusieurs secondes.

Le facteur exact n'a pas beaucoup d'importance : 1.5 réutilise mieux la mémoire libérée, 2 est
plus simple. Ce qui compte, c'est qu'il soit **multiplicatif**.

## À retenir

1. Pile : rapide, automatique, petite. Tas : souple, manuel, et c'est là qu'on se trompe.
2. `malloc` compte en octets et ne met rien à zéro. Teste son retour.
3. `realloc` peut déplacer le bloc : passe par une variable temporaire, et ne garde pas de
   pointeurs vers l'intérieur.
4. `free(bloc); bloc = NULL;` — le geste complet.
5. Fuite, double `free`, utilisation après libération, dépassement : les quatre fautes.
6. Le C ne sait pas qui possède la mémoire. Choisis une convention et écris-la.
7. Un tableau dynamique double sa capacité, et garde `taille` séparée de `capacite`.

**Exercices : `07_memoire`.**

---

C'est la fin du premier bloc du cours. Tu sais maintenant lire un rapport de sanitizer, tenir des
pointeurs, ne pas déborder d'un tableau, manipuler des chaînes sans te faire mal, et posséder de
la mémoire.

La suite — structures et alignement, préprocesseur, compilation séparée, puis les allocateurs
maison — part de là.
