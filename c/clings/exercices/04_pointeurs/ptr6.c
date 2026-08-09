#include "verif.h"

const int PAS_FINI = 1;

char *construire_message(int score) {
    char tampon[32];
    snprintf(tampon, sizeof tampon, "score : %d", score);
    return tampon;
}

int main(void) {
    char *message = construire_message(42);

    VERIFIE_TEXTE(message, "score : 42", "le message est correct");
    return BILAN();
}
