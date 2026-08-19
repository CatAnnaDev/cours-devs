#include "verif.h"

const int PAS_FINI = 0;

#define REMETTRE_A_ZERO(a, b) \
    do {                      \
        (a) = 0;              \
        (b) = 0;              \
    } while (0)

int main(void) {
    int x = 5;
    int y = 7;
    int condition = 0;

    if (condition)
        REMETTRE_A_ZERO(x, y);

    VERIFIE_ENTIER(x, 5, "x garde sa valeur");
    VERIFIE_ENTIER(y, 7, "y garde sa valeur");
    return BILAN();
}
