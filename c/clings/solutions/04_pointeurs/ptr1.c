#include "verif.h"

const int PAS_FINI = 0;

void ecrire_dans(int *cible, int valeur) {
    *cible = valeur;
}

int main(void) {
    int nombre = 0;
    int *adresse = &nombre;

    VERIFIE(adresse == &nombre, "adresse pointe bien sur nombre");
    VERIFIE_ENTIER(*adresse, 0, "on lit 0 a travers le pointeur");

    ecrire_dans(&nombre, 42);
    VERIFIE_ENTIER(nombre, 42, "nombre a ete modifie");
    return BILAN();
}
