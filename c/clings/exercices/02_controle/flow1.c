#include "verif.h"

const int PAS_FINI = 1;

const char *mention(int note) {
    if (note >= 14) {
        return "bien";
    }
    if (note >= 10) {
        return "passable";
    }
    return "insuffisant";
}

int main(void) {
    VERIFIE_TEXTE(mention(18), "tres bien", "18 : tres bien");
    VERIFIE_TEXTE(mention(15), "bien", "15 : bien");
    VERIFIE_TEXTE(mention(11), "passable", "11 : passable");
    VERIFIE_TEXTE(mention(4), "insuffisant", "4 : insuffisant");
    return BILAN();
}
