#include "verif.h"

const int PAS_FINI = 0;

#define TAILLE 4
#define NOM "clings"

int main(void) {
    int tableau[TAILLE] = {1, 2, 3, 4};

    int total = 0;
    for (int i = 0; i < TAILLE; i++) {
        total += tableau[i];
    }

    VERIFIE_ENTIER(total, 10, "la somme des cases vaut 10");
    VERIFIE_ENTIER(sizeof tableau / sizeof tableau[0], TAILLE, "le tableau a bien TAILLE cases");
    VERIFIE_TEXTE(NOM, "clings", "une macro peut aussi porter du texte");
    return BILAN();
}
