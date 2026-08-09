#include <utility>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

struct Boite {
    std::vector<verif::Sonde> stock;

    void ranger(verif::Sonde sonde) {
        stock.push_back(sonde);
    }
};

int main() {
    Boite boite;
    boite.stock.reserve(4);

    verif::Compteur::remettre_a_zero();
    boite.ranger(verif::Sonde(3));

    VERIFIE_ENTIER(verif::Compteur::copies, 0, "aucune copie depuis une temporaire");
    VERIFIE_ENTIER(boite.stock.at(0).valeur, 3, "la valeur est rangee");
    return BILAN();
}
