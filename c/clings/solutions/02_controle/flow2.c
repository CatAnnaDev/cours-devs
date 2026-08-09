#include "verif.h"

const int PAS_FINI = 0;

int somme_jusqua(int n) {
    int total = 0;
    for (int i = 1; i <= n; i++) {
        total += i;
    }
    return total;
}

int main(void) {
    VERIFIE_ENTIER(somme_jusqua(1), 1, "somme jusqu'a 1");
    VERIFIE_ENTIER(somme_jusqua(5), 15, "somme jusqu'a 5");
    VERIFIE_ENTIER(somme_jusqua(100), 5050, "somme jusqu'a 100");
    return BILAN();
}
