# 15 — Les processus

`notions/terminal.md` décrit les cinq prises d'un programme du point de vue de celui qui s'en
sert ; ici on est du côté de celui qui les crée. La norme C ne connaît qu'une façon de lancer un
programme, `system`, au résultat défini par l'implémentation : tout le reste est **POSIX**. Chaque
chiffre vient d'un programme lancé sur la machine de référence : arm64, macOS 27, Apple clang 21.

## Ce qu'est un processus

**Un espace d'adressage** — code, pile, tas, globales — dont les adresses n'ont aucun sens
ailleurs. **Une table de descripteurs**, indexée par de petits entiers, où `0`, `1` et `2` sont
`stdin`, `stdout` et `stderr` ; un processus neuf lancé depuis un shell en a trois ouverts, vérifié
par `fcntl(i, F_GETFD)` de 0 à 255. Chaque case pointe vers une **description de fichier ouverte**,
objet du noyau portant la position de lecture et les drapeaux : deux cases peuvent viser la même
description, et c'est tout le mécanisme de `dup2` et de l'héritage. **Une identité** enfin : `pid`
(`sizeof(pid_t)` vaut `4` ici), `ppid`, répertoire courant, `umask`, environnement. Rien n'est
illimité — `sysctl` annonce `kern.maxproc: 4000` et `kern.maxprocperuid: 2666` — et
`<sys/syscall.h>` donne les vrais appels système : `fork` est le 2, `wait4` le 7, `kill` le 37,
`pipe` le 42, `execve` le 59, `dup2` le 90, `posix_spawn` le 244.

## `fork` : la fonction qui rend deux fois

```c
pid_t r = fork();
if (r < 0)  { perror("fork"); return 1; }   // echec : aucun enfant n'existe
if (r == 0) { /* je suis l'enfant */ }      // 0, toujours, dans l'enfant
else        { /* je suis le parent, r est le pid de l'enfant */ }
```

Vérifié : le parent affiche `pid=24763`, l'enfant `fork a rendu 0, pid=24764 ppid=24763`. L'enfant
ne recommence pas à `main` : il reprend **à la ligne qui suit le `fork`**, même pile. En cas
d'échec, `errno` vaut le plus souvent `EAGAIN` (35 sur macOS, 11 sous Linux).

### Ce qui est copié, ce qui est partagé

L'enfant reçoit un nouveau `pid` ; répertoire courant, `umask`, environnement et dispositions de
signaux sont copiés. La mémoire l'est **logiquement**, en copie à l'écriture, le noyau ne
dupliquant une page qu'à la première écriture : vérifié, un enfant qui met une globale à `20` et un
entier du tas à `200` laisse le parent voir `10` et `100`. Les **descripteurs**, eux, visent la
**même description ouverte**, donc la même position : sur `ABCDEFGHIJ` l'enfant lit `A`, le parent
lit `B`.

Sont copiés **en l'état**, verrous compris : tampons `stdio`, mutex, table `atexit`. Sont remis à
zéro : temps CPU, `alarm` en cours, signaux en attente. Et des threads, seul l'appelant survit. Ce
dernier point est vérifié : un parent qui a brûlé `0,307911 s` de CPU et posé `alarm(30)` donne un
enfant à `0,000039 s` et `alarm` restante `0` ; et à quatre threads, l'enfant hérite de toute la
mémoire mais **un seul thread y tourne**, un mutex que les autres tenaient y restant verrouillé
pour toujours. Après un `fork` dans un programme multithread, la seule issue raisonnable est
`exec`.

### Le piège du tampon non vide

`printf` n'écrit pas, il remplit un tampon — vidé à chaque saut de ligne vers un terminal, mais
seulement quand il est plein ou à la fin ailleurs. Si `fork` tombe entre les deux, il part en
double.

```c
printf("ligne unique\n");     // reste dans le tampon si stdout n'est pas un terminal
if (fork() == 0) exit(0);     // exit vide le tampon de l'enfant, qui contient deja la ligne
wait(NULL); return 0;         // puis le parent vide le sien en sortant
```

