#include "verif.h"

const int PAS_FINI = 1;

int compter_mots(const char *phrase) {
    int mots = 0;
    int dans_un_mot = 0;

    for (const char *curseur = phrase; *curseur != '\0'; curseur++) {
        if (*curseur == ' ') {
            dans_un_mot = 0;
        } else {
            mots++;
        }
    }
    return mots;
}

int main(void) {
    VERIFIE_ENTIER(compter_mots(""), 0, "phrase vide");
    VERIFIE_ENTIER(compter_mots("un"), 1, "un seul mot");
    VERIFIE_ENTIER(compter_mots("un deux trois"), 3, "trois mots");
    VERIFIE_ENTIER(compter_mots("  espaces   partout  "), 2, "les espaces multiples ne comptent pas");
    return BILAN();
}
