#include <utility>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

struct Tampon {
    std::vector<verif::Sonde> donnees;

    ~Tampon() = default;
    Tampon() = default;
    Tampon(const Tampon &) = default;
    Tampon &operator=(const Tampon &) = default;
};

int main() {
    Tampon source;
    source.donnees.reserve(3);
    source.donnees.emplace_back(1);
    source.donnees.emplace_back(2);
    source.donnees.emplace_back(3);

    verif::Compteur::remettre_a_zero();
    Tampon destination = std::move(source);

    VERIFIE_ENTIER(destination.donnees.size(), 3, "les donnees ont suivi");
    VERIFIE_ENTIER(verif::Compteur::copies, 0, "aucune copie : c'est un deplacement");
    return BILAN();
}
