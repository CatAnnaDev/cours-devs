#include "verif.h"
#include <stdint.h>

const int PAS_FINI = 0;

#define CHARGE_MAX 64

typedef enum {
    CHARGE_OK,
    CHARGE_TRONQUEE,
    CHARGE_TROP_LONGUE,
    CHARGE_ILLISIBLE
} EtatCharge;

static long octets_restants(FILE *flux) {
    long ici = ftell(flux);
    if (ici < 0 || fseek(flux, 0, SEEK_END) != 0) {
        return -1;
    }
    long fin = ftell(flux);
    if (fin < 0 || fseek(flux, ici, SEEK_SET) != 0) {
        return -1;
    }
    return fin - ici;
}

static EtatCharge lire_charge(FILE *flux, uint8_t *donnees, uint32_t *taille) {
    uint8_t entete[4];
    *taille = 0;
    if (fread(entete, 1, sizeof entete, flux) != sizeof entete) {
        return CHARGE_TRONQUEE;
    }
    uint32_t longueur = ((uint32_t)entete[0] << 24) | ((uint32_t)entete[1] << 16) |
                        ((uint32_t)entete[2] << 8) | (uint32_t)entete[3];
    if (longueur > CHARGE_MAX) {
        return CHARGE_TROP_LONGUE;
    }
    long reste = octets_restants(flux);
    if (reste < 0) {
        return CHARGE_ILLISIBLE;
    }
    if ((unsigned long)longueur > (unsigned long)reste) {
        return CHARGE_TRONQUEE;
    }
    if (fread(donnees, 1, longueur, flux) != longueur) {
        return CHARGE_TRONQUEE;
    }
    *taille = longueur;
    return CHARGE_OK;
}

static FILE *fichier_charge(uint32_t longueur_annoncee, uint32_t octets_presents) {
    FILE *flux = tmpfile();
    if (flux == NULL) {
        return NULL;
    }
    const uint8_t entete[4] = {
        (uint8_t)(longueur_annoncee >> 24), (uint8_t)(longueur_annoncee >> 16),
        (uint8_t)(longueur_annoncee >> 8),  (uint8_t)longueur_annoncee,
    };
    if (fwrite(entete, 1, sizeof entete, flux) != sizeof entete) {
        fclose(flux);
        return NULL;
    }
    for (uint32_t i = 0; i < octets_presents; i++) {
        if (fputc((int)(uint8_t)(i + 1), flux) == EOF) {
            fclose(flux);
            return NULL;
        }
    }
    if (fflush(flux) != 0) {
        fclose(flux);
        return NULL;
    }
    rewind(flux);
    return flux;
}

int main(void) {
    uint8_t *donnees = suivi_malloc(CHARGE_MAX);
    VERIFIE(donnees != NULL, "le tampon du lecteur fait la taille maximale du format");
    if (donnees == NULL) {
        return BILAN();
    }
    uint32_t taille = 0;
    FILE *flux = NULL;

    flux = fichier_charge(12u, 12u);
    VERIFIE(flux != NULL, "cas 1 : une charge de douze octets, tous presents");
    VERIFIE_ENTIER(lire_charge(flux, donnees, &taille), CHARGE_OK, "elle est acceptee");
    VERIFIE_ENTIER(taille, 12u, "avec sa longueur");
    VERIFIE_ENTIER(donnees[0], 1, "et son premier octet");
    VERIFIE_ENTIER(donnees[11], 12, "et son dernier");
    fclose(flux);

    flux = fichier_charge(4096u, 4096u);
    VERIFIE(flux != NULL, "cas 2 : une charge de 4096 octets, tous presents");
    VERIFIE_ENTIER(lire_charge(flux, donnees, &taille), CHARGE_TROP_LONGUE,
                   "le format n'en autorise pas tant : refus");
    VERIFIE_ENTIER(taille, 0u, "et rien n'est annonce au reste du programme");
    fclose(flux);

    flux = fichier_charge(60u, 10u);
    VERIFIE(flux != NULL, "cas 3 : soixante octets annonces, dix presents");
    VERIFIE_ENTIER(lire_charge(flux, donnees, &taille), CHARGE_TRONQUEE,
                   "la longueur tient dans le format, mais pas dans le fichier");
    VERIFIE_ENTIER(taille, 0u, "rien n'est annonce non plus");
    fclose(flux);

    flux = fichier_charge(0xFFFFFFF0u, 4u);
    VERIFIE(flux != NULL, "cas 4 : quatre milliards d'octets annonces, quatre presents");
    VERIFIE_ENTIER(lire_charge(flux, donnees, &taille), CHARGE_TROP_LONGUE,
                   "aucune allocation, aucune lecture : refus immediat");
    VERIFIE_ENTIER(taille, 0u, "et toujours rien d'annonce");
    fclose(flux);

    suivi_free(donnees);
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
