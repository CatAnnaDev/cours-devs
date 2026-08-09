#include "verif.h"

const int PAS_FINI = 1;

size_t ma_longueur(const char *texte) {
    size_t longueur = 0;
    while (texte[longueur] != '\0') {
        longueur++;
    }
    return longueur - 1;
}

int main(void) {
    VERIFIE_ENTIER(ma_longueur(""), 0, "chaine vide");
    VERIFIE_ENTIER(ma_longueur("bonjour"), 7, "bonjour fait 7");
    VERIFIE_ENTIER(ma_longueur("a b"), 3, "les espaces comptent");
    return BILAN();
}
