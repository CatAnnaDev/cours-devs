#include "verif.h"

const int PAS_FINI = 0;

int identiques(const char *a, const char *b) {
    return strcmp(a, b) == 0;
}

int main(void) {
    char premier[8];
    snprintf(premier, sizeof premier, "salut");

    VERIFIE(identiques(premier, "salut"), "meme contenu");
    VERIFIE(!identiques(premier, "Salut"), "la casse compte");
    VERIFIE(!identiques(premier, "salu"), "la longueur compte");
    return BILAN();
}
