#include "verif.h"

const int PAS_FINI = 0;

int maximum(const int *valeurs, size_t taille) {
    int record = valeurs[0];
    for (size_t i = 1; i < taille; i++) {
        if (valeurs[i] > record) {
            record = valeurs[i];
        }
    }
    return record;
}

int main(void) {
    const int nombres[5] = {3, 9, 2, 7, 4};

    VERIFIE_ENTIER(maximum(nombres, 5), 9, "le maximum vaut 9");
    return BILAN();
}
