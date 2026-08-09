#include "verif.h"

const int PAS_FINI = 1;

int divisions_par_deux(int n) {
    int compte = 0;
    while (n > 1) {
        compte++;
    }
    return compte;
}

int main(void) {
    VERIFIE_ENTIER(divisions_par_deux(1), 0, "1 : aucune division");
    VERIFIE_ENTIER(divisions_par_deux(8), 3, "8 : trois divisions");
    VERIFIE_ENTIER(divisions_par_deux(1000), 9, "1000 : neuf divisions");
    return BILAN();
}
