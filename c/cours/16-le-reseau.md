# 16 — Le réseau

La norme C ignore le réseau : tout ce qui suit est **POSIX**, hérité de l'interface socket de BSD.
Chaque chiffre vient d'un programme compilé et lancé ici, sur arm64, macOS 27 et Apple clang 21.

## Une socket est un descripteur de plus

`socket()` rend un `int`, pris dans la même table que `stdin` : sur un programme neuf,
`socket(AF_INET, SOCK_STREAM, 0)` rend **3**, juste après les trois descripteurs standards.

Vérifié : `fstat` sur cette socket donne `S_ISSOCK` vrai, `S_ISREG` faux et `st_size` à **0**, et
`lseek` rend `-1` avec `errno = ESPIPE` (**29**). Cela autorise tout ce qu'on sait des fichiers :
`read`, `write`, `close`, `select`, `poll`, `fcntl`, `dup2`, la redirection, l'héritage par `fork`.
Ce que le descripteur **ne dit pas** compte davantage : ni position, ni taille, ni nom, ni
frontière entre les appels, et surtout rien sur l'état du pair — un descripteur valide peut
désigner une connexion morte depuis dix minutes. Relu ici, `SOL_SOCKET` vaut **65535** sur macOS
contre `1` sous Linux : ne jamais l'écrire en dur.

## TCP contre UDP

**TCP** garantit que les octets arrivent **dans l'ordre d'émission**, **sans duplication ni
altération**, et que sinon l'appel **échoue** au lieu de mentir. Il ne garantit **pas** que les
données soient arrivées à l'application distante — un `write` qui rend 17 signifie « 17 octets
copiés dans le tampon du noyau » — ni qu'une connexion silencieuse soit vivante. Et surtout il **ne
conserve aucune frontière de message** : trois `write` de 3 octets peuvent ressortir en un seul
`read` de 9, en trois `read` de 3, ou en n'importe quel découpage entre les deux. Mesuré ici sur
60 exécutions : **28 fois `3`, une fois `6`, 31 fois `9`**. Le regroupement n'est pas une règle,
c'est une possibilité — et c'est exactement ce qui rend le cadrage obligatoire.

**UDP** ne garantit ni arrivée, ni ordre, ni unicité, mais la **frontière est exacte** : vérifié,
trois envois de 3, 3 et 10 octets ressortent en trois `recvfrom` de 3, 3 et 10. Le revers est la
troncature **silencieuse** — lire ce datagramme de 10 dans un tampon de 4 rend **4**, les 6 autres
sont **jetés** — et le refus net des gros : `sendto` de 70 000 octets rend `-1` avec `EMSGSIZE`.

## L'ordre des octets

Un entier de plusieurs octets n'a pas la même disposition partout : vérifié, `0x01020304` occupe en
mémoire les octets `04 03 02 01`. Les protocoles ont donc figé un **ordre réseau**, gros-boutiste ;
`htons` et `ntohs` traitent 16 bits (un port), `htonl` et `ntohl` 32 bits (une adresse IPv4).

Le piège est que **arm64 et x86-64 sont tous deux petit-boutistes** : aucune des deux plateformes
courantes n'est neutre, la conversion est donc toujours visible, et l'oublier donne un port faux et
non un port correct. Vérifié, `htons(80)` rend **20480** et `htons(8080)` rend **36895** — en
mémoire `8080` s'écrit `90 1f` côté hôte et `1f 90` converti — tandis que `127.0.0.1` passé à
`inet_pton` donne `s_addr = 0x0100007f`, dont le `ntohl` vaut `0x7f000001`. Ce sont des
conversions d'entiers ordinaires, utilisables partout : le noyau ne les **exige** que dans
`sockaddr_in`, mais tu t'en serviras aussi pour tes propres champs — le préfixe de longueur de la
section suivante en est un.

## La séquence serveur, la séquence client

Cinq appels côté serveur, deux côté client. Les deux programmes ci-dessous sont complets — hors
`#include` et `static void mort(const char *m) { perror(m); exit(1); }` — et ont été compilés et
lancés tels quels.

