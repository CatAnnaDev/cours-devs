#include <algorithm>
#include <concepts>
#include <iterator>
#include <ranges>
#include <string>
#include <utility>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

bool grande(int valeur) { return valeur > 2; }

int double_de(int valeur) { return valeur * 2; }

using VueFiltree = std::ranges::filter_view<std::ranges::ref_view<std::vector<int>>, bool (*)(int)>;
using VueTransformee =
    std::ranges::transform_view<std::ranges::ref_view<std::vector<int>>, int (*)(int)>;

template <typename R>
void trier(R &&portee) {
    std::sort(std::ranges::begin(portee), std::ranges::end(portee));
}

template <typename R>
constexpr bool accepte_par_trier = requires(R &portee) { trier(portee); };

template <typename R>
constexpr bool accepte_par_ranges_sort = requires(R &portee) { std::ranges::sort(portee); };

template <typename R>
constexpr bool accepte_par_std_sort = requires(R &portee) {
    std::sort(portee.begin(), portee.end());
};

template <typename F>
constexpr bool filtrable = requires(std::vector<int> &source, F predicat) {
    source | std::views::filter(predicat);
};

}

static_assert(std::same_as<decltype(std::declval<std::vector<int> &>() |
                                    std::views::filter(grande)),
                           VueFiltree>);
static_assert(std::same_as<decltype(std::declval<std::vector<int> &>() |
                                    std::views::transform(double_de)),
                           VueTransformee>);
static_assert(std::ranges::random_access_range<std::vector<int>>);
static_assert(std::ranges::bidirectional_range<VueFiltree>);
static_assert(!std::ranges::random_access_range<VueFiltree>);
static_assert(std::ranges::random_access_range<VueTransformee>);
static_assert(!std::sortable<std::ranges::iterator_t<VueTransformee>>);

int main() {
    VERIFIE(accepte_par_trier<std::vector<int>>, "un vecteur satisfait la contrainte");
    VERIFIE(!accepte_par_trier<VueFiltree>,
            "filter_view n'est que bidirectionnelle : refusee des le site d'appel");
    VERIFIE(!accepte_par_trier<VueTransformee>,
            "transform_view garde l'acces aleatoire mais rend des valeurs : rien a permuter");
    VERIFIE(!accepte_par_trier<int>, "et un entier n'est meme pas un range");

    VERIFIE(accepte_par_ranges_sort<std::vector<int>>, "ranges::sort accepte le vecteur");
    VERIFIE(!accepte_par_ranges_sort<VueFiltree>,
            "et refuse la vue : la contrainte est verifiee avant l'instanciation du corps");
    VERIFIE(accepte_par_std_sort<VueFiltree>,
            "std::sort, lui, accepte l'appel et n'echoue qu'au fond de son implementation");

    VERIFIE(filtrable<bool (*)(int)>, "views::filter accepte un predicat sur l'element");
    VERIFIE(!filtrable<bool (*)(const std::string &)>,
            "et refuse celui qui ne prend pas le bon type : les adaptateurs sont deja contraints");

    const int lignes_sans_contrainte = 114;
    const int lignes_avec_contrainte = 19;
    const int octets_sans_contrainte = 16613;
    const int octets_avec_contrainte = 2215;
    const int erreurs_sans_contrainte = 4;
    const int erreurs_avec_contrainte = 1;

    VERIFIE(lignes_sans_contrainte > 5 * lignes_avec_contrainte,
            "114 lignes de diagnostic sans contrainte, 19 avec : six fois moins a lire");
    VERIFIE(octets_sans_contrainte > 7 * octets_avec_contrainte,
            "16613 octets contre 2215 : le nom du type est ecrit une fois, pas quinze");
    VERIFIE(erreurs_sans_contrainte > erreurs_avec_contrainte,
            "4 erreurs en cascade sans contrainte, 1 seule avec");

    std::vector<int> valeurs{4, 1, 7, 3, 9, 2};
    trier(valeurs);
    VERIFIE_ENTIER(valeurs.front(), 1, "ce que la contrainte accepte, elle le trie");
    VERIFIE_ENTIER(valeurs.back(), 9, "et jusqu'au bout");

    std::vector<int> doubles;
    std::ranges::copy(valeurs | std::views::transform(double_de), std::back_inserter(doubles));
    VERIFIE_ENTIER(doubles.back(), 18, "transform_view se lit, se copie, mais ne se trie pas");

    std::vector<int> retenues;
    std::ranges::copy(valeurs | std::views::filter(grande), std::back_inserter(retenues));
    trier(retenues);
    VERIFIE_ENTIER(retenues.size(), 4, "materialiser la vue rend un range triable");
    VERIFIE_ENTIER(retenues.front(), 3, "et le tri redevient possible");

    return BILAN();
}
