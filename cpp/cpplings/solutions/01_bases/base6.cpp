#include <string>

#include "verif.hpp"

const bool PAS_FINI = false;

std::string appeler(int) {
    return "entier";
}

std::string appeler(const char *) {
    return "pointeur";
}

int main() {
    VERIFIE_TEXTE(appeler(0), "entier", "0 est un entier");
    VERIFIE_TEXTE(appeler(nullptr), "pointeur", "nullptr est un pointeur");
    return BILAN();
}
