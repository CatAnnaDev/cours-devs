#include <concepts>
#include <string>

#include "verif.hpp"

const bool PAS_FINI = true;

template <typename T>
concept Additionnable = requires(const T &a, const T &b) {
    { a + b } -> std::convertible_to<T>;
};

struct Marqueur {};

template <typename T>
std::string categorie(T) {
    return "entier";
}

template <typename T>
std::string categorie(T) {
    return "reel";
}

int main() {
    VERIFIE(Additionnable<int>, "deux entiers s'additionnent");
    VERIFIE(!Additionnable<Marqueur>, "un type sans operateur + ne s'additionne pas");

    VERIFIE_TEXTE(categorie(3), "entier", "un int est un entier");
    VERIFIE_TEXTE(categorie(3u), "entier", "un unsigned aussi");
    VERIFIE_TEXTE(categorie(3.5), "reel", "un double est un reel");
    VERIFIE_TEXTE(categorie(3.5f), "reel", "un float aussi");
    return BILAN();
}
