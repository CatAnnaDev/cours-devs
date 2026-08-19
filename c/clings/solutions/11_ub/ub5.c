#include "verif.h"

const int PAS_FINI = 0;

const char *categorie(int age) {
    if (age < 13) {
        return "enfant";
    }
    if (age < 20) {
        return "adolescent";
    }
    return "adulte";
}

int main(void) {
    VERIFIE_TEXTE(categorie(8), "enfant", "huit ans donne enfant");
    VERIFIE_TEXTE(categorie(15), "adolescent", "quinze ans donne adolescent");
    VERIFIE_TEXTE(categorie(40), "adulte", "quarante ans donne adulte");
    return BILAN();
}
