#include "verif.h"

const int PAS_FINI = 0;

long long produit(int a, int b) {
    return (long long)a * b;
}

int main(void) {
    VERIFIE_ENTIER(produit(2, 3), 6, "petit produit");
    VERIFIE_ENTIER(produit(100000, 100000), 10000000000LL, "grand produit");
    return BILAN();
}
