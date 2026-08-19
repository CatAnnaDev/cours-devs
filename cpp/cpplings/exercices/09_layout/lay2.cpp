#include <type_traits>
#include <utility>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

struct ParOrdreCroissant {
    bool operator()(double gauche, double droite) const { return gauche < droite; }
};

template <typename Politique>
class Intervalle {
public:
    Intervalle(double premiere_borne, double seconde_borne)
        : minimum(premiere_borne), maximum(seconde_borne) {
        if (!politique(minimum, maximum)) {
            std::swap(minimum, maximum);
        }
    }

    double bas() const { return minimum; }
    double haut() const { return maximum; }
    double largeur() const { return maximum - minimum; }

private:
    Politique politique;
    double minimum;
    double maximum;
};

}

int main() {
    using Borne = Intervalle<ParOrdreCroissant>;

    const Borne deja_triee(1.5, 4.0);
    const Borne a_l_envers(4.0, 1.5);

    VERIFIE(std::is_empty_v<ParOrdreCroissant>, "la politique n'a aucun etat");
    VERIFIE_ENTIER(sizeof(ParOrdreCroissant), 1, "un type vide pese un octet quand il est seul");
    VERIFIE_ENTIER(sizeof(Borne), 16, "mais dans Intervalle il ne doit rien couter du tout");
    VERIFIE_ENTIER(sizeof(Borne), 2 * sizeof(double), "deux double, et rien de plus");
    VERIFIE_ENTIER(alignof(Borne), 8, "l'alignement reste celui du double");

    VERIFIE_REEL(deja_triee.bas(), 1.5, "l'intervalle deja trie ne bouge pas");
    VERIFIE_REEL(deja_triee.haut(), 4.0, "sa borne haute non plus");
    VERIFIE_REEL(a_l_envers.bas(), 1.5, "l'intervalle inverse est remis a l'endroit");
    VERIFIE_REEL(a_l_envers.largeur(), 2.5, "la politique a bien servi");
    return BILAN();
}
