#include <functional>

#include "verif.hpp"

const bool PAS_FINI = false;

std::function<int()> fabriquer(int valeur) {
    return [valeur]() { return valeur * 2; };
}

int main() {
    auto calcul = fabriquer(21);

    VERIFIE_ENTIER(calcul(), 42, "la lambda renvoie le double");
    return BILAN();
}
