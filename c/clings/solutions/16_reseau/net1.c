#include "verif.h"

#include <arpa/inet.h>
#include <errno.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <sys/wait.h>
#include <unistd.h>

const int PAS_FINI = 0;

#define MESSAGE "octets en ordre reseau"

enum {
    FILE_D_ATTENTE = 8,
    RECU_MAX = 64,
    CODE_SOCKET_RATEE = 21,
    CODE_CONNEXION_RATEE = 22,
    CODE_ECRITURE_RATEE = 23
};

typedef struct {
    int bind_reussi;
    int erreur_de_bind;
    uint16_t port_annonce;
    uint16_t port_reconstruit;
    int code_de_l_enfant;
    ssize_t octets_recus;
    char recu[RECU_MAX];
} Essai;

static struct sockaddr_in adresse_locale(uint16_t port) {
    struct sockaddr_in adresse;
    memset(&adresse, 0, sizeof adresse);
    adresse.sin_family = AF_INET;
    adresse.sin_addr.s_addr = htonl(INADDR_LOOPBACK);
    adresse.sin_port = htons(port);
    return adresse;
}

static uint16_t port_hote(const struct sockaddr_in *adresse) {
    return ntohs(adresse->sin_port);
}

static uint16_t port_octet_par_octet(const struct sockaddr_in *adresse) {
    const unsigned char *octets = (const unsigned char *)&adresse->sin_port;
    return (uint16_t)((octets[0] << 8) | octets[1]);
}

static void client(const struct sockaddr_in *cible) {
    int prise = socket(AF_INET, SOCK_STREAM, 0);
    if (prise < 0) {
        _exit(CODE_SOCKET_RATEE);
    }
    if (connect(prise, (const struct sockaddr *)cible, sizeof *cible) != 0) {
        _exit(CODE_CONNEXION_RATEE);
    }
    if (write(prise, MESSAGE, sizeof MESSAGE - 1) != (ssize_t)(sizeof MESSAGE - 1)) {
        _exit(CODE_ECRITURE_RATEE);
    }
    close(prise);
    _exit(0);
}

static Essai un_aller_retour(void) {
    Essai essai = {0, 0, 0, 0, -1, -1, {0}};
    int ecoute = socket(AF_INET, SOCK_STREAM, 0);
    if (ecoute < 0) {
        return essai;
    }
    int un = 1;
    setsockopt(ecoute, SOL_SOCKET, SO_REUSEADDR, &un, sizeof un);

    struct sockaddr_in demande = adresse_locale(0);
    if (bind(ecoute, (const struct sockaddr *)&demande, sizeof demande) != 0) {
        essai.erreur_de_bind = errno;
        close(ecoute);
        return essai;
    }
    essai.bind_reussi = 1;
    if (listen(ecoute, FILE_D_ATTENTE) != 0) {
        close(ecoute);
        return essai;
    }

    struct sockaddr_in reelle;
    socklen_t taille = sizeof reelle;
    if (getsockname(ecoute, (struct sockaddr *)&reelle, &taille) != 0) {
        close(ecoute);
        return essai;
    }
    essai.port_annonce = port_hote(&reelle);
    essai.port_reconstruit = port_octet_par_octet(&reelle);

    fflush(stdout);
    pid_t enfant = fork();
    if (enfant < 0) {
        close(ecoute);
        return essai;
    }
    if (enfant == 0) {
        close(ecoute);
        struct sockaddr_in cible = adresse_locale(essai.port_annonce);
        client(&cible);
    }

    int statut = 0;
    if (waitpid(enfant, &statut, 0) == enfant && WIFEXITED(statut)) {
        essai.code_de_l_enfant = WEXITSTATUS(statut);
    }
    if (essai.code_de_l_enfant == 0) {
        int service = accept(ecoute, NULL, NULL);
        if (service >= 0) {
            essai.octets_recus = read(service, essai.recu, RECU_MAX - 1);
            if (essai.octets_recus > 0) {
                essai.recu[essai.octets_recus] = '\0';
            }
            close(service);
        }
    }
    close(ecoute);
    return essai;
}

int main(void) {
    Essai essai = un_aller_retour();

    VERIFIE_ENTIER(essai.erreur_de_bind, 0, "bind accepte l'adresse de la boucle locale");
    VERIFIE_ENTIER(essai.bind_reussi, 1, "le serveur est attache a 127.0.0.1");
    VERIFIE(essai.port_annonce > 1024, "le noyau a choisi un port libre au-dessus de 1024");
    VERIFIE_ENTIER(essai.port_annonce, essai.port_reconstruit,
                   "le port relu est ramene en ordre machine");
    VERIFIE_ENTIER(essai.code_de_l_enfant, 0, "le client a joint ce port precis");
    VERIFIE_ENTIER(essai.octets_recus, sizeof MESSAGE - 1, "le serveur recoit tout le message");
    VERIFIE_TEXTE(essai.recu, MESSAGE, "et le contenu attendu");

    return BILAN();
}