```
$ ./double > f.txt ; cat f.txt      # ligne unique / ligne unique   <- deux fois, idem dans un tube
$ ./double  (sur un vrai terminal)  # ligne unique                  <- une seule
```

Le bug ne se voit pas en développement et sort sous redirection ; même mécanisme pour `atexit`,
vérifié, un gestionnaire posé avant le `fork` s'exécutant deux fois. Deux parades, ensemble :
**`fflush(NULL)` juste avant `fork`**, `NULL` voulant dire tous les flux, et **`_exit` et jamais
`exit` dans l'enfant**. Vérifié : avec `_exit`, la ligne ne sort qu'une fois.

## Attendre ses enfants

```c
int st;  pid_t f = wait(&st);         // n'importe quel enfant, bloque ; idem waitpid(-1, &st, 0)
pid_t g = waitpid(r, &st, 0);         // celui-la precisement, bloque
pid_t h = waitpid(r, &st, WNOHANG);   // rend 0 tout de suite s'il vit encore
```

`WNOHANG` transforme l'attente en sondage : vérifié sur un enfant qui dort 200 ms, une boucle
espaçant ses appels d'un `usleep(1000)` a fait **160 tours à `0`** avant de recevoir le pid et le
code `7`. Sans cette pause, la même boucle en fait deux millions : sonder sans attendre, c'est
brûler un cœur pour rien. Quand il ne reste aucun enfant, `wait`
rend `-1` et `errno` vaut `ECHILD`. Quant à `st`, c'est un entier **encodé**, jamais à lire tel
quel : un enfant qui rend `3` donne `st = 768`, soit `3 << 8`, un enfant tué par `SIGKILL` donne
`st = 9` (vérifié). `WIFEXITED` dit qu'il s'est terminé normalement et `WEXITSTATUS` donne alors
son code, **les huit bits de poids faible seulement** ; `WIFSIGNALED` dit qu'il a été tué et
`WTERMSIG` donne le signal. Les huit bits sont vérifiés — `exit(300)` donne `44`, `exit(-1)` donne
`255`, d'où la plage 0-255 — et `int code = WIFEXITED(st) ? WEXITSTATUS(st) : 128 + WTERMSIG(st);`
reconstruit le `128 + n` du shell, vérifié sur un enfant tué par `SIGPIPE` : `13`, donc `141`.

### Le zombie et l'orphelin

Un processus terminé **ne disparaît pas** : le noyau garde son pid et son statut jusqu'à ce que le
parent les réclame. C'est le **zombie**, que `ps` montre entre la mort de l'enfant et le
`waitpid` :

```
$ ps -o pid,stat,comm -p 25193
  PID STAT COMM
25193 Z    <defunct>          # apres waitpid : plus aucune ligne
```

Il ne consomme ni mémoire ni CPU, mais occupe une **entrée de la table des processus**, qui est
finie : le programme de test qui `fork` en boucle sans jamais attendre a échoué au **2194ᵉ enfant**
sur `EAGAIN`, soit `kern.maxprocperuid` moins les processus déjà en cours. L'**orphelin** est le
cas inverse : adopté, vérifié, son `ppid` passant de `25238` à `1` — `launchd` ici, `init` ou
`systemd` sous Linux — il ne fuit pas, contrairement au zombie.

```c
signal(SIGCHLD, SIG_IGN);                     // POSIX : plus aucun zombie, mais wait rend ECHILD
while (waitpid(-1, NULL, WNOHANG) > 0) { }    // sondage, dans la boucle principale ou sur SIGCHLD
```

La première est vérifiée — cinq enfants ignorés, aucun zombie, `wait` rendant `-1` — mais brutale :
elle interdit de relever les codes.

## `exec` : remplacer le programme sans changer de processus

Six fonctions, un seul appel système, `execve`. Les lettres disent la forme des arguments : `l`
pour des arguments **l**istés terminés par `(char *)NULL`, `v` pour un **v**ecteur terminé par
`NULL`, `p` pour un nom cherché dans le `PATH`, `e` pour un **e**nvironnement fourni.

