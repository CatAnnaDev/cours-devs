# 14 — Les fichiers

Le C n'a pas un système d'entrées-sorties, il en a deux, empilés l'un sur l'autre, et les confondre
coûte cher. `notions/binaire.md` traite ce qu'on met *dans* un fichier ; ce chapitre-ci traite le
tuyau. Tous les chiffres ont été mesurés sur arm64, macOS 27, Apple clang 21, APFS.

## Les deux étages

En bas, les **descripteurs** POSIX : un `int`, indice dans la table des fichiers ouverts du
processus. Au-dessus, les **flux** de la norme C : un descripteur, un tampon, deux drapeaux d'état.

| | Flux `FILE *` | Descripteur `int` |
|---|---|---|
| en-tête, norme | `<stdio.h>`, norme C : partout | `<fcntl.h>`, `<unistd.h>`, POSIX |
| ouvrir, lire, écrire, fermer | `fopen`, `fread`, `fwrite`, `fclose` | `open`, `read`, `write`, `close` |
| formater, se déplacer, convertir | `fprintf`, `fseek`, `ftell` | rien, `lseek`, `fdopen`, `fileno` |
| tampon, échec | oui ; `NULL`, `EOF`, `ferror`, `errno` | aucun ; `-1` et `errno` |

Le tampon n'est pas un confort, c'est le facteur de performance. Quatre mébioctets, `-O2`, APFS :

| Écrire 4 Mio | Temps | | Lire 4 Mio | Temps |
|---|---|---|---|---|
| `fputc` octet par octet | 60 ms | | `fgetc` octet par octet | 50 ms |
| `write(fd, &c, 1)` | 5 500 ms | | `read(fd, &c, 1)` | 1 280 ms |
| `write(fd, bloc, 65536)` | 0,6 ms | | `fread` par 64 Kio | 0,3 ms |

Quatre-vingt-dix fois plus lent en écriture : le prix d'un appel système par octet, que le flux
ramène à un appel par tampon plein. Un descripteur nu n'est **pas** rapide, il est *brut*. Écris
donc `FILE *` par défaut, et descends aux descripteurs pour `fsync`, `mmap`, `O_NOFOLLOW`, un tube,
une socket — mais pas pour la création exclusive, que le mode `"wx"` de C11 couvre en C standard.
On passe d'un étage à l'autre, mais jamais **sur le même fichier à la fois** : un `fputs`, un
`write(fileno(f), ...)`, un `fputs` donnent un fichier, vérifié, qui commence par le texte du
`write`, le tampon n'étant parti qu'au `fclose`.

## Ouvrir

| Mode | Absent | Présent | Position | Lit | Écrit |
|---|---|---|---|---|---|
| `"r"` | échoue, `ENOENT` | intact | début | oui | non |
| `"w"` | créé | **vidé tout de suite** | début | non | oui |
| `"a"` | créé | intact | fin | non | oui, toujours à la fin |
| `"r+"` | échoue, `ENOENT` | intact | début | oui | oui |
| `"wx"` | créé | échoue, `EEXIST` | début | non | oui |

**`"w"` détruit à l'ouverture, pas à l'écriture.** Vérifié : un fichier de douze octets rouvert en
`"w"` puis fermé sans un seul `fputs` fait zéro octet ; le contenu part au `fopen`, avant toute
vérification. `"w+"` et `"a+"` sont `"w"` et `"a"` rendus lisibles, aux mêmes conditions — avec une
réserve sur `"a+"` : la norme ne fixe que la destination des écritures, jamais la position de
lecture initiale. Sur macOS elle démarre à la fin, sur glibc au début. Un `fseek(f, 0, SEEK_SET)`
explicite avant toute lecture, ou rien n'est portable.

**`"a"` n'est pas `"w"` avec un `fseek`.** La norme est explicite : en mode ajout, toute écriture
est reportée à la fin du fichier, *quels que soient les `fseek` intercalés*. Vérifié : `fopen("a")`
puis `fseek(f, 0, SEEK_SET)` et `fputs("XYZ")` donne `douze octetsXYZ`, là où `"r+"` donnerait
`XYZze octets`. C'est de là que vient la sûreté de `"a"` entre processus concurrents — mais elle
n'est pas donnée par la norme C, qui ignore les autres processus : elle vient de POSIX et de
`O_APPEND`, et elle vaut **par appel à `write`**, pas par ligne. Mesuré : trois processus écrivant
9 000 enregistrements de 200 octets par `fwrite` tamponné en corrompent 419 ; les mêmes en
`_IONBF`, aucun. Un enregistrement n'est sûr que s'il part d'un seul coup.

