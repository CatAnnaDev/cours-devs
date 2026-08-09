#include "verif.h"

const int PAS_FINI = 0;

int somme(const int *valeurs, size_t taille) {
    int total = 0;
    for (size_t i = 0; i < taille; i++) {
        total += valeurs[i];
    }
    return total;
}

int main(void) {
    int nombres[3] = {10, 20, 30};

    VERIFIE_ENTIER(somme(nombres, 3), 60, "somme des trois");
    return BILAN();
}