```c
int ecoute = socket(AF_INET, SOCK_STREAM, 0);          // 1. un fd, rien de plus
int un = 1;                                            // 2. rendre le port reutilisable
if (setsockopt(ecoute, SOL_SOCKET, SO_REUSEADDR, &un, sizeof un) < 0) mort("setsockopt");
struct sockaddr_in adr = {0};                          // 3. une adresse LOCALE
adr.sin_family = AF_INET;
adr.sin_addr.s_addr = htonl(INADDR_LOOPBACK);          // 127.0.0.1 en ordre reseau
adr.sin_port = htons(0);                               // 0 = le noyau choisit le port
if (bind(ecoute, (struct sockaddr *)&adr, sizeof adr) < 0) mort("bind");
if (listen(ecoute, 128) < 0) mort("listen");           // 4. ouvrir la file d'attente
socklen_t l = sizeof adr;                              // 5. relire le port obtenu
if (getsockname(ecoute, (struct sockaddr *)&adr, &l) < 0) mort("getsockname");
printf("ecoute sur 127.0.0.1:%u\n", ntohs(adr.sin_port));
fflush(stdout);

int c = accept(ecoute, NULL, NULL);                    // 6. accept rend un NOUVEAU fd
if (c < 0) mort("accept");
char t[4096];  ssize_t n;
while ((n = read(c, t, sizeof t)) > 0)                 // 0 = le pair a ferme son ecriture
    for (ssize_t f = 0; f < n; ) {                     // write peut en placer moins
        ssize_t w = write(c, t + f, (size_t)(n - f));
        if (w < 0) { if (errno == EINTR) continue; mort("write"); }
        f += w;
    }
if (n < 0) mort("read");
close(c); close(ecoute);
```

`accept` surprend : il ne transforme pas la socket d'écoute, il en **crée une nouvelle**. `ecoute`
reste disponible pour le client suivant et `c` porte la connexion ; ses deux derniers arguments,
ici `NULL`, donnent au besoin l'adresse du pair. Le second argument de `listen` est un **conseil**
pour la file des connexions pas encore acceptées — POSIX dit « hint », et macOS écrête
silencieusement à `kern.ipc.somaxconn`, qui vaut **128** ici.

```c
int c = socket(AF_INET, SOCK_STREAM, 0);               // 1. le meme appel que le serveur
struct sockaddr_in adr = {0};                          // 2. l'adresse DISTANTE cette fois
adr.sin_family = AF_INET;
adr.sin_port = htons((unsigned short)atoi(argv[1]));
if (inet_pton(AF_INET, "127.0.0.1", &adr.sin_addr) != 1) mort("inet_pton");
if (connect(c, (struct sockaddr *)&adr, sizeof adr) < 0) mort("connect");  // bind implicite

size_t taille = strlen(argv[2]);
for (size_t f = 0; f < taille; ) {
    ssize_t w = write(c, argv[2] + f, taille - f);
    if (w < 0) { if (errno == EINTR) continue; mort("write"); }
    f += (size_t)w;
}
shutdown(c, SHUT_WR);                                  // FIN : "je n'ecrirai plus, mais je lis"

char t[4096];  size_t lu = 0;  ssize_t n;
while ((n = read(c, t + lu, sizeof t - lu)) > 0) lu += (size_t)n;
printf("echo : %zu octets \"%.*s\"\n", lu, (int)lu, t);
close(c);
```

```
$ ./srv &                          ecoute sur 127.0.0.1:64468           <- serveur
$ ./cli 64468 "bonjour le reseau"  echo : 17 octets "bonjour le reseau" <- client
```

Le client ne fait **pas** de `bind` : le noyau lui donne un port éphémère, pris dans
`net.inet.ip.portrange`, de **49152** à **65535** ici. `shutdown(c, SHUT_WR)` envoie le FIN sans
fermer le descripteur : le `read` du serveur rend `0`, mais le client peut encore lire la réponse.

## Le port 0 et `getsockname`

