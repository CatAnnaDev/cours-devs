#include "verif.h"

const int PAS_FINI = 0;

int assembler(char *tampon, size_t taille, const char *nom, int score) {
    int voulu = snprintf(tampon, taille, "%s:%d", nom, score);
    return voulu >= 0 && (size_t)voulu < taille;
}

int main(void) {
    char petit[6];
    char grand[32];

    VERIFIE(assembler(grand, sizeof grand, "anna", 42), "ca tient dans 32 octets");
    VERIFIE_TEXTE(grand, "anna:42", "le texte assemble");
    VERIFIE(!assembler(petit, sizeof petit, "anna", 42), "ca ne tient pas dans 6 octets");
    return BILAN();
}
