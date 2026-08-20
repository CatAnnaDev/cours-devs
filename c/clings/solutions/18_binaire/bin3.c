#include "verif.h"
#include <stdint.h>

const int PAS_FINI = 0;

#define ENREG_OCTETS 7
#define ENREGS 3

typedef struct {
    uint8_t type;
    uint32_t identifiant;
    uint16_t points;
} Enregistrement;

static const Enregistrement CATALOGUE[ENREGS] = {
    {1, 1234u, 42u},
    {2, 70000u, 7u},
    {255, 0u, 65535u},
};

static int ecrire_enregistrement(FILE *flux, const Enregistrement *enr) {
    const uint8_t octets[ENREG_OCTETS] = {
        enr->type,
        (uint8_t)(enr->identifiant >> 24), (uint8_t)(enr->identifiant >> 16),
        (uint8_t)(enr->identifiant >> 8),  (uint8_t)enr->identifiant,
        (uint8_t)(enr->points >> 8),       (uint8_t)enr->points,
    };
    return fwrite(octets, 1, sizeof octets, flux) == sizeof octets;
}

static int lire_enregistrement(FILE *flux, Enregistrement *enr) {
    uint8_t octets[ENREG_OCTETS];
    if (fread(octets, 1, sizeof octets, flux) != sizeof octets) {
        return 0;
    }
    enr->type = octets[0];
    enr->identifiant = ((uint32_t)octets[1] << 24) | ((uint32_t)octets[2] << 16) |
                       ((uint32_t)octets[3] << 8) | (uint32_t)octets[4];
    enr->points = (uint16_t)(((uint16_t)octets[5] << 8) | (uint16_t)octets[6]);
    return 1;
}

static FILE *ecrire_le_catalogue(void) {
    FILE *flux = tmpfile();
    if (flux == NULL) {
        return NULL;
    }
    for (int i = 0; i < ENREGS; i++) {
        if (!ecrire_enregistrement(flux, &CATALOGUE[i])) {
            fclose(flux);
            return NULL;
        }
    }
    if (fflush(flux) != 0) {
        fclose(flux);
        return NULL;
    }
    return flux;
}

int main(void) {
    VERIFIE(sizeof(Enregistrement) > ENREG_OCTETS,
            "en memoire la struct occupe plus que la somme de ses champs");

    FILE *flux = ecrire_le_catalogue();
    VERIFIE(flux != NULL, "le catalogue est ecrit dans un fichier temporaire");
    if (flux == NULL) {
        return BILAN();
    }

    VERIFIE_ENTIER(ftell(flux), (long)ENREGS * ENREG_OCTETS,
                   "le fichier pese exactement la taille annoncee par le format");

    rewind(flux);
    uint8_t premier[ENREG_OCTETS];
    VERIFIE_ENTIER(fread(premier, 1, sizeof premier, flux), sizeof premier,
                   "les premiers octets se relisent");
    VERIFIE_ENTIER(premier[0], 0x01, "octet 0 : le type");
    VERIFIE_ENTIER(premier[1], 0x00, "octet 1 : identifiant, poids fort");
    VERIFIE_ENTIER(premier[2], 0x00, "octet 2 : identifiant");
    VERIFIE_ENTIER(premier[3], 0x04, "octet 3 : identifiant");
    VERIFIE_ENTIER(premier[4], 0xD2, "octet 4 : identifiant, poids faible");
    VERIFIE_ENTIER(premier[5], 0x00, "octet 5 : points, poids fort");
    VERIFIE_ENTIER(premier[6], 0x2A, "octet 6 : points, poids faible");

    rewind(flux);
    for (int i = 0; i < ENREGS; i++) {
        Enregistrement relu = {0, 0, 0};
        VERIFIE(lire_enregistrement(flux, &relu), "un enregistrement se relit");
        VERIFIE_ENTIER(relu.type, CATALOGUE[i].type, "avec son type");
        VERIFIE_ENTIER(relu.identifiant, CATALOGUE[i].identifiant, "avec son identifiant");
        VERIFIE_ENTIER(relu.points, CATALOGUE[i].points, "avec ses points");
    }

    VERIFIE_ENTIER(ftell(flux), (long)ENREGS * ENREG_OCTETS,
                   "et la relecture s'arrete pile a la fin du dernier");

    fclose(flux);
    return BILAN();
}
