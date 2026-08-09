#include "verif.h"

const int PAS_FINI = 0;

int doubler(int n) {
    return n * 2;
}

int negatif(int n) {
    return -n;
}

int appliquer(int valeur, int (*operation)(int)) {
    return operation(valeur);
}

int main(void) {
    VERIFIE_ENTIER(appliquer(21, doubler), 42, "appliquer doubler");
    VERIFIE_ENTIER(appliquer(7, negatif), -7, "appliquer negatif");
    return BILAN();
}
