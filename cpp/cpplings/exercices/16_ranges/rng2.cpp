#include <algorithm>
#include <functional>
#include <ranges>
#include <string_view>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

struct Mesure {
    std::string_view station;
    int altitude;
    int temperature;
};

struct ParAltitude {
    bool operator()(const Mesure &gauche, const Mesure &droite) const {
        return gauche.altitude < droite.altitude;
    }
};

std::vector<Mesure> releve() {
    return {{"aiguille", 3842, -14}, {"col", 2764, -3}, {"refuge", 3167, -8},
            {"lac", 2105, 4},        {"crete", 3510, -11}};
}

}

int main() {
    std::vector<Mesure> mesures = releve();

    auto fin = std::sort(mesures, std::less{}, &Mesure::altitude);

    VERIFIE(fin == mesures.end(), "ranges::sort rend la fin du range, pas void");
    VERIFIE_TEXTE(mesures.front().station, "lac", "la projection trie sur le champ");
    VERIFIE_TEXTE(mesures.back().station, "aiguille", "sans ecrire de comparateur");
    VERIFIE(std::ranges::is_sorted(mesures, std::less{}, &Mesure::altitude),
            "is_sorted prend la meme projection");

    std::vector<Mesure> classique = releve();
    std::sort(classique.begin(), classique.end(), ParAltitude{});

    VERIFIE(std::ranges::equal(mesures, classique, {}, &Mesure::station, &Mesure::station),
            "meme ordre que std::sort avec un foncteur ecrit a la main");

    auto trouve = std::ranges::find(mesures, 3167, &Mesure::altitude);
    VERIFIE(trouve != mesures.end(), "ranges::find compare la valeur a la projection");
    VERIFIE_TEXTE(trouve->station, "refuge", "et rend l'element entier, pas le champ");

    VERIFIE_ENTIER(
        std::ranges::count_if(mesures, [](int t) { return t < 0; }, &Mesure::temperature), 4,
        "count_if projette avant d'appeler le predicat");

    auto plus_froide = std::ranges::min_element(mesures, {}, &Mesure::temperature);
    VERIFIE_TEXTE(plus_froide->station, "aiguille", "min_element sur un champ");

    auto queue = std::ranges::subrange(mesures.begin() + 2, mesures.end());
    std::ranges::sort(queue, std::greater{}, &Mesure::altitude);
    VERIFIE_TEXTE(mesures[0].station, "lac", "un subrange laisse le debut tranquille");
    VERIFIE_TEXTE(mesures[2].station, "aiguille", "et trie le reste dans l'autre sens");

    VERIFIE(std::ranges::any_of(mesures, [](std::string_view n) { return n == "crete"; },
                                &Mesure::station),
            "toute la famille accepte une projection");

    return BILAN();
}
