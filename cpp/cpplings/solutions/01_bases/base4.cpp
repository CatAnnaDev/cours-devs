#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

int main() {
    std::vector<int> trois_cinq(3, 5);
    std::vector<int> deux_valeurs{3, 5};

    VERIFIE_ENTIER(trois_cinq.size(), 3, "trois cases");
    VERIFIE_ENTIER(trois_cinq[0], 5, "qui valent toutes 5");
    VERIFIE_ENTIER(deux_valeurs.size(), 2, "deux cases");
    VERIFIE_ENTIER(deux_valeurs[0], 3, "la premiere vaut 3");
    return BILAN();
}
