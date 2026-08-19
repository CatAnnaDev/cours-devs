#include "verif.h"

const int PAS_FINI = 0;

#define DOUBLE(x) ((x) * 2)

int main(void) {
    VERIFIE_ENTIER(DOUBLE(5), 10, "le cas facile");
    VERIFIE_ENTIER(DOUBLE(1 + 2), 6, "le cas qui casse");
    VERIFIE_ENTIER(DOUBLE(2) * 3, 12, "le resultat sert dans un calcul plus grand");
    return BILAN();
}
