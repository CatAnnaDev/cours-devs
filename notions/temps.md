# Le temps

Trois horloges différentes, et à peu près autant de bugs classiques.

## Les horloges

| Horloge | Ce qu'elle mesure | Peut reculer | Pour quoi |
|---|---|---|---|
| **mur** (`wall clock`) | l'heure qu'il est | **oui** | afficher une date, horodater |
| **monotone** | le temps écoulé depuis un point arbitraire | non | **mesurer une durée** |
| **processeur** (`cpu time`) | le temps passé à calculer | non | profiler |

L'horloge murale peut reculer : synchronisation NTP, changement d'heure, utilisateur qui règle sa
pendule. Mesurer une durée avec elle donne parfois des durées **négatives**, et le code qui n'y
avait pas pensé boucle à l'infini ou divise par zéro.

**Règle unique** : pour mesurer un écart, jamais l'horloge murale.

| Langage | Monotone |
|---|---|
| C | `clock_gettime(CLOCK_MONOTONIC, ...)` |
| C++ | `std::chrono::steady_clock` |
| Rust | `std::time::Instant` |
| Java | `System.nanoTime()` |
| C# | `Stopwatch` |

Le piège du nommage : `std::chrono::system_clock` et `System.currentTimeMillis()` sont des horloges
**murales**. Le nom ne le dit pas.

## Delta time

Dans une boucle de jeu, on multiplie les vitesses par le temps écoulé depuis l'image précédente :

```c
position += vitesse * delta;
```

Sans ça, le jeu va deux fois plus vite à 120 images par seconde qu'à 60 — un défaut classique des
jeux des années 80 et 90, qui devenaient injouables sur du matériel plus rapide.

Trois pièges avec `delta` :

**Le pic.** Un chargement, une fenêtre déplacée, un point d'arrêt dans le débogueur, et `delta`
vaut deux secondes. Tout ce qui est intégré fait un bond énorme : le personnage traverse un mur,
la physique explose. **Borne-le** :

```c
if (delta > 0.1) delta = 0.1;
```

Le jeu ralentit au lieu de sauter. C'est le bon compromis.

**L'accumulation.** Additionner `delta` pour tenir un temps total accumule les erreurs d'arrondi
(voir `virgule-flottante.md`). Pour un compte à rebours, un timer d'animation, un cycle
jour/nuit : accumule en `double`, ou compte les images, ou repars d'un instant de départ.

**Le lissage exponentiel dépendant du framerate.** L'erreur la plus répandue :

```c
camera = camera * 0.9 + cible * 0.1;      // faux : dépend du framerate
```

À 120 images par seconde, la caméra suit deux fois plus vite qu'à 60. La forme correcte :

```c
double facteur = 1.0 - pow(0.1, delta);   // « il reste 10 % après une seconde »
camera = camera + (cible - camera) * facteur;
```

Cette formule sert partout : caméras, inertie, lissage de valeurs, effacement progressif d'une
texture de traces.

## Le pas de temps fixe

La physique n'aime pas les pas variables : le résultat dépend du framerate, les collisions ratent,
et deux machines ne simulent pas la même chose.

La solution standard — l'accumulateur :

```c
accumulateur += delta;
while (accumulateur >= PAS) {
    simuler(PAS);
    accumulateur -= PAS;
}
double alpha = accumulateur / PAS;
afficher(interpoler(etat_precedent, etat_courant, alpha));
```

La simulation avance par pas identiques, quel que soit le framerate. Le rendu, lui, **interpole**
entre les deux derniers états — sinon on voit un saccadement à 60 images par seconde avec une
physique à 50 Hz.

Le `while` doit être borné : si la simulation est plus lente que le temps réel, l'accumulateur
grandit sans fin et le programme se fige. Limite à quelques itérations et accepte de ralentir.

## Les durées, les unités, les entiers

Stocke des durées en **entiers**, dans la plus petite unité utile — millisecondes, microsecondes,
nanosecondes. Pas en `float` : voir `virgule-flottante.md`, un `float` de secondes perd sa
précision après quelques heures.

Et **mets l'unité dans le nom** : `delai_ms`, `duree_us`. La moitié des bugs de temps sont des
confusions d'unité, et le compilateur ne peut rien pour toi. Les langages qui ont un type de durée
— `std::chrono::milliseconds`, `TimeSpan`, `Duration` — le font pour toi : utilise-les.

## Les dates, et pourquoi c'est pire

Le temps physique est simple. Le temps **civil** est un champ de mines :

- une journée ne fait pas toujours 24 heures — passage à l'heure d'été ;
- une minute ne fait pas toujours 60 secondes — secondes intercalaires ;
- certaines heures locales **n'existent pas**, d'autres existent **deux fois** ;
- les fuseaux changent par décision politique, plusieurs fois par an dans le monde ;
- « demain à la même heure » n'est pas « dans 24 heures ».

Trois règles qui évitent l'essentiel :

1. **Stocke en UTC**, convertis à l'affichage.
2. **N'écris jamais ta propre arithmétique de dates.** Une bibliothèque de fuseaux, toujours.
3. **Pour un instant précis, stocke un instant** (epoch). Pour un rendez-vous humain, stocke la
   date locale **et** le fuseau — parce que si le fuseau change, le rendez-vous doit rester à
   14 h.

## Mesurer une durée correctement

```c
struct timespec debut, fin;
clock_gettime(CLOCK_MONOTONIC, &debut);
travail();
clock_gettime(CLOCK_MONOTONIC, &fin);

double secondes = (fin.tv_sec - debut.tv_sec)
                + (fin.tv_nsec - debut.tv_nsec) * 1e-9;
```

Et si la durée mesurée est courte, une seule mesure ne veut rien dire : voir `mesurer.md`.

## À retenir

1. Horloge monotone pour les durées, murale pour les dates. Jamais l'inverse.
2. Borne `delta` : un pic est plus dangereux qu'un ralenti.
3. Un lissage doit passer par `pow(taux, delta)` pour ne pas dépendre du framerate.
4. Physique à pas fixe, rendu interpolé.
5. Durées en entiers, unité dans le nom.
6. UTC pour stocker, bibliothèque pour convertir, jamais d'arithmétique maison.
