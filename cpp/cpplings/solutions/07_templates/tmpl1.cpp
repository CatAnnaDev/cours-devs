#include <string>

#include "verif.hpp"

const bool PAS_FINI = false;

template <typename T>
T maximum(const T &a, const T &b) {
    return a < b ? b : a;
}

int main() {
    VERIFIE_ENTIER(maximum(3, 7), 7, "le maximum de deux entiers");
    VERIFIE_REEL(maximum(1.5, 0.5), 1.5, "le maximum de deux reels");
    VERIFIE_TEXTE(maximum(std::string("abc"), std::string("abd")), "abd",
                  "le maximum de deux chaines");
    return BILAN();
}
