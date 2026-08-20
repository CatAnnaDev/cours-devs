#include "verif.h"

#include <arpa/inet.h>
#include <errno.h>
#include <netinet/in.h>
#include <netinet/tcp.h>
#include <sys/socket.h>
#include <unistd.h>

const int PAS_FINI = 1;

#define ENVOI "quelques octets pour un pair qui n'est plus la"

enum {
    FILE_D_ATTENTE = 8,
    ESSAIS_MAX = 64,
    PAUSE_US = 200,
    ECRITURE_OK = 0,
    PAIR_PARTI = 1,
    ERREUR_INCONNUE = 2
};

typedef struct {
    int emetteur;
    int recepteur;
} Paire;

typedef struct {
    int essais;
    ssize_t dernier_retour;
    int erreur;
} Ecriture;

static int preparer_l_emetteur(int prise) {
    int un = 1;
    return setsockopt(prise, IPPROTO_TCP, TCP_NODELAY, &un, sizeof un);
}

static int interpreter_l_ecriture(ssize_t retour, int erreur) {
    if (retour >= 0) {
        return ECRITURE_OK;
    }
    if (erreur == ECONNRESET) {
        return PAIR_PARTI;
    }
    return ERREUR_INCONNUE;
}

static Ecriture ecrire_jusqu_a_l_echec(int prise, const char *donnees, size_t taille) {
    Ecriture bilan = {0, 0, 0};
    while (bilan.essais < ESSAIS_MAX) {
        bilan.essais++;
        bilan.dernier_retour = write(prise, donnees, taille);
        if (bilan.dernier_retour < 0) {
            bilan.erreur = errno;
            return bilan;
        }
        usleep(PAUSE_US);
    }
    return bilan;
}

static Paire paire_locale(void) {
    Paire paire = {-1, -1};
    int ecoute = socket(AF_INET, SOCK_STREAM, 0);
    if (ecoute < 0) {
        return paire;
    }
    int un = 1;
    setsockopt(ecoute, SOL_SOCKET, SO_REUSEADDR, &un, sizeof un);

    struct sockaddr_in adresse;
    memset(&adresse, 0, sizeof adresse);
    adresse.sin_family = AF_INET;
    adresse.sin_addr.s_addr = htonl(INADDR_LOOPBACK);
    adresse.sin_port = htons(0);
    socklen_t taille = sizeof adresse;
    if (bind(ecoute, (const struct sockaddr *)&adresse, sizeof adresse) != 0 ||
        listen(ecoute, FILE_D_ATTENTE) != 0 ||
        getsockname(ecoute, (struct sockaddr *)&adresse, &taille) != 0) {
        close(ecoute);
        return paire;
    }

    int emetteur = socket(AF_INET, SOCK_STREAM, 0);
    if (emetteur < 0 ||
        connect(emetteur, (const struct sockaddr *)&adresse, sizeof adresse) != 0) {
        close(ecoute);
        return paire;
    }
    int recepteur = accept(ecoute, NULL, NULL);
    close(ecoute);
    if (recepteur < 0) {
        close(emetteur);
        return paire;
    }
    paire.emetteur = emetteur;
    paire.recepteur = recepteur;
    return paire;
}

int main(void) {
    Paire pair_parti = paire_locale();
    Paire ferme_en_ecriture = paire_locale();
    VERIFIE(pair_parti.emetteur >= 0 && ferme_en_ecriture.emetteur >= 0,
            "deux connexions locales sont etablies");
    if (pair_parti.emetteur < 0 || ferme_en_ecriture.emetteur < 0) {
        return BILAN();
    }

    VERIFIE_ENTIER(preparer_l_emetteur(pair_parti.emetteur), 0, "l'emetteur est prepare");
    VERIFIE_ENTIER(preparer_l_emetteur(ferme_en_ecriture.emetteur), 0, "le second aussi");

    close(pair_parti.recepteur);
    fflush(stdout);
    Ecriture apres_depart = ecrire_jusqu_a_l_echec(pair_parti.emetteur, ENVOI, sizeof ENVOI - 1);
    int lecture_du_depart = interpreter_l_ecriture(apres_depart.dernier_retour, apres_depart.erreur);
    close(pair_parti.emetteur);

    shutdown(ferme_en_ecriture.emetteur, SHUT_WR);
    fflush(stdout);
    ssize_t retour_apres_shutdown = write(ferme_en_ecriture.emetteur, ENVOI, sizeof ENVOI - 1);
    int erreur_apres_shutdown = errno;
    int lecture_du_shutdown = interpreter_l_ecriture(retour_apres_shutdown, erreur_apres_shutdown);
    close(ferme_en_ecriture.emetteur);
    close(ferme_en_ecriture.recepteur);

    VERIFIE(apres_depart.essais < ESSAIS_MAX, "le pair parti est detecte en quelques essais");
    VERIFIE_ENTIER(apres_depart.dernier_retour, -1, "write refuse d'ecrire dans le vide");
    VERIFIE_ENTIER(apres_depart.erreur, EPIPE, "et pose EPIPE plutot que de tuer le programme");
    VERIFIE_ENTIER(lecture_du_depart, PAIR_PARTI, "l'echec est reconnu comme un depart du pair");

    VERIFIE_ENTIER(retour_apres_shutdown, -1, "ecrire apres shutdown(SHUT_WR) echoue aussi");
    VERIFIE_ENTIER(erreur_apres_shutdown, EPIPE, "avec le meme EPIPE");
    VERIFIE_ENTIER(lecture_du_shutdown, PAIR_PARTI, "et la meme lecture de l'erreur");

    return BILAN();
}
