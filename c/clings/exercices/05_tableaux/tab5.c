#include "verif.h"

const int PAS_FINI = 1;

size_t chercher(const int *valeurs, size_t taille, int cible) {
    for (size_t i = 0; i < taille; i++) {
        if (valeurs[i] == cible) {
            return i;
        }
    }
    return -1;
}

int main(void) {
    int nombres[5] = {4, 8, 15, 16, 23};

    VERIFIE_ENTIER(chercher(nombres, 5, 15), 2, "15 est a l'indice 2");
    VERIFIE_ENTIER(chercher(nombres, 5, 4), 0, "4 est a l'indice 0");
    VERIFIE_ENTIER(chercher(nombres, 5, 99), -1, "99 est absent");
    return BILAN();
}
