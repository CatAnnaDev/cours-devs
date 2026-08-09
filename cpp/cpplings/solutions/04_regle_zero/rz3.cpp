#include <cstddef>
#include <utility>

#include "verif.hpp"

const bool PAS_FINI = false;

class Tableau {
public:
    explicit Tableau(std::size_t taille) : donnees_(new int[taille]()), taille_(taille) {}

    ~Tableau() { delete[] donnees_; }

    Tableau(Tableau &&autre) noexcept : donnees_(autre.donnees_), taille_(autre.taille_) {
        autre.donnees_ = nullptr;
        autre.taille_ = 0;
    }

    Tableau(const Tableau &) = delete;
    Tableau &operator=(const Tableau &) = delete;

    std::size_t taille() const { return taille_; }
    int *donnees() { return donnees_; }

private:
    int *donnees_;
    std::size_t taille_;
};

int main() {
    Tableau source(4);
    source.donnees()[0] = 42;

    Tableau destination = std::move(source);

    VERIFIE_ENTIER(destination.taille(), 4, "la taille a suivi");
    VERIFIE_ENTIER(destination.donnees()[0], 42, "les donnees aussi");
    VERIFIE_ENTIER(source.taille(), 0, "la source est vide mais valide");
    return BILAN();
}
