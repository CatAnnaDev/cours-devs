#include <algorithm>
#include <array>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

constexpr int PICS = 8;

constexpr long long somme_glissante(int nombre) {
    int *tampon = new int[static_cast<unsigned>(nombre)];
    for (int i = 0; i < nombre; i++) {
        tampon[i] = (i * i) % 17;
    }
    long long total = 0;
    for (int i = 0; i < nombre; i++) {
        total += tampon[i];
    }
    delete[] tampon;
    return total;
}

constexpr std::vector<int> collecter(int nombre) {
    std::vector<int> pics;
    pics.reserve(static_cast<unsigned>(nombre));
    for (int i = 0; i < nombre; i++) {
        pics.push_back((i * i) % 17);
    }
    return pics;
}

constexpr std::array<int, PICS> figer() {
    std::vector<int> pics = collecter(PICS);
    std::array<int, PICS> fige{};
    for (int i = 0; i < PICS; i++) {
        fige[static_cast<unsigned>(i)] = pics[static_cast<unsigned>(i)];
    }
    return fige;
}

constexpr std::array<int, PICS> TABLE = figer();

}

static_assert(somme_glissante(8) == 55, "l'allocation a vecu et est morte pendant la compilation");
static_assert(TABLE[3] == 9, "la table, elle, a survecu : elle ne contient plus d'allocation");
static_assert(TABLE[7] == 15, "chaque case est calculee une fois pour toutes");

int main() {
    VERIFIE_ENTIER(somme_glissante(8), 55, "la meme fonction tourne aussi a l'execution");
    VERIFIE_ENTIER(TABLE.size(), PICS, "std::array est un objet litteral : il peut etre constexpr");
    VERIFIE_ENTIER(TABLE[3], 9, "et sa valeur est deja dans le binaire");

    std::vector<int> a_l_execution = collecter(PICS);
    VERIFIE_ENTIER(a_l_execution.size(), PICS, "la meme collecte, allouee sur le tas cette fois");
    VERIFIE(std::equal(TABLE.begin(), TABLE.end(), a_l_execution.begin()),
            "les deux mondes calculent exactement la meme chose");

    return BILAN();
}
