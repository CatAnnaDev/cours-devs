#include <algorithm>
#include <concepts>
#include <iterator>
#include <ranges>
#include <span>
#include <string_view>
#include <utility>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

struct Carotte {
    std::vector<int> profondeurs;

    const std::vector<int> &mesures() const { return profondeurs; }
};

Carotte forer() { return Carotte{{40, 12, 75, 12, 3, 61, 88, 7}}; }

std::vector<int> profondeurs_brutes() { return {40, 12, 75, 12, 3, 61, 88, 7}; }

bool profonde(int profondeur) { return profondeur > 10; }

long long somme(std::ranges::input_range auto &&portee) {
    long long total = 0;
    for (int valeur : portee) {
        total += valeur;
    }
    return total;
}

}

static_assert(std::same_as<decltype(std::views::all(std::declval<std::vector<int> &>())),
                           std::ranges::ref_view<std::vector<int>>>);
static_assert(std::same_as<decltype(std::views::all(std::vector<int>{})),
                           std::ranges::owning_view<std::vector<int>>>);
static_assert(std::same_as<decltype(std::ranges::max_element(std::vector<int>{})),
                           std::ranges::dangling>);
static_assert(!std::ranges::borrowed_range<std::vector<int>>);
static_assert(std::ranges::borrowed_range<std::string_view>);
static_assert(std::ranges::borrowed_range<std::span<const int>>);
static_assert(std::ranges::borrowed_range<std::ranges::ref_view<std::vector<int>>>);

int main() {
    auto sur_temporaire = profondeurs_brutes() | std::views::filter(profonde);
    VERIFIE_ENTIER(somme(sur_temporaire), 288,
                   "un temporaire branche directement est adopte par owning_view");

    std::vector<int> nommee = profondeurs_brutes();
    auto sur_nommee = nommee | std::views::filter(profonde);
    VERIFIE_ENTIER(somme(sur_nommee), 288, "une lvalue donne un ref_view, qui ne possede rien");

    std::vector<int> materialisee;
    std::ranges::copy(forer().mesures() | std::views::filter(profonde),
                      std::back_inserter(materialisee));
    VERIFIE_ENTIER(somme(materialisee), 288,
                   "consommer la vue dans l'expression qui cree le temporaire est sans danger");

    const Carotte gardee = forer();
    auto sur_source_gardee = gardee.mesures() | std::views::filter(profonde);
    VERIFIE_ENTIER(somme(sur_source_gardee), 288,
                   "un accesseur qui rend une reference ne prolonge rien : garder la source");

    VERIFIE_ENTIER(sizeof(std::ranges::ref_view<std::vector<int>>), sizeof(void *),
                   "un ref_view, c'est un pointeur : il ne possede rien");
    VERIFIE_ENTIER(sizeof(std::ranges::owning_view<std::vector<int>>), sizeof(std::vector<int>),
                   "un owning_view, c'est le conteneur deplace a l'interieur de la vue");

    auto empruntee = std::ranges::subrange(nommee.begin(), nommee.end());
    VERIFIE_ENTIER(somme(empruntee), 298,
                   "un subrange est borrowed : ses iterateurs survivent a la vue elle-meme");

    VERIFIE_ENTIER(std::ranges::count_if(profondeurs_brutes(), profonde), 6,
                   "un algorithme consomme le temporaire avant sa mort");
    VERIFIE(std::ranges::max_element(nommee) != nommee.end(),
            "sur une lvalue, l'algorithme rend un vrai iterateur");

    return BILAN();
}
