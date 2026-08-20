# 18 — Les formats binaires

Tous les chiffres viennent de programmes compilés et lancés sur la machine de référence : arm64,
macOS, Apple clang 21, `-O0` avec les deux sanitizers, sauf les temps et l'assembleur, en `-O2`.
Les tailles d'autres plateformes viennent de compilations croisées ; IEEE 754 est détaillé dans
`notions/virgule-flottante.md`.

## Pourquoi un format binaire

Un format texte écrit `12345` sur cinq octets, et sa relecture demande une machine à états :
espaces, signe, accumulation, débordement. Un format binaire l'écrit sur quatre octets fixes et le
relit avec trois décalages. Mesuré sur un million d'entiers 32 bits, analysés depuis un tampon en
mémoire :

| | taille | analyse |
|---|---|---|
| texte, un `%d\n` par valeur | 8 337 485 octets | 12,0 ms (`strtol`) |
| binaire, 4 octets gros-boutistes | 4 000 000 octets | 0,23 ms (décalages) |

Deux fois plus petit, **cinquante-deux fois plus rapide à analyser** — et l'écart de vitesse ne
vient pas de la taille : un décodeur binaire n'a pas de grammaire. Troisième gain, souvent le plus
important : **l'absence d'ambiguïté**. Vérifié, `1.0/3.0` écrit en `%g` donne `0.333333`, que
`strtod` relit en une valeur **différente** de l'originale ; il faut `%.17g`, dix-neuf caractères
pour huit octets. Le prix, lui, se paie tous les jours : **un fichier binaire est illisible**, ni
`grep` ni `diff` utile. Choisis-le quand le volume, la vitesse ou la fidélité l'exigent, pas par
réflexe.

## Les quatre décisions d'un format

Un format binaire, ce n'est pas « écrire les octets » : c'est quatre décisions, à prendre
**explicitement**, à écrire dans un document, et à ne plus changer.

**1. Le boutisme.** Dans quel ordre les octets d'un entier partent-ils dans le fichier ?
Gros-boutiste ou petit-boutiste, peu importe, mais choisis-en un et écris-le.

**2. Les tailles fixes.** Chaque champ a une largeur décidée par le format, pas par la plateforme :
`uint32_t`, jamais `int` ni `long`. Vérifié par compilation croisée, `sizeof(long)` vaut 8 sur
macOS et Linux 64 bits, **4** sur Windows x64 et i386. Un format qui dit « un `long` » ne dit rien.

**3. L'alignement.** Un format ne l'hérite pas, il le décide. Le plus simple est de n'en avoir
aucun, les champs se suivant sans trou ; pour mapper le fichier et le lire en place, il faut au
contraire du remplissage **explicite**, écrit dans le format, à zéro.

**4. Les versions.** Un numéro dans l'en-tête dès la version 1, avant d'en avoir besoin. Le
rajouter après coup est impossible : un lecteur qui ne l'attend pas ne saura jamais le lire.

## Le boutisme

Un entier de plusieurs octets occupe plusieurs adresses, et l'ordre n'est pas universel.
**Gros-boutiste** : le poids fort à la plus petite adresse, comme on écrit un nombre à la main.
**Petit-boutiste** : le poids faible d'abord. Vérifié, `0x01020304` occupe ici `04 03 02 01` —
x86-64 est petit-boutiste, et arm64 l'est ici — l'architecture, elle, sait faire les deux, c'est le
système qui fige le choix. Les protocoles d'Internet ont arrêté le gros-boutiste à la fin des
années 1970, et la littérature de l'époque est franche sur la raison : aucun des deux ordres n'est
meilleur, seul l'accord compte. D'où *ordre réseau* comme synonyme de gros-boutiste
(chapitre 16), repris par convention par les formats de fichiers. Voici la **seule** façon correcte
de sérialiser, dans les deux sens.

