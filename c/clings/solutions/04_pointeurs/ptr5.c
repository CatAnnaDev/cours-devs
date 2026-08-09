#include "verif.h"

const int PAS_FINI = 0;

void faire_pointer_sur(int **cible, int *nouvelle_cible) {
    *cible = nouvelle_cible;
}

int main(void) {
    int a = 1;
    int b = 2;
    int *curseur = &a;

    VERIFIE_ENTIER(*curseur, 1, "curseur pointe sur a");
    faire_pointer_sur(&curseur, &b);
    VERIFIE_ENTIER(*curseur, 2, "curseur pointe maintenant sur b");
    return BILAN();
}
