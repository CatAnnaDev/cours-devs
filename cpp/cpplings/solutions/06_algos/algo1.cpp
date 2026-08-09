#include <algorithm>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

int main() {
    std::vector<int> nombres = {3, 1, 4, 1, 5};

    std::sort(nombres.begin(), nombres.end(), [](int a, int b) { return a > b; });

    VERIFIE_ENTIER(nombres.front(), 5, "le plus grand en premier");
    VERIFIE_ENTIER(nombres.back(), 1, "le plus petit en dernier");
    return BILAN();
}
