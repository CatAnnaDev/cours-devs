#include "verif.h"

const int PAS_FINI = 0;

int main(void) {
    int *bloc = suivi_malloc(4 * sizeof(int));
    VERIFIE(bloc != NULL, "l'allocation a reussi");

    bloc[0] = 1;
    bloc[3] = 4;

    int premier = bloc[0];
    int dernier = bloc[3];

    suivi_free(bloc);

    VERIFIE_ENTIER(premier, 1, "premier element");
    VERIFIE_ENTIER(dernier, 4, "dernier element");
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
