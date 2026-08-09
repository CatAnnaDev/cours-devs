#include <algorithm>
#include <cstddef>

#include "verif.hpp"

const bool PAS_FINI = true;

class Tampon {
public:
    explicit Tampon(std::size_t taille) : donnees_(new int[taille]()), taille_(taille) {}

    ~Tampon() { delete[] donnees_; }

    Tampon &operator=(const Tampon &) = delete;

    int *donnees() { return donnees_; }
    std::size_t taille() const { return taille_; }

private:
    int *donnees_;
    std::size_t taille_;
};

int main() {
    Tampon original(4);
    original.donnees()[0] = 42;

    {
        Tampon copie(original);
        copie.donnees()[0] = 7;
        VERIFIE_ENTIER(copie.donnees()[0], 7, "la copie a sa propre valeur");
    }

    VERIFIE_ENTIER(original.donnees()[0], 42, "l'original n'a pas bouge");
    return BILAN();
}
