#include <array>
#include <span>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

int somme(const std::vector<int> &valeurs) {
    int total = 0;
    for (int valeur : valeurs) {
        total += valeur;
    }
    return total;
}

int main() {
    std::vector<int> vecteur = {1, 2, 3};
    std::array<int, 4> tableau = {1, 2, 3, 4};

    VERIFIE_ENTIER(somme(vecteur), 6, "depuis un vector");
    VERIFIE_ENTIER(somme(tableau), 10, "depuis un array, sans conversion");
    VERIFIE_ENTIER(somme(std::span(vecteur).subspan(1)), 5, "une sous-vue");
    return BILAN();
}
