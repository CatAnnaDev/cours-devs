#include "verif.h"

const int PAS_FINI = 0;

void construire_message(char *tampon, size_t taille, int score) {
    snprintf(tampon, taille, "score : %d", score);
}

int main(void) {
    char message[32];
    construire_message(message, sizeof message, 42);

    VERIFIE_TEXTE(message, "score : 42", "le message est correct");
    return BILAN();
}
