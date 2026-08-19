#include "verif.h"
#include <sys/wait.h>
#include <unistd.h>

const int PAS_FINI = 0;

#define CHEMIN_ECHO "/bin/echo"
#define CHEMIN_ABSENT "/bin/echo_que_personne_n_installe"
#define NOMBRE_DE(tableau) (sizeof (tableau) / sizeof (tableau)[0])

enum {
    CODE_EXEC_RATE = 90,
    ARGUMENTS_MAX = 8,
    SORTIE_MAX = 128
};

static const char *MOTS_LONGS[] = {"echo", "exec", "remplace", "le", "programme"};
static const char *MOTS_COURTS[] = {"echo", "fini"};

typedef struct {
    int code_de_sortie;
    size_t octets_lus;
} Capture;

static void preparer_arguments(char *arguments[], size_t capacite, const char *mots[],
                               size_t nombre_de_mots) {
    size_t i = 0;
    while (i < nombre_de_mots && i + 1 < capacite) {
        arguments[i] = (char *)mots[i];
        i++;
    }
    arguments[i] = NULL;
}

static Capture lancer_et_capturer(const char *chemin, char *const arguments[], char *sortie,
                                  size_t capacite) {
    Capture capture = {-1, 0};
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
        dup2(tube[1], STDOUT_FILENO);
        close(tube[1]);
        close(tube[0]);
        execv(chemin, arguments);
        _exit(CODE_EXEC_RATE);
    }
    close(tube[1]);
    while (capture.octets_lus + 1 < capacite) {
        ssize_t lus = read(tube[0], sortie + capture.octets_lus,
                           capacite - 1 - capture.octets_lus);
        if (lus <= 0) {
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
    char *arguments[ARGUMENTS_MAX] = {NULL};
    char sortie[SORTIE_MAX];

    preparer_arguments(arguments, ARGUMENTS_MAX, MOTS_LONGS, NOMBRE_DE(MOTS_LONGS));
    VERIFIE_TEXTE(arguments[0], "echo", "argv[0] porte le nom du programme, pas un argument");
    VERIFIE(arguments[NOMBRE_DE(MOTS_LONGS)] == NULL,
            "la liste longue se termine par un pointeur nul");

    Capture longue = lancer_et_capturer(CHEMIN_ECHO, arguments, sortie, sizeof sortie);
    VERIFIE_TEXTE(sortie, "exec remplace le programme\n", "le parent recoit la sortie de echo");
    VERIFIE_ENTIER(longue.code_de_sortie, 0, "echo s'est termine avec le code zero");

    preparer_arguments(arguments, ARGUMENTS_MAX, MOTS_COURTS, NOMBRE_DE(MOTS_COURTS));
    VERIFIE(arguments[NOMBRE_DE(MOTS_COURTS)] == NULL,
            "la liste courte se termine elle aussi par un pointeur nul");

    Capture courte = lancer_et_capturer(CHEMIN_ECHO, arguments, sortie, sizeof sortie);
    VERIFIE_TEXTE(sortie, "fini\n",
                  "la seconde commande n'herite pas de la premiere");
    VERIFIE_ENTIER(courte.code_de_sortie, 0, "la seconde commande sort avec le code zero");

    Capture ratee = lancer_et_capturer(CHEMIN_ABSENT, arguments, sortie, sizeof sortie);
    VERIFIE_ENTIER(ratee.octets_lus, 0, "un exec qui rate n'ecrit rien dans le tube");
    VERIFIE_ENTIER(ratee.code_de_sortie, CODE_EXEC_RATE,
                   "le code place apres exec ne tourne que si exec a rate");
    return BILAN();
}