`adr.sin_port = htons(0)` demande au noyau de choisir un port libre, `getsockname` relit celui qui
a été attribué. C'est **la** bonne pratique pour un test. Un port en dur est **occupé tôt ou
tard**, et `bind` rend `-1` avec `errno = EADDRINUSE` (**48** ici, `98` sous Linux) : le test
échoue pour une raison sans rapport avec ce qu'il teste. Il **interdit le parallélisme**. Et il
reste **coincé en TIME_WAIT** une trentaine de secondes. Avec le port 0 le programme apprend son
port et l'annonce au client, d'où le `fflush(stdout)` qui suit.

## La vérité sur `read` et `write`

`read` et `write` **rendent ce qu'ils ont pu**, pas ce qu'on leur a demandé ; sur une socket c'est
le cas normal. Vérifié en lecture : un `read(fd, buf, 10)` alors que 5 octets seulement sont
arrivés rend **5**, immédiatement, et un `read` sur un flux dont le pair a fermé rend **0**, ce qui
signifie fin de flux, jamais erreur. Vérifié en écriture, sur une socket non bloquante dont le pair
ne lit rien : un `write` de 8 Mio rend **1 365 672**, puis **131 072**, puis `-1` avec
`errno = EAGAIN` (**35** = `EWOULDBLOCK` ici), le total variant entre **0,3 et 1,5 Mio**.

```c
static int ecrire_tout(int fd, const void *p, size_t n)
{
    const char *o = p;
    for (size_t fait = 0; fait < n; ) {
        ssize_t w = write(fd, o + fait, n - fait);
        if (w < 0) { if (errno == EINTR) continue; return -1; }
        fait += (size_t)w;
    }
    return 0;
}
static int lire_tout(int fd, void *p, size_t n)     // 0 = ok, 1 = fin prematuree, -1 = erreur
{
    char *o = p;
    for (size_t fait = 0; fait < n; ) {
        ssize_t r = read(fd, o + fait, n - fait);
        if (r < 0) { if (errno == EINTR) continue; return -1; }
        if (r == 0) return 1;
        fait += (size_t)r;
    }
    return 0;
}
```

`EINTR` n'est pas une erreur mais un signal arrivé pendant l'attente : on recommence. La
distinction entre `0` et `-1` doit remonter à l'appelant. Et `EAGAIN` dit de retourner attendre,
pas de tourner à vide.

## Le cadrage des messages

TCP ne conservant aucune frontière, c'est au protocole applicatif de les remettre.

| Cadrage | Coût par message | Données binaires | Taille | Lecture |
|---|---|---|---|---|
| **Préfixe de longueur** | 2, 4 ou 8 octets fixes | oui, telles quelles | bornée par le préfixe | deux `lire_tout` |
| **Délimiteur** | 1 octet, plus l'échappement | non sans échappement | libre | recherche + tampon glissant |
| **Taille fixe** | 0 | oui | figée à la compilation | un `lire_tout` |

Le **préfixe de longueur** gagne presque toujours : le récepteur sait avant de lire combien
d'octets attendre, donc il alloue juste ce qu'il faut et refuse d'emblée un message déraisonnable.

```c
uint32_t n = htonl((uint32_t)taille);      // lecture : lire_tout(fd, &n, 4) puis ntohl(n)
ecrire_tout(fd, &n, 4);  ecrire_tout(fd, message, taille);
```

Vérifié sur trois messages de 7, 2 et 18 octets émis en rafale et lus après 200 ms : le récepteur
ressort exactement `"bonjour"`, `"le"`, `"reseau tout entier"`, alors qu'ils étaient arrivés
agglomérés. Le **délimiteur** — le `\n` de SMTP ou de Redis — oblige à l'échapper dans le contenu
et à faire tourner un tampon sans borne connue ; la **taille fixe** n'est que le préfixe rendu
implicite. Toujours borner la longueur annoncée **avant** d'allouer, sans quoi un pair hostile peut
réclamer 4 Gio.

## Attendre proprement

Un serveur à un seul fil doit savoir lequel de ses descripteurs est prêt sans bloquer sur un autre.

