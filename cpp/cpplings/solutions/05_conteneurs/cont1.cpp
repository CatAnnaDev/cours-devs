#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

int main() {
    std::vector<verif::Sonde> sondes;
    sondes.reserve(100);

    verif::Compteur::remettre_a_zero();

    for (int i = 0; i < 100; i++) {
        sondes.emplace_back(i);
    }

    VERIFIE_ENTIER(sondes.size(), 100, "cent elements");
    VERIFIE(sondes.capacity() >= 100, "la capacite suffit");
    VERIFIE_ENTIER(verif::Compteur::deplacements, 0, "aucun deplacement : pas de reallocation");
    return BILAN();
}
