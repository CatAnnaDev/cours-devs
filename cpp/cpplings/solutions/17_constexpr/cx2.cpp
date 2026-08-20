#include <string_view>
#include <type_traits>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

int melanges_a_l_execution = 0;

constexpr unsigned melanger(unsigned empreinte, char lettre) {
    if (!std::is_constant_evaluated()) {
        melanges_a_l_execution++;
    }
    return (empreinte ^ static_cast<unsigned char>(lettre)) * 16777619u;
}

consteval unsigned empreinte_de(std::string_view nom) {
    unsigned empreinte = 2166136261u;
    for (char lettre : nom) {
        empreinte = melanger(empreinte, lettre);
    }
    return empreinte;
}

std::string_view nom_lu() { return "gneiss"; }

}

int main() {
    constexpr unsigned BASALTE = empreinte_de("basalte");
    constexpr unsigned GNEISS = empreinte_de("gneiss");

    static_assert(BASALTE != GNEISS, "deux noms, deux empreintes");

    VERIFIE_ENTIER(melanges_a_l_execution, 0,
                   "consteval : les treize melanges ont eu lieu a la compilation, pas un ici");

    std::string_view nom = nom_lu();
    VERIFIE_TEXTE(nom, "gneiss", "ce nom-la vient de l'execution : consteval le refuserait");
    VERIFIE(GNEISS != 0u, "l'empreinte du nom connu est deja dans le binaire");

    unsigned a_la_main = melanger(BASALTE, 'x');
    VERIFIE(a_la_main != BASALTE, "melanger, lui, est reste constexpr");
    VERIFIE_ENTIER(melanges_a_l_execution, 1,
                   "constexpr appele a l'execution s'evalue a l'execution : le compteur bouge");

    return BILAN();
}
