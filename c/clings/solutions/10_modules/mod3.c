#include "config.h"
#include "verif.h"

const int PAS_FINI = 0;

int main(void) {
    VERIFIE_ENTIER(niveau_verbosite, 1, "la verbosite part a un");

    config_augmenter();
    VERIFIE_ENTIER(niveau_verbosite, 2, "config.c ecrit dans la variable que mod3.c lit");

    config_augmenter();
    VERIFIE_ENTIER(niveau_verbosite, 3, "les increments s'accumulent");

    niveau_verbosite = 10;
    config_augmenter();
    VERIFIE_ENTIER(niveau_verbosite, 11, "config.c lit la valeur que mod3.c a posee");
    return BILAN();
}