**Le `"b"` ne fait rien ici et tout ailleurs.** Vérifié sur macOS : un fichier contenant
`61 0d 0a 62 0d 0a` relu en `"r"` et en `"rb"` rend les mêmes six octets — sur les systèmes POSIX
les deux modes sont identiques, et la norme le permet. Sur **Windows**, le mode texte traduit
`\r\n` en `\n` en lecture et l'inverse en écriture, et arrête historiquement la lecture sur un
`0x1A`. Mets `b` partout où tu manipules du binaire : gratuit ici, indispensable là-bas.

**Teste le retour, toujours.** `fopen` rend `NULL` et pose `errno` : vérifié, chemin inexistant
`ENOENT`, `"r+"` sur un fichier absent `ENOENT` aussi puisque `r+` ne crée rien, `"wx"` sur un
fichier existant `EEXIST`.

## Lire

`fread(pointeur, taille, nombre, flux)` rend **le nombre d'éléments complets lus**, pas un nombre
d'octets. Sur un fichier de 550 octets, `fread(t, 100, 10, f)` rend `5`, et pas `5,5`. Vérifié :
les 50 octets du sixième élément partiel ont été consommés — `ftell` vaut 550 — mais la norme dit
leur valeur **indéterminée**. La position, elle, n'est indéterminée qu'après une *erreur* de
lecture : atteindre la fin de fichier n'en est pas une, et les 550 sont garantis. D'où l'usage :
**passe `1` comme taille** et le compte d'octets comme nombre, ce qui rend `550`.

`feof` **ne prédit pas** la fin, il la constate : le drapeau ne se lève qu'après une lecture qui a
buté dessus, d'où la boucle que tout le monde écrit et qui traite la dernière ligne deux fois.

```c
while (!feof(f)) {                          while (fgets(ligne, sizeof ligne, f) != NULL)
    fgets(ligne, sizeof ligne, f);              traiter(ligne);
    traiter(ligne);                         if (ferror(f)) { /* erreur, et non fin de fichier */ }
}
```

Vérifié sur un fichier de trois lignes `un`, `deux`, `trois` : la version de gauche fait **quatre**
tours et affiche `trois` deux fois, parce que le `fgets` du quatrième tour échoue sans rien écrire
dans `ligne`, qui contient encore la ligne précédente. Celle de droite fait trois tours. La règle :
**boucle sur le retour de la fonction de lecture**, et sers-toi de `feof` et `ferror` *après* la
boucle — `ferror` est le seul moyen de distinguer un fichier fini d'un disque en panne, les deux
sortant pareillement. Par blocs, la même forme donne
`while ((lus = fread(tampon, 1, sizeof tampon, f)) > 0) traiter(tampon, lus);` : on traite `lus`
octets, jamais `sizeof tampon`.

## Les lignes

`fgets(ligne, n, f)` lit au plus `n - 1` octets, **conserve le `\n`** s'il l'a rencontré, et
termine par `\0` ; pour couper ce saut de ligne même quand il manque,
`ligne[strcspn(ligne, "\n")] = '\0';`.

**La troncature est silencieuse.** Vérifié avec `char petit[8]` sur une ligne de 26 lettres : trois
appels rendent `abcdefg`, `hijklmn`, `opqrstu`, tous sans `\n`, puis `vwxyz\n`. Aucune erreur,
juste des morceaux traités comme des lignes. Le seul signe est **l'absence de `\n` en fin de
tampon**, qui veut dire ligne coupée *ou* dernière ligne sans saut final : seul `feof` tranche.

`getline` (POSIX.1-2008, absent de la norme C) règle les deux problèmes d'un coup : il alloue et
agrandit lui-même, et rend le nombre d'octets lus.

```c
char *ligne = NULL; size_t capacite = 0;    // getline alloue au premier tour, réutilise ensuite
ssize_t lus;
while ((lus = getline(&ligne, &capacite, f)) != -1) {
    if (lus > 0 && ligne[lus - 1] == '\n') ligne[--lus] = '\0';
    traiter(ligne, (size_t)lus);
}
free(ligne);                                // une seule fois, après la boucle
```

