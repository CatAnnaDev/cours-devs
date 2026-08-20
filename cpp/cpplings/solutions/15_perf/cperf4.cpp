#include <algorithm>
#include <cstddef>
#include <utility>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

constexpr std::size_t NOMBRE_DE_VALEURS = 512;
constexpr std::size_t NOMBRE_DE_DISTINCTES = 128;
constexpr long long COMPARAISONS_A_LA_MAIN = 131327;
constexpr long long BUDGET_DE_COMPARAISONS = 20 * (long long)NOMBRE_DE_VALEURS;

long long comparaisons = 0;

struct Avant {
    bool operator()(int gauche, int droit) const {
        comparaisons++;
        return gauche < droit;
    }
};

struct Identiques {
    bool operator()(int gauche, int droit) const {
        comparaisons++;
        return gauche == droit;
    }
};

std::vector<int> echantillon() {
    std::vector<int> valeurs;
    valeurs.reserve(NOMBRE_DE_VALEURS);
    for (std::size_t rang = 0; rang < NOMBRE_DE_VALEURS; rang++) {
        valeurs.push_back((int)((rang * 37) % NOMBRE_DE_DISTINCTES));
    }
    return valeurs;
}

std::vector<int> trier_et_dedoublonner_a_la_main(std::vector<int> valeurs) {
    for (std::size_t rang = 0; rang + 1 < valeurs.size(); rang++) {
        std::size_t plus_petit = rang;
        for (std::size_t suivant = rang + 1; suivant < valeurs.size(); suivant++) {
            if (Avant{}(valeurs[suivant], valeurs[plus_petit])) {
                plus_petit = suivant;
            }
        }
        if (plus_petit != rang) {
            std::swap(valeurs[rang], valeurs[plus_petit]);
        }
    }

    std::vector<int> distinctes;
    distinctes.reserve(valeurs.size());
    for (std::size_t rang = 0; rang < valeurs.size(); rang++) {
        if (rang == 0 || Avant{}(distinctes.back(), valeurs[rang])) {
            distinctes.push_back(valeurs[rang]);
        }
    }
    return distinctes;
}

std::vector<int> trier_et_dedoublonner_par_algorithme(std::vector<int> valeurs) {
    std::sort(valeurs.begin(), valeurs.end(), Avant{});
    valeurs.erase(std::unique(valeurs.begin(), valeurs.end(), Identiques{}), valeurs.end());
    return valeurs;
}

}

int main() {
    const std::vector<int> depart = echantillon();

    comparaisons = 0;
    const std::vector<int> a_la_main = trier_et_dedoublonner_a_la_main(depart);
    const long long comparaisons_a_la_main = comparaisons;

    comparaisons = 0;
    const std::vector<int> par_algorithme = trier_et_dedoublonner_par_algorithme(depart);
    const long long comparaisons_par_algorithme = comparaisons;

    VERIFIE_ENTIER(depart.size(), NOMBRE_DE_VALEURS, "cinq cent douze valeurs au depart");
    VERIFIE_ENTIER(a_la_main.size(), NOMBRE_DE_DISTINCTES, "cent vingt-huit distinctes a la main");
    VERIFIE_ENTIER(par_algorithme.size(), NOMBRE_DE_DISTINCTES, "cent vingt-huit par l'algorithme");
    VERIFIE_EGAL(par_algorithme, a_la_main, "les deux methodes rendent exactement la meme liste");
    VERIFIE_ENTIER(par_algorithme.front(), 0, "triee en ordre croissant, de zero");
    VERIFIE_ENTIER(par_algorithme.back(), 127, "jusqu'a cent vingt-sept");

    VERIFIE_ENTIER(comparaisons_a_la_main, COMPARAISONS_A_LA_MAIN,
                   "la boucle a la main compare n fois n sur deux, plus le balayage final");
    VERIFIE(comparaisons_par_algorithme < BUDGET_DE_COMPARAISONS,
            "n log n tient dans vingt comparaisons par element, n au carre non");
    VERIFIE(comparaisons_a_la_main > 10 * comparaisons_par_algorithme,
            "meme resultat, plus de dix fois moins de comparaisons");
    VERIFIE(comparaisons_par_algorithme >= (long long)NOMBRE_DE_VALEURS - 1,
            "l'algorithme compare quand meme, il compare seulement mieux");
    return BILAN();
}
