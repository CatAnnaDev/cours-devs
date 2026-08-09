#include <memory>

#include "verif.hpp"

const bool PAS_FINI = false;

int traiter(int valeur) {
    auto sonde = std::make_unique<verif::Sonde>(valeur);

    if (valeur < 0) {
        return -1;
    }

    return sonde->valeur * 2;
}

int main() {
    verif::Compteur::remettre_a_zero();

    VERIFIE_ENTIER(traiter(21), 42, "chemin normal");
    VERIFIE_ENTIER(traiter(-1), -1, "chemin d'erreur");
    VERIFIE_ENTIER(verif::Compteur::constructions, verif::Compteur::destructions,
                   "autant de destructions que de constructions");
    return BILAN();
}
