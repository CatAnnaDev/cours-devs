#include <algorithm>
#include <functional>
#include <ranges>
#include <string_view>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

struct Capteur {
    std::string_view nom;
    int derive;

    int amplitude() const { return derive < 0 ? -derive : derive; }
};

struct ParDifference {
    bool operator()(const Capteur &gauche, const Capteur &droite) const {
        return gauche.derive - droite.derive < 0;
    }
};

struct ParDerive {
    bool operator()(const Capteur &gauche, const Capteur &droite) const {
        return gauche.derive < droite.derive;
    }
};

std::vector<Capteur> banc() {
    return {{"alpha", 1500000000}, {"beta", -2000000000}, {"gamma", 12000}, {"delta", -3400},
            {"epsilon", 0},        {"zeta", 750000},      {"eta", -12000}};
}

std::size_t longueur_du_nom(const Capteur &capteur) { return capteur.nom.size(); }

}

int main() {
    std::vector<Capteur> capteurs = banc();

    std::ranges::sort(capteurs, ParDifference{});

    VERIFIE_TEXTE(capteurs.front().nom, "beta",
                  "la projection compare les champs, pas leur difference");
    VERIFIE_TEXTE(capteurs.back().nom, "alpha", "et deux extremes opposes ne debordent jamais");
    VERIFIE(std::ranges::is_sorted(capteurs, std::less{}, &Capteur::derive),
            "is_sorted prend la meme projection");

    std::vector<Capteur> temoin = banc();
    std::ranges::sort(temoin, ParDerive{});
    VERIFIE(std::ranges::equal(capteurs, temoin, {}, &Capteur::nom, &Capteur::nom),
            "meme ordre qu'un comparateur ecrit correctement a la main");

    auto premier_positif = std::ranges::lower_bound(capteurs, 0, std::less{}, &Capteur::derive);
    VERIFIE(premier_positif != capteurs.end(), "lower_bound rend un iterateur");
    VERIFIE_TEXTE(premier_positif->nom, "epsilon",
                  "la projection s'applique a l'element, la valeur cherchee reste brute");

    VERIFIE(std::ranges::binary_search(capteurs, 750000, std::less{}, &Capteur::derive),
            "binary_search cherche dans le champ projete");
    VERIFIE(!std::ranges::binary_search(capteurs, 1, std::less{}, &Capteur::derive),
            "et ne trouve rien quand la valeur n'y est pas");

    auto egaux = std::ranges::equal_range(capteurs, 12000, std::less{}, &Capteur::derive);
    VERIFIE_ENTIER(std::ranges::distance(egaux), 1, "equal_range aussi");
    VERIFIE_TEXTE(egaux.front().nom, "gamma", "et rend les elements entiers");

    auto trouve = std::ranges::find(capteurs, -3400, &Capteur::derive);
    VERIFIE_TEXTE(trouve->nom, "delta", "find projette l'element et compare a la valeur donnee");

    auto plus_stable = std::ranges::min_element(capteurs, std::less{}, &Capteur::amplitude);
    VERIFIE_TEXTE(plus_stable->nom, "epsilon", "une projection peut etre une fonction membre");

    auto plus_bruyant = std::ranges::max_element(capteurs, std::less{}, &Capteur::amplitude);
    VERIFIE_TEXTE(plus_bruyant->nom, "beta", "elle est appelee par std::invoke, pas par toi");

    auto nom_le_plus_court = std::ranges::min_element(capteurs, std::less{}, longueur_du_nom);
    VERIFIE_TEXTE(nom_le_plus_court->nom, "eta", "une projection est n'importe quel appelable");

    VERIFIE_ENTIER(
        std::ranges::count_if(capteurs, [](int d) { return d < 0; }, &Capteur::derive), 3,
        "trois derives negatives");

    std::ranges::sort(capteurs, std::greater{}, &Capteur::nom);
    VERIFIE_TEXTE(capteurs.front().nom, "zeta", "changer d'ordre ne change pas la projection");

    return BILAN();
}
