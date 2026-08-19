#include "verif.h"

const int PAS_FINI = 1;

#define VERSION 3
#define COLLER(a, b) a##b
#define TEXTE(x) #x

int main(void) {
    int niveau_2 = 20;

    VERIFIE_ENTIER(COLLER(niveau_, 2), 20, "## colle deux jetons en un seul nom");
    VERIFIE_TEXTE(TEXTE(niveau), "niveau", "# met un jeton entre guillemets");
    VERIFIE_TEXTE(TEXTE(VERSION), "3", "# doit voir la valeur derriere le nom");

#if VERSION > 3
    VERIFIE(1, "la branche recente est compilee");
#else
    VERIFIE(0, "la vieille branche ne doit pas etre compilee");
#endif

    return BILAN();
}
