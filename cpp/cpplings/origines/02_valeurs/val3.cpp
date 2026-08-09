#include <utility>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

int main() {
    std::vector<verif::Sonde> sondes;
    sondes.reserve(2);

    verif::Sonde premiere(1);
    verif::Compteur::remettre_a_zero();

    sondes.push_back(premiere);

    VERIFIE_ENTIER(verif::Compteur::copies, 0, "aucune copie");
    VERIFIE_ENTIER(verif::Compteur::deplacements, 1, "un deplacement");
    VERIFIE_ENTIER(sondes[0].valeur, 1, "la valeur est arrivee");
    VERIFIE_ENTIER(premiere.valeur, -1, "la source a ete pillee");
    return BILAN();
}
