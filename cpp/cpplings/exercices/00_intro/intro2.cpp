#include <string>

#include "verif.hpp"

const bool PAS_FINI = true;

int main() {
    std::string nom = "anna"
    VERIFIE_TEXTE(nom, "anna", "le nom est correct");
    return BILAN();
}