```c
execl ("/bin/ls", "ls", "-l", (char *)NULL);          // chemin, arguments listes
execlp("ls",      "ls", "-l", (char *)NULL);          // cherche dans PATH
execv ("/bin/ls", (char *[]){ "ls", "-l", NULL });    // vecteur ; execvp = vecteur + PATH
execle("./p", "p", (char *)NULL, (char *[]){ "SEULE=une", NULL });   // environnement impose
```

Le premier argument est le chemin ou le nom, le second ce que le programme lira dans `argv[0]`, et
rien n'oblige les deux à coïncider. Vérifié : la ligne `execle` donne un `environ` d'**une seule**
variable. `execvpe` existe sous glibc, pas sur macOS.

| Survit à l'`exec` | Est remplacé ou perdu |
|---|---|
| `pid`, `ppid`, `umask`, répertoire courant | code, données, pile, tas — tout l'espace d'adressage |
| descripteurs ouverts sauf `FD_CLOEXEC` ; signaux **ignorés** ; l'environnement, sauf `execle` et `execve` qui l'imposent | tampons `stdio` non vidés, table `atexit` ; signaux **traités**, qui retombent à `SIG_DFL` ; les autres threads |

Vérifié par un programme qui en `exec` un second : `pid` et `ppid` identiques de part et d'autre,
`MA_VAR=transmise` lue après, `write` sur le descripteur 5 hérité qui rend `7`, `write` sur le 6
marqué `FD_CLOEXEC` qui rend `-1`, `SIGINT` retombé à `SIG_DFL`, `SIGTERM` resté `SIG_IGN`. Et un
`printf` non vidé suivi d'un `exec` réussi disparaît sans trace : `fflush` avant `exec` aussi.

Il n'y a pas non plus de code de retour à tester en cas de succès : il n'y a plus de programme pour
le lire. **Toute ligne écrite après un `exec` est un traitement d'erreur**, d'où le `perror(...)`
puis `_exit(127)` qui l'accompagne. Les deux échecs mesurés : `ENOENT`,
`No such file or directory` ; et sans le bit `x`, `EACCES`, `Permission denied` — les `127` et
`126` du shell.

## Le tube

```c
int t[2];
if (pipe(t) < 0) { perror("pipe"); return 1; }   // t[0] : LECTURE   t[1] : ECRITURE
```

Vérifié : sur un processus qui n'a que ses trois descripteurs standards, `pipe` rend `3` et `4`.
Entre les deux, un tampon circulaire du noyau, sans nom ni fichier, dont la capacité mesurée en
écriture non bloquante est de **65 536 octets** exactement, quelle que soit la taille des
écritures ; au-delà, `write` bloque. POSIX garantit qu'une écriture d'au plus `PIPE_BUF` octets est
**atomique** : `PIPE_BUF` vaut `512` sur macOS, le plancher POSIX, `4096` sous Linux.

Après un `fork`, le tube a **quatre** extrémités : les deux du parent et les deux copies de
l'enfant.

> `read` sur un tube ne rend `0`, c'est-à-dire la fin de fichier, que lorsque **toutes** les copies
> de l'extrémité d'écriture sont fermées, dans tous les processus.

Il suffit d'un descripteur d'écriture oublié, chez celui qui lit compris, pour bloquer le lecteur.
Vérifié, avec un enfant qui écrit `bonjour` puis ferme et sort :

```
parent qui ferme son extremite d'ecriture : lu 8 octets, puis read a rendu 0 : fin de fichier
parent qui l'oublie                       : lu 8 octets, puis read bloque pour toujours
```

Le second cas n'a pu être mesuré qu'avec une `alarm(1)` posée par `sigaction`, `sa_flags` à `0` :
avec `signal`, macOS installe le gestionnaire en sémantique BSD, le bit `SA_RESTART` étant mis —
`sa_flags` relu vaut `0x2` — et l'appel bloqué **reprend** au lieu de rendre `EINTR`. La règle,
sans exception : **chacun ferme aussitôt les extrémités dont il ne se sert pas**.

## `dup2` : rediriger un descripteur

