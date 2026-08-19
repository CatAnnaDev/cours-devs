#include <utility>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

template <typename T>
void ajouter(std::vector<verif::Sonde> &cible, T &&valeur) {
    cible.push_back(std::forward<T>(valeur));
}

int main() {
    std::vector<verif::Sonde> sondes;
    sondes.reserve(4);

    verif::Sonde nommee(1);

    verif::Compteur::remettre_a_zero();
    ajouter(sondes, nommee);
    VERIFIE_ENTIER(verif::Compteur::copies, 1, "une lvalue est copiee, une seule fois");

    verif::Compteur::remettre_a_zero();
    ajouter(sondes, verif::Sonde(2));
    VERIFIE_ENTIER(verif::Compteur::copies, 0, "une rvalue n'est jamais copiee");
    VERIFIE_ENTIER(verif::Compteur::deplacements, 1, "elle est seulement deplacee, une fois");

    VERIFIE_ENTIER(sondes.size(), 2, "les deux sondes sont bien dans le vecteur");
    return BILAN();
}
