#include <array>
#include <cstddef>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

int appels_du_chemin_rapide = 0;

const std::array<unsigned char, 256> &table_des_bits() {
    static const std::array<unsigned char, 256> table = [] {
        std::array<unsigned char, 256> remplie{};
        for (std::size_t motif = 0; motif < remplie.size(); motif++) {
            unsigned char bits = 0;
            for (int rang = 0; rang < 8; rang++) {
                bits = static_cast<unsigned char>(bits + ((motif >> rang) & 1u));
            }
            remplie[motif] = bits;
        }
        return remplie;
    }();
    return table;
}

int bits_par_table(unsigned valeur) {
    appels_du_chemin_rapide++;
    const std::array<unsigned char, 256> &table = table_des_bits();
    return table[valeur & 0xFFu] + table[(valeur >> 8) & 0xFFu] + table[(valeur >> 16) & 0xFFu] +
           table[valeur >> 24];
}

constexpr int bits_par_boucle(unsigned valeur) {
    int total = 0;
    while (valeur != 0u) {
        total += static_cast<int>(valeur & 1u);
        valeur >>= 1;
    }
    return total;
}

constexpr int bits_a_un(unsigned valeur) {
    if consteval {
        return bits_par_boucle(valeur);
    } else {
        return bits_par_table(valeur);
    }
}

unsigned valeur_lue() { return 0xF0F0F0F0u; }

}

static_assert(bits_a_un(0u) == 0, "le chemin constant sait repondre sur zero");
static_assert(bits_a_un(0xFFFFFFFFu) == 32, "et sur tous les bits");
static_assert(bits_a_un(0xF0F0F0F0u) == 16,
              "la table n'a pas ete consultee : elle n'existe meme pas encore");

int main() {
    constexpr int SEIZE = bits_a_un(0xF0F0F0F0u);
    VERIFIE_ENTIER(SEIZE, 16, "la valeur est deja dans le binaire");
    VERIFIE_ENTIER(appels_du_chemin_rapide, 0, "et le chemin rapide n'a pas ete emprunte");

    unsigned lu = valeur_lue();
    VERIFIE_ENTIER(bits_a_un(lu), 16, "a l'execution, la meme fonction rend la meme chose");
    VERIFIE_ENTIER(appels_du_chemin_rapide, 1,
                   "mais par le chemin rapide, comme le compteur le dit");

    VERIFIE_ENTIER(bits_a_un(0xF0F0F0F0u), 16,
                   "un argument constant ne suffit pas hors d'un contexte constant");
    VERIFIE_ENTIER(appels_du_chemin_rapide, 2, "le compteur le prouve une seconde fois");

    VERIFIE_ENTIER(bits_par_boucle(lu), 16, "les deux implementations restent d'accord");

    return BILAN();
}
