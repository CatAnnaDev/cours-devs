#include "verif.h"
#include <sys/wait.h>
#include <unistd.h>

const int PAS_FINI = 0;

#define CHEMIN_ECHO "/bin/echo"
#define MESSAGE "sortie redirigee"
#define SORTIE_ATTENDUE MESSAGE "\n"

enum {
    CODE_REDIRECTION_RATEE = 91,
    CODE_EXEC_RATE = 90,
    SORTIE_MAX = 128
};

typedef struct {
    int code_de_sortie;
    size_t octets_lus;
    int fin_de_fichier;
} Capture;

static int brancher_la_sortie_standard_sur(int descripteur) {
    return dup2(descripteur, STDOUT_FILENO);
}

static Capture lancer_echo_et_capturer(char *sortie, size_t capacite) {
    Capture capture = {-1, 0, 0};
    int tube[2];
    sortie[0] = '\0';
    if (pipe(tube) != 0) {
        return capture;
    }
    fflush(stdout);
    pid_t enfant = fork();
    if (enfant < 0) {
        close(tube[0]);
        close(tube[1]);
        return capture;
    }
    if (enfant == 0) {
        if (brancher_la_sortie_standard_sur(tube[1]) < 0) {
            _exit(CODE_REDIRECTION_RATEE);
        }
        close(tube[1]);
        close(tube[0]);
        execl(CHEMIN_ECHO, "echo", MESSAGE, (char *)NULL);
        _exit(CODE_EXEC_RATE);
    }
    close(tube[1]);
    while (capture.octets_lus + 1 < capacite) {
        ssize_t lus = read(tube[0], sortie + capture.octets_lus,
                           capacite - 1 - capture.octets_lus);
        if (lus < 0) {
            break;
        }
        if (lus == 0) {
            capture.fin_de_fichier = 1;
            break;
        }
        capture.octets_lus += (size_t)lus;
    }
    sortie[capture.octets_lus] = '\0';
    close(tube[0]);
    int statut = 0;
    if (waitpid(enfant, &statut, 0) == enfant && WIFEXITED(statut)) {
        capture.code_de_sortie = WEXITSTATUS(statut);
    }
    return capture;
}

int main(void) {
    char sortie[SORTIE_MAX];
    Capture capture = lancer_echo_et_capturer(sortie, sizeof sortie);

    VERIFIE_ENTIER(capture.code_de_sortie, 0, "l'enfant a bien execute echo");
    VERIFIE_ENTIER(capture.octets_lus, sizeof SORTIE_ATTENDUE - 1,
                   "le tube a recu tous les octets ecrits par echo");
    VERIFIE_TEXTE(sortie, SORTIE_ATTENDUE, "la sortie standard de l'enfant arrive dans le tube");
    VERIFIE(capture.fin_de_fichier, "la fin de fichier arrive des que l'enfant a termine");
    return BILAN();
}
