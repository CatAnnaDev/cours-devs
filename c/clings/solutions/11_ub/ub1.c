#include "verif.h"

const int PAS_FINI = 0;

unsigned long long puissance_de_deux(int exposant) {
    return 1ULL << exposant;
}

int main(void) {
    VERIFIE_ENTIER(puissance_de_deux(3), 8LL, "deux puissance 3 vaut 8");
    VERIFIE_ENTIER(puissance_de_deux(30), 1073741824LL, "deux puissance 30 tient dans un int");
    VERIFIE_ENTIER(puissance_de_deux(32), 4294967296LL, "deux puissance 32 deborde de l'int");
    VERIFIE_ENTIER(puissance_de_deux(40), 1099511627776LL, "deux puissance 40 aussi");
    return BILAN();
}