| | `select` | `poll` | `kqueue` | `epoll` |
|---|---|---|---|---|
| Disponibilité | POSIX, partout | POSIX, partout | BSD et macOS | Linux |
| Coût par appel | O(n) descripteurs | O(n) descripteurs | O(prêts) | O(prêts) |
| Limite de descripteur | `FD_SETSIZE` = **1024** | aucune | aucune | aucune |
| Enregistrement | à chaque appel | à chaque appel | **une fois** | **une fois** |
| Entrée modifiée | oui, à reconstruire | non (`revents` à part) | non | non |
| Octets prêts connus | non | non | **oui** (`data`) | non |
| Fin de flux distincte | non | `POLLHUP`, mais pas exclusif de `POLLIN` | `EV_EOF` | `EPOLLRDHUP` |
| Fichiers, signaux, minuteurs | non | non | **oui** | via des `*fd` |

Mesuré ici : `sizeof(fd_set)` vaut **128** octets, `sizeof(struct pollfd)` **8**,
`sizeof(struct kevent)` **32**, et trois attentes sans donnée pour un délai demandé de 120 ms sont
revenues en **125,0**, **121,1** et **121,0** ms. `select` reste lisible mais plafonne à
`FD_SETSIZE = 1024` : au-delà, écrire dans un `fd_set` est un débordement. `poll` lève la limite
mais rescanne tout à chaque tour ; `kqueue` et `epoll` enregistrent une fois et ne rendent que les
prêts, ce qui compte à mille connexions. `kqueue` suit aussi minuteurs, signaux et processus, et
son champ `data` porte le **nombre d'octets lisibles** — vérifié, `5` après un envoi de 5 — tandis
que `EV_EOF` (**0x8000**) peut arriver **avec des données en attente**, observé à `data = 16`.

```c
non_bloquant(ecoute);                              // sinon accept peut bloquer apres le reveil
int kq = kqueue();                                 // un fd de plus, qui garde la liste
struct kevent ch, evs[64];
EV_SET(&ch, ecoute, EVFILT_READ, EV_ADD, 0, 0, NULL);
if (kevent(kq, &ch, 1, NULL, 0, NULL) < 0) mort("kevent");    // enregistrement : UNE fois
char t[4096];
for (;;) {
    int n = kevent(kq, NULL, 0, evs, 64, NULL);    // attente : rend SEULEMENT les prets
    if (n < 0) { if (errno == EINTR) continue; mort("kevent"); }
    for (int i = 0; i < n; i++) {
        int fd = (int)evs[i].ident;
        if (fd == ecoute) {
            int c;
            while ((c = accept(ecoute, NULL, NULL)) >= 0) {   // vider TOUTE la file
                non_bloquant(c);
                EV_SET(&ch, c, EVFILT_READ, EV_ADD, 0, 0, NULL);
                kevent(kq, &ch, 1, NULL, 0, NULL);
            }
            continue;
        }
        ssize_t r;
        while ((r = read(fd, t, sizeof t)) > 0)    // lire jusqu'a EAGAIN, pas une seule fois
            if (ecrire_tout(fd, t, (size_t)r) < 0) break;    // en vrai : mettre en attente
        if (r == 0 || (evs[i].flags & EV_EOF)) close(fd);   // EV_EOF peut venir AVEC des donnees
    }
}
```

Lancé avec trois clients simultanés, ce serveur les a servis tous les trois sur un seul fil. Deux
points comptent. La socket d'écoute doit être **non bloquante**, sinon un `accept` après réveil
peut bloquer. Et le drainage jusqu'à `EAGAIN` dépend du mode : enregistré avec `EV_ADD` seul,
kqueue travaille par **niveau** et re-signale tant qu'il reste des octets — vérifié, une lecture
partielle produit bien un nouvel événement. C'est avec `EV_CLEAR`, et avec `EPOLLET` sous Linux,
que le drainage complet devient obligatoire. Ici `accept` **transmet** `O_NONBLOCK` à la socket
acceptée, alors que POSIX ne l'exige pas et que Linux documente l'inverse : le rendre explicite est
portable.

## Ce qui casse en vrai

### `SIGPIPE`

