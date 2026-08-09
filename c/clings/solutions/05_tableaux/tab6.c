#include "verif.h"

const int PAS_FINI = 0;

int main(void) {
    int source[4] = {1, 2, 3, 4};
    int copie[4];

    memcpy(copie, source, sizeof source);
    copie[0] = 99;

    VERIFIE_ENTIER(copie[1], 2, "la copie contient les memes valeurs");
    VERIFIE_ENTIER(copie[0], 99, "on a modifie la copie");
    VERIFIE_ENTIER(source[0], 1, "la source n'a pas bouge");
    return BILAN();
}
