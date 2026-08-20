# 19 — Le texte

Les notions transverses — les trois niveaux, la normalisation, ce qui casse dans tous les
langages — sont dans `notions/unicode.md`. Ici on fait ce qu'aucun langage à type texte ne laisse
faire : on ouvre les octets, et on écrit le décodeur à la main.

Tout ce qui est chiffré dans ce chapitre a été compilé et lancé sur la machine de référence :
arm64, macOS 27, Apple clang 21, `-std=c17`, avec les options du lanceur d'exercices.

## Octet, point de code, graphème

| Niveau | Ce que c'est | Qui le définit |
|---|---|---|
| **octet** | l'unité de stockage, ce que compte `strlen` | le C |
| **point de code** | un numéro entre U+0000 et U+10FFFF | Unicode |
| **graphème** | ce que l'humain voit comme un dessin | Unicode, annexe UAX #29 |

Les trois coïncident en ASCII pur, et seulement là. Voici un mot où ils diffèrent tous les trois :

```c
const char *s = "👨‍👩‍👧";     // trois personnes et deux liants invisibles
printf("%zu\n", strlen(s));   // 18
```

**18 octets, 5 points de code, 1 graphème.** Les cinq points de code sont U+1F468, U+200D, U+1F469,
U+200D, U+1F467 : deux jointeurs sans chasse (`E2 80 8D`) collent trois bonshommes en un seul
dessin. Compté avec Swift, qui implémente UAX #29 : 18, 5 et 1.

Moins exotique : « noël » décomposé — `n o e` suivi du tréma combinant U+0308 — fait 6 octets, 5
points de code, 4 graphèmes, et s'affiche exactement comme la forme composée, qui en fait 5 et 4.
Canoniquement équivalentes pour Unicode, différentes pour `strcmp`.

## UTF-8

UTF-8 encode un point de code sur un à quatre octets. La longueur se lit dans les bits de tête du
premier octet, et **tous** les octets suivants commencent par `10`.

| Points de code | Octets | Motif binaire | Utiles |
|---|---|---|---|
| U+0000 – U+007F | 1 | `0xxxxxxx` | 7 bits |
| U+0080 – U+07FF | 2 | `110xxxxx 10xxxxxx` | 11 bits |
| U+0800 – U+FFFF | 3 | `1110xxxx 10xxxxxx 10xxxxxx` | 16 bits |
| U+10000 – U+10FFFF | 4 | `11110xxx 10xxxxxx 10xxxxxx 10xxxxxx` | 21 bits |

Le plafond U+10FFFF et la limite à quatre octets viennent de la RFC 3629 (2003) ; la première
version montait à six octets et U+7FFFFFFF.

### Quatre propriétés, et ce qu'elles achètent

**L'ASCII est de l'UTF-8, octet pour octet.** U+0000 à U+007F s'encodent sur un octet identique à
leur valeur ASCII : tout le code C qui manipulait de l'ASCII continue de marcher sans être touché.

**Aucun octet ASCII n'apparaît dans une séquence multi-octets.** Vérifié en encodant les 1 111 936
points de code non-ASCII de tout Unicode : 0 violation. Donc `strchr(chemin, '/')` ne tombe jamais
au milieu d'un caractère.

**Il n'y a aucun octet nul, sauf celui de U+0000.** Vérifié sur les 1 112 064 points de code : un
seul octet nul dans tout l'encodage. C'est ce qui rend UTF-8 compatible avec la convention de
chaîne du C — ce qu'UTF-16 ne permet pas, où `"AB"` s'écrit `41 00 42 00` et où `strlen` rend 1.

**L'ordre des octets suit l'ordre des points de code.** Vérifié sur les 1 112 063 paires
consécutives de tout Unicode, hors substituts : 0 inversion. `memcmp` sur de l'UTF-8 valide trie
donc comme un tri par numéro de point de code — utile pour un index, mais ce n'est **pas** l'ordre
alphabétique, voir plus bas.

