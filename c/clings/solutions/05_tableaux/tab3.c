#include "verif.h"

const int PAS_FINI = 0;

int main(void) {
    int nombres[10] = {0};
    int *curseur = nombres;

    VERIFIE_ENTIER(sizeof(nombres), 40, "le tableau fait 40 octets");
    VERIFIE_ENTIER(sizeof(nombres) / sizeof(nombres[0]), 10, "il a 10 cases");
    VERIFIE_ENTIER(sizeof(curseur), sizeof(void *), "le pointeur fait la taille d'une adresse");
    VERIFIE(curseur == &nombres[0], "un tableau se convertit en pointeur sur sa premiere case");
    return BILAN();
}
