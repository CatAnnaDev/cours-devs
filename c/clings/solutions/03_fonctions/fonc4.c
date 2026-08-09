#include "verif.h"

const int PAS_FINI = 0;

int somme_tableau(const int *valeurs, size_t taille) {
    int total = 0;
    for (size_t i = 0; i < taille; i++) {
        total += valeurs[i];
    }
    return total;
}

int main(void) {
    int nombres[5] = {1, 2, 3, 4, 5};
    size_t taille = sizeof(nombres) / sizeof(nombres[0]);

    VERIFIE_ENTIER(taille, 5, "le tableau a 5 cases");
    VERIFIE_ENTIER(somme_tableau(nombres, taille), 15, "somme du tableau");
    return BILAN();
}