L'auto-synchronisation découle de la deuxième propriété : depuis n'importe quel octet, on retrouve
le début du caractère en reculant tant que `(octet & 0xC0) == 0x80`, trois pas au maximum.

## Décoder à la main

Le décodeur tient en trente lignes, et il refuse tout ce qui n'est pas canonique.

```c
typedef struct { uint32_t point; int taille; } Lu;

static Lu lire_point(const unsigned char *p, size_t reste) {
    if (reste == 0) return (Lu){0, 0};
    unsigned char c = p[0];
    if (c < 0x80) return (Lu){c, 1};                    // ASCII, cas majoritaire
    int n;
    uint32_t v;
    if      ((c & 0xE0) == 0xC0) { n = 2; v = c & 0x1Fu; }
    else if ((c & 0xF0) == 0xE0) { n = 3; v = c & 0x0Fu; }
    else if ((c & 0xF8) == 0xF0) { n = 4; v = c & 0x07u; }
    else return (Lu){0, -1};                            // 10xxxxxx orphelin, ou 0xF8..0xFF
    if (reste < (size_t)n) return (Lu){0, -1};          // tronque en fin de tampon
    for (int i = 1; i < n; i++) {
        if ((p[i] & 0xC0) != 0x80) return (Lu){0, -1};  // continuation manquante
        v = (v << 6) | (p[i] & 0x3Fu);
    }
    static const uint32_t plancher[5] = {0, 0, 0x80, 0x800, 0x10000};
    if (v < plancher[n] || v > 0x10FFFF || (v >= 0xD800 && v <= 0xDFFF)) return (Lu){0, -1};
    return (Lu){v, n};
}
```

Le masque du premier octet dépend de la longueur : `0x1F`, `0x0F`, `0x07`, soit les bits utiles.
Chaque continuation en apporte six, d'où le `<< 6`. Et `plancher` rejette les surlongues, la ligne
la plus importante du fichier.

Lancé sur `"un € et un 👍"`, il rend douze points de code, dont `U+20AC` sur trois octets et
`U+1F44D` sur quatre : **17 octets pour 12 points de code**. Compter les points de code n'a même
pas besoin du décodeur, il suffit de compter les octets qui ne sont pas des continuations, soit
`points += ((*p & 0xC0) != 0x80);` sur chaque octet.

## Valider, et pourquoi c'est de la sécurité

Un décodeur permissif est une faille. Trois familles à rejeter.

**Les surlongues.** Rien n'empêche mécaniquement d'encoder `/` (U+002F) sur deux octets : `C0 AF`
se relit `00000 101111`, soit 0x2F, et un décodeur laxiste rend bien `/`. Unicode l'interdit
formellement : une seule séquence par point de code, la plus courte.

**Les substituts.** U+D800 à U+DFFF n'existent que dans UTF-16, pour coder les paires ; encodés en
UTF-8 — `ED A0 80` et suivants — ils ne représentent aucun caractère.

**Les continuations orphelines et les octets impossibles.** Un `10xxxxxx` en tête, un `C0`, un
`C1`, ou quoi que ce soit de `F5` à `FF` : aucun ne peut commencer une séquence valide.

Ces règles donnent la table « Well-Formed UTF-8 Byte Sequences » du standard Unicode : les seuls
premiers octets légaux sont `00..7F`, `C2..DF`, `E0..F4`, avec des seconds octets restreints après
`E0` (`A0..BF`), `ED` (`80..9F`) et `F4` (`80..8F`). Passé en force brute sur tout l'espace des
séquences, le décodeur ci-dessus accepte exactement 128 séquences d'un octet, 1920 de deux, 61 440
de trois et 1 048 576 de quatre — soit un point de code par séquence, ni plus ni moins.

### Le cas réel

