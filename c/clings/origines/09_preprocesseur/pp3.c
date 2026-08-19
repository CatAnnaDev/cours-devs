#include "verif.h"

const int PAS_FINI = 1;

#define SOMME(a, b) (a) + (b)

int main(void) {
    VERIFIE_ENTIER(SOMME(1, 2), 3, "seule, la macro a l'air juste");
    VERIFIE_ENTIER(SOMME(1, 2) * 3, 9, "multipliee, la macro derape");
    VERIFIE_ENTIER(-SOMME(1, 2), -3, "niee, la macro derape aussi");
    return BILAN();
}
