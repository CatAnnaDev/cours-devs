#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

constexpr long long empiler(int marches) {
    return marches == 0 ? 0 : static_cast<long long>(marches) + empiler(marches - 1);
}

constexpr long long accumuler(int marches) {
    long long total = 0;
    for (int marche = 1; marche <= marches; marche++) {
        total += marche;
    }
    return total;
}

constexpr long long puissance(long long base, int exposant) {
    return exposant == 0 ? 1
                         : exposant % 2 == 0 ? puissance(base * base, exposant / 2)
                                             : base * puissance(base, exposant - 1);
}

int marches_lues() { return 2000; }

}

static_assert(empiler(500) == 125250LL, "500 appels imbriques tiennent sous la limite");
static_assert(empiler(2000) == 2001000LL, "la boucle n'empile rien : la profondeur reste a 1");
static_assert(accumuler(100000) == 5000050000LL, "seul le nombre d'etapes la limite desormais");
static_assert(puissance(2, 62) == 4611686018427387904LL, "une recursion en log2 tient sans effort");

int main() {
    VERIFIE_ENTIER(accumuler(2000), 2001000, "la meme boucle tourne aussi a l'execution");
    VERIFIE_ENTIER(empiler(500), 125250, "et la recursion aussi, sans limite de 512 cette fois");

    int marches = marches_lues();
    VERIFIE_ENTIER(accumuler(marches), 2001000,
                   "avec un argument d'execution, plus aucune limite de l'evaluateur constant");
    VERIFIE_ENTIER(empiler(marches), 2001000,
                   "2000 appels imbriques : la pile de l'execution les encaisse");

    VERIFIE_ENTIER(puissance(3, 20), 3486784401LL, "l'exponentiation rapide fait 20 fois moins");

    return BILAN();
}
