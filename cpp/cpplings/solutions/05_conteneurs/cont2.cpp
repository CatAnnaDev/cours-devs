#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

int main() {
    std::vector<int> nombres = {1, 2, 3};

    nombres.push_back(4);

    VERIFIE_ENTIER(nombres[0], 1, "le premier element vaut toujours 1");
    VERIFIE_ENTIER(nombres.size(), 4, "quatre elements");
    return BILAN();
}
