#include <algorithm>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

int main() {
    std::vector<int> nombres = {1, 2, 2, 3, 4, 4, 5};

    std::erase_if(nombres, [](int n) { return n % 2 == 0; });

    VERIFIE_ENTIER(nombres.size(), 3, "il reste trois impairs");
    VERIFIE_ENTIER(nombres[0], 1, "le premier vaut 1");
    VERIFIE_ENTIER(nombres[1], 3, "le deuxieme vaut 3");
    VERIFIE_ENTIER(nombres[2], 5, "le troisieme vaut 5");
    return BILAN();
}
