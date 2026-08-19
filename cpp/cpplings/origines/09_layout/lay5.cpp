#include <cstddef>
#include <cstdint>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

constexpr std::size_t NOMBRE_DE_PARTICULES = 1024;

struct Particule {
    float position_x;
    float position_y;
    float position_z;
    float vitesse_x;
    float vitesse_y;
    float vitesse_z;
    std::uint32_t identifiant;
    float masse;
};

using Nuage = std::vector<Particule>;

Nuage construire() {
    Nuage nuage;
    nuage.reserve(NOMBRE_DE_PARTICULES);
    for (std::size_t indice = 0; indice < NOMBRE_DE_PARTICULES; indice++) {
        const float rang = (float)indice;
        nuage.push_back(Particule{rang, rang + 1.0f, rang + 2.0f, 0.5f, 0.25f, 0.125f,
                                  (std::uint32_t)indice, 1.0f + 0.25f * (float)(indice % 4)});
    }
    return nuage;
}

std::size_t nombre_de(const Nuage &nuage) {
    return nuage.size();
}

float position_y_de(const Nuage &nuage, std::size_t indice) {
    return nuage[indice].position_y;
}

std::uint32_t identifiant_de(const Nuage &nuage, std::size_t indice) {
    return nuage[indice].identifiant;
}

float masse_totale(const Nuage &nuage) {
    float total = 0.0f;
    for (const Particule &particule : nuage) {
        total += particule.masse;
    }
    return total;
}

std::size_t octets_traverses_par_masse_totale(const Nuage &nuage) {
    return nuage.size() * sizeof(Particule);
}

}

int main() {
    const Nuage nuage = construire();

    VERIFIE_ENTIER(nombre_de(nuage), NOMBRE_DE_PARTICULES, "mille vingt-quatre particules");
    VERIFIE_REEL(masse_totale(nuage), 1408.0, "la masse totale ne change pas d'un poil");
    VERIFIE_REEL(position_y_de(nuage, 3), 4.0, "les autres champs sont toujours la");
    VERIFIE_ENTIER(identifiant_de(nuage, 1000), 1000, "les identifiants aussi");

    VERIFIE_ENTIER(octets_traverses_par_masse_totale(nuage), NOMBRE_DE_PARTICULES * sizeof(float),
                   "la boucle chaude ne doit traverser que la colonne des masses");
    VERIFIE_ENTIER(NOMBRE_DE_PARTICULES * sizeof(float), 4096,
                   "quatre kilo-octets, contre trente-deux pour le tableau de structures");
    return BILAN();
}
