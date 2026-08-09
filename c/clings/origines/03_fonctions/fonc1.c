#include "verif.h"

const int PAS_FINI = 1;

int main(void) {
    VERIFIE_ENTIER(carre(5), 25, "carre de 5");
    VERIFIE_ENTIER(carre(-3), 9, "carre de -3");
    return BILAN();
}

int carre(int n) {
    return n * n;
}