```c
static void ecrire_be32(uint8_t *p, uint32_t v) {        // poids fort en premier
    p[0] = (uint8_t)(v >> 24);  p[1] = (uint8_t)(v >> 16);
    p[2] = (uint8_t)(v >> 8);   p[3] = (uint8_t)v;
}
static uint32_t lire_be32(const uint8_t *p) {
    return ((uint32_t)p[0] << 24) | ((uint32_t)p[1] << 16)
         | ((uint32_t)p[2] << 8)  | (uint32_t)p[3];
}
static void ecrire_le32(uint8_t *p, uint32_t v) {        // poids faible en premier
    p[0] = (uint8_t)v;          p[1] = (uint8_t)(v >> 8);
    p[2] = (uint8_t)(v >> 16);  p[3] = (uint8_t)(v >> 24);
}
static uint32_t lire_le32(const uint8_t *p) {
    return (uint32_t)p[0]         | ((uint32_t)p[1] << 8)
         | ((uint32_t)p[2] << 16) | ((uint32_t)p[3] << 24);
}
```

Ce code **ne teste jamais le boutisme de la machine**, et c'est le point entier : un décalage porte
sur une valeur, pas sur une disposition mémoire, donc `v >> 24` donne le poids fort partout. Tout
code qui commence par « détecter si on est petit-boutiste » a deux chemins, dont un seul sera testé
chez toi. Et il ne coûte rien : vérifié en `-O2` sur arm64, `lire_be32` compile en `ldr` puis
`rev`, `ecrire_be32` en `rev` puis `str`, `lire_le32` en un seul `ldr` — comme un `memcpy`. Le cast
`(uint32_t)` n'est pas décoratif : sans lui `p[0]` est promu en `int` signé, et `p[0] << 24` avec
`p[0] == 0x80` déborde. Vérifié, UBSan abandonne —
`left shift of 128 by 24 places cannot be represented in type 'int'`. Indéfini franc, dans un
décodeur qui passe tous les tests tant qu'aucun octet de tête ne dépasse 127. Et pourquoi pas
`htonl` ? POSIX n'en définit que quatre — `htons`, `ntohs`, `htonl`, `ntohl` — 16 et 32 bits,
gros-boutiste seulement. `htonll` **existe sur macOS**, dans `<sys/_endian.h>`, mais ni dans POSIX
ni dans la glibc ; et `<endian.h>` est absent du SDK macOS, comme `<stdbit.h>` ajouté par C23.

## Pourquoi on n'écrit jamais une struct telle quelle

```c
typedef struct {
    uint8_t type; uint32_t identifiant; uint16_t compte; double valeur;
} Enr;                                  // 15 octets utiles
```

Vérifié : `sizeof(Enr)` vaut **24** sur arm64 macOS, `offsetof` à 0, 4, 8 et 16 — neuf octets de
remplissage, 60 % de charge en trop. Par compilation croisée vers i386 Linux, la **même**
déclaration donne `sizeof` **20** et `offsetof(Enr, valeur)` **12**, `double` s'y alignant sur 4 :
un `fwrite(&e, sizeof e, 1, f)` produit deux fichiers pour un seul code source. Le mécanisme est au
chapitre 08. Vérifié aussi, en remplissant les quatre champs après avoir sali la pile :

```
01 de de de 44 33 22 11 66 55 de de de de de de 00 00 00 00 00 00 f0 3f
   ^^^^^^^^                   ^^^^^^^^^^^^^^^^^
```

Les `de` sont le contenu antérieur de la pile : le remplissage prend des valeurs **non
spécifiées**, rien n'oblige le compilateur à l'initialiser. Écrire la structure telle quelle, c'est
publier neuf octets de mémoire arbitraire — une fuite qui a valu des CVE à des noyaux et des
protocoles. Le reste des dégâts est sur la même ligne : `44 33 22 11` pour `0x11223344`, le
boutisme de la machine ; l'ordre des champs figé par la déclaration ; les tailles figées par l'ABI.
D'où la règle : **une struct est une forme en mémoire, un format une forme sur disque, et les deux
ne se ressemblent jamais par accident.** On sérialise champ par champ, et la struct peut alors être
ordonnée pour la mémoire — vérifié, `valeur` d'abord et `type` en dernier fait tomber `sizeof` à
**16** — sans toucher au fichier.

