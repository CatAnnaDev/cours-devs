#include "verif.h"

const int PAS_FINI = 0;

int main(void) {
    size_t taille = 5;
    int *nombres = suivi_malloc(taille * sizeof(int));

    VERIFIE(nombres != NULL, "l'allocation a reussi");

    for (size_t i = 0; i < taille; i++) {
        nombres[i] = (int)i * 10;
    }

    VERIFIE_ENTIER(nombres[4], 40, "la derniere case vaut 40");

    suivi_free(nombres);
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