En octobre 2000, la faille dite « Web Server Folder Traversal » d'IIS 4.0 et 5.0 (MS00-078,
CVE-2000-0884) reposait là-dessus : le serveur filtrait `../` dans l'URL, puis décodait l'UTF-8
**après** le filtre, avec un décodeur qui acceptait les surlongues. Les vers Nimda et Code Blue
s'en sont servis en masse. Reproduit ici en trente lignes :

```
requete : /pages/../../etc/passwd            -> filtre : rejete
requete : /pages/..%c0%af..%c0%afetc/passwd  -> filtre : ACCEPTE
                       puis decodage permissif : /pages/../../etc/passwd
```

La règle qui en sort : **décoder d'abord, valider ensuite, filtrer sur la forme décodée**, et
rejeter l'entrée invalide au lieu de la réparer. Les fonctions du système sont strictes : sur
macOS, `iconv` rend `-1` avec `errno = EILSEQ` sur `C0 AF`, `E0 80 AF` et `ED A0 80`. Ne compte
pas sur `mbrtowc` pour cela : selon la locale il accepte des surlongues en silence. Un validateur
que tu contrôles vaut mieux qu'une fonction dont la sévérité dépend d'un état global.

## Ce que le C ne fait pas pour toi

`char` veut dire octet. La bibliothèque standard traite des octets, et quand elle prétend traiter
des caractères, elle consulte la locale — un état global réglé par `setlocale`, qui vaut `"C"`
tant que tu n'as rien fait.

| Ce que tu veux | Ce que tu écris | Ce que ça fait vraiment | |
|---|---|---|---|
| copier, concaténer, écrire | `memcpy`, `snprintf`, `fwrite` | des octets | marche |
| égalité exacte | `strcmp(a, b) == 0` | des octets | marche, à normalisation égale |
| chercher un mot | `strstr` | des octets | marche, voir plus bas |
| chercher `/`, `:`, `\n` | `strchr` | un octet ASCII | marche |
| compter les caractères | `strlen` | compte des octets | **ment** |
| couper à n caractères | `%.*s`, `strncpy` | coupe des octets | **ment** |
| aligner en colonnes | `printf("%-10s")` | remplit en octets | **ment** |
| mettre en majuscules | `toupper` | un octet, selon la locale | **ment** |
| est-ce une lettre | `isalpha` | un octet, selon la locale | **ment** |
| trier | `strcmp`, `qsort` | ordre des octets | **ment** |

Trois exemples mesurés. `strlen("héllo")` rend **6**. `printf("|%-8s|", "héllo")` écrit
`|héllo  |` : le remplissage compte les 6 octets déjà écrits, donc 2 espaces au lieu de 3, et la
colonne est de travers. `snprintf(t, n, "%.2s", "héllo")` écrit `68 C3` — un `h` et la moitié d'un
`é` : de l'UTF-8 invalide produit par la bibliothèque standard elle-même.

`toupper` mérite un paragraphe. La norme exige que son argument soit représentable en
`unsigned char`, ou `EOF` ; tout le reste est un comportement indéfini. Or sur arm64 macOS `char`
est **signé** (`CHAR_MIN` vaut -128), donc `toupper(c)` sur le premier octet d'un `é` passe -61 et
sort du tableau de la locale. Ici ça rend -61 en silence, le pire des cas. Écris toujours
`toupper((unsigned char)c)` — et même correct, `toupper` ne peut pas changer la casse d'une lettre
accentuée : elle fait deux octets, la fonction en rend un.

Les types larges ne sauvent pas. `wchar_t` fait 4 octets ici, 2 sur Windows : sa taille n'est pas
fixée par la norme. `MB_CUR_MAX` vaut 1 en locale `"C"` et 4 après
`setlocale(LC_ALL, "fr_FR.UTF-8")` — sans `setlocale`, la locale `"C"` ne décode que l'ASCII, donc
`mbrtowc` refuse tout octet au-delà de 0x7F. Les
littéraux typés, eux, sont fiables : `u8"..."` est de l'UTF-8 garanti par le compilateur, `U"..."`
de l'UTF-32. Détail : `u8"a"[0]` est un `char` en C17 et un `unsigned char` en C23 (le `char8_t`).