## Lire des octets sans faute

Une fois le fichier chargé dans un `uint8_t *`, la tentation est de caster :

```c
const uint32_t *p = (const uint32_t *)(tampon + 1);   // faute
uint32_t v = *p;
```

Deux fautes en une. **L'alignement** : rien ne garantit que `tampon + 1` soit un multiple de 4, et
**convertir** une adresse mal alignée en `uint32_t *` est déjà indéfini, avant même de déréférencer
— vérifié, UBSan le dit, `load of misaligned address ... which requires 4 byte alignment`. Et
**l'aliasing strict** : lire un objet à travers un type incompatible autorise le compilateur à
supposer que les deux accès ne se recouvrent pas, et à réordonner (chapitre 11). D'où `memcpy` :

```c
uint32_t v;
memcpy(&v, tampon + 1, sizeof v);   // toujours legal, toujours defini
```

Mais `memcpy` règle l'alignement, **pas le boutisme** : vérifié, les mêmes octets lus par `memcpy`
donnent ici `04030201` là où le fichier contient `01 02 03 04`. `memcpy` lit un motif de bits, les
décalages un nombre.

## Les flottants

Un `double` n'a pas de représentation garantie par la norme C. En pratique, à peu près toutes les
machines qui t'intéressent implémentent **IEEE 754** binaire : `float` sur 32 bits, `double` sur
64, mêmes champs signe/exposant/mantisse. La macro censée l'annoncer est un piège, et elle a même
changé de nom : `__STDC_IEC_559__` en C17, rendue obsolescente par C23 au profit de
`__STDC_IEC_60559_BFP__`. Vérifié, Apple clang 21 ne définit **ni l'une ni l'autre**, en `-std=c17`
comme en `-std=c23`. Ce
qui n'est **pas** portable : `long double`. Vérifié, il fait 8 octets et 53 bits de mantisse sur
arm64 macOS — identique à `double` — et 16 octets et 64 bits sur x86-64 macOS. Aucun format ne doit
en contenir, ni compter sur la charge utile des NaN. On passe par un entier de même taille avec
`memcpy`, jamais par une union ni un cast :

```c
static void ecrire_double(uint8_t *p, double d) {
    uint64_t bits;
    memcpy(&bits, &d, sizeof bits);     // 8 octets vers 8 octets, aucun calcul
    ecrire_be64(p, bits);               // puis un entier ordinaire
}
static double lire_double(const uint8_t *p) {
    double d;
    uint64_t bits = lire_be64(p);
    memcpy(&d, &bits, sizeof d);
    return d;
}
```

Vérifié sur onze valeurs, l'aller-retour est **bit à bit identique** : `1.0` donne
`3ff0000000000000`, le plus petit dénormalisé `0000000000000001`, l'infini `7ff0000000000000`. Et
il couvre les deux cas que `==` ne sait justement pas traiter : le NaN silencieux
`7ff8000000000000`, qui n'est égal à rien, pas même à lui-même ; et `-0.0`, qui donne
`8000000000000000` alors que `-0.0 == +0.0` est vrai. Seule l'égalité bit à bit les distingue. On
ne compare rien, on recopie.

## L'en-tête

Un fichier binaire commence par un en-tête à taille fixe, dont le contenu permet de refuser un
fichier **avant** de l'interpréter. **Le nombre magique** : les premiers octets, qui identifient le
format. Vérifié sur un PNG du système, la signature fait huit octets, `89 50 4E 47 0D 0A 1A 0A` :
`\x89PNG`, puis des octets choisis pour détecter les transferts qui abîment les fins de ligne. Ce
n'est pas de la sécurité mais de l'hygiène — sans ce contrôle, un fichier fourni par erreur est lu
comme le bon. **La version** : deux octets, on la lit, on la compare, on refuse ce qu'on ne sait
pas lire. **Les longueurs** : nombre d'éléments, tailles, décalages, les champs les plus dangereux.
L'ordre compte, et la validation doit pouvoir échouer sur un fichier trop court :

