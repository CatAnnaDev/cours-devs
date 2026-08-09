#include "verif.h"

const int PAS_FINI = 0;

void echanger(int *a, int *b) {
    int temporaire = *a;
    *a = *b;
    *b = temporaire;
}

int main(void) {
    int gauche = 1;
    int droite = 2;

    echanger(&gauche, &droite);

    VERIFIE_ENTIER(gauche, 2, "gauche vaut maintenant 2");
    VERIFIE_ENTIER(droite, 1, "droite vaut maintenant 1");
    return BILAN();
}