## Couper, comparer, chercher

### Couper

Tronquer à un nombre d'octets arbitraire coupe un caractère en deux. Le remède tient en une
boucle : reculer tant qu'on est sur une continuation.

```c
static size_t couper_octets(const char *s, size_t n, size_t max) {
    if (n <= max) return n;
    size_t i = max;
    while (i > 0 && ((unsigned char)s[i] & 0xC0) == 0x80) i--;
    return i;
}
```

Sur `"crème brûlée"`, une coupe naïve à 3 rend `63 72 C3` — invalide ; `couper_octets` rend 2
octets, `"cr"`. La boucle ne tourne jamais plus de trois fois. Ça garantit de l'UTF-8 valide, pas
des graphèmes entiers : couper juste avant un accent combinant reste possible.

### Comparer

Pour l'égalité exacte, `strcmp` est correct et rapide — à condition que les deux côtés soient dans
la même forme de normalisation. Les deux écritures de « noël » vues plus haut ne sont pas égales
octet à octet, et le seul remède est de normaliser en NFC à l'entrée, ce qui demande une
bibliothèque. Insensible à la casse, il n'y a pas de version courte : `strcasecmp` est ASCII seul,
et la vraie opération Unicode s'appelle le *case folding*, pas `toupper`.

### Chercher

Bonne nouvelle, due à l'auto-synchronisation : **`strstr` avec une aiguille UTF-8 valide dans un
foin UTF-8 valide ne peut trouver qu'à une frontière de point de code.** Un faux positif à cheval
sur deux caractères est impossible, parce que le premier octet d'une aiguille valide n'est jamais
une continuation. Vérifié sur 200 000 recherches aléatoires : 51 818 trouvées, 0 hors frontière.

Mauvaise nouvelle : `strchr` avec un octet non-ASCII n'a aucune de ces garanties. `strchr(t, 0xA8)`
sur `"crème"` trouve la deuxième moitié du `è` : cherche des chaînes, jamais des octets isolés
au-dessus de 0x7F. Et une frontière de point de code n'est pas une frontière de graphème — chercher
`"e"` dans le « noël » décomposé trouve l'octet 2, et couper juste après sépare la lettre du tréma.

## L'ordre alphabétique n'existe pas

`strcmp` rend l'ordre des octets, donc celui des points de code : un ordre parfaitement défini,
stable, reproductible, et qui n'est l'ordre alphabétique d'**aucune** langue. Six mots français,
triés avec `strcmp` puis avec `strcoll` sous `fr_FR.UTF-8` :

```
strcmp   Ane eau zoo zèbre âne élan
fr_FR    Ane âne eau élan zèbre zoo
```

