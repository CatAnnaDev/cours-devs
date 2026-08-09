# L'aléatoire

Il n'y en a pas. Il n'y a que des suites déterministes qui en ont l'air — et c'est une excellente
nouvelle, parce que c'est reproductible.

## Pseudo-aléatoire

Un générateur pseudo-aléatoire est une fonction qui, à partir d'un état interne, produit un nombre
et un nouvel état. Même graine, **même suite**, à chaque exécution, sur chaque machine.

```c
uint32_t etat = 12345;

uint32_t suivant(void) {
    etat ^= etat << 13;
    etat ^= etat >> 17;
    etat ^= etat << 5;
    return etat;
}
```

C'est un xorshift : trois lignes, très rapide, suffisant pour un jeu.

Le déterminisme n'est pas un défaut, c'est **la** fonctionnalité :

- un bug qui n'arrive qu'avec la graine 4172 se reproduit à volonté ;
- un monde généré procéduralement tient dans un entier au lieu d'un fichier de sauvegarde ;
- un test avec de l'aléatoire devient reproductible ;
- deux joueurs en réseau simulent la même chose sans rien s'envoyer.

**Note toujours la graine quelque part.** Un bug aléatoire sans graine est un bug qu'on ne
corrigera pas.

## Choisir son générateur

| Générateur | Vitesse | Qualité | Usage |
|---|---|---|---|
| `rand()` du C | moyenne | **mauvaise** | à éviter |
| xorshift | très rapide | correcte | jeux, effets |
| PCG | rapide | bonne | par défaut moderne |
| Mersenne Twister | moyenne | bonne | scientifique, gros état |
| ChaCha20, `getrandom` | lente | **cryptographique** | mots de passe, jetons, sel |

`rand()` mérite son mauvais classement : ses bits de poids faible sont souvent quasi périodiques —
d'où la vieille recommandation d'utiliser les bits de poids fort — sa portée est parfois limitée à
32767, et son implémentation varie d'un système à l'autre, ce qui détruit le déterminisme.

**Et la ligne à ne jamais franchir** : un générateur non cryptographique ne doit **jamais** servir à
un mot de passe, un jeton de session, un identifiant secret ou un sel. Ces suites sont
prédictibles ; c'est justement le but.

## Le piège du modulo

```c
int de = suivant() % 6 + 1;      // biaisé
```

Si le générateur produit 0 à 2³²−1, ces valeurs ne se répartissent pas également sur six restes :
les premiers restes apparaissent une fois de plus que les autres. Le biais est minuscule ici, et
énorme quand la borne est grande devant la portée du générateur.

La correction — le **rejet** :

```c
uint32_t borne(uint32_t maximum) {
    uint32_t limite = UINT32_MAX - (UINT32_MAX % maximum);
    uint32_t valeur;
    do {
        valeur = suivant();
    } while (valeur >= limite);
    return valeur % maximum;
}
```

On jette les valeurs de la zone qui déborde. La boucle se répète rarement, et le résultat est
uniforme.

Le même piège existe avec les flottants : `suivant() / (double)UINT32_MAX` est correct pour du
`[0,1]`, mais convertir en `float` puis multiplier par une borne réintroduit un biais aux
extrémités.

## Uniforme n'est pas ce que tu veux

Une distribution uniforme donne du bruit, et le bruit **ne ressemble pas à du hasard perçu**.

**Le regroupement.** Sur cent tirages uniformes dans un carré, l'œil voit des paquets et des trous,
et trouve ça « pas aléatoire ». Pour des positions bien réparties — arbres, étoiles, points
d'échantillonnage — il faut du **bruit bleu** (Poisson disk), qui interdit deux points trop
proches.

**La malchance.** Un taux de drop de 10 % signifie qu'un joueur sur trois n'a rien après dix
essais. Les jeux corrigent avec un **compteur de pitié** : la probabilité monte à chaque échec, et
devient certaine au bout de N. Le joueur perçoit alors le système comme *plus* juste, alors qu'il
est moins aléatoire.

**Le sac.** Tetris moderne ne tire pas les pièces au hasard : il mélange un sac des sept pièces et
le distribue. Impossible d'avoir trois S d'affilée, impossible d'attendre une barre pendant
vingt pièces.

**Pour choisir des positions au hasard dans un disque** :

```c
double angle = alea() * 2.0 * M_PI;
double rayon = sqrt(alea()) * rayon_max;   // la racine n'est pas décorative
```

Sans la racine carrée, les points se concentrent au centre : la surface d'un anneau croît avec le
rayon.

## Le hachage comme aléatoire

Pour du bruit procédural — terrain, textures, variation par instance — on ne veut pas une suite,
on veut **une valeur reproductible en fonction d'une position**.

```glsl
float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}
```

Aucun état, aucun ordre d'appel, parallélisable : chaque pixel ou chaque instance calcule sa propre
valeur. C'est ce qu'utilisent tous les shaders du dossier `shader/`, et c'est ce qui permet à mille
touffes d'herbe d'être toutes différentes sans stocker mille nombres.

## Mélanger

L'algorithme correct est Fisher-Yates, et il tient en trois lignes :

```c
for (size_t i = taille - 1; i > 0; i--) {
    size_t j = borne(i + 1);
    echanger(&tableau[i], &tableau[j]);
}
```

Le `i + 1` est essentiel : `j` doit pouvoir valoir `i`, sinon certaines permutations deviennent
impossibles et le mélange est biaisé.

L'anti-pattern classique — trier avec un comparateur aléatoire — **ne mélange pas uniformément**, et
peut même planter : un comparateur incohérent n'est pas un ordre, et certains tris sortent alors
des bornes du tableau.

## À retenir

1. Pseudo-aléatoire = déterministe. Note ta graine.
2. `rand()` est mauvais ; xorshift ou PCG pour un jeu.
3. Jamais un générateur non cryptographique pour un secret.
4. `% n` biaise : rejette la zone qui déborde.
5. L'uniforme ne ressemble pas au hasard perçu : bruit bleu, pitié, sac.
6. Racine carrée pour un tirage uniforme dans un disque.
7. Pour du procédural, hache la position ; pas de générateur à état.
8. Fisher-Yates pour mélanger, jamais un tri aléatoire.
