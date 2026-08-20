#include <iterator>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

constexpr int racine_entiere(int carre) {
    int racine = 0;
    while ((racine + 1) * (racine + 1) <= carre) {
        racine++;
    }
    return racine;
}

int profondeur_mesuree() { return 144; }

template <int PALIER>
struct Sondeuse {
    static constexpr int PAS = PALIER;
};

}

int main() {
    int mesure = profondeur_mesuree();

    static_assert(racine_entiere(mesure) == 12,
                  "un argument constant rend l'appel constant : le static_assert passe");
    static_assert(racine_entiere(0) == 0, "et la fonction sait aussi repondre sur les bords");
    static_assert(racine_entiere(399) == 19, "l'evaluation a bien lieu a la compilation");

    constexpr int RACINE = racine_entiere(mesure);
    VERIFIE_ENTIER(RACINE, 12, "une variable constexpr force l'evaluation a la compilation");

    VERIFIE_ENTIER(racine_entiere(mesure), 12,
                   "argument d'execution : evaluee a l'execution, et c'est normal");

    const int copie = racine_entiere(mesure);
    VERIFIE_ENTIER(copie, 12, "const ne dit rien du moment ou la valeur est calculee");

    VERIFIE_ENTIER(Sondeuse<racine_entiere(400)>::PAS, 20,
                   "servir d'argument template prouve que la valeur vient de la compilation");

    int paliers[racine_entiere(81)] = {};
    paliers[8] = 3;
    VERIFIE_ENTIER(std::size(paliers), 9, "une taille de tableau est un contexte constant");
    VERIFIE_ENTIER(paliers[8], 3, "et le tableau se remplit normalement a l'execution");

    return BILAN();
}
