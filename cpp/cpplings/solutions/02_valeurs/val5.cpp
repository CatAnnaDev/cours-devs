#include <string>

#include "verif.hpp"

const bool PAS_FINI = false;

std::string construire(int score) {
    std::string texte = "score:" + std::to_string(score);
    return texte;
}

int main() {
    std::string message = construire(42);
    VERIFIE_TEXTE(message, "score:42", "le message est correct");
    return BILAN();
}
