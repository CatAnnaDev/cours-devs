#include "verif.h"

const int PAS_FINI = 0;

int indice_valide(int indice, size_t taille) {
    return indice >= 0 && (size_t)indice < taille;
}

int main(void) {
    VERIFIE(indice_valide(0, 4), "0 est valide dans un tableau de 4");
    VERIFIE(indice_valide(3, 4), "3 est valide dans un tableau de 4");
    VERIFIE(!indice_valide(4, 4), "4 est hors bornes");
    VERIFIE(!indice_valide(-1, 4), "-1 est hors bornes");
    return BILAN();
}
