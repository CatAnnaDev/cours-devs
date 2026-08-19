#include "verif.h"

const int PAS_FINI = 1;

#define MAX(a, b) ((a) > (b) ? (a) : (b))

static int suivant(int *compteur) {
    *compteur += 1;
    return *compteur;
}

int main(void) {
    int compteur = 0;
    int plus_grand = MAX(suivant(&compteur), 0);

    VERIFIE_ENTIER(compteur, 1, "l'argument n'a ete evalue qu'une fois");
    VERIFIE_ENTIER(plus_grand, 1, "le plus grand des deux vaut 1");
    return BILAN();
}
