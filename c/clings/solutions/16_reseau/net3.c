#include "verif.h"

#include <arpa/inet.h>
#include <errno.h>
#include <netinet/in.h>
#include <signal.h>
#include <sys/socket.h>
#include <sys/wait.h>
#include <unistd.h>

const int PAS_FINI = 0;

enum {
    FILE_D_ATTENTE = 8,
    TAILLE_DU_MESSAGE = 1 << 20,
    TAMPON_DEMANDE = 4096,
    CODE_SOCKET_RATEE = 21,
    CODE_CONNEXION_RATEE = 22,
    CODE_ECRITURE_RATEE = 23
};

typedef struct {
    size_t octets;
    int appels;
    int fin_de_flux;
    int erreur;
} Reception;

static Reception recevoir_exactement(int prise, char *destination, size_t attendu) {
    Reception bilan = {0, 0, 0, 0};
    while (bilan.octets < attendu) {
        ssize_t lus = read(prise, destination + bilan.octets, attendu - bilan.octets);
        bilan.appels++;
        if (lus > 0) {
            bilan.octets += (size_t)lus;
            continue;
        }
        if (lus == 0) {
            bilan.fin_de_flux = 1;
            break;
        }
        if (errno == EINTR) {
            continue;
        }
        bilan.erreur = errno;
        break;
    }
    return bilan;
}

static int ecrire_tout(int prise, const char *source, size_t taille) {
    size_t envoyes = 0;
    while (envoyes < taille) {
        ssize_t ecrits = write(prise, source + envoyes, taille - envoyes);
        if (ecrits > 0) {
            envoyes += (size_t)ecrits;
            continue;
        }
        if (ecrits < 0 && errno == EINTR) {
            continue;
        }
        return 0;
    }
    return 1;
}

static int ouvrir_ecoute_locale(uint16_t *port) {
    int ecoute = socket(AF_INET, SOCK_STREAM, 0);
    if (ecoute < 0) {
        return -1;
    }
    int un = 1;
    setsockopt(ecoute, SOL_SOCKET, SO_REUSEADDR, &un, sizeof un);
    int tampon = TAMPON_DEMANDE;
    setsockopt(ecoute, SOL_SOCKET, SO_RCVBUF, &tampon, sizeof tampon);

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
        return -1;
    }
    *port = ntohs(adresse.sin_port);
    return ecoute;
}

static void client(uint16_t port, const char *message, size_t taille) {
    signal(SIGPIPE, SIG_IGN);
    int prise = socket(AF_INET, SOCK_STREAM, 0);
    if (prise < 0) {
        _exit(CODE_SOCKET_RATEE);
    }
    struct sockaddr_in cible;
    memset(&cible, 0, sizeof cible);
    cible.sin_family = AF_INET;
    cible.sin_addr.s_addr = htonl(INADDR_LOOPBACK);
    cible.sin_port = htons(port);
    if (connect(prise, (const struct sockaddr *)&cible, sizeof cible) != 0) {
        _exit(CODE_CONNEXION_RATEE);
    }
    if (!ecrire_tout(prise, message, taille)) {
        _exit(CODE_ECRITURE_RATEE);
    }
    close(prise);
    _exit(0);
}

static char emis[TAILLE_DU_MESSAGE];
static char recu[TAILLE_DU_MESSAGE];

int main(void) {
    for (size_t indice = 0; indice < TAILLE_DU_MESSAGE; indice++) {
        emis[indice] = (char)(indice * 131 + 7);
    }

    uint16_t port = 0;
    int ecoute = ouvrir_ecoute_locale(&port);
    VERIFIE(ecoute >= 0, "le serveur local est en ecoute");
    if (ecoute < 0) {
        return BILAN();
    }

    fflush(stdout);
    pid_t enfant = fork();
    if (enfant == 0) {
        close(ecoute);
        client(port, emis, TAILLE_DU_MESSAGE);
    }
    VERIFIE(enfant > 0, "le client est lance");
    if (enfant < 0) {
        close(ecoute);
        return BILAN();
    }

    int service = accept(ecoute, NULL, NULL);
    Reception message = {0, 0, 0, 0};
    Reception apres_la_fin = {0, 0, 0, 0};
    char reste[16];
    if (service >= 0) {
        message = recevoir_exactement(service, recu, TAILLE_DU_MESSAGE);
        apres_la_fin = recevoir_exactement(service, reste, 1);
        close(service);
    }
    int statut = 0;
    int code_de_l_enfant = -1;
    if (waitpid(enfant, &statut, 0) == enfant && WIFEXITED(statut)) {
        code_de_l_enfant = WEXITSTATUS(statut);
    }
    close(ecoute);

    VERIFIE_ENTIER(message.octets, TAILLE_DU_MESSAGE, "le message entier a ete recu");
    VERIFIE(message.appels > 1, "il a fallu plus d'un read pour rassembler un mega-octet");
    VERIFIE_ENTIER(message.erreur, 0, "aucune erreur de lecture");
    VERIFIE(memcmp(recu, emis, TAILLE_DU_MESSAGE) == 0, "les octets arrivent dans l'ordre et sans trou");
    VERIFIE_ENTIER(apres_la_fin.octets, 0, "plus rien a lire apres le message");
    VERIFIE_ENTIER(apres_la_fin.fin_de_flux, 1, "read rend 0 : le pair a ferme, ce n'est pas une erreur");
    VERIFIE_ENTIER(apres_la_fin.erreur, 0, "et errno n'a pas ete confondu avec la fin de flux");
    VERIFIE_ENTIER(code_de_l_enfant, 0, "le client a pu envoyer tout son message");

    return BILAN();
}
