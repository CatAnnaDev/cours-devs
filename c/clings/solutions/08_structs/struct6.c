#include "verif.h"

const int PAS_FINI = 0;

typedef struct {
    unsigned int visible : 1;
    unsigned int calque : 3;
    unsigned int teinte : 4;
} Drapeaux;

int main(void) {
    Drapeaux drapeaux = {.visible = 1, .calque = 5, .teinte = 12};

    VERIFIE_ENTIER(drapeaux.visible, 1, "un seul bit suffit pour visible");
    VERIFIE_ENTIER(drapeaux.calque, 5, "calque doit pouvoir monter jusqu'a 7");
    VERIFIE_ENTIER(drapeaux.teinte, 12, "teinte doit pouvoir monter jusqu'a 15");
    VERIFIE_ENTIER(sizeof(Drapeaux), 4, "les trois champs logent dans un seul unsigned int");
    return BILAN();
}
