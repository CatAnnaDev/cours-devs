#include "verif.h"

const int PAS_FINI = 0;

int main(void) {
    VERIFIE_ENTIER(sizeof(char), 1, "un char fait 1 octet");
    VERIFIE_ENTIER(sizeof(short), 2, "un short fait 2 octets");
    VERIFIE_ENTIER(sizeof(int), 4, "un int fait 4 octets");
    VERIFIE_ENTIER(sizeof(long), 8, "un long fait 8 octets");
    return BILAN();
}