`dup2(source, cible)` ferme `cible` si elle est ouverte, puis en fait une seconde case pointant sur
la **même description ouverte** que `source` ; les deux numéros deviennent interchangeables.

```c
int f = open("sortie.txt", O_WRONLY | O_CREAT | O_TRUNC, 0644);
dup2(f, STDOUT_FILENO);   // stdout ecrit desormais dans le fichier : c'est le "> f" du shell
close(f);                 // le numero d'origine ne sert plus a rien : on le rend
```

Vérifié : `open` rend toujours **le plus petit descripteur libre**, d'où `3` ici ; `dup2` sur un
descripteur invalide rend `-1` avec `EBADF`, et `dup2(f, f)` ne ferme rien. **L'ordre compte :
`dup2` d'abord, `close` ensuite** — fermer `t[1]` avant de l'avoir dupliqué sur `stdout` ferme le
tube.

### `ls | grep tube`, en entier

Un tube, deux `fork`, un `dup2` par côté, un `exec` par enfant, tout fermé chez le parent.

```c
int t[2]; if (pipe(t) < 0) { perror("pipe"); return 1; }
pid_t gauche = fork();                                        // le producteur : ls
if (gauche == 0) {
    close(t[0]); dup2(t[1], STDOUT_FILENO); close(t[1]);      // sa sortie part dans le tube
    execlp("ls", "ls", (char *)NULL); perror("ls"); _exit(127);   // perror : exec a echoue
}
pid_t droite = fork();                                        // le consommateur : grep
if (droite == 0) {
    close(t[1]); dup2(t[0], STDIN_FILENO); close(t[0]);       // son entree vient du tube
    execlp("grep", "grep", "tube", (char *)NULL); perror("grep"); _exit(127);
}
close(t[0]); close(t[1]);                                     // INDISPENSABLE : sans cette ligne,
int st;                                                       // grep n'a jamais sa fin de fichier
waitpid(gauche, &st, 0); waitpid(droite, &st, 0);
return WIFEXITED(st) ? WEXITSTATUS(st) : 128 + WTERMSIG(st);  // le code du DERNIER maillon
```

Lancé, il affiche les fichiers dont le nom contient `tube` et rend le code de `grep`. Sans les
`close` du parent, variante lancée aussi : `ls` finit, `grep` bloque, tué après quatre secondes.

## Les signaux

Un signal est une **notification asynchrone** d'un entier, sans charge utile ; deux occurrences en
attente du même signal fusionnent.

| Signal | Numéro macOS | Par défaut | Interceptable |
|---|---|---|---|
| `SIGINT` | 2 | termine | oui — le Ctrl-C, envoyé à tout le groupe au premier plan |
| `SIGTERM` | 15 | termine | oui — la demande polie d'arrêt, celle de `kill` sans option |
| `SIGKILL` | 9 | termine | **non**, et `SIGSTOP` non plus |
| `SIGPIPE` | 13 | termine | oui |
| `SIGCHLD` | 20 | ignoré | oui — un enfant vient de changer d'état ; `17` sous Linux |

Les numéros diffèrent parce que macOS suit BSD. Vérifié aussi : `sigaction(SIGKILL, ...)` et
`sigaction(SIGSTOP, ...)` rendent `-1` avec `EINVAL` — la garantie qu'un processus reste tuable.

**`SIGPIPE` est le seul dont on se prenne la mort par surprise.** Écrire dans un tube dont plus
personne ne tient l'extrémité de lecture tue le processus par défaut : vérifié, un programme qui
ouvre un tube, ferme `t[0]` et écrit un octet meurt sans rien afficher, le shell rendant `141`,
soit `128 + 13`. C'est cette action par défaut qui arrête proprement `yes | head -2` : le shell
rend `141` pour le premier maillon du tube.

Avec `signal(SIGPIPE, SIG_IGN)`, le même `write` rend `-1` avec `EPIPE`, `Broken pipe`, et le
programme continue. C'est ce que font les programmes qui doivent survivre à un lecteur parti — un
serveur, `curl` — et qui préfèrent traiter l'erreur plutôt que mourir.

