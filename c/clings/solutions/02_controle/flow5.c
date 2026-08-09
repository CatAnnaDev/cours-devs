#include "verif.h"

const int PAS_FINI = 0;

int lire_bit(unsigned valeur, int position) {
    return (int)((valeur >> position) & 1u);
}

unsigned allumer_bit(unsigned valeur, int position) {
    return valeur | (1u << position);
}

unsigned eteindre_bit(unsigned valeur, int position) {
    return valeur & ~(1u << position);
}

int main(void) {
    VERIFIE_ENTIER(lire_bit(0b1010u, 1), 1, "le bit 1 de 1010 vaut 1");
    VERIFIE_ENTIER(lire_bit(0b1010u, 2), 0, "le bit 2 de 1010 vaut 0");
    VERIFIE_ENTIER(allumer_bit(0b1010u, 0), 0b1011u, "allumer le bit 0");
    VERIFIE_ENTIER(eteindre_bit(0b1010u, 3), 0b0010u, "eteindre le bit 3");
    return BILAN();
}
