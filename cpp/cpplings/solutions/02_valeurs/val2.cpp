#include <utility>

#include "verif.hpp"

const bool PAS_FINI = false;

verif::Sonde fabriquer(int valeur) {
    verif::Sonde locale(valeur);
    return locale;
}

int main() {
    verif::Compteur::remettre_a_zero();

    verif::Sonde resultat = fabriquer(7);

    VERIFIE_ENTIER(resultat.valeur, 7, "la valeur est correcte");
    VERIFIE_ENTIER(verif::Compteur::copies, 0, "aucune copie");
    VERIFIE_ENTIER(verif::Compteur::deplacements, 0, "aucun deplacement non plus");
    return BILAN();
}
