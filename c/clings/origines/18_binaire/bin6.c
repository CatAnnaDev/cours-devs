#include "verif.h"
#include <stdint.h>

const int PAS_FINI = 1;

#define REEL_OCTETS 8

_Static_assert(sizeof(double) == REEL_OCTETS, "ce format suppose un double sur huit octets");

static void ecrire_reel_gros_boutiste(uint8_t *sortie, double valeur) {
    uint64_t bits = *(const uint64_t *)&valeur;
    memcpy(sortie, &bits, sizeof bits);
}

static double lire_reel_gros_boutiste(const uint8_t *octets) {
    uint64_t bits;
    memcpy(&bits, octets, sizeof bits);
    return *(const double *)&bits;
}

static int aller_retour_par_fichier(double valeur, double *relu) {
    uint8_t tampon[REEL_OCTETS];
    FILE *flux = tmpfile();
    if (flux == NULL) {
        return 0;
    }
    ecrire_reel_gros_boutiste(tampon, valeur);
    int ok = fwrite(tampon, 1, sizeof tampon, flux) == sizeof tampon && fflush(flux) == 0;
    rewind(flux);
    memset(tampon, 0, sizeof tampon);
    ok = ok && fread(tampon, 1, sizeof tampon, flux) == sizeof tampon;
    fclose(flux);
    *relu = lire_reel_gros_boutiste(tampon);
    return ok;
}

int main(void) {
    static const uint8_t UN_VIRGULE_CINQ[REEL_OCTETS] = {0x3F, 0xF8, 0, 0, 0, 0, 0, 0};
    static const uint8_t PI[REEL_OCTETS] = {0x40, 0x09, 0x21, 0xFB, 0x54, 0x44, 0x2D, 0x18};
    uint8_t tampon[REEL_OCTETS];

    ecrire_reel_gros_boutiste(tampon, 1.5);
    VERIFIE(memcmp(tampon, UN_VIRGULE_CINQ, REEL_OCTETS) == 0,
            "1,5 s'ecrit 3F F8 00 00 00 00 00 00, exposant en tete");
    VERIFIE_ENTIER(tampon[0], 0x3F, "l'octet de poids fort porte le signe et l'exposant");

    ecrire_reel_gros_boutiste(tampon, -2.0);
    VERIFIE_ENTIER(tampon[0], 0xC0, "le bit de signe est le premier bit du premier octet");

    ecrire_reel_gros_boutiste(tampon, 0.0);
    static const uint8_t HUIT_ZEROS[REEL_OCTETS] = {0, 0, 0, 0, 0, 0, 0, 0};
    VERIFIE(memcmp(tampon, HUIT_ZEROS, REEL_OCTETS) == 0, "le zero positif est huit octets nuls");

    VERIFIE_REEL(lire_reel_gros_boutiste(PI), 3.14159265358979,
                 "un reel venu d'ailleurs se relit");

    double relu = 0.0;
    VERIFIE(aller_retour_par_fichier(0.1, &relu), "un dixieme fait l'aller-retour");
    VERIFIE(relu == 0.1, "et revient bit pour bit, sans arrondi supplementaire");

    VERIFIE(aller_retour_par_fichier(-0.0, &relu), "le zero negatif fait l'aller-retour");
    VERIFIE(relu == 0.0 && signbit(relu), "et garde son signe, que la comparaison ignore");

    VERIFIE(aller_retour_par_fichier(1e300, &relu), "un tres grand nombre fait l'aller-retour");
    VERIFIE(relu == 1e300, "sans perdre son exposant");

    VERIFIE(aller_retour_par_fichier(HUGE_VAL, &relu), "l'infini fait l'aller-retour");
    VERIFIE(isinf(relu) && relu > 0.0, "et reste l'infini positif");

    VERIFIE(aller_retour_par_fichier(nan(""), &relu), "ce qui n'est pas un nombre passe aussi");
    VERIFIE(isnan(relu), "et reste un non-nombre, sans qu'on promette rien de sa charge utile");

    return BILAN();
}
