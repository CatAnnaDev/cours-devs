#include "verif.h"

const int PAS_FINI = 0;

int main(void) {
    double somme = 0.1 + 0.2;

    VERIFIE_PROCHE(somme, 0.3, 1e-9, "0.1 + 0.2 vaut a peu pres 0.3");
    VERIFIE(somme != 0.3, "mais pas exactement");
    return BILAN();
}
