#include "verif.h"

const int PAS_FINI = 1;

int main(void) {
    int *bloc = suivi_malloc(sizeof(int));
    VERIFIE(bloc != NULL, "l'allocation a reussi");

    *bloc = 7;
    VERIFIE_ENTIER(*bloc, 7, "on relit 7");

    suivi_free(bloc);
    free(bloc);

    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