```c
if (!prendre(&f, 4, &magique) || memcmp(magique, "CDVB", 4) != 0) return -1;
if (!lire_u16(&f, &version)  || version  != VERSION) return -2;
if (!lire_u16(&f, &drapeaux) || drapeaux != 0)       return -3;   // reserve : doit valoir 0
if (!lire_u32(&f, &n))                               return -4;
if (n > (taille - ENTETE) / ENR_MIN)                 return -5;   // cf. section suivante
```

Les drapeaux réservés valent zéro et **on le vérifie** : c'est ce qui te laissera leur donner un
sens plus tard sans qu'un vieux fichier arbitraire passe pour un nouveau. Et à la fin, un contrôle
que presque personne ne fait : `f.position == taille`. Un fichier qui a des octets en trop est un
fichier qu'on n'a pas compris.

## Toute longueur lue est hostile

La faute qui produit le plus de failles réelles tient en une phrase : **une longueur lue dans un
fichier est une valeur choisie par celui qui a écrit le fichier.** Elle vaut `0xFFFFFFFF` aussi
facilement que 5, et le contrôle naïf est faux, silencieusement :

```c
if (decalage + longueur > taille) return ERREUR;   // FAUX
```

Vérifié avec `taille = 64`, `decalage = 8`, `longueur = 0xFFFFFFFC` en `uint32_t` : la somme vaut
**4**, le test passe, et le `memcpy` qui suit part sur quatre milliards d'octets. Le débordement
d'un non signé n'est **pas** indéfini — il enroule, la norme le définit — donc UBSan ne dit rien ;
seul ASan attrape la conséquence, quand il y a un ASan. La forme correcte n'additionne jamais :
elle soustrait ce qui est déjà borné.

```c
if (decalage > taille) return ERREUR;              // sinon taille - decalage enroule
if (longueur > taille - decalage) return ERREUR;   // aucune somme, aucun debordement
```

Le premier test n'est pas décoratif : vérifié, avec `decalage = 100` et `taille = 64`,
`taille - decalage` vaut **4 294 967 260**, et sans lui le second accepte tout. Le motif complet
tient dans un curseur qui maintient l'invariant `position <= taille` et un seul point de passage :

```c
typedef struct { const uint8_t *base; size_t taille; size_t position; } Flux;

static int prendre(Flux *f, size_t n, const uint8_t **sortie) {
    if (n > f->taille - f->position) return 0;   // sur, car position <= taille
    *sortie = f->base + f->position;
    f->position += n;                            // preserve l'invariant
    return 1;
}
```

Toute lecture passe par là, aucune longueur n'échappe au contrôle, et deux bornes s'ajoutent.
**Borner par le format** : une longueur de nom a un maximum documenté, `if (longueur > NOM_MAX)`,
ce qui refuse tôt et laisse un tampon fixe suffire. **Borner par ce qui reste du fichier** : le
rôle de `prendre`, et ce qui interdit l'allocation démesurée. Un compteur de `n` éléments d'au
moins `ENR_MIN` octets se valide **avant** le `calloc`, par
`if (n > (taille - ENTETE) / ENR_MIN)` ; sans lui, 55 octets annonçant `0xFFFFFFFF` éléments
demandent plusieurs téraoctets. La division remplace la multiplication, qui aurait enroulé — même
règle pour `malloc(n * taille_element)` : vérifié, `0x2000000000000001 * 16` vaut **16**. Le
lecteur complet bâti sur ce motif a été lancé : sur un fichier valide de 55 octets, **les 55
troncatures possibles sont rejetées**, et quatre fichiers hostiles — nombre d'éléments
`0xFFFFFFFF`, longueur de nom `0xFFFF`, magique cassé, version inconnue — échouent chacun sur son
propre contrôle.

