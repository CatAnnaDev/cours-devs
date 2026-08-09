#include "verif.h"

const int PAS_FINI = 1;

int incrementer(int valeur) {
    valeur++;
    return valeur;
}

int main(void) {
    int compteur = 10;
    incrementer(compteur);

    VERIFIE_ENTIER(compteur, 11, "le compteur a bien avance");
    return BILAN();
}
