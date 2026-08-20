#include "verif.h"

#include <arpa/inet.h>
#include <errno.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <sys/wait.h>
#include <unistd.h>

const int PAS_FINI = 0;

#define MESSAGE "le port vient du noyau"
#define PORT_LAISSE_AU_NOYAU 0

enum {
    FILE_D_ATTENTE = 8,
    RECU_MAX = 64,
    CODE_SOCKET_RATEE = 21,
    CODE_CONNEXION_RATEE = 22,
    CODE_ECRITURE_RATEE = 23
};

typedef struct {
    int descripteur;
    uint16_t port;
    int erreur;
} Serveur;

static Serveur ouvrir_un_serveur(void) {
    Serveur serveur = {-1, 0, 0};
    int ecoute = socket(AF_INET, SOCK_STREAM, 0);
    if (ecoute < 0) {
        serveur.erreur = errno;
        return serveur;
    }
    int un = 1;
    if (setsockopt(ecoute, SOL_SOCKET, SO_REUSEADDR, &un, sizeof un) != 0) {
        serveur.erreur = errno;
        close(ecoute);
        return serveur;
    }

    struct sockaddr_in adresse;
    memset(&adresse, 0, sizeof adresse);
    adresse.sin_family = AF_INET;
    adresse.sin_addr.s_addr = htonl(INADDR_LOOPBACK);
    adresse.sin_port = htons(PORT_LAISSE_AU_NOYAU);

    if (bind(ecoute, (const struct sockaddr *)&adresse, sizeof adresse) != 0) {
        serveur.erreur = errno;
        close(ecoute);
        return serveur;
    }
    if (listen(ecoute, FILE_D_ATTENTE) != 0) {
        serveur.erreur = errno;
        close(ecoute);
        return serveur;
    }

    socklen_t taille = sizeof adresse;
    if (getsockname(ecoute, (struct sockaddr *)&adresse, &taille) != 0) {
        serveur.erreur = errno;
        close(ecoute);
        return serveur;
    }

    serveur.port = ntohs(adresse.sin_port);
    serveur.descripteur = ecoute;
    return serveur;
}

static void fermer_serveur(Serveur *serveur) {
    if (serveur->descripteur >= 0) {
        close(serveur->descripteur);
        serveur->descripteur = -1;
    }
}

static void client(uint16_t port) {
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
    if (write(prise, MESSAGE, sizeof MESSAGE - 1) != (ssize_t)(sizeof MESSAGE - 1)) {
        _exit(CODE_ECRITURE_RATEE);
    }
    close(prise);
    _exit(0);
}

static ssize_t un_message_sur(const Serveur *serveur, char *recu, size_t capacite, int *code_client) {
    *code_client = -1;
    if (serveur->descripteur < 0) {
        return -1;
    }
    fflush(stdout);
    pid_t enfant = fork();
    if (enfant < 0) {
        return -1;
    }
    if (enfant == 0) {
        client(serveur->port);
    }
    int statut = 0;
    if (waitpid(enfant, &statut, 0) == enfant && WIFEXITED(statut)) {
        *code_client = WEXITSTATUS(statut);
    }
    if (*code_client != 0) {
        return -1;
    }
    int service = accept(serveur->descripteur, NULL, NULL);
    if (service < 0) {
        return -1;
    }
    ssize_t lus = read(service, recu, capacite - 1);
    close(service);
    if (lus < 0) {
        return -1;
    }
    recu[lus] = '\0';
    return lus;
}

int main(void) {
    Serveur premier = ouvrir_un_serveur();
    Serveur second = ouvrir_un_serveur();

    char recu_premier[RECU_MAX] = {0};
    char recu_second[RECU_MAX] = {0};
    int code_premier = -1;
    int code_second = -1;
    ssize_t octets_premier = un_message_sur(&premier, recu_premier, sizeof recu_premier,
                                            &code_premier);
    ssize_t octets_second = un_message_sur(&second, recu_second, sizeof recu_second, &code_second);

    VERIFIE_ENTIER(premier.erreur, 0, "le premier serveur demarre");
    VERIFIE_ENTIER(second.erreur, 0, "le second serveur demarre lui aussi");
    VERIFIE(premier.port > 1024 && second.port > 1024 && premier.port != second.port,
            "chaque serveur a recu son propre port libre");
    VERIFIE_ENTIER(code_premier, 0, "un client joint le premier serveur");
    VERIFIE_ENTIER(code_second, 0, "un client joint le second serveur");
    VERIFIE_ENTIER(octets_premier, sizeof MESSAGE - 1, "le premier serveur recoit tout le message");
    VERIFIE_TEXTE(recu_premier, MESSAGE, "et le contenu attendu");
    VERIFIE_ENTIER(octets_second, sizeof MESSAGE - 1, "le second serveur recoit tout le message");
    VERIFIE_TEXTE(recu_second, MESSAGE, "et le contenu attendu la aussi");

    fermer_serveur(&premier);
    fermer_serveur(&second);
    return BILAN();
}
