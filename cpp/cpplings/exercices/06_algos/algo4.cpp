#include <ranges>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

int main() {
    std::vector<int> nombres = {1, 2, 3, 4, 5, 6};

    auto vue = nombres | std::views::filter([](int n) { return n % 2 == 0; }) |
               std::views::transform([](int n) { return n; });

    int total = 0;
    int compte = 0;
    for (int valeur : vue) {
        total += valeur;
        compte++;
    }

    VERIFIE_ENTIER(compte, 3, "trois nombres pairs");
    VERIFIE_ENTIER(total, 120, "20 + 40 + 60");
    VERIFIE_ENTIER(nombres.size(), 6, "le vecteur d'origine n'a pas bouge");
    return BILAN();
}
