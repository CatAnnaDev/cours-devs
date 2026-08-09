#include "verif.h"

const int PAS_FINI = 0;

int somme_par_pointeur(const int *debut, size_t taille) {
    int total = 0;
    for (const int *curseur = debut; curseur < debut + taille; curseur++) {
        total += *curseur;
    }
    return total;
}

int main(void) {
    int nombres[4] = {1, 2, 3, 4};

    VERIFIE_ENTIER(somme_par_pointeur(nombres, 4), 10, "somme du tableau");
    VERIFIE_ENTIER(*(nombres + 2), 3, "nombres + 2 pointe sur le troisieme");
    VERIFIE_ENTIER((int)(sizeof(int)), 4, "un int fait 4 octets");
    return BILAN();
}