Vérifié : sur une ligne de 27 octets, `lus` vaut 27, le `\n` est conservé, `capacite` vaut 32 sur
macOS — la glibc choisit une autre première allocation. Le retour, un `ssize_t` valant `-1` en fin
de fichier comme en cas d'erreur, est la **longueur vraie** : sur `a\0b\n` il rend 4 là où `strlen`
rend 1. Et il est gratuit : 300 000 lignes en **5,8 ms contre 6,0 ms pour `fgets`**, tampon repris.

## Le tampon

`setvbuf(flux, tampon, mode, taille)` fixe la politique, et la norme exige qu'il soit appelé
**avant toute autre opération** sur le flux.

| Mode | Vidé quand | Pour quoi |
|---|---|---|
| `_IOFBF` | tampon plein, `fflush`, `fclose` | fichiers, tubes : le moins d'appels possible |
| `_IOLBF` | à chaque `\n`, ou tampon plein | terminal : on veut voir la ligne |
| `_IONBF` | jamais, un appel système par écriture | `stderr`, journaux de plantage |

La norme C ne dit qu'une chose : l'entrée et la sortie standard sont complètement tamponnées **si
et seulement s'il est établi qu'elles ne désignent pas un périphérique interactif**, `stderr` ne
l'étant jamais au départ. Relevé dans la structure `FILE` de macOS : `stdout` sur un terminal est
en `_IOLBF` avec 4 096 octets, sur un **tube** en `_IOFBF` avec **16 384 octets**, sur un fichier
ordinaire en `_IOFBF` avec 4 096, et `stderr` toujours en `_IONBF` sans tampon. Pour un fichier ou
un tube, la taille est le `st_blksize` du descripteur, et non `BUFSIZ`, qui vaut 1 024 ici.

Vérification du mode sans lire la structure : un programme qui fait `printf("ligne\n")` puis
`_exit(0)`, lequel ne vide rien, affiche `ligne` dans un terminal et **rien du tout** dans un tube.
D'où le grand classique : des `printf` mêlés à des `fprintf(stderr, ...)` paraissent en ordre à
l'écran et sortent mélangés dans `programme > journal.txt 2>&1`. Le gain, mesuré côté lecteur d'un
tube : 2 000 lignes de 10 octets sortent en **deux `write`**, de 16 384 et 3 616 octets.

**`fflush` n'est pas `fsync`.** `fflush(f)` recopie le tampon de la bibliothèque vers le noyau :
les octets deviennent visibles des autres processus, et restent perdus si la machine s'arrête.
`fsync(fd)`, POSIX, demande au noyau d'écrire sur le support ; `fflush(NULL)` vide tous les flux.
Mesuré par ligne écrite : `fflush` seul, **2 à 8 us** ; suivi de `fsync`, **20 à 50 us** ; suivi de
`fcntl(fd, F_FULLFSYNC, 0)`, **2 850 us**. Ce dernier est propre à macOS, dont le manuel dit que
`fsync` **ne garantit pas** la vidange du cache du disque ; sous Linux, `fsync` la demande.

## Se déplacer

`fseek(f, decalage, origine)` avec `SEEK_SET`, `SEEK_CUR` ou `SEEK_END`, `ftell(f)` pour la
position, `rewind(f)` pour revenir au début — et `rewind` efface au passage les drapeaux de fin de
fichier et d'erreur, vérifié, ce que `fseek(f, 0, SEEK_SET)` ne fait que pour la fin de fichier.
Encore faut-il **un fichier ordinaire** : vérifié sur un tube, `ftell` et `fseek` rendent `-1` avec
`errno = ESPIPE` (29), « Illegal seek », et un terminal ou une socket répondent pareil.

Il faut aussi **le mode binaire**. La norme dit que pour un flux **texte**, la valeur rendue par
`ftell` contient une information *non spécifiée*, utilisable seulement telle quelle dans un
`fseek(…, SEEK_SET)` : pas d'arithmétique dessus, pas de taille. Elle ajoute qu'un flux **binaire**
n'est pas tenu de gérer `SEEK_END` utilement. Sur POSIX les deux marchent et rendent un décalage en
octets ; le code portable n'en dépend pas.

Deux pièges de plus. `fseek` **au-delà de la fin** est légal : vérifié, `fseek(f, 100, SEEK_SET)`
suivi d'un `fputc('Z')` sur un fichier neuf donne 101 octets dont les cent premiers sont nuls, trou
que le système de fichiers n'alloue pas forcément. Et `ftell` rend un `long` : 8 octets ici, mais
**4 sur Windows**, ce qui y plafonne à 2 Gio — `ftello` et `fseeko`, POSIX, prennent un `off_t`.

