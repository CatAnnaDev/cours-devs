#include "verif.h"

const int PAS_FINI = 1;

int main(void) {
    size_t taille = 8;
    int *compteurs = suivi_malloc(taille * sizeof(int));
    VERIFIE(compteurs != NULL, "l'allocation a reussi");

    int total = 0;
    for (size_t i = 0; i < taille; i++) {
        total += compteurs[i];
    }

    VERIFIE_ENTIER(total, 0, "tout est initialise a zero");

    compteurs[3] += 5;
    VERIFIE_ENTIER(compteurs[3], 5, "on peut incrementer sans initialiser");

    suivi_free(compteurs);
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
