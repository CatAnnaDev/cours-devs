# Le texte

Un `char` n'est pas un caractère. Cette phrase résume à peu près tout ce qui casse dans le
traitement du texte.

## Trois notions qu'on confond

| Notion | Exemple sur « é » | Ce que c'est |
|---|---|---|
| **octet** | 2 octets en UTF-8 | l'unité de stockage |
| **point de code** | U+00E9 | l'unité d'Unicode |
| **graphème** | 1 | ce que l'humain appelle « un caractère » |

Les trois coïncident en ASCII pur, et **seulement là**. C'est pourquoi tant de code marche jusqu'au
jour où quelqu'un tape un accent.

Pire : « é » peut s'écrire de **deux** façons — un seul point de code (U+00E9), ou deux (U+0065
« e » + U+0301 « accent aigu »). Les deux s'affichent pareil, ne sont pas égales octet par octet, et
n'ont pas la même longueur. C'est le sujet de la **normalisation** (NFC / NFD), et macOS et Linux
n'ont historiquement pas fait le même choix pour les noms de fichiers.

## UTF-8

L'encodage qui a gagné, pour de bonnes raisons.

| Points de code | Octets |
|---|---|
| U+0000 – U+007F (ASCII) | 1 |
| U+0080 – U+07FF (latin accentué, grec, cyrillique) | 2 |
| U+0800 – U+FFFF (CJK, la plupart des langues) | 3 |
| U+10000 – U+10FFFF (emoji, écritures rares) | 4 |

Trois propriétés qui expliquent son succès :

**L'ASCII est de l'UTF-8 valide**, octet pour octet. Tout le code existant qui traitait de l'ASCII
continue de marcher.

**Aucun octet de continuation ne ressemble à un octet ASCII.** Chercher `'/'` dans un chemin UTF-8
ne peut jamais tomber au milieu d'un caractère.

**Il est auto-synchronisant.** À partir de n'importe quel octet, on retrouve le début du caractère
en reculant tant que l'octet commence par `10`.

Les autres : UTF-16 (Java, C#, JavaScript en interne) utilise 2 ou 4 octets, ce qui donne les
fameuses *surrogate pairs* et fait que `"emoji".length` vaut 2 en JavaScript. UTF-32 utilise 4
octets pour tout — simple et gaspilleur.

## Ce qui casse, concrètement

**Compter les caractères.** `strlen("héllo")` vaut 6 en C. `"👍".length` vaut 2 en Java, C# et
JavaScript. `"👨‍👩‍👧".length` vaut 8 : c'est trois personnes liées par des jointeurs invisibles.

**Couper une chaîne.** Tronquer à 100 octets peut couper un caractère en deux et produire une
séquence invalide. Tronquer à 100 points de code peut séparer une lettre de son accent.

**Inverser une chaîne.** Inverser les octets d'un texte UTF-8 le détruit. Inverser les points de
code déplace les accents sur les mauvaises lettres.

**Changer la casse.** `'a' - 32` ne marche que sur l'ASCII. Et même correctement fait, la casse est
**dépendante de la langue** : en turc, la majuscule de `i` est `İ`, pas `I`. Le bug classique :
comparer une commande en la passant en majuscules, sur un système en turc.

**Comparer.** `strcmp` compare des octets. L'ordre obtenu n'est l'ordre alphabétique d'aucune
langue : les majuscules passent avant les minuscules, et les accents partent à la fin. Trier des
noms correctement demande une bibliothèque de collation.

## La stratégie qui marche

> **Traite le texte comme des octets, et ne l'interprète que quand tu n'as pas le choix.**

Copier, concaténer, stocker, transmettre, comparer pour l'égalité exacte : tout ça marche
parfaitement en octets, sans rien comprendre à Unicode.

Interpréter — compter, couper, trier, changer la casse — demande une vraie bibliothèque. Il n'y a
pas de version simple, et écrire la sienne est une erreur qu'on ne fait qu'une fois.

Trois règles qui évitent presque tout :

1. **UTF-8 partout, du fichier au réseau à la base.** Décode à l'entrée, encode à la sortie,
   travaille en UTF-8 au milieu.
2. **Ne suppose jamais un caractère par octet, ni par point de code.**
3. **Valide à la frontière.** Une entrée qui n'est pas de l'UTF-8 valide est rejetée là, pas trois
   couches plus loin.

## Par langage

| Langage | Type texte | Unité | Piège principal |
|---|---|---|---|
| C | `char*` | octet | tout est à ta charge |
| C++ | `std::string` | octet | `size()` compte les octets |
| Rust | `String` / `&str` | octet, **UTF-8 garanti** | on ne peut pas indexer par entier, et c'est voulu |
| Java | `String` | UTF-16 | `length()` compte les unités, pas les caractères |
| C# | `string` | UTF-16 | idem |
| Python 3 | `str` | point de code | conversion implicite absente, ce qui est une bonne chose |

Rust est le plus strict : une `String` est **toujours** de l'UTF-8 valide, et `chaine[3]` ne
compile pas. Ça agace au début, et ça supprime une catégorie entière de bugs.

## Et les fichiers

**Le BOM** (`EF BB BF` en UTF-8) est un marqueur d'octets en tête de fichier. Il est inutile en
UTF-8, Windows en met parfois, et il casse les scripts shell et les fichiers JSON. Ne l'écris
jamais ; sois prêt à le sauter en lecture.

**Les fins de ligne** : `\n` partout sauf Windows, qui utilise `\r\n`. Git peut convertir
automatiquement, ce qui règle le problème ou le crée, selon la configuration.

**Les noms de fichiers** ne sont pas garantis valides en UTF-8 sous Linux — ce sont des suites
d'octets. Un programme qui suppose le contraire plante sur les fichiers de quelqu'un d'autre.

## À retenir

1. Octet, point de code, graphème : trois choses différentes.
2. UTF-8 partout ; il est compatible ASCII et auto-synchronisant.
3. Compter, couper, trier, changer la casse : jamais à la main.
4. `"👍".length` vaut 2 en Java, C# et JavaScript.
5. Traite le texte comme des octets tant que tu peux.
6. Valide l'UTF-8 à la frontière, pas au milieu.