## Écrire sans se faire couper

Écrire droit dans le fichier final, c'est accepter qu'une coupure au milieu laisse un fichier à
moitié écrit, donc perdu, l'ancien ayant été effacé par le `"w"`. Trois gestes règlent ça :

```c
int ecrire_atomique(const char *chemin, const void *donnees, size_t taille) {
    char modele[1024];
    int n = snprintf(modele, sizeof modele, "%s.tmpXXXXXX", chemin);
    if (n < 0 || (size_t)n >= sizeof modele) { errno = ENAMETOOLONG; return -1; }
    int fd = mkstemp(modele);                        // même dossier : même système de fichiers
    if (fd == -1) return -1;
    FILE *f = fdopen(fd, "wb");                      // le flux adopte le descripteur
    if (f == NULL) { int e = errno; close(fd); unlink(modele); errno = e; return -1; }
    if (fwrite(donnees, 1, taille, f) != taille || fflush(f) != 0 || fsync(fileno(f)) != 0) {
        int e = errno; fclose(f); unlink(modele); errno = e; return -1;
    }
    if (fclose(f) != 0) { int e = errno; unlink(modele); errno = e; return -1; }
    if (rename(modele, chemin) != 0) { int e = errno; unlink(modele); errno = e; return -1; }
    return 0;
}
```

**Ce que `rename` garantit.** POSIX : remplacer une cible existante est **atomique**, un lecteur
voyant l'ancien fichier ou le nouveau, jamais un mélange. Vérifié. La **norme C** est bien plus
faible : cible existante, comportement *défini par l'implémentation*. L'atomicité vient de POSIX.

**Ce qu'il ne garantit pas.** Que les octets soient sur le disque : sans le `fsync` du temporaire,
le `rename` peut précéder le contenu et publier un fichier vide. Pour que le *nom* tienne aussi, il
faut `fsync` sur le **dossier** ouvert en `O_RDONLY` — vérifié, il rend `0` sur macOS.

**Ce qui l'empêche.** `rename` ne traverse pas les systèmes de fichiers : vérifié entre un disque
RAM et le disque principal, `-1` et `errno = EXDEV` (18), « Cross-device link ». D'où le `mkstemp`
**dans le dossier de destination** : franchir la frontière exige une copie complète puis `unlink`.

## Les erreurs

Un appel pose `errno` **quand il échoue** et ne le remet pas à zéro quand il réussit : `errno` ne
se lit qu'après un retour d'échec, ou après l'avoir mis à `0` soi-même. `perror("contexte")` écrit
`contexte: message` sur `stderr`, `strerror(errno)` rend le message seul. Numéros relevés sur
macOS ; les plus bas valent aussi pour Linux, mais pas tous — `EAGAIN` y vaut 11 et non 35.

| Code | macOS | Message | Quand |
|---|---|---|---|
| `ENOENT` | 2 | No such file or directory | un composant du chemin n'existe pas |
| `EACCES` | 13 | Permission denied | droits insuffisants sur le fichier ou un dossier |
| `EEXIST` | 17 | File exists | `"wx"` ou `O_CREAT \| O_EXCL` sur une cible existante |
| `EXDEV` | 18 | Cross-device link | `rename` ou `link` entre systèmes de fichiers |
| `EISDIR` | 21 | Is a directory | lecture ou écriture sur un dossier |
| `ENOSPC` | 28 | No space left on device | disque plein |
| `ESPIPE` | 29 | Illegal seek | `fseek` sur un tube, une socket, un terminal |

`ENOSPC` mérite un mot, parce qu'il arrive **en retard**. Vérifié en remplissant un disque RAM de
10 Mo par tranches de 100 octets : les 90 685 premiers `fwrite` rendent 100 sans erreur, le 90 686e
rend `0` avec `errno = ENOSPC`, et surtout `fflush` **et** `fclose` rendent tous les deux `-1` avec
le même `errno`. Qui ignore le retour de `fclose` écrit des fichiers tronqués sans le savoir.

## Le code que tout le monde écrit mal

Lire un fichier entier en mémoire, dans la version qu'on trouve partout :

```c
char *lire_mal(const char *chemin) {
    FILE *f = fopen(chemin, "r");
    fseek(f, 0, SEEK_END);
    long taille = ftell(f);
    rewind(f);
    char *tampon = malloc((size_t)taille);
    fread(tampon, 1, (size_t)taille, f);
    fclose(f);
    return tampon;
}
```

