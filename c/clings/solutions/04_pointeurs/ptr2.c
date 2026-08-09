#include "verif.h"

const int PAS_FINI = 0;

int longueur_ou_zero(const char *texte) {
    if (texte == NULL) {
        return 0;
    }
    return (int)strlen(texte);
}

int main(void) {
    VERIFIE_ENTIER(longueur_ou_zero("salut"), 5, "longueur de salut");
    VERIFIE_ENTIER(longueur_ou_zero(NULL), 0, "NULL renvoie 0 sans planter");
    return BILAN();
}
