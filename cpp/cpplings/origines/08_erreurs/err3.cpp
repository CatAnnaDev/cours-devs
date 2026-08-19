#include <cstddef>
#include <optional>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

int position(const std::vector<int> &valeurs, int cible) {
    for (std::size_t i = 0; i < valeurs.size(); i++) {
        if (valeurs[i] == cible) {
            return (int)i;
        }
    }
    return -1;
}

int main() {
    const std::vector<int> valeurs = {5, 7, 9};

    const auto trouve = position(valeurs, 7);
    VERIFIE(trouve.has_value(), "7 est bien present");
    VERIFIE_ENTIER(*trouve, 1, "il est a l'indice 1");

    const auto absent = position(valeurs, 8);
    VERIFIE(!absent.has_value(), "8 est absent");
    VERIFIE_ENTIER(absent.value_or(99), 99, "value_or fournit le repli");
    return BILAN();
}
