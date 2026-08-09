#include <algorithm>
#include <string>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

int main() {
    std::vector<std::string> noms = {"zoe", "anna", "marc"};

    std::sort(noms.begin(), noms.end(), [](const std::string &a, const std::string &b) {
        return a < b;
    });

    VERIFIE_TEXTE(noms.front(), "anna", "le premier est anna");
    VERIFIE_TEXTE(noms.back(), "zoe", "le dernier est zoe");
    return BILAN();
}
