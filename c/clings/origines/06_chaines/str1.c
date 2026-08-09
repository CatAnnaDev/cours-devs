#include "verif.h"

const int PAS_FINI = 1;

int main(void) {
    char mot[4] = {'a', 'b', 'c', 'd'};

    VERIFIE_ENTIER(strlen(mot), 3, "strlen compte 3 caracteres");
    VERIFIE_ENTIER(sizeof(mot), 4, "le tableau fait 4 octets");
    VERIFIE_ENTIER(mot[3], 0, "la derniere case est le zero terminal");
    return BILAN();
}