## Faire évoluer un format

Ce qui décide si un format survit à sa première version, c'est ce que fait un **ancien** lecteur
devant un **nouveau** fichier. **La version est un contrat** : un majeur qui change veut dire
« refuse si tu ne connais pas », un mineur « lis ce que tu connais, ignore le reste ». Dis lequel —
un lecteur qui refuse tout ne peut plus évoluer, un lecteur qui accepte tout ne garantit plus rien.

**Ajouter un champ à la fin d'un enregistrement casse tout** : ils se suivent, et un ancien lecteur
qui en attend `n` de taille fixe se désynchronise dès le premier. Cela ne marche que si chaque
enregistrement porte sa longueur et que le lecteur saute jusqu'à elle.

**Les champs optionnels se font par blocs étiquetés** — PNG, RIFF (WAV, AVI), MP4. Chaque bloc
porte une étiquette de quatre octets et sa longueur ; le lecteur traite ce qu'il connaît et
saute le reste. Vérifié sur 26 octets, un bloc `TETE` connu et un `XTRA` inventé : l'inconnu est
sauté, et le parcours refuse le fichier si la longueur de `XTRA` devient `0xFFFFFFFF`.

```c
while (position != taille) {
    if (taille - position < 8) return -1;        // etiquette + longueur
    etiquette = base + position;
    longueur  = lire_be32(base + position + 4);
    position += 8;
    if (longueur > taille - position) return -2; // la meme borne que partout
    if (memcmp(etiquette, "TETE", 4) == 0) traiter(base + position, longueur);
    position += longueur;                        // inconnu : on saute, on ne devine pas
}
```

**Ce qui casse la compatibilité :** changer la taille d'un champ, son boutisme ou son sens,
réordonner des champs, en retirer un, donner un sens à des bits réservés qu'aucune version
antérieure ne vérifiait. **Ce qui ne la casse pas :** ajouter un bloc étiqueté, une valeur à une
énumération que le lecteur sait rejeter, un sens à un drapeau réservé que toutes les versions
publiées vérifiaient à zéro. Enfin : ouvre toujours en mode binaire, `"rb"` et `"wb"` — sur macOS
et Linux le `b` ne fait rien, sur Windows un `0x0A` devient deux octets à l'écriture et un `0x1A`
termine la lecture (chapitre 14).

## À retenir

1. Le binaire achète taille, vitesse et fidélité — vérifié, deux fois plus petit et cinquante-deux
   fois plus rapide à analyser qu'un texte équivalent — et le paie en illisibilité.
2. Quatre décisions, prises explicitement et écrites : boutisme, tailles fixes, alignement,
   versions. Une seule laissée implicite suffit à rendre le format non portable.
3. On sérialise par décalages, jamais en testant le boutisme de la machine. Le cast en `uint32_t`
   avant le décalage est obligatoire : `p[0] << 24` avec `p[0] >= 128` est indéfini.
4. Une struct ne s'écrit jamais telle quelle : la même déclaration fait 24 octets sur arm64 macOS
   et 20 sur i386, et son remplissage indéterminé fuit le contenu de la pile dans le fichier.
5. `memcpy` pour lire un motif de bits, jamais un cast de pointeur : ni contrainte d'alignement, ni
   problème d'aliasing. Mais il ne corrige pas le boutisme.
6. Les flottants passent par un entier de même taille via `memcpy` ; `float` et `double` sont
   portables en pratique, `long double` ne l'est pas — 8 octets sur arm64 macOS, 16 sur x86-64.
7. Toute longueur lue est hostile : on soustrait, on n'additionne jamais, on borne par le format
   **et** par ce qui reste du fichier, et on valide tout compteur avant la première allocation.

**Exercices : `18_binaire`.**
