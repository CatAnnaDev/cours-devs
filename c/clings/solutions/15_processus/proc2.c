#include "verif.h"
#include <signal.h>
#include <sys/wait.h>
#include <unistd.h>

const int PAS_FINI = 0;

enum {
    CODE_DE_L_ENFANT_SAGE = 3
};

typedef struct {
    int termine_normalement;
    int code_de_sortie;
    int tue_par_un_signal;
    int numero_du_signal;
} Fin;

static Fin analyser_le_statut(int statut) {
    Fin fin;
    fin.termine_normalement = WIFEXITED(statut) != 0;
    fin.code_de_sortie = WIFEXITED(statut) ? WEXITSTATUS(statut) : -1;
    fin.tue_par_un_signal = WIFSIGNALED(statut) != 0;
    fin.numero_du_signal = WIFSIGNALED(statut) ? WTERMSIG(statut) : 0;
    return fin;
}

static pid_t lancer_un_enfant_qui_sort(int code) {
    fflush(stdout);
    pid_t enfant = fork();
    if (enfant == 0) {
        _exit(code);
    }
    return enfant;
}

static pid_t lancer_un_enfant_qui_se_tue(int numero_du_signal) {
    fflush(stdout);
    pid_t enfant = fork();
    if (enfant == 0) {
        raise(numero_du_signal);
        _exit(0);
    }
    return enfant;
}

int main(void) {
    pid_t sage = lancer_un_enfant_qui_sort(CODE_DE_L_ENFANT_SAGE);
    VERIFIE(sage > 0, "le premier enfant est ne");
    int statut_du_sage = 0;
    VERIFIE_ENTIER(waitpid(sage, &statut_du_sage, 0), sage, "waitpid a recolte le premier enfant");
    VERIFIE(statut_du_sage != CODE_DE_L_ENFANT_SAGE, "le statut brut n'est pas le code de sortie");

    Fin fin_du_sage = analyser_le_statut(statut_du_sage);
    VERIFIE(fin_du_sage.termine_normalement, "le premier enfant s'est termine normalement");
    VERIFIE_ENTIER(fin_du_sage.code_de_sortie, CODE_DE_L_ENFANT_SAGE, "son code de sortie vaut 3");
    VERIFIE(!fin_du_sage.tue_par_un_signal, "aucun signal ne l'a tue");

    pid_t frappe = lancer_un_enfant_qui_se_tue(SIGTERM);
    VERIFIE(frappe > 0, "le second enfant est ne");
    int statut_du_frappe = 0;
    VERIFIE_ENTIER(waitpid(frappe, &statut_du_frappe, 0), frappe,
                   "waitpid a recolte le second enfant");

    Fin fin_du_frappe = analyser_le_statut(statut_du_frappe);
    VERIFIE(!fin_du_frappe.termine_normalement, "le second enfant n'est pas sorti par exit");
    VERIFIE(fin_du_frappe.tue_par_un_signal, "un signal l'a tue");
    VERIFIE_ENTIER(fin_du_frappe.numero_du_signal, SIGTERM, "ce signal est SIGTERM");
    return BILAN();
}
