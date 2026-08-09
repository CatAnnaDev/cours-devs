# 03 — Les fonctions

## Déclarer avant d'utiliser

Le compilateur C lit le fichier **de haut en bas, une seule fois**. Au moment où il rencontre un
appel, il doit déjà connaître la fonction.

```c
int carre(int n);          // le prototype : la signature, puis un point-virgule

int main(void) {
    return carre(5);
}

int carre(int n) {         // la définition, plus loin
    return n * n;
}
```

Sans le prototype, en C17, c'est une **erreur**. En C89 c'était toléré, avec des conséquences
délirantes : le compilateur supposait un retour `int` et ne vérifiait aucun argument. Beaucoup de
vieux code et de tutoriels traînent encore avec cet héritage.

Le prototype vit normalement dans un fichier `.h`, la définition dans le `.c`. C'est tout le sujet
de la compilation séparée, section `10_modules`.

**`int f()` et `int f(void)` ne veulent pas dire la même chose en C.** Le premier signifie « je ne
dis rien sur les paramètres » et désactive la vérification ; le second signifie « aucun
paramètre ». Écris toujours `void`.

## Tout est passé par copie

C'est la règle unique, et elle n'a aucune exception.

```c
void incrementer(int valeur) {
    valeur++;              // modifie la copie locale
}

int compteur = 10;
incrementer(compteur);     // compteur vaut toujours 10
```

Le paramètre `valeur` est une **variable locale** initialisée avec une copie de l'argument. La
modifier ne change rien chez l'appelant.

Pour modifier une variable de l'appelant, il faut lui passer son **adresse** :

```c
void incrementer(int *valeur) {
    (*valeur)++;
}

incrementer(&compteur);    // compteur vaut 11
```

Et remarque que la règle tient toujours : c'est **l'adresse** qui est copiée. Le pointeur local est
une copie ; ce qu'il désigne, non.

Les parenthèses de `(*valeur)++` ne sont pas décoratives : `*valeur++` incrémente **le pointeur**,
puis déréférence. Deux caractères, deux programmes différents.

## Les deux façons de renvoyer

Une fonction C ne renvoie qu'une valeur. Quand il en faut plusieurs, ou quand il faut à la fois un
résultat et un état d'erreur, on utilise des **paramètres de sortie** :

```c
int diviser(int a, int b, int *resultat) {
    if (b == 0) {
        return 0;
    }
    *resultat = a / b;
    return 1;
}

int quotient;
if (diviser(10, 2, &quotient)) {
    // quotient vaut 5
}
```

La convention la plus répandue : **le retour dit si ça s'est bien passé, les pointeurs portent les
résultats**. C'est ce que font `strtol`, `sscanf`, et toutes les API système.

Une bonne fonction à paramètre de sortie **n'écrit rien en cas d'échec**. Sinon l'appelant se
retrouve avec une valeur à moitié remplie, et ce genre de bug se paie cher.

## Les tableaux perdent leur taille

```c
void afficher(int tableau[10]) {
    printf("%zu\n", sizeof(tableau));    // 8, pas 40
}
```

Le `[10]` est **décoratif** : le compilateur transforme le paramètre en `int *`. Un tableau passé
à une fonction se convertit en pointeur sur son premier élément, et **sa taille est perdue**.

D'où la signature standard, qu'on retrouve dans toute la bibliothèque :

```c
int somme(const int *valeurs, size_t taille);
```

La taille se passe **à côté**, toujours. C'est la source d'une immense partie des failles de
sécurité en C : quelqu'un a passé la mauvaise taille, ou l'a oubliée.

Le `const` n'est pas cosmétique non plus : il documente que la fonction ne modifiera rien, et le
compilateur le vérifie. Prends l'habitude de le mettre partout où c'est vrai — c'est du contrat
gratuit.

## Portée et durée de vie

