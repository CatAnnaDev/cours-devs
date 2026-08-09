#include "verif.h"

const int PAS_FINI = 1;

char en_majuscule(char c) {
    if (c >= 'a' && c <= 'z') {
        return c;
    }
    return c;
}

int main(void) {
    VERIFIE_ENTIER('A', 65, "'A' vaut 65");
    VERIFIE_ENTIER(en_majuscule('c'), 'C', "c devient C");
    VERIFIE_ENTIER(en_majuscule('Z'), 'Z', "Z reste Z");
    VERIFIE_ENTIER(en_majuscule('7'), '7', "un chiffre ne change pas");
    return BILAN();
}
