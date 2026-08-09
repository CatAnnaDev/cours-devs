
#include "verif.hpp"

const bool PAS_FINI = true;

int carre(int n) {
    return n * n;
}

int main() {
    static_assert(carre(5) == 25, "calcule a la compilation");

    const int cote = 7;
    VERIFIE_ENTIER(carre(cote), 49, "et aussi a l'execution");
    return BILAN();
}
