#include "verif.h"

const int PAS_FINI = 0;

int main(void) {
    size_t taille = 2;
    int *nombres = suivi_malloc(taille * sizeof(int));
    VERIFIE(nombres != NULL, "premiere allocation");

    nombres[0] = 1;
    nombres[1] = 2;

    size_t nouvelle_taille = 4;
    int *agrandi = suivi_realloc(nombres, nouvelle_taille * sizeof(int));
    VERIFIE(agrandi != NULL, "agrandissement reussi");
    nombres = agrandi;

    nombres[2] = 3;
    nombres[3] = 4;

    VERIFIE_ENTIER(nombres[0], 1, "les anciennes valeurs sont conservees");
    VERIFIE_ENTIER(nombres[3], 4, "et les nouvelles sont ecrites");

    suivi_free(nombres);
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
