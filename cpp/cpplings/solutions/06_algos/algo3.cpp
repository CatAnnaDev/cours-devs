#include <algorithm>
#include <numeric>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

int main() {
    std::vector<int> nombres = {3, 8, 12, 5};

    auto trouve = std::find_if(nombres.begin(), nombres.end(), [](int n) { return n > 10; });
    const bool present = trouve != nombres.end();

    VERIFIE(present, "il existe un element superieur a 10");
    VERIFIE_ENTIER(present ? *trouve : -1, 12, "c'est 12");

    auto absent = std::find_if(nombres.begin(), nombres.end(), [](int n) { return n > 100; });
    VERIFIE(absent == nombres.end(), "aucun element superieur a 100");

    const int total = std::accumulate(nombres.begin(), nombres.end(), 0);
    VERIFIE_ENTIER(total, 28, "la somme vaut 28");
    return BILAN();
}
