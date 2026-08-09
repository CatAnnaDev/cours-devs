#include "verif.h"

const int PAS_FINI = 0;

void renverser(int *valeurs, size_t taille) {
    for (size_t i = 0; i < taille / 2; i++) {
        int temporaire = valeurs[i];
        valeurs[i] = valeurs[taille - 1 - i];
        valeurs[taille - 1 - i] = temporaire;
    }
}

int main(void) {
    int nombres[5] = {1, 2, 3, 4, 5};
    renverser(nombres, 5);

    VERIFIE_ENTIER(nombres[0], 5, "premier devenu dernier");
    VERIFIE_ENTIER(nombres[2], 3, "le milieu ne bouge pas");
    VERIFIE_ENTIER(nombres[4], 1, "dernier devenu premier");
    return BILAN();
}
