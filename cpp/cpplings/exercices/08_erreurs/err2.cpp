#include <type_traits>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

struct Fragile {
    static inline int copies = 0;
    static inline int deplacements = 0;

    int valeur = 0;

    Fragile() = default;
    explicit Fragile(int v) : valeur(v) {}

    Fragile(const Fragile &autre) : valeur(autre.valeur) { copies++; }
    Fragile(Fragile &&autre) : valeur(autre.valeur) { deplacements++; }

    Fragile &operator=(const Fragile &autre) {
        valeur = autre.valeur;
        copies++;
        return *this;
    }

    Fragile &operator=(Fragile &&autre) noexcept {
        valeur = autre.valeur;
        deplacements++;
        return *this;
    }

    ~Fragile() = default;
};

}

int main() {
    std::vector<Fragile> fragiles;

    for (int valeur = 0; valeur < 8; valeur++) {
        fragiles.emplace_back(valeur);
    }

    VERIFIE(std::is_nothrow_move_constructible_v<Fragile>, "le deplacement est marque noexcept");
    VERIFIE_ENTIER(Fragile::copies, 0, "aucune copie pendant les reallocations");
    VERIFIE(Fragile::deplacements > 0, "que des deplacements");
    VERIFIE_ENTIER(fragiles.size(), 8, "les huit elements sont la");
    return BILAN();
}
