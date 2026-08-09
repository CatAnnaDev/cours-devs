#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

int main() {
    std::vector<verif::Sonde> sondes;
    sondes.reserve(3);
    sondes.emplace_back(1);
    sondes.emplace_back(2);
    sondes.emplace_back(3);

    verif::Compteur::remettre_a_zero();

    int total = 0;
    for (const auto &sonde : sondes) {
        total += sonde.valeur;
    }

    VERIFIE_ENTIER(total, 6, "la somme est correcte");
    VERIFIE_ENTIER(verif::Compteur::copies, 0, "aucune copie");
    return BILAN();
}
