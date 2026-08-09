#include "verif.h"

const int PAS_FINI = 1;

int main(void) {
    double somme = 0.1 + 0.2;

    VERIFIE(somme == 0.3, "0.1 + 0.2 vaut a peu pres 0.3");
    VERIFIE(somme != 0.3, "mais pas exactement");
    return BILAN();
}
