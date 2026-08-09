# La mémoire

Valable partout : C, C++, Rust, Java, C#, GLSL. Les langages cachent plus ou moins la mécanique,
mais la machine est la même.

## Une adresse, c'est un nombre

La mémoire est un immense tableau d'octets, numérotés de 0 à quelques milliards. Une adresse est
un indice dans ce tableau. Un pointeur est une variable qui contient une adresse. C'est tout.

Ce qui change d'un langage à l'autre, ce n'est pas la mécanique — c'est **qui a le droit de
manipuler ces nombres**, et **qui décide quand la zone cesse d'être valide**.

| Langage | Qui libère | Ce que tu peux casser |
|---|---|---|
| C | toi, à la main | tout |
| C++ | les destructeurs, si tu les laisses faire | tout, mais rarement par accident |
| Rust | le compilateur, à la compilation | rien, sans `unsafe` |
| Java, C# | le ramasse-miettes | rien, mais tu peux fuir |

## Trois zones

| Zone | Qui décide | Quand ça meurt | Taille typique |
|---|---|---|---|
| **statique** | le compilateur | fin du programme | fixe |
| **pile** | le compilateur | sortie de la fonction | 1 à 8 Mo |
| **tas** | toi (ou le GC) | quand plus personne ne la tient | la RAM |

**La pile** est un simple pointeur qu'on déplace. Réserver une variable locale coûte une addition.
Elle est petite : une récursion trop profonde ou un tableau local de dix mégaoctets la fait
déborder, et le message est brutal (`stack overflow`, `SIGSEGV`).

**Le tas** est géré par un allocateur, qui tient une structure de données des blocs libres.
Allouer coûte entre quelques dizaines et quelques centaines de nanosecondes — mille fois plus
qu'une variable locale. C'est le coût le plus sous-estimé de tout le développement.

**Le statique** contient les constantes, les chaînes littérales, les variables globales. C'est
pourquoi écrire dans une chaîne littérale plante en C : la zone est en lecture seule.

## Ce que coûte vraiment une allocation

Ce n'est pas seulement le temps de `malloc` :

1. **Trouver un bloc libre** — parcours d'une structure, parfois un verrou si l'allocateur est
   partagé entre threads.
2. **Rater le cache** — le nouveau bloc n'est pas dans le cache, le premier accès coûte cent
   cycles.
3. **Fragmenter** — mille petites allocations dispersées rendent tous les parcours futurs plus
   lents.
4. **Libérer** — et, avec un ramasse-miettes, provoquer une pause plus tard, à un moment que tu
   ne choisis pas.

D'où la règle qui vaut dans tous les langages : **le meilleur allocateur est celui qu'on
n'appelle pas.**

Les trois façons de ne pas l'appeler :

- **Réserver d'avance** : `reserve`, `ensureCapacity`, `with_capacity`. Une allocation au lieu de
  huit.
- **Réutiliser** : un pool d'objets, un tampon qu'on vide au lieu de le recréer.
- **Mettre sur la pile** : un tableau local plutôt qu'un vecteur, quand la taille est bornée et
  petite.

## L'alignement

Le processeur lit la mémoire par blocs, à des adresses multiples de la taille du type. Un `int` de
4 octets veut une adresse multiple de 4. Le compilateur ajoute donc du **remplissage** :

```c
struct Mauvais {   // 24 octets
    char  a;       // 1 octet + 7 de remplissage
    double b;      // 8
    char  c;       // 1 + 7 de remplissage
};

struct Bon {       // 16 octets
    double b;      // 8
    char  a;       // 1
    char  c;       // 1 + 6 de remplissage
};
```

**Range tes champs du plus grand au plus petit.** C'est gratuit, et sur un million d'objets ça
fait huit mégaoctets de différence — donc autant de cache économisé.

Ça vaut aussi en C#, en Rust (sauf `repr(C)`, où l'ordre est imposé), et pour les tampons GPU où
les règles d'alignement sont encore plus strictes (`std140` aligne les `vec3` sur 16 octets).

## Le ramasse-miettes ne supprime pas le problème

En Java ou C#, tu ne peux pas libérer trop tôt. Tu peux encore :

**Fuir.** Un objet référencé par une collection statique n'est jamais collecté. C'est la fuite
classique en Java, et elle ressemble à un `HashMap` qu'on remplit et qu'on ne vide jamais.

**Payer des pauses.** Le GC doit s'exécuter, et il choisit son moment. Sur un jeu à 60 images par
seconde, une pause de 20 ms est une image sautée. C'est pour ça que le code de jeu en C# évite
d'allouer **par image** — et que `csharplings` a une section entière là-dessus.

**Payer l'indirection.** Un tableau d'objets en Java est un tableau de références : chaque
élément est ailleurs en mémoire. Un tableau de structures en C, C++ ou Rust est contigu. Voir
`cache.md`.

## Les fautes classiques, et où elles existent

| Faute | C | C++ | Rust | Java / C# |
|---|---|---|---|---|
| dépassement de tampon | oui | oui | non | non |
| utilisation après libération | oui | oui | non | non |
| double libération | oui | oui | non | non |
| fuite | oui | oui | possible | oui |
| pointeur pendant | oui | oui | non | non |

Rust supprime les quatre premières par son système de propriété — c'est son unique argument, et il
est énorme. Les langages à GC en suppriment quatre aussi, au prix des pauses et de l'indirection.
C et C++ te laissent tout, et te donnent en échange les sanitizers pour les attraper.

## Les outils

| Outil | Ce qu'il attrape | Où |
|---|---|---|
| AddressSanitizer | dépassements, use-after-free, double free | C, C++, à la compilation |
| UndefinedBehaviorSanitizer | débordements, décalages, alignement | C, C++ |
| valgrind | tout ça, plus les fuites, sans recompiler | Linux surtout |
| ThreadSanitizer | courses de données | C, C++, Go |
| Miri | UB dans du `unsafe` | Rust |
| un profileur de tas | fuites et pics | Java, C#, natif |

Sur macOS ARM, LeakSanitizer n'existe pas : compte tes allocations à la main, ce que font de toute
façon tous les moteurs sérieux.

## À retenir

1. Une adresse est un indice dans un tableau d'octets. Le reste est de la politique de langage.
2. Pile : rapide, petite, automatique. Tas : souple, cher, manuel ou collecté.
3. Le coût d'une allocation dépasse largement `malloc` : cache, fragmentation, pauses.
4. Réserve, réutilise, ou mets sur la pile.
5. Range les champs du plus grand au plus petit.
6. Un ramasse-miettes supprime les fautes, pas les fuites ni le coût.
