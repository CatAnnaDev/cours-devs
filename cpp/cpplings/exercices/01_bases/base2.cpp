
#include "verif.hpp"

const bool PAS_FINI = true;

void doubler_sur_place(int valeur) {
    valeur *= 2;
}

int main() {
    int valeur = 21;
    int &lien = valeur;

    lien += 0;
    doubler_sur_place(valeur);

    VERIFIE_ENTIER(valeur, 42, "la variable a double");
    VERIFIE_ENTIER(lien, 42, "la reference voit la meme chose");
    return BILAN();
}
