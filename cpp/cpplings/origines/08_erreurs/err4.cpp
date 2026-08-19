#include <expected>
#include <string>

#include "verif.hpp"

const bool PAS_FINI = true;

std::expected<int, std::string> diviser(int a, int b) {
    return a / b;
}

int main() {
    const auto bon = diviser(10, 2);
    VERIFIE(bon.has_value(), "10 / 2 a reussi");
    VERIFIE_ENTIER(*bon, 5, "10 / 2 vaut 5");

    const auto mauvais = diviser(10, 0);
    VERIFIE(!mauvais.has_value(), "diviser par zero echoue proprement");
    VERIFIE_TEXTE(mauvais.error(), "division par zero", "et l'erreur porte sa cause");
    return BILAN();
}
