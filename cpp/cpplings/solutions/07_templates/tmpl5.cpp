#include "verif.hpp"

const bool PAS_FINI = false;

template <typename... T>
auto somme(T... valeurs) {
    return (valeurs + ... + 0);
}

int main() {
    VERIFIE_ENTIER(somme(), 0, "aucune valeur, donc zero");
    VERIFIE_ENTIER(somme(1), 1, "une seule valeur");
    VERIFIE_ENTIER(somme(1, 2, 3, 4), 10, "quatre valeurs d'un coup");
    VERIFIE_REEL(somme(1.5, 2.5), 4.0, "et ca marche aussi pour des reels");
    return BILAN();
}
