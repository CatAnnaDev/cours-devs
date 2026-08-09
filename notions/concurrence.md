# La concurrence

Faire plusieurs choses à la fois, et les trois façons de se tromper.

## Concurrence et parallélisme

Ce ne sont pas la même chose, et confondre les deux mène à de mauvais choix.

**La concurrence** structure un programme en tâches indépendantes. Elle a du sens même sur un seul
cœur : pendant qu'une tâche attend le réseau, une autre travaille.

**Le parallélisme** exécute réellement plusieurs calculs en même temps, sur plusieurs cœurs.

D'où deux outils différents pour deux problèmes :

| Ton problème | L'outil |
|---|---|
| attendre (réseau, disque, base) | asynchrone, `async`/`await`, boucle d'événements |
| calculer (image, physique, compression) | threads, parallélisme de données |

Utiliser des threads pour de l'attente gaspille de la mémoire — chaque thread coûte une pile de
plusieurs centaines de kilo-octets. Utiliser de l'asynchrone pour du calcul ne parallélise rien.

## La course de données

Deux threads accèdent à la même variable, au moins un écrit, sans synchronisation. C'est un
**comportement indéfini**, pas seulement un résultat imprévisible.

```c
compteur++;      // trois opérations : lire, ajouter, écrire
```

Deux threads exécutent ça un million de fois chacun : le résultat n'est pas deux millions. Les
lectures et écritures s'entrelacent, et des incréments se perdent.

Et ce n'est pas qu'une question d'entrelacement : **le compilateur et le processeur réordonnent les
instructions**. Ce que tu as écrit n'est pas ce qui s'exécute, et sans synchronisation, un autre
thread peut voir les effets dans un ordre différent.

C'est pourquoi les astuces artisanales — un `bool` « prêt » testé en boucle — ne marchent pas. Il
faut le dire au compilateur.

## Les outils, du plus simple au plus dangereux

### Ne rien partager

La solution la plus fiable. Chaque thread travaille sur ses données, et le résultat est agrégé à la
fin. Pas de verrou, pas de course, et ça monte en charge linéairement.

C'est ce que font les moteurs de jeu : découper l'image en zones, chaque thread la sienne.

### Message plutôt que mémoire partagée

Des files de messages entre threads. Aucun état commun, donc aucune course possible. C'est le
modèle de Go, d'Erlang, et des systèmes d'acteurs.

### Le verrou

```cpp
std::mutex verrou;
{
    std::lock_guard<std::mutex> garde(verrou);
    compteur++;
}
```

Correct et simple. Deux façons de se tromper :

**Oublier de verrouiller quelque part.** Un seul accès non protégé suffit à casser la garantie.
D'où l'intérêt d'encapsuler la donnée et son verrou ensemble — Rust l'impose avec `Mutex<T>` :
on ne peut littéralement pas accéder à la donnée sans prendre le verrou.

**L'interblocage.** Deux threads, deux verrous, pris dans l'ordre inverse : chacun attend l'autre,
pour toujours. La parade est un **ordre global d'acquisition** — toujours prendre A avant B — ou
`std::scoped_lock` qui prend plusieurs verrous d'un coup, sans interblocage.

Et la règle qui évite l'essentiel : **garde le verrou le moins longtemps possible, et n'appelle
jamais de code inconnu en le tenant** — surtout pas un rappel, qui pourrait reprendre le même
verrou.

### L'atomique

```cpp
std::atomic<int> compteur{0};
compteur++;            // indivisible
```

Une opération atomique n'est jamais coupée en deux. Plus rapide qu'un verrou pour un compteur, et
bien plus subtile dès qu'il y a plusieurs variables : rendre chaque variable atomique **ne rend pas
la séquence atomique**.

Les ordres mémoire (`relaxed`, `acquire`, `release`, `seq_cst`) sont le sujet le plus piégeux du
domaine. La règle de survie : **utilise `seq_cst`, le défaut**, tant que tu n'as pas mesuré que ça
compte et lu la littérature.

### Le lock-free

Des structures sans verrou, à base d'atomiques et de boucles de comparaison-échange. Très rapides,
et **très difficiles à écrire correctement** — le problème ABA, la gestion de la mémoire, la
validation. Ce n'est pas un domaine où l'on improvise : utilise une bibliothèque éprouvée.

## Le faux partage

Deux threads écrivent dans deux variables différentes, mais situées dans la même ligne de cache de
64 octets. Chaque écriture invalide la ligne chez l'autre, et le programme parallèle devient plus
lent que le séquentiel.

Rien dans le code ne le montre. Voir `cache.md` — c'est le piège numéro un du parallélisme.

## Async, et la couleur des fonctions

`async`/`await` transforme le code en machine à états : au lieu de bloquer, la fonction rend la
main et reprend plus tard.

Trois choses à savoir :

**Une fonction asynchrone ne s'appelle que depuis une fonction asynchrone.** C'est la « couleur des
fonctions » : la contagion remonte toute la pile d'appels, et il faut souvent tout convertir.

**Bloquer dans du code asynchrone bloque tout.** Un calcul long ou un appel bloquant dans une tâche
gèle la boucle d'événements et toutes les autres tâches avec.

**Asynchrone ne veut pas dire parallèle.** En JavaScript et en Python, tout tourne sur un seul
thread par défaut. Pour du calcul, il faut de vrais threads ou des processus.

## Les outils qui trouvent les bugs

Les bugs de concurrence sont **non déterministes** : ils apparaissent une fois sur mille, sur la
machine du client, en production. Les tests ne les trouvent pas de manière fiable.

| Outil | Ce qu'il fait |
|---|---|
| ThreadSanitizer | détecte les courses de données à l'exécution (C, C++, Go, Rust) |
| Helgrind (valgrind) | courses et interblocages |
| Loom (Rust) | explore **tous** les entrelacements possibles d'un petit test |
| le compilateur Rust | interdit les courses à la compilation, sans `unsafe` |

**Utilise ThreadSanitizer dès que tu écris du code multithread.** Il trouve en une exécution ce
qu'un test aurait raté mille fois. Il ralentit d'un facteur cinq à quinze : c'est un outil de
développement, pas de production.

## À retenir

1. Concurrence pour attendre, parallélisme pour calculer. Deux outils différents.
2. Une course de données est un comportement indéfini, pas un résultat imprévisible.
3. La meilleure synchronisation est celle dont on n'a pas besoin : ne partage rien.
4. Encapsule la donnée avec son verrou ; garde-le le moins longtemps possible.
5. Atomique par variable ≠ atomique par séquence. `seq_cst` tant que tu n'as pas mesuré.
6. Le faux partage rend le parallèle plus lent, sans rien montrer.
7. `async` est contagieux, et n'est pas du parallélisme.
8. ThreadSanitizer dès la première ligne de multithread.
