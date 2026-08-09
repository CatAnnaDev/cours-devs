# notions

De la culture transverse. Rien ici n'est lié à un langage ni à un projet : ce sont les choses qui
resservent en C, en Rust, en Java, en C#, dans un shader et dans à peu près tout ce que tu
écriras.

À lire **quand tu en croises le besoin**, pas d'un bloc. Chaque page est indépendante, et se
termine par un « à retenir » qu'on peut relire seul.

## Par où commencer

Si tu n'en lis que trois : **`big-o.md`**, **`cache.md`**, **`memoire.md`**. Dans cet ordre. Ce
sont ceux qui changent la façon dont on écrit tout le reste.

## Les données

| Page | Ce qu'elle explique |
|---|---|
| [`big-o.md`](big-o.md) | la complexité : ton code restera-t-il rapide quand les données grossissent |
| [`collections.md`](collections.md) | tableau, liste, dictionnaire, ensemble — laquelle et à quel prix |
| [`hachage.md`](hachage.md) | pourquoi une table de hachage est en O(1), et quand elle cesse de l'être |
| [`memoire.md`](memoire.md) | pile, tas, adresses, alignement, ce que coûte vraiment une allocation |
| [`cache.md`](cache.md) | pourquoi le même calcul peut être dix fois plus lent selon l'ordre des accès |

## Les nombres et le texte

| Page | Ce qu'elle explique |
|---|---|
| [`virgule-flottante.md`](virgule-flottante.md) | pourquoi `0.1 + 0.2 != 0.3`, et les quatre endroits où ça mord |
| [`unicode.md`](unicode.md) | octet, point de code, graphème — et tout ce qui casse quand on les confond |
| [`aleatoire.md`](aleatoire.md) | pseudo-aléatoire, biais du modulo, et pourquoi l'uniforme n'a pas l'air aléatoire |
| [`binaire.md`](binaire.md) | bits, ordre des octets, formats de fichiers, écriture atomique |

## Le code et la machine

| Page | Ce qu'elle explique |
|---|---|
| [`compilation.md`](compilation.md) | de la source au processus, et ce que l'optimiseur fait déjà pour toi |
| [`erreurs.md`](erreurs.md) | codes de retour, exceptions, types somme — et erreur contre bug |
| [`concurrence.md`](concurrence.md) | courses de données, verrous, atomiques, faux partage |
| [`temps.md`](temps.md) | trois horloges, delta time, pas de temps fixe, et les dates |

## Faire vite

| Page | Ce qu'elle explique |
|---|---|
| [`mesurer.md`](mesurer.md) | benchmarker sans mentir, profiler, savoir si on est limité par le calcul ou la mémoire |
| [`optimisations.md`](optimisations.md) | les optimisations qui marchent quel que soit le langage |

## Les formats

| Page | Ce qu'elle explique |
|---|---|
| [`json.md`](json.md) | lire et écrire du JSON sans erreur |

---

Une seule idée à garder avant de commencer : **on écrit d'abord du code clair qui marche ; on
optimise ensuite, et seulement ce qui en a besoin** — voir `mesurer.md` pour savoir quoi.
