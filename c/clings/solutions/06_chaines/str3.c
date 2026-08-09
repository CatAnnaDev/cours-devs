#include "verif.h"

const int PAS_FINI = 0;

int main(void) {
    char petit[8];
    const char *long_texte = "un texte beaucoup trop long";

    snprintf(petit, sizeof petit, "%s", long_texte);

    VERIFIE_ENTIER(strlen(petit), 7, "la copie est tronquee a 7 caracteres");
    VERIFIE_ENTIER(petit[7], 0, "et reste terminee par un zero");
    return BILAN();
}
