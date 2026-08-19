#include "verif.h"
#include <sys/wait.h>
#include <unistd.h>

const int PAS_FINI = 1;

enum {
    LIGNE_MAX = 128,
    ARGUMENTS_MAX = 16,
    SORTIE_MAX = 128,
    CODE_INTROUVABLE = 127,
    CODE_ERREUR_INTERNE = -1
};

typedef struct {
    char ligne[LIGNE_MAX];
    char *arguments[ARGUMENTS_MAX];
    size_t nombre;
} Commande;

static int decouper(Commande *commande, const char *texte) {
    size_t longueur = strlen(texte);
    if (longueur + 1 > sizeof commande->ligne) {
        return 0;
    }
    memcpy(commande->ligne, texte, longueur + 1);
    commande->nombre = 0;

    char *curseur = commande->ligne;
    while (*curseur != '\0') {
        while (*curseur == ' ') {
            curseur++;
        }
        if (*curseur == '\0') {
            break;
        }
        if (commande->nombre + 1 >= ARGUMENTS_MAX) {
            return 0;
        }
        commande->arguments[commande->nombre] = curseur;
        commande->nombre++;
        while (*curseur != '\0' && *curseur != ' ') {
            curseur++;
        }
        if (*curseur == ' ') {
            *curseur = '\0';
            curseur++;
        }
    }
    commande->arguments[commande->nombre] = NULL;
    return 1;
}

static int executer(const Commande *commande, char *sortie, size_t capacite) {
    int tube[2];
    sortie[0] = '\0';
    if (pipe(tube) != 0) {
        return CODE_ERREUR_INTERNE;
    }
    fflush(stdout);
    pid_t enfant = fork();
    if (enfant < 0) {
        close(tube[0]);
        close(tube[1]);
        return CODE_ERREUR_INTERNE;
    }
    if (enfant == 0) {
        dup2(tube[1], STDOUT_FILENO);
        close(tube[1]);
        close(tube[0]);
        execvp(commande->arguments[0], commande->arguments);
        _exit(CODE_INTROUVABLE);
    }
    close(tube[1]);

    size_t total = 0;
    while (total + 1 < capacite) {
        ssize_t lus = read(tube[0], sortie + total, capacite - 1 - total);
        if (lus <= 0) {
            break;
        }
        total += (size_t)lus;
    }
    sortie[total] = '\0';
    close(tube[0]);

    int statut = 0;
    if (WIFEXITED(statut)) {
        return WEXITSTATUS(statut);
    }
    return CODE_ERREUR_INTERNE;
}

int main(void) {
    Commande commande;
    char sortie[SORTIE_MAX];

    VERIFIE(decouper(&commande, "/bin/echo mini shell"), "la ligne de commande est decoupee");
    VERIFIE_ENTIER(commande.nombre, 3, "la ligne donne trois mots");
    VERIFIE_TEXTE(commande.arguments[0], "/bin/echo", "le premier mot est le programme a lancer");
    VERIFIE_TEXTE(commande.arguments[2], "shell", "le dernier mot est le dernier argument");
    VERIFIE(commande.arguments[commande.nombre] == NULL,
            "le tableau d'arguments se termine par un pointeur nul");
    VERIFIE_ENTIER(executer(&commande, sortie, sizeof sortie), 0,
                   "une commande qui reussit rend le code zero");
    VERIFIE_TEXTE(sortie, "mini shell\n", "le mini shell recupere la sortie de la commande");

    VERIFIE(decouper(&commande, "   /usr/bin/false   "), "les espaces en trop sont ignores");
    VERIFIE_ENTIER(commande.nombre, 1, "cette ligne ne donne qu'un mot");
    VERIFIE_ENTIER(executer(&commande, sortie, sizeof sortie), 1,
                   "une commande qui echoue rend son propre code de sortie");

    VERIFIE(decouper(&commande, "commande_absente_du_systeme"), "la ligne inconnue est decoupee");
    VERIFIE_ENTIER(executer(&commande, sortie, sizeof sortie), CODE_INTROUVABLE,
                   "une commande introuvable rend le code 127");
    return BILAN();
}
