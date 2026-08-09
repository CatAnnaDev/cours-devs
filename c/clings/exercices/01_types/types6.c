#include "verif.h"

const int PAS_FINI = 1;

double moyenne(int somme, int nombre) {
    return somme / nombre;
}

int main(void) {
    VERIFIE_REEL(moyenne(7, 2), 3.5, "7 / 2 vaut 3.5");
    VERIFIE_REEL(moyenne(10, 4), 2.5, "10 / 4 vaut 2.5");
    return BILAN();
}
