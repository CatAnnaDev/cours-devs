#include "verif.h"
#include <stdint.h>

const int PAS_FINI = 1;

#define U32_OCTETS 4

static void ecrire_u32_gros_boutiste(uint8_t *sortie, uint32_t valeur) {
    memcpy(sortie, &valeur, sizeof valeur);
}

static uint32_t lire_u32_gros_boutiste(const uint8_t *octets) {
    uint32_t valeur;
    memcpy(&valeur, octets, sizeof valeur);
    return valeur;
}

static int aller_retour_par_fichier(uint32_t valeur, uint32_t *relu) {
    uint8_t tampon[U32_OCTETS];
    FILE *flux = tmpfile();
    if (flux == NULL) {
        return 0;
    }
    ecrire_u32_gros_boutiste(tampon, valeur);
    int ok = fwrite(tampon, 1, sizeof tampon, flux) == sizeof tampon && fflush(flux) == 0;
    rewind(flux);
    memset(tampon, 0, sizeof tampon);
    ok = ok && fread(tampon, 1, sizeof tampon, flux) == sizeof tampon;
    fclose(flux);
    *relu = lire_u32_gros_boutiste(tampon);
    return ok;
}

int main(void) {
    uint8_t tampon[U32_OCTETS];

    ecrire_u32_gros_boutiste(tampon, 0x12345678u);
    VERIFIE_ENTIER(tampon[0], 0x12, "l'octet de poids fort part en premier");
    VERIFIE_ENTIER(tampon[1], 0x34, "puis le suivant");
    VERIFIE_ENTIER(tampon[2], 0x56, "puis le suivant");
    VERIFIE_ENTIER(tampon[3], 0x78, "et l'octet de poids faible ferme la marche");

    ecrire_u32_gros_boutiste(tampon, 1u);
    VERIFIE_ENTIER(tampon[0], 0x00, "un tient sur son dernier octet, pas sur le premier");
    VERIFIE_ENTIER(tampon[3], 0x01, "et c'est bien le dernier qui le porte");

    static const uint8_t VENU_D_AILLEURS[U32_OCTETS] = {0xDE, 0xAD, 0xBE, 0xEF};
    VERIFIE_ENTIER(lire_u32_gros_boutiste(VENU_D_AILLEURS), 0xDEADBEEFu,
                   "la relecture suit la meme convention que l'ecriture");

    uint32_t relu = 0;
    VERIFIE(aller_retour_par_fichier(0x0A0B0C0Du, &relu), "l'aller-retour par fichier a eu lieu");
    VERIFIE_ENTIER(relu, 0x0A0B0C0Du, "et rend la valeur de depart");

    VERIFIE(aller_retour_par_fichier(0u, &relu), "zero passe aussi par le fichier");
    VERIFIE_ENTIER(relu, 0u, "et vaut toujours zero");

    VERIFIE(aller_retour_par_fichier(0xFFFFFFFFu, &relu), "la valeur maximale aussi");
    VERIFIE_ENTIER(relu, 0xFFFFFFFFu, "et ne perd aucun bit");

    return BILAN();
}
