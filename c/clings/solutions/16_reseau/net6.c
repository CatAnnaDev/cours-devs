#include "verif.h"

#include <arpa/inet.h>
#include <errno.h>
#include <netinet/in.h>
#include <sys/event.h>
#include <sys/socket.h>
#include <sys/wait.h>
#include <unistd.h>

const int PAS_FINI = 0;

#define MESSAGE "l'octet tardif"

enum {
    FILE_D_ATTENTE = 8,
    RECU_MAX = 64,
    DELAI_MAX_MS = 1000,
    RETARD_US = 30000,
    CODE_ECRITURE_RATEE = 23
};

typedef struct {
    int emetteur;
    int recepteur;
} Paire;

typedef struct {
    int appels_a_read;
    int reveils;
    ssize_t octets;
    int expire;
    int erreur;
} Attente;

static Attente attendre_puis_lire(int prise, char *tampon, size_t capacite, int delai_ms) {
    Attente bilan = {0, 0, -1, 0, 0};
    int file = kqueue();
    if (file < 0) {
        bilan.erreur = errno;
        return bilan;
    }
    struct kevent changement;
    EV_SET(&changement, prise, EVFILT_READ, EV_ADD | EV_ENABLE, 0, 0, NULL);
    if (kevent(file, &changement, 1, NULL, 0, NULL) != 0) {
        bilan.erreur = errno;
        close(file);
        return bilan;
    }
    struct timespec delai;
    delai.tv_sec = delai_ms / 1000;
    delai.tv_nsec = (long)(delai_ms % 1000) * 1000000L;
    struct kevent evenement;
    int prets = kevent(file, NULL, 0, &evenement, 1, &delai);
    close(file);
    if (prets < 0) {
        bilan.erreur = errno;
        return bilan;
    }
    if (prets == 0) {
        bilan.expire = 1;
        return bilan;
    }
    bilan.reveils = prets;
    bilan.appels_a_read = 1;
    ssize_t lus = read(prise, tampon, capacite - 1);
    if (lus < 0) {
        bilan.erreur = errno;
        return bilan;
    }
    tampon[lus] = '\0';
    bilan.octets = lus;
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
    Paire paire = paire_locale();
    VERIFIE(paire.emetteur >= 0 && paire.recepteur >= 0, "une connexion locale est etablie");
    if (paire.emetteur < 0) {
        return BILAN();
    }

    fflush(stdout);
    pid_t enfant = fork();
    if (enfant == 0) {
        close(paire.recepteur);
        usleep(RETARD_US);
        if (write(paire.emetteur, MESSAGE, sizeof MESSAGE - 1) != (ssize_t)(sizeof MESSAGE - 1)) {
            _exit(CODE_ECRITURE_RATEE);
        }
        close(paire.emetteur);
        _exit(0);
    }
    VERIFIE(enfant > 0, "le pair bavard est lance");
    if (enfant < 0) {
        close(paire.emetteur);
        close(paire.recepteur);
        return BILAN();
    }
    close(paire.emetteur);

    char recu[RECU_MAX];
    recu[0] = '\0';
    Attente bilan = attendre_puis_lire(paire.recepteur, recu, sizeof recu, DELAI_MAX_MS);

    int statut = 0;
    int code_de_l_enfant = -1;
    if (waitpid(enfant, &statut, 0) == enfant && WIFEXITED(statut)) {
        code_de_l_enfant = WEXITSTATUS(statut);
    }
    close(paire.recepteur);

    VERIFIE_ENTIER(code_de_l_enfant, 0, "le pair a bien fini par ecrire");
    VERIFIE_ENTIER(bilan.expire, 0, "l'attente n'a pas expire");
    VERIFIE_ENTIER(bilan.erreur, 0, "aucune erreur pendant l'attente");
    VERIFIE_ENTIER(bilan.reveils, 1, "un seul reveil, au moment ou l'octet arrive");
    VERIFIE_ENTIER(bilan.appels_a_read, 1, "un seul read, pas un par microseconde d'attente");
    VERIFIE_ENTIER(bilan.octets, sizeof MESSAGE - 1, "le message tardif est arrive en entier");
    VERIFIE_TEXTE(recu, MESSAGE, "et le contenu attendu");

    return BILAN();
}
