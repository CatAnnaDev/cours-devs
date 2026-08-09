#include <string>
#include <utility>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

struct Personnage {
    std::string nom;
    std::vector<int> inventaire;
};

int main() {
    Personnage source{"anna", {1, 2, 3}};
    Personnage destination = std::move(source);

    VERIFIE_TEXTE(destination.nom, "anna", "le nom a suivi");
    VERIFIE_ENTIER(destination.inventaire.size(), 3, "l'inventaire aussi");
    VERIFIE(source.inventaire.empty(), "la source a bien ete deplacee, pas copiee");
    return BILAN();
}
