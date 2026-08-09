#include "verif.h"

const int PAS_FINI = 0;

int main(void) {
    int nombres[4] = {10, 20, 30, 40};

    nombres[3] = 99;

    VERIFIE_ENTIER(nombres[3], 99, "la derniere case vaut 99");
    return BILAN();
}
