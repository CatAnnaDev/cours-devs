#include "compteur.h"
#include "verif.h"

const int PAS_FINI = 0;

int main(void) {
    VERIFIE_ENTIER(compteur_incrementer(41), 42, "l'appel traverse bien vers compteur.c");
    VERIFIE_ENTIER(compteur_incrementer(0), 1, "zero devient un");
    VERIFIE_ENTIER(compteur_incrementer(-1), 0, "moins un remonte a zero");
    return BILAN();
}
