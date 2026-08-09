#include <map>
#include <string>

#include "verif.hpp"

const bool PAS_FINI = false;

int main() {
    std::map<std::string, int> scores = {{"anna", 12}, {"marc", 7}};

    int total = 0;
    std::string noms;
    for (const auto &[nom, score] : scores) {
        total += score;
        noms += nom;
    }

    VERIFIE_ENTIER(total, 19, "la somme des scores");
    VERIFIE_TEXTE(noms, "annamarc", "les noms dans l'ordre des cles");
    return BILAN();
}
