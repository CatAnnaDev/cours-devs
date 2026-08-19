#include "verif.h"
#include <sys/wait.h>
#include <unistd.h>

const int PAS_FINI = 0;

typedef enum {
    ROLE_ECHEC,
    ROLE_ENFANT,
    ROLE_PARENT
} Role;

enum {
    BIT_RETOUR_NUL = 1,
    BIT_PID_PROPRE = 2,
    BIT_PARENT_RETROUVE = 4,
    BILAN_ATTENDU = 7
};

static Role role_apres_fork(pid_t retour) {
    if (retour < 0) {
        return ROLE_ECHEC;
    }
    if (retour == 0) {
        return ROLE_ENFANT;
    }
    return ROLE_PARENT;
}

int main(void) {
    VERIFIE(role_apres_fork(-1) == ROLE_ECHEC, "un retour negatif signale l'echec de fork");

    pid_t pid_du_parent = getpid();
    fflush(stdout);
    pid_t retour = fork();
    if (retour < 0) {
        VERIFIE(0, "fork a echoue, impossible de continuer");
        return BILAN();
    }
    if (retour == 0) {
        int bilan = 0;
        if (role_apres_fork(retour) == ROLE_ENFANT) {
            bilan |= BIT_RETOUR_NUL;
        }
        if (getpid() != pid_du_parent) {
            bilan |= BIT_PID_PROPRE;
        }
        if (getppid() == pid_du_parent) {
            bilan |= BIT_PARENT_RETROUVE;
        }
        _exit(bilan);
    }

    VERIFIE(role_apres_fork(retour) == ROLE_PARENT,
            "dans le parent, fork rend le pid de l'enfant");
    VERIFIE(retour > 0, "ce pid est strictement positif");
    VERIFIE_ENTIER(getpid(), pid_du_parent, "le parent a garde son propre pid");

    int statut = 0;
    VERIFIE_ENTIER(waitpid(retour, &statut, 0), retour, "waitpid rend le pid de l'enfant attendu");
    VERIFIE(WIFEXITED(statut), "l'enfant s'est termine normalement");

    int bilan_de_l_enfant = WEXITSTATUS(statut);
    VERIFIE((bilan_de_l_enfant & BIT_RETOUR_NUL) != 0, "dans l'enfant, fork a rendu zero");
    VERIFIE((bilan_de_l_enfant & BIT_PID_PROPRE) != 0, "l'enfant a recu un pid a lui");
    VERIFIE((bilan_de_l_enfant & BIT_PARENT_RETROUVE) != 0, "getppid rend le pid du parent");
    VERIFIE_ENTIER(bilan_de_l_enfant, BILAN_ATTENDU, "les trois constats de l'enfant sont vrais");
    return BILAN();
}
