#include <type_traits>
#include <utility>

#include "verif.hpp"

const bool PAS_FINI = false;

class Ressource {
public:
    Ressource() = default;

    Ressource(const Ressource &) = delete;
    Ressource &operator=(const Ressource &) = delete;

    Ressource(Ressource &&) noexcept = default;
    Ressource &operator=(Ressource &&) noexcept = default;

    int valeur = 7;
};

int main() {
    static_assert(!std::is_copy_constructible_v<Ressource>, "la copie doit etre interdite");
    static_assert(std::is_move_constructible_v<Ressource>, "le deplacement doit rester possible");

    Ressource source;
    Ressource destination = std::move(source);

    VERIFIE_ENTIER(destination.valeur, 7, "le deplacement marche");
    return BILAN();
}
