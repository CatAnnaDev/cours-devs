#include "verif.h"
#include <sys/wait.h>
#include <unistd.h>

const int PAS_FINI = 0;

#define MESSAGE "tube ouvert"

typedef enum {
    COTE_LECTEUR,
    COTE_ECRIVAIN
} Cote;

static void fermer_l_extremite_inutile(const int tube[2], Cote cote) {
    if (cote == COTE_LECTEUR) {
        close(tube[1]);
    } else {
        close(tube[0]);
    }
}

static int ecrire_tout(int descripteur, const char *texte, size_t taille) {
    size_t total = 0;
    while (total < taille) {
        ssize_t ecrits = write(descripteur, texte + total, taille - total);
        if (ecrits <= 0) {
            return 0;
        }
        total += (size_t)ecrits;
    }
    return 1;
}

static ssize_t lire_exactement(int descripteur, char *tampon, size_t taille) {
    size_t total = 0;
    while (total < taille) {
        ssize_t lus = read(descripteur, tampon + total, taille - total);
        if (lus < 0) {
            return -1;
        }
        if (lus == 0) {
            break;
        }
        total += (size_t)lus;
    }
    return (ssize_t)total;
}

int main(void) {
    int tube[2];
    VERIFIE_ENTIER(pipe(tube), 0, "le tube est cree");

    fflush(stdout);
    pid_t enfant = fork();
    if (enfant < 0) {
        VERIFIE(0, "fork a echoue, impossible de continuer");
        return BILAN();
    }
    if (enfant == 0) {
        fermer_l_extremite_inutile(tube, COTE_ECRIVAIN);
        int ecrit = ecrire_tout(tube[1], MESSAGE, sizeof MESSAGE - 1);
        close(tube[1]);
        _exit(ecrit ? 0 : 1);
    }

    fermer_l_extremite_inutile(tube, COTE_LECTEUR);

    char recu[sizeof MESSAGE];
    memset(recu, 0, sizeof recu);
    ssize_t lus = lire_exactement(tube[0], recu, sizeof MESSAGE - 1);
    VERIFIE_ENTIER(lus, sizeof MESSAGE - 1, "le parent lit tous les octets du message");
    VERIFIE_TEXTE(recu, MESSAGE, "le message traverse le tube intact");

    char de_trop = 0;
    VERIFIE_ENTIER(read(tube[0], &de_trop, 1), 0,
                   "la lecture suivante rend zero, plus personne n'ecrit");
    close(tube[0]);

    int statut = 0;
    VERIFIE_ENTIER(waitpid(enfant, &statut, 0), enfant, "l'enfant est recolte");
    VERIFIE(WIFEXITED(statut), "l'enfant s'est termine normalement");
    VERIFIE_ENTIER(WEXITSTATUS(statut), 0, "l'enfant a pu ecrire dans le tube");
    return BILAN();
}