Deux fautes dans la première ligne : les majuscules passent avant toutes les minuscules (`Ane`
avant `eau` est un hasard heureux, `Zoo` avant `abc` ne l'est pas), et tout ce qui porte un accent
part à la fin, parce que son premier octet vaut au moins 0xC3.

Et il n'y a pas un ordre correct, il y en a un par langue. Trois mots, deux locales :

```
sv_SE    yta zoo äpple
fr_FR    äpple yta zoo
```

En suédois `ä` est une lettre à part entière, après `z` ; en français c'est un `a` avec un signe.
Les deux tris sont justes, aucun tri unique ne satisfait les deux.

Trier du texte demande l'algorithme de collation Unicode (UTS #10) : des poids sur plusieurs
niveaux — lettre de base, puis accent, puis casse —, une table de référence, une adaptation par
langue, des contractions (le `ch` de l'espagnol traditionnel compte pour une lettre) et des
expansions (`œ` se compare comme `oe`). En C portable, le maximum disponible est `strcoll` et
`strxfrm` après `setlocale(LC_COLLATE, ...)`. Trois réserves, et la première est la plus gênante :
la locale est un état **global** du processus, donc inutilisable pour trier deux langues à la fois
— macOS offre bien `strcoll_l` et une `locale_t` par fil, mais c'est une extension, pas du C
portable. Ensuite le nom de la locale n'est pas portable. Enfin la table est celle du système, donc
le même programme trie différemment ailleurs. Pour un tri identique partout, il faut ICU. Enfin,
`strxfrm` transforme une chaîne en clé binaire que `strcmp` ordonne comme `strcoll` ordonne les
chaînes : n transformées au lieu de n log n collations.

## Les autres encodages, et la seule règle qui tient

Tu croiseras encore **ISO 8859-1** (Latin-1) et son cousin **Windows-1252**, qui remplit les
positions 0x80–0x9F et sert de défaut à beaucoup de vieux logiciels ; **ISO 8859-15**, Latin-1
plus l'euro ; **UTF-16** avec ou sans marque d'ordre ; et les encodages nationaux comme Shift-JIS
ou GBK. Aucun n'est un bon choix aujourd'hui, tous existent dans des fichiers que tu vas lire.

Quand deux encodages se croisent, le symptôme est le *mojibake* : de l'UTF-8 relu comme du Latin-1
transforme `café` en `cafÃ©`, parce que `C3 A9` se relit comme deux caractères séparés. Vérifié
avec `iconv`. C'est réversible tant qu'on n'a rien réencodé par-dessus, définitif au deuxième
aller-retour.

La marque d'ordre, `EF BB BF` en UTF-8, est le troisième piège. Elle ne sert à rien — il n'y a pas
d'ordre d'octets à marquer —, Windows en met parfois, et elle casse les scripts shell, les fichiers
JSON et les analyseurs qui attendent un premier caractère précis. Ne l'écris jamais, sois prêt à la
sauter en lecture. La seule règle qui tient, elle, est celle-ci :

> **Décoder à l'entrée, travailler en interne dans un seul encodage, encoder à la sortie.**

L'encodage interne est UTF-8, sans exception. La conversion se fait à la frontière, par `iconv`
(POSIX ; sur macOS il faut lier avec `-liconv`), et tout ce qui n'est pas décodable est **rejeté
là**, pas trois couches plus loin. Un programme qui ne sait pas dans quel encodage est un tampon
donné a déjà le bug ; il attend juste la bonne entrée. Le prix est ridicule : la phrase « Les naïfs
mangeaient des oeufs à la crème brûlée. » fait 53 octets pour 47 points de code, soit environ 13 %
de plus qu'un encodage à un octet par caractère, et zéro pour un texte anglais. Note au passage que
la phrase d'origine, avec sa ligature, ne s'écrirait même pas en Latin-1 : c'est exactement le
genre de caractère qui a fait abandonner ces jeux.

## À retenir

1. Octet, point de code, graphème : la famille emoji en compte 18, 5 et 1 ; `strlen` voit 18.
2. UTF-8 tient en quatre motifs de bits ; aucun octet ASCII n'apparaît dans une séquence, aucun
   octet nul hors U+0000, et l'ordre des octets est celui des points de code.
3. Le décodeur fait trente lignes, et sa ligne critique est le plancher qui rejette les surlongues.
4. Surlongues, substituts, continuations orphelines : trois familles à rejeter, jamais à réparer.
   Décoder d'abord, valider ensuite, filtrer après — l'inverse a donné CVE-2000-0884.
5. `strlen`, `%.*s`, `%-10s`, `toupper`, `isalpha` comptent ou testent des octets ; `toupper` sur
   un `char` signé négatif est en plus un comportement indéfini.
6. `strstr` sur de l'UTF-8 valide ne trouve qu'à une frontière de point de code ; `strchr` sur un
   octet au-dessus de 0x7F ne garantit rien.
7. `strcmp` trie par point de code, ce qui n'est l'ordre alphabétique d'aucune langue : `äpple`
   passe après `zoo` en suédois et avant `yta` en français, et les deux sont justes.

**Exercices : `19_texte`.**