Écrire sur une connexion dont le pair est parti envoie `SIGPIPE` (**13**), dont l'action par défaut
est de **tuer le processus** : vérifié, le programme meurt avec le code de sortie **141**, sans le
moindre message, et sa sortie non vidée disparaît avec lui. Trois parades, toutes vérifiées :
`signal(SIGPIPE, SIG_IGN)` au démarrage, la plus simple ; l'option `SO_NOSIGPIPE` par socket,
spécifique BSD et macOS, valeur **4130** ; et le drapeau `MSG_NOSIGNAL` de `send`, qui **existe**
sur macOS 27 et vaut **524288**. Dans les trois cas l'écriture rend `-1`, `errno = EPIPE` (**32**).

### Le pair qui disparaît

Un pair correct envoie un FIN et le `read` local rend `0` — et `poll` le signale, `POLLIN` et
`POLLHUP` levés ensemble, ce qui est le piège du tableau plus haut : fermer sur `POLLHUP` sans
avoir lu perd les octets encore en attente.

Le cas vraiment sournois est le pair qui coupe **brutalement**, par `RST`. Seule une écriture le
découvre, et il en faut deux : vérifié, le **premier** `write` rend **4**, succès complet, parce
qu'il ne fait que remplir un tampon local ; c'est le **second**, 100 ms plus tard, qui rend `-1`
avec `EPIPE`, une fois le `RST` revenu. Un pair qui se contente de ne plus répondre, lui, ne
produit rien avant la fin des retransmissions — des minutes. Un pair qui coupe brutalement — forcé
ici par `SO_LINGER` à zéro — fait rendre au `read` local `-1` avec `errno = ECONNRESET` (**54**) et
non `0`. La seule détection fiable reste applicative.

### `TIME_WAIT` et `SO_REUSEADDR`

Le côté qui ferme **le premier** garde la connexion en `TIME_WAIT`, le temps que d'éventuels
paquets retardataires expirent. Sur cette machine `net.inet.tcp.msl` vaut **15000** ms, et
`TIME_WAIT` a été observé au `netstat` pendant **32 à 35 s** sur deux mesures, soit les deux MSL
attendus ; pendant tout ce temps un `bind` sur ce port rend `-1` avec `errno = EADDRINUSE`.
`SO_REUSEADDR`, posé **avant** le `bind`, lève exactement cette interdiction : vérifié, le rebind
échoue sans l'option et réussit avec. Son absence explique qu'un serveur refuse de redémarrer
aussitôt après un arrêt. À ne pas confondre avec `SO_REUSEPORT` (**512**), qui fait écouter
plusieurs sockets à la fois.

### Nagle et `TCP_NODELAY`

L'algorithme de Nagle retient un petit envoi tant qu'un envoi précédent n'a pas été acquitté, pour
éviter d'inonder le réseau de segments d'un octet ; il est **actif par défaut**, `TCP_NODELAY` relu
sur une socket neuve valant **0**. Il ne coûte rien à un flux continu, mais pénalise le motif
*écriture, écriture, lecture*. Mesuré sur 2000 allers-retours en boucle locale, quatre exécutions :
**28,5 à 34,5 µs** avec Nagle contre **9,3 à 13,9 µs** avec `TCP_NODELAY` ; sur un vrai réseau,
l'acquittement différé porte l'écart à des dizaines de millisecondes. La bonne réponse est presque
toujours d'**assembler l'en-tête et le corps en un seul `write`**, ce que `writev` fait sans
recopie.

## À retenir

1. Une socket est un descripteur ordinaire, mais sans position, sans taille, sans état du pair.
2. TCP garantit l'ordre des octets, jamais les frontières de tes messages : cadre-les toi-même.
3. `htons` est obligatoire : arm64 et x86-64 sont petit-boutistes, et `htons(8080)` rend `36895`.
4. Serveur : `socket`, `setsockopt`, `bind`, `listen`, `accept` qui rend un **nouveau** fd ;
   client : `socket` puis `connect`, le port local étant implicite.
5. `read` et `write` rendent ce qu'ils ont pu : boucler, reprendre sur `EINTR`, `0` n'est pas `-1`.
6. Cadrer soi-même par préfixe de longueur, et **borner** la longueur annoncée avant d'allouer.
7. Ignorer `SIGPIPE`, poser `SO_REUSEADDR` avant `bind` : écrire vers un mort réussit une fois.

**Exercices : `16_reseau`.**
