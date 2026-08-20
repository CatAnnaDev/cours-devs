#include "verif.h"

#include <arpa/inet.h>
#include <errno.h>
#include <fcntl.h>
#include <netinet/in.h>
#include <poll.h>
#include <sys/socket.h>
#include <sys/wait.h>
#include <unistd.h>

const int PAS_FINI = 1;

enum {
    FILE_D_ATTENTE = 8,
    TAILLE_DU_MESSAGE = 1 << 20,
    TAMPON_DEMANDE = 4096,
    DELAI_MAX_MS = 1000
};

typedef struct {
    size_t octets_ecrits;
    int appels_a_write;
    int attentes;
    int erreur;
} Envoi;

typedef struct {
    size_t octets_lus;
    int identique;
} Rapport;

static int attendre_de_la_place(int prise, int delai_ms) {
    struct pollfd surveille;
    surveille.fd = prise;
    surveille.events = POLLOUT;
    surveille.revents = 0;
    return poll(&surveille, 1, delai_ms) == 1 && (surveille.revents & POLLOUT) != 0;
}

static Envoi tout_envoyer(int prise, const char *source, size_t taille) {
    Envoi bilan = {0, 0, 0, 0};
    ssize_t ecrits = write(prise, source, taille);
    bilan.appels_a_write = 1;
    if (ecrits < 0) {
        bilan.erreur = errno;
        return bilan;
    }
    bilan.octets_ecrits = taille;
    return bilan;
}

static int ouvrir_ecoute_locale(struct sockaddr_in *adresse) {
    int ecoute = socket(AF_INET, SOCK_STREAM, 0);
    if (ecoute < 0) {
        return -1;
    }
    int un = 1;
    setsockopt(ecoute, SOL_SOCKET, SO_REUSEADDR, &un, sizeof un);
    int tampon = TAMPON_DEMANDE;
    setsockopt(ecoute, SOL_SOCKET, SO_RCVBUF, &tampon, sizeof tampon);

    memset(adresse, 0, sizeof *adresse);
    adresse->sin_family = AF_INET;
    adresse->sin_addr.s_addr = htonl(INADDR_LOOPBACK);
    adresse->sin_port = htons(0);
    socklen_t taille = sizeof *adresse;
    if (bind(ecoute, (const struct sockaddr *)adresse, sizeof *adresse) != 0 ||
        listen(ecoute, FILE_D_ATTENTE) != 0 ||
        getsockname(ecoute, (struct sockaddr *)adresse, &taille) != 0) {
        close(ecoute);
        return -1;
    }
    return ecoute;
}

static int connecter_vers(const struct sockaddr_in *adresse) {
    int prise = socket(AF_INET, SOCK_STREAM, 0);
    if (prise < 0) {
        return -1;
    }
    int tampon = TAMPON_DEMANDE;
    setsockopt(prise, SOL_SOCKET, SO_SNDBUF, &tampon, sizeof tampon);
    if (connect(prise, (const struct sockaddr *)adresse, sizeof *adresse) != 0 ||
        fcntl(prise, F_SETFL, O_NONBLOCK) != 0) {
        close(prise);
        return -1;
    }
    return prise;
}

static int tampon_d_emission(int prise) {
    int taille = 0;
    socklen_t longueur = sizeof taille;
    if (getsockopt(prise, SOL_SOCKET, SO_SNDBUF, &taille, &longueur) != 0) {
        return -1;
    }
    return taille;
}

static char emis[TAILLE_DU_MESSAGE];
static char recu[TAILLE_DU_MESSAGE];

static void lecteur(int prise, int rapport) {
    Rapport bilan = {0, 0};
    while (bilan.octets_lus < TAILLE_DU_MESSAGE) {
        ssize_t lus = read(prise, recu + bilan.octets_lus, TAILLE_DU_MESSAGE - bilan.octets_lus);
        if (lus <= 0) {
            break;
        }
        bilan.octets_lus += (size_t)lus;
    }
    bilan.identique = bilan.octets_lus == TAILLE_DU_MESSAGE &&
                      memcmp(recu, emis, TAILLE_DU_MESSAGE) == 0;
    write(rapport, &bilan, sizeof bilan);
    close(rapport);
    close(prise);
    _exit(0);
}

int main(void) {
    for (size_t indice = 0; indice < TAILLE_DU_MESSAGE; indice++) {
        emis[indice] = (char)(indice * 131 + 7);
    }

    struct sockaddr_in adresse;
    int ecoute = ouvrir_ecoute_locale(&adresse);
    int emetteur = ecoute < 0 ? -1 : connecter_vers(&adresse);
    int recepteur = emetteur < 0 ? -1 : accept(ecoute, NULL, NULL);
    int tuyau[2] = {-1, -1};
    if (ecoute >= 0) {
        close(ecoute);
    }
    VERIFIE(recepteur >= 0 && pipe(tuyau) == 0, "une connexion locale est etablie");
    if (recepteur < 0 || tuyau[0] < 0) {
        return BILAN();
    }

    int tampon = tampon_d_emission(emetteur);
    VERIFIE(tampon > 0 && tampon < TAILLE_DU_MESSAGE,
            "le tampon d'emission est trop petit pour le message");

    fflush(stdout);
    pid_t enfant = fork();
    if (enfant == 0) {
        close(emetteur);
        close(tuyau[0]);
        lecteur(recepteur, tuyau[1]);
    }
    VERIFIE(enfant > 0, "le lecteur est lance");
    if (enfant < 0) {
        return BILAN();
    }
    close(recepteur);
    close(tuyau[1]);

    Envoi bilan = tout_envoyer(emetteur, emis, TAILLE_DU_MESSAGE);
    close(emetteur);

    Rapport rapport = {0, 0};
    ssize_t lus = read(tuyau[0], &rapport, sizeof rapport);
    close(tuyau[0]);
    int statut = 0;
    int code_de_l_enfant = -1;
    if (waitpid(enfant, &statut, 0) == enfant && WIFEXITED(statut)) {
        code_de_l_enfant = WEXITSTATUS(statut);
    }

    VERIFIE_ENTIER(code_de_l_enfant, 0, "le lecteur a termine proprement");
    VERIFIE_ENTIER(lus, sizeof rapport, "et rendu son compte");
    VERIFIE_ENTIER(bilan.erreur, 0, "aucune erreur d'ecriture");
    VERIFIE_ENTIER(bilan.octets_ecrits, TAILLE_DU_MESSAGE, "l'envoi annonce le message entier");
    VERIFIE(bilan.appels_a_write > 1, "un seul write n'a pas suffi a placer un mega-octet");
    VERIFIE(bilan.attentes > 0, "le noyau a refuse d'en prendre plus au moins une fois");
    VERIFIE_ENTIER(rapport.octets_lus, TAILLE_DU_MESSAGE, "le pair a recu autant d'octets qu'annonce");
    VERIFIE_ENTIER(rapport.identique, 1, "et exactement les memes, dans l'ordre");

    return BILAN();
}
