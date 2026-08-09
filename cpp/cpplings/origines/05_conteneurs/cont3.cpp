#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

int main() {
    std::vector<verif::Sonde> sondes;
    sondes.reserve(3);

    verif::Compteur::remettre_a_zero();

    for (int i = 0; i < 3; i++) {
        sondes.push_back(verif::Sonde(i));
    }

    VERIFIE_ENTIER(sondes.size(), 3, "trois elements");
    VERIFIE_ENTIER(verif::Compteur::constructions, 3, "trois constructions");
    VERIFIE_ENTIER(verif::Compteur::copies, 0, "aucune copie");
    VERIFIE_ENTIER(verif::Compteur::deplacements, 0, "aucun deplacement");
    return BILAN();
}