Ce qu'un gestionnaire a le droit de faire est **très court**. La norme C n'y autorise, hors signal
levé par `abort` ou `raise`, que `abort`, `_Exit`, `quick_exit` et `signal` sur le même signal, et
ne laisse toucher aux objets statiques que par affectation à un `volatile sig_atomic_t` ou à un
atomique sans verrou ; POSIX élargit à une liste **async-signal-safe** dont `write`, `read`,
`close`, `open`, `_exit`, `kill`, `waitpid` et `sigaction`. `printf`, `malloc` et `free` n'y sont
pas : ils peuvent interbloquer sur le verrou interne de l'allocateur. Le gestionnaire pose un
drapeau, et c'est tout.

```c
static volatile sig_atomic_t demande_arret = 0;   // sizeof : 4 ici, SIG_ATOMIC_MAX 2147483647
static void sur_sigint(int s) { (void)s; demande_arret = 1; }   // une affectation, rien de plus

struct sigaction sa = { .sa_handler = sur_sigint, .sa_flags = 0 };  // pas SA_RESTART : read rend EINTR
sigemptyset(&sa.sa_mask);        // le masque : les signaux bloques pendant l'execution du gestionnaire
sigaction(SIGINT, &sa, NULL);     // sigaction et pas signal, dont la semantique varie d'un systeme a l'autre
```

## Ce qui coûte

Moyennes sur au moins 500 itérations, `-O2`, trois exécutions concordantes à moins de 5 %.

| Opération | Coût mesuré |
|---|---|
| appel d'une fonction non inlinée | **0,8 ns** |
| appel système `getppid` | **84 ns** |
| `pthread_create` + `pthread_join` | **12 µs** |
| `fork` + `_exit` + `waitpid` | **440 µs**, dont **270 µs** pour le `fork` seul |
| `posix_spawn("/usr/bin/true")` + `waitpid` | **730 µs** |
| `fork` + `execl("/usr/bin/true")` + `waitpid` | **1,18 ms** |
| `system("true")` | **2,2 ms** |

Un `fork` coûte **environ 340 000 appels de fonction** : ce n'est pas une opération de boucle
interne. Un thread coûte **trente-six fois moins**, n'ayant ni espace d'adressage ni table à
construire, et `system` est le pire, ajoutant un `/bin/sh` complet. Le coût du `fork` est en
revanche **indépendant de la taille du tas** : 274 µs avec 0 Mio alloué, 270 µs avec 2048 Mio
touchés page par page. Sous Linux, non mesuré ici, il est réputé nettement moins cher.

`posix_spawn` existe parce que `fork` + `exec` est un gaspillage doublé d'un piège : il construit
un espace d'adressage complet pour le jeter à la ligne suivante, et entre les deux un programme
multithread n'a droit qu'à des fonctions async-signal-safe. Il fait les deux d'un coup, avec des
actions déclarées d'avance (`posix_spawn_file_actions_adddup2`). Sur macOS c'est un **vrai appel
système**, le 244, et ça se voit : **730 µs contre 1,18 ms**, soit 38 % de moins.

## À retenir

1. `fork` rend deux fois : `0` dans l'enfant, le pid dans le parent, `-1` en cas d'échec.
2. Mémoire copiée à l'écriture, mais descripteurs **partagés** : même position de lecture.
3. `fflush(NULL)` avant `fork`, `_exit` dans l'enfant : sinon la ligne sort deux fois, mais
   seulement sous redirection.
4. Le statut de `wait` s'ouvre avec `WIFEXITED` / `WEXITSTATUS`, qui ne garde que huit bits.
5. Qui n'attend pas ses enfants remplit la table des processus : échec au 2194ᵉ ici.
6. `exec` ne rend la main que s'il a échoué ; tout ce qui suit est un traitement d'erreur.
7. Un tube ne donne sa fin de fichier que lorsque **toutes** les extrémités d'écriture sont
   fermées, celles du parent comprises : c'est le blocage numéro un.

**Exercices : `15_processus`.**
