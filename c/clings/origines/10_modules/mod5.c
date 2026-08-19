#include "couleur.h"
#include "verif.h"

const int PAS_FINI = 1;

int main(void) {
    VERIFIE_ENTIER(couleur_melanger(10, 20), 30, "le melange additionne ses deux teintes");
    VERIFIE_ENTIER(couleur_melanger(0, 0), 0, "deux teintes nulles donnent zero");
    VERIFIE_ENTIER(couleur_melanger(255, -55), 200, "le second argument compte vraiment");
    return BILAN();
}
