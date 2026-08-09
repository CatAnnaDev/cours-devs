#include "verif.h"

const int PAS_FINI = 0;

void copier_sur(char *destination, size_t taille, const char *source) {
    strncpy(destination, source, taille - 1);
    destination[taille - 1] = '\0';
}

int main(void) {
    char tampon[6];

    copier_sur(tampon, sizeof tampon, "abcdefgh");

    VERIFIE_ENTIER(strlen(tampon), 5, "la copie fait 5 caracteres");
    VERIFIE_TEXTE(tampon, "abcde", "et contient le debut de la source");
    return BILAN();
}
