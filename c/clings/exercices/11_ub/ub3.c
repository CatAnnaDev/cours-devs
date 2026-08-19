#include "verif.h"
#include <limits.h>

const int PAS_FINI = 1;

int vers_entier(double valeur) {
    return (int)valeur;
}

int main(void) {
    VERIFIE_ENTIER(vers_entier(3.9), 3, "la partie decimale est jetee");
    VERIFIE_ENTIER(vers_entier(-2.5), -2, "la troncature ramene vers zero");
    VERIFIE_ENTIER(vers_entier(1e20), INT_MAX, "au dela du maximum on sature");
    VERIFIE_ENTIER(vers_entier(-1e20), INT_MIN, "en dessous du minimum aussi");
    VERIFIE_ENTIER(vers_entier(nan("")), 0, "ce qui n'est pas un nombre devient zero");
    return BILAN();
}
