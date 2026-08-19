#include "verif.h"

const int PAS_FINI = 1;

_Bool drapeau_depuis_octet(unsigned char brut) {
    _Bool drapeau;
    memcpy(&drapeau, &brut, sizeof drapeau);
    return drapeau;
}

int main(void) {
    VERIFIE_ENTIER(drapeau_depuis_octet(0), 0, "l'octet 0 donne faux");
    VERIFIE_ENTIER(drapeau_depuis_octet(1), 1, "l'octet 1 donne vrai");
    VERIFIE_ENTIER(drapeau_depuis_octet(2), 1, "tout octet non nul donne vrai");
    VERIFIE_ENTIER(drapeau_depuis_octet(200), 1, "meme un octet plus lointain");
    return BILAN();
}
