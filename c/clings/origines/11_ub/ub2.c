#include "verif.h"
#include <limits.h>

const int PAS_FINI = 1;

int division_sure(int a, int b) {
    return a / b;
}

int modulo_sur(int a, int b) {
    return a % b;
}

int main(void) {
    VERIFIE_ENTIER(division_sure(7, 2), 3, "sept divise par deux fait trois");
    VERIFIE_ENTIER(modulo_sur(7, 2), 1, "et il reste un");
    VERIFIE_ENTIER(division_sure(INT_MIN, -1), INT_MAX, "le quotient impossible est sature");
    VERIFIE_ENTIER(modulo_sur(INT_MIN, -1), 0, "le reste de cette division vaut zero");
    VERIFIE_ENTIER(division_sure(7, 0), 0, "diviser par zero rend zero par convention");
    VERIFIE_ENTIER(modulo_sur(7, 0), 0, "le reste par zero aussi");
    return BILAN();
}
