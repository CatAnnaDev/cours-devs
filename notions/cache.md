# Le cache

La notion la plus rentable de toutes, et celle qu'on apprend le plus tard. Deux programmes qui
font exactement le même nombre d'opérations peuvent avoir un facteur dix entre eux, uniquement
selon **l'ordre dans lequel ils touchent la mémoire**.

## La mémoire est lente, et le processeur le sait

| Niveau | Taille | Latence approximative |
|---|---|---|
| registre | quelques dizaines d'octets | 0 cycle |
| cache L1 | 32 à 64 Ko | ~4 cycles |
| cache L2 | 256 Ko à 2 Mo | ~12 cycles |
| cache L3 | 8 à 64 Mo | ~40 cycles |
| RAM | des gigaoctets | **~200 cycles** |

Un accès RAM coûte deux cents cycles. Pendant ce temps, le processeur aurait pu faire deux cents
additions. **Le calcul est gratuit ; c'est la mémoire qui coûte.**

C'est un renversement complet par rapport à l'intuition, et il date des années 90. Compter les
opérations pour estimer la vitesse d'un programme moderne ne marche plus.

## Deux localités

Le cache mise sur deux paris, et ton travail est de les rendre gagnants.

**La localité spatiale** — si tu lis un octet, tu liras bientôt ses voisins. Le processeur ne
charge donc jamais un octet : il charge une **ligne de cache** de 64 octets, soit seize `int`.

**La localité temporelle** — si tu lis une donnée, tu la reliras bientôt. Elle reste donc en
cache.

D'où la règle unique : **parcours la mémoire dans l'ordre, et réutilise ce que tu viens de
toucher.**

## La démonstration en six lignes

```c
for (int y = 0; y < N; y++)
    for (int x = 0; x < N; x++)
        somme += grille[y][x];      // dans l'ordre

for (int x = 0; x < N; x++)
    for (int y = 0; y < N; y++)
        somme += grille[y][x];      // en colonnes
```

Même nombre d'additions, même résultat. Sur une grille de 4096 × 4096, la seconde version est
**cinq à dix fois plus lente** : chaque accès saute de 16 Ko, donc chaque ligne de cache chargée
sert **un seul** élément au lieu de seize.

Fais la mesure une fois. C'est cinq minutes, et c'est le genre de chiffre qu'on n'oublie plus.

## Tableau de structures, ou structure de tableaux

```c
struct Particule { float x, y, z; float vx, vy, vz; int equipe; };
struct Particule particules[100000];         // AoS : Array of Structures
```

```c
struct Particules {
    float x[100000], y[100000], z[100000];
    float vx[100000], vy[100000], vz[100000];
    int equipe[100000];
};                                            // SoA : Structure of Arrays
```

Une boucle qui ne met à jour que les positions à partir des vitesses :

- **AoS** charge 28 octets par particule alors qu'elle en utilise 24, et les `equipe` polluent le
  cache pour rien.
- **SoA** parcourt six tableaux contigus, chaque ligne de cache est utilisée à 100 %, et le
  compilateur peut vectoriser.

L'écart typique va de 2× à 4×. C'est pour ça que les moteurs de jeu modernes utilisent des ECS à
stockage en colonnes, et que les shaders travaillent sur des tampons séparés.

**Ce n'est pas toujours le bon choix** : si tu touches tous les champs d'une seule particule à la
fois, l'AoS gagne. La question à se poser est : *qu'est-ce que ma boucle chaude lit vraiment ?*

## Le faux partage

Deux threads qui écrivent dans deux variables **différentes** mais situées dans la **même ligne
de cache** se battent : chaque écriture invalide la ligne chez l'autre.

```c
struct { int compteur_a; int compteur_b; } partage;   // 8 octets, même ligne
```

Deux threads, deux compteurs indépendants, et un programme parallèle **plus lent** que la version
séquentielle. La correction est un remplissage à 64 octets, ou un compteur local par thread agrégé
à la fin.

C'est le piège numéro un du parallélisme, parce que rien dans le code ne le montre.

## Ce qui casse un cache, en pratique

| Coupable | Pourquoi | Remède |
|---|---|---|
| liste chaînée | chaque nœud est ailleurs | tableau |
| tableau d'objets en Java / C# | tableau de références | tableaux de primitifs, ou `struct` en C# |
| `map` / `unordered_map` | nœuds dispersés | `vector` trié pour les petites tailles |
| pointeur vers pointeur | deux sauts | aplatir |
| grosse structure peu utilisée | remplit les lignes pour rien | séparer chaud et froid |
| accès aléatoire | aucun pari gagné | trier les accès si possible |

## Ce qu'il ne faut pas en conclure

**N'optimise pas ce que tu n'as pas mesuré.** Une boucle exécutée trois fois par frame n'a aucune
importance, même parcourue en colonnes. La localité compte sur les boucles chaudes, celles qui
parcourent beaucoup de données.

**La lisibilité passe d'abord.** Un SoA est plus pénible à écrire et à lire qu'un AoS. On le fait
quand la mesure le justifie, pas par principe.

Voir `mesurer.md` pour savoir si tu es limité par le calcul ou par la mémoire.

## À retenir

1. Un accès RAM coûte deux cents cycles. Le calcul est gratuit à côté.
2. Le processeur charge des lignes de 64 octets : parcours dans l'ordre.
3. Parcourir une matrice en colonnes coûte un facteur cinq à dix.
4. SoA quand la boucle chaude ne lit qu'une partie des champs.
5. Le faux partage rend un programme parallèle plus lent que le séquentiel.
6. Mesure avant, et seulement sur les boucles chaudes.
