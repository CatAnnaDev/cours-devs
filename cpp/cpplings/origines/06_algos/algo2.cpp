#include <functional>

#include "verif.hpp"

const bool PAS_FINI = true;

std::function<int()> fabriquer(int valeur) {
    int local = valeur;
    return [&local]() { return local * 2; };
}

int main() {
    auto calcul = fabriquer(21);

    VERIFIE_ENTIER(calcul(), 42, "la lambda renvoie le double");
    return BILAN();
}
