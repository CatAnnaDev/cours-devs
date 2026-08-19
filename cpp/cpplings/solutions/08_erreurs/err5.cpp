#include <stdexcept>
#include <string>

#include "verif.hpp"

const bool PAS_FINI = false;

struct ErreurFichier : std::runtime_error {
    explicit ErreurFichier(const std::string &chemin)
        : std::runtime_error("fichier introuvable : " + chemin) {}
};

std::string message_de(int cas) {
    try {
        if (cas == 0) {
            throw ErreurFichier("carte.png");
        }
        throw std::runtime_error("autre chose");
    } catch (const std::exception &erreur) {
        return erreur.what();
    }
}

int main() {
    VERIFIE_TEXTE(message_de(0), "fichier introuvable : carte.png", "le message complet survit");
    VERIFIE_TEXTE(message_de(1), "autre chose", "l'autre erreur garde aussi le sien");
    return BILAN();
}
