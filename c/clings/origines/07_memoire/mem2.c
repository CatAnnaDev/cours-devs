#include "verif.h"

const int PAS_FINI = 1;

int somme_des_carres(size_t taille) {
    int *tampon = suivi_malloc(taille * sizeof(int));
    if (tampon == NULL) {
        return -1;
    }

    for (size_t i = 0; i < taille; i++) {
        tampon[i] = (int)(i * i);
    }

    int total = 0;
    for (size_t i = 0; i < taille; i++) {
        total += tampon[i];
    }

    return total;
}

int main(void) {
    VERIFIE_ENTIER(somme_des_carres(4), 14, "0 + 1 + 4 + 9");
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
