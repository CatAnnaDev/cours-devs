#include "verif.h"
#include <stdint.h>

const int PAS_FINI = 1;

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
    return fwrite(enr, sizeof *enr, 1, flux) == 1;
}

static int lire_enregistrement(FILE *flux, Enregistrement *enr) {
    return fread(enr, sizeof *enr, 1, flux) == 1;
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