Cinq fautes, toutes vérifiées en la lançant sous ASan et UBSan :

1. **`fopen` non testé.** Sur un fichier absent, `f` vaut `NULL` et le `fseek` déréférence `NULL`.
2. **`ftell` cru sur parole.** Sur un tube, il rend `-1`, donc `malloc((size_t)-1)` : ASan répond
   `requested allocation size 0xffffffffffffffff` ; sans ASan, `malloc` rend `NULL` et `fread` rend
   0 en posant `EFAULT`, si bien que l'appelant récupère un `NULL` silencieux plutôt qu'un
   plantage. Pire sur un **dossier** : `fopen(".", "r")` réussit sur macOS et `ftell` annonce la
   taille de l'entrée de dossier — 64 octets à vide sur APFS, 32 de plus par entrée —, `fread`
   échoue avec `EISDIR`, et l'appelant reçoit ces octets non initialisés.
3. **Le retour de `fread` ignoré.** La taille annoncée n'est pas la taille lue : le fichier a pu
   rétrécir entre le `ftell` et le `fread`, ou la lecture échouer à mi-chemin.
4. **Pas d'octet nul final.** `malloc(taille)` et non `taille + 1` : ce n'est pas une chaîne, et un
   `strlen` dessus donne `heap-buffer-overflow ... READ of size 9` sur une région de 8 octets.
5. **Le mode `"r"` et les chemins d'erreur.** `"r"` au lieu de `"rb"` fausse le compte sur Windows,
   et toute sortie anticipée ajoutée plus tard fuira le `FILE *` et le tampon.

La version correcte refuse ce qui n'est pas un fichier ordinaire, traite la taille comme une
estimation et non comme une vérité, et libère tout sur chaque chemin d'échec :

```c
char *lire_fichier(const char *chemin, size_t *taille_sortie) {
    FILE *f = fopen(chemin, "rb");
    if (f == NULL) return NULL;
    struct stat infos;                                        // POSIX : <sys/stat.h>
    if (fstat(fileno(f), &infos) != 0 || !S_ISREG(infos.st_mode)) {
        fclose(f); errno = EINVAL; return NULL;               // ni dossier, ni tube, ni terminal
    }
    size_t capacite = (size_t)infos.st_size + 1;              // + 1 pour l'octet nul final
    char *tampon = malloc(capacite);
    if (tampon == NULL) { fclose(f); return NULL; }
    size_t taille = 0;
    for (;;) {
        taille += fread(tampon + taille, 1, capacite - taille, f);
        if (taille < capacite) break;                         // moins que demandé : fin ou erreur
        char *agrandi = realloc(tampon, capacite * 2);        // le fichier a grandi entre-temps
        if (agrandi == NULL) { free(tampon); fclose(f); errno = ENOMEM; return NULL; }
        tampon = agrandi; capacite *= 2;
    }
    if (ferror(f)) { free(tampon); fclose(f); errno = EIO; return NULL; }
    fclose(f); tampon[taille] = '\0';
    if (taille_sortie != NULL) *taille_sortie = taille;
    return tampon;
}
```

Vérifiée sous ASan et UBSan sur huit entrées : fichier ordinaire, vide, de 3,3 Mo, à octet nul
(5 octets rendus, `strlen` à 2, d'où la taille en sortie), un dossier, `/dev/stdin` sur un tube, un
fichier absent, `/dev/zero` — les quatre derniers rendant `NULL`, sans fuite ni débordement.

## À retenir

1. Un `FILE *` est un descripteur plus un tampon, et 90 fois plus rapide qu'un `write` par octet.
2. `"w"` vide le fichier dès le `fopen` ; `"a"` écrit toujours à la fin, malgré les `fseek`.
3. `fread` rend un nombre d'éléments : passe `1` comme taille et lis le compte d'octets.
4. `feof` constate la fin, ne la prédit pas : boucle sur le retour de lecture, `ferror` après.
5. `fgets` garde le `\n` et tronque en silence ; `getline` rend la longueur vraie, au même prix.
6. `fflush` va au noyau, `fsync` va au disque, et sur macOS seul `F_FULLFSYNC` vide son cache.
7. Écriture atomique : temporaire dans le **même dossier**, `fsync`, `rename` — et teste `fclose`.

**Exercices : `14_fichiers`.**
