#include "verif.h"

#include <arpa/inet.h>
#include <errno.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <unistd.h>

const int PAS_FINI = 0;

enum {
    FILE_D_ATTENTE = 8,
    MESSAGES = 4,
    TAMPON_MAX = 64,
    ETIQUETTE_MAX = 64,
    OCTETS_SUR_LE_FIL = 39
};

static const char *const A_ENVOYER[MESSAGES] = {"un", "", "deux mots", "trois\nlignes"};

typedef struct {
    int emetteur;
    int recepteur;
} Paire;

static int ecrire_tout(int prise, const void *source, size_t taille) {
    const char *octets = source;
    size_t envoyes = 0;
    while (envoyes < taille) {
        ssize_t ecrits = write(prise, octets + envoyes, taille - envoyes);
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

static int lire_tout(int prise, void *destination, size_t taille) {
    char *octets = destination;
    size_t lus_total = 0;
    while (lus_total < taille) {
        ssize_t lus = read(prise, octets + lus_total, taille - lus_total);
        if (lus > 0) {
            lus_total += (size_t)lus;
            continue;
        }
        if (lus < 0 && errno == EINTR) {
            continue;
        }
        return 0;
    }
    return 1;
}

static ssize_t envoyer_message(int prise, const char *texte) {
    uint32_t taille = (uint32_t)strlen(texte);
    uint32_t prefixe = htonl(taille);
    if (!ecrire_tout(prise, &prefixe, sizeof prefixe)) {
        return -1;
    }
    if (!ecrire_tout(prise, texte, taille)) {
        return -1;
    }
    return (ssize_t)(sizeof prefixe + taille);
}

static ssize_t recevoir_message(int prise, char *tampon, size_t capacite) {
    uint32_t prefixe = 0;
    if (!lire_tout(prise, &prefixe, sizeof prefixe)) {
        return -1;
    }
    size_t taille = ntohl(prefixe);
    if (taille >= capacite) {
        return -1;
    }
    if (!lire_tout(prise, tampon, taille)) {
        return -1;
    }
    tampon[taille] = '\0';
    return (ssize_t)taille;
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

    ssize_t sur_le_fil = 0;
    for (int indice = 0; indice < MESSAGES; indice++) {
        ssize_t ecrits = envoyer_message(paire.emetteur, A_ENVOYER[indice]);
        if (ecrits < 0) {
            sur_le_fil = -1;
            break;
        }
        sur_le_fil += ecrits;
    }
    shutdown(paire.emetteur, SHUT_WR);

    char recus[MESSAGES][TAMPON_MAX];
    ssize_t longueurs[MESSAGES];
    for (int indice = 0; indice < MESSAGES; indice++) {
        recus[indice][0] = '\0';
        longueurs[indice] = recevoir_message(paire.recepteur, recus[indice], TAMPON_MAX);
    }
    char apres[TAMPON_MAX];
    apres[0] = '\0';
    ssize_t apres_la_fin = recevoir_message(paire.recepteur, apres, TAMPON_MAX);

    close(paire.emetteur);
    close(paire.recepteur);

    VERIFIE_ENTIER(sur_le_fil, OCTETS_SUR_LE_FIL,
                   "quatre messages et leurs quatre prefixes tiennent en 39 octets");
    char etiquette[ETIQUETTE_MAX];
    for (int indice = 0; indice < MESSAGES; indice++) {
        snprintf(etiquette, sizeof etiquette, "message %d : la longueur annoncee", indice + 1);
        VERIFIE_ENTIER(longueurs[indice], (ssize_t)strlen(A_ENVOYER[indice]), etiquette);
        snprintf(etiquette, sizeof etiquette, "message %d : le contenu exact", indice + 1);
        VERIFIE_TEXTE(recus[indice], A_ENVOYER[indice], etiquette);
    }
    VERIFIE_ENTIER(apres_la_fin, -1, "apres le dernier message vient la fin du flux");

    return BILAN();
}
