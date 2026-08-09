#include "verif.h"

const int PAS_FINI = 1;

int main(void) {
    VERIFIE_ENTIER(sizeof(char), 1, "un char fait 1 octet");
    VERIFIE_ENTIER(sizeof(short), 2, "un short fait 2 octets");
    VERIFIE_ENTIER(sizeof(int), A_FAIRE, "un int fait 4 octets");
    VERIFIE_ENTIER(sizeof(long), A_FAIRE, "un long fait 8 octets");
    return BILAN();
}
