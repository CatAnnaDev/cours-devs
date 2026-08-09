#include "verif.h"

const int PAS_FINI = 0;

const char *categorie(char c) {
    switch (c) {
        case 'a':
        case 'e':
        case 'i':
        case 'o':
        case 'u':
            return "voyelle";
        case '0':
        case '1':
        case '2':
            return "chiffre";
        default:
            return "autre";
    }
}

int main(void) {
    VERIFIE_TEXTE(categorie('e'), "voyelle", "e est une voyelle");
    VERIFIE_TEXTE(categorie('1'), "chiffre", "1 est un chiffre");
    VERIFIE_TEXTE(categorie('z'), "autre", "z est autre chose");
    return BILAN();
}
