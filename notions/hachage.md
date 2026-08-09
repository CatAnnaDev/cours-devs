# Le hachage

Comment une table de hachage peut retrouver une clé en temps constant, et pourquoi ça se dégrade
en temps linéaire quand on s'y prend mal.

## L'idée

Une fonction de hachage transforme une clé quelconque — une chaîne, un objet — en un entier. On
prend le reste de la division par la taille du tableau, et on a un indice.

```
indice = hachage(cle) % nombre_de_cases
```

Chercher devient : calculer le hachage, aller à l'indice, comparer. Une opération, quel que soit
le nombre d'éléments. C'est le O(1) moyen.

Trois choses peuvent le casser, et ce sont les trois sections suivantes.

## Les collisions

Deux clés différentes peuvent donner le même indice. C'est inévitable : il y a une infinité de
chaînes possibles et un nombre fini de cases.

Deux façons de gérer.

**Le chaînage** — chaque case contient une liste des éléments qui y tombent.

```
[0] -> "anna" -> "marc"
[1] -> vide
[2] -> "zoe"
```

Simple, tolérant à une forte charge. Mais chaque nœud est une allocation ailleurs en mémoire :
mauvais pour le cache (voir `cache.md`).

**L'adressage ouvert** — tout est dans le tableau. Si la case est prise, on va voir la suivante.

```
[0] "anna"
[1] "marc"      (voulait 0, décalé)
[2] "zoe"
```

Contigu, donc rapide. Mais il faut garder le tableau à moitié vide, et la suppression demande des
précautions : effacer une case couperait la chaîne de sondage. On marque donc la case comme
« effacée » plutôt que vide — les fameuses *tombstones*.

C'est ce que font les implémentations modernes (`absl::flat_hash_map`, `hashbrown` en Rust, le
`Dictionary` de .NET), pour la contiguïté.

## Le facteur de charge

C'est le rapport `éléments / cases`. Quand il monte, les collisions explosent.

| Charge | Sondages moyens (adressage ouvert) |
|---|---|
| 0.5 | 1.5 |
| 0.75 | 2.5 |
| 0.9 | **5.5** |
| 0.95 | **10.5** |

D'où le **rehash** : quand la charge dépasse un seuil (typiquement 0.7 à 0.9), la table double sa
taille et **replace tous les éléments**. C'est une opération en O(n), amortie sur les insertions —
la même logique que le doublement d'un tableau dynamique.

Conséquence pratique : **réserve d'avance quand tu connais l'ordre de grandeur.** `reserve`,
`with_capacity`, le constructeur avec capacité initiale. Ça évite plusieurs rehash complets.

Et conséquence sournoise : **un rehash invalide les itérateurs**. En C++, `unordered_map` invalide
les itérateurs au rehash mais pas les références ; en Rust et en Java, ça se voit à la compilation
ou lève une exception.

## Une bonne fonction de hachage

Trois propriétés, dans l'ordre d'importance :

**Déterministe** — la même clé donne toujours le même résultat, dans la même exécution.

**Bien répartie** — changer un seul bit de la clé doit changer environ la moitié des bits du
hachage. C'est l'*effet avalanche*. Sans lui, des clés similaires tombent dans les mêmes cases.

**Rapide** — elle est appelée à chaque recherche.

Les mauvaises fonctions classiques :

```c
size_t mauvais(const char *texte) {
    size_t somme = 0;
    while (*texte) somme += *texte++;
    return somme;
}
```

`"abc"`, `"acb"` et `"cba"` donnent le même hachage. Toutes les anagrammes entrent en collision, et
sur un jeu de mots réels c'est catastrophique.

Ce qu'on utilise en vrai : FNV-1a (quatre lignes, correct), xxHash ou wyhash (rapides et bien
répartis), SipHash (résistant aux attaques). FNV-1a, pour l'exemple :

```c
size_t fnv1a(const char *texte) {
    size_t hachage = 14695981039346656037u;
    while (*texte) {
        hachage ^= (unsigned char)*texte++;
        hachage *= 1099511628211u;
    }
    return hachage;
}
```

## Les trois pièges

### La clé mutable

Modifier une clé après insertion la rend **introuvable** : son hachage a changé, elle n'est plus
dans la bonne case. L'élément est là, il occupe de la place, et personne ne peut le retrouver.

C'est pour ça que Rust et Java demandent des clés immuables, et que mettre un objet mutable comme
clé de `HashMap` est un bug qui attend son heure.

### `hashCode` et `equals` désaccordés

Si deux objets sont égaux, ils **doivent** avoir le même hachage. L'inverse n'est pas requis.

Redéfinir `equals` sans redéfinir `hashCode` — l'erreur numéro un en Java et en C# — donne une
table qui perd des éléments : l'objet est cherché dans une case, il est rangé dans une autre.

### L'attaque par collisions

Si un attaquant connaît ta fonction de hachage, il peut fabriquer des milliers de clés qui
entrent toutes en collision. Ta table devient une liste chaînée, tes recherches passent en O(n), et
ton serveur tombe avec quelques kilo-octets de requêtes bien choisies. C'est la *HashDoS*.

La parade est un **hachage à graine aléatoire au démarrage** : c'est ce que font Python, Rust et
Java depuis leurs versions respectives. Conséquence visible : l'ordre de parcours d'un
`HashMap` change d'une exécution à l'autre, et c'est volontaire.

## Quand ne pas utiliser de table de hachage

**Moins de quelques dizaines d'éléments** : un tableau parcouru linéairement est plus rapide. Pas
de hachage à calculer, tout est contigu, le cache fait le reste. Le seuil est plus haut qu'on ne
croit — souvent 30 à 100 éléments.

**Quand tu as besoin de l'ordre** : une table de hachage n'en a aucun. Il faut un arbre (`map`,
`TreeMap`) ou un tableau trié.

**Quand tu as besoin de bornes** — « toutes les clés entre X et Y ». Impossible en hachage.

**Quand les clés sont de petits entiers denses** : un tableau indexé directement est imbattable.
Un identifiant de 0 à 9999 n'a pas besoin d'être haché.

## À retenir

1. Hachage puis modulo : un indice, donc une recherche en temps constant moyen.
2. Chaînage ou adressage ouvert ; le second est plus rapide grâce au cache.
3. Le facteur de charge décide de tout : réserve d'avance.
4. Un bon hachage a l'effet avalanche ; la somme des octets ne l'a pas.
5. Une clé mutable devient introuvable.
6. `equals` sans `hashCode` perd des éléments.
7. Sous quelques dizaines d'éléments, un tableau linéaire gagne.