```c
int compteur_global = 0;          // visible partout, vit tout le programme
static int compteur_fichier = 0;  // visible dans CE fichier seulement

void fonction(void) {
    int local = 0;                // détruit à la sortie
    static int persistant = 0;    // vit tout le programme, visible ici seulement
    persistant++;
}
```

`static` a **deux sens différents** selon l'endroit, et c'est déroutant :

| Où | Ce que `static` veut dire |
|---|---|
| variable globale ou fonction | « privé à ce fichier », invisible depuis les autres |
| variable locale | « garde sa valeur entre les appels », allouée une fois pour toutes |

Le premier sens est le plus utile : dans un fichier `.c`, **tout ce qui n'est pas dans le `.h`
devrait être `static`**. Ça évite les collisions de noms à l'édition de liens et permet au
compilateur d'optimiser plus librement.

Le second est à manier avec précaution : une variable `static` locale est un état global déguisé,
et rend la fonction non réentrante.

## Le piège de l'adresse locale

```c
char *construire(int score) {
    char tampon[32];
    snprintf(tampon, sizeof tampon, "score : %d", score);
    return tampon;                 // le tampon n'existe plus
}
```

`tampon` vit dans le cadre de pile de `construire`, détruit au `return`. L'adresse renvoyée
désigne une zone qui va être réutilisée par le prochain appel de fonction.

Le compilateur t'avertit (`-Wreturn-stack-address`) et ASan le détecte
(`stack-use-after-return`). Mais le symptôme sans outils est le pire qui soit : ça marche, jusqu'à
ce que ça ne marche plus, selon ce qui a été appelé entre-temps.

Les trois solutions, par ordre de préférence :

1. **L'appelant fournit le tampon** : `void construire(char *tampon, size_t taille, int score)`.
   C'est la convention de toute la bibliothèque standard.
2. **La fonction alloue et le documente** : `char *construire(int score)` avec `malloc`, et
   l'appelant devra `free`. À écrire dans le nom ou la documentation, sinon on fuit.
3. Une variable `static` — non. Ça marche, et ça casse au premier usage concurrent ou au deuxième
   appel imbriqué.

## Les pointeurs de fonction

Une fonction a une adresse, donc on peut la ranger dans une variable.

```c
int doubler(int n) { return n * 2; }

int appliquer(int valeur, int (*operation)(int)) {
    return operation(valeur);
}

appliquer(21, doubler);    // 42
```

La syntaxe se lit **de l'intérieur vers l'extérieur** : `operation` est un pointeur (`*operation`)
vers une fonction qui prend un `int` et renvoie un `int`.

C'est laid, et `typedef` arrange tout :

```c
typedef int (*Operation)(int);

int appliquer(int valeur, Operation operation) {
    return operation(valeur);
}
```

À quoi ça sert vraiment : `qsort` prend une fonction de comparaison, une machine à états range une
fonction par état, un système de rappels stocke des pointeurs de fonction, et une table de
dispatch remplace un `switch` géant. C'est l'équivalent C d'une interface.

## Ce que `main` renvoie

```c
int main(void)                       // sans arguments
int main(int argc, char **argv)      // avec la ligne de commande
```

Le retour est le **code de sortie** : `0` signifie succès, tout le reste un échec. C'est ce que le
shell teste avec `$?`, et ce que `clings` utilise pour savoir si ton exercice passe.

Dans `main`, et seulement dans `main`, l'omission du `return` équivaut à `return 0`.

## À retenir

1. Déclarer avant d'utiliser ; `int f(void)`, pas `int f()`.
2. Tout est passé par copie, y compris les pointeurs.
3. Un tableau en paramètre est un pointeur : passe la taille à côté.
4. `static` veut dire « privé au fichier » sur une globale, « persistant » sur une locale.
5. Ne renvoie jamais l'adresse d'une variable locale.
6. `const` sur les paramètres d'entrée : c'est du contrat gratuit.

**Exercices : `03_fonctions`.**
