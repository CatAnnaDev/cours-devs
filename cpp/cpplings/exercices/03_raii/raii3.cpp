#include <memory>
#include <utility>

#include "verif.hpp"

const bool PAS_FINI = true;

int main() {
    auto premier = std::make_unique<int>(42);

    auto second = premier;

    VERIFIE(premier == nullptr, "le premier ne possede plus rien");
    VERIFIE(second != nullptr, "le second possede le bloc");
    VERIFIE_ENTIER(*second, 42, "la valeur a suivi");
    return BILAN();
}
