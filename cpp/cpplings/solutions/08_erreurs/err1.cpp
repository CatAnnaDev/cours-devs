#include <memory>
#include <stdexcept>

#include "verif.hpp"

const bool PAS_FINI = false;

void travailler(int valeur) {
    auto sonde = std::make_unique<verif::Sonde>(valeur);

    if (sonde->valeur < 0) {
        throw std::runtime_error("valeur negative");
    }
}

int main() {
    verif::Compteur::remettre_a_zero();

    try {
        travailler(-1);
        VERIFIE(false, "une exception aurait du sortir");
    } catch (const std::runtime_error &) {
        VERIFIE(true, "l'exception a bien ete attrapee");
    }

    VERIFIE_ENTIER(verif::Compteur::constructions, 1, "une seule Sonde a ete construite");
    VERIFIE_ENTIER(verif::Compteur::destructions, 1, "detruite malgre l'exception");
    return BILAN();
}
