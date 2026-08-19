#include <cstddef>
#include <cstdint>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

constexpr std::size_t NOMBRE_DE_PARTICULES = 1024;

struct Nuage {
    std::vector<float> position_x;
    std::vector<float> position_y;
    std::vector<float> position_z;
    std::vector<float> vitesse_x;
    std::vector<float> vitesse_y;
    std::vector<float> vitesse_z;
    std::vector<std::uint32_t> identifiant;
    std::vector<float> masse;
};

Nuage construire() {
    Nuage nuage;
    nuage.position_x.reserve(NOMBRE_DE_PARTICULES);
    nuage.position_y.reserve(NOMBRE_DE_PARTICULES);
    nuage.position_z.reserve(NOMBRE_DE_PARTICULES);
    nuage.vitesse_x.reserve(NOMBRE_DE_PARTICULES);
    nuage.vitesse_y.reserve(NOMBRE_DE_PARTICULES);
    nuage.vitesse_z.reserve(NOMBRE_DE_PARTICULES);
    nuage.identifiant.reserve(NOMBRE_DE_PARTICULES);
    nuage.masse.reserve(NOMBRE_DE_PARTICULES);

    for (std::size_t indice = 0; indice < NOMBRE_DE_PARTICULES; indice++) {
        const float rang = (float)indice;
        nuage.position_x.push_back(rang);
        nuage.position_y.push_back(rang + 1.0f);
        nuage.position_z.push_back(rang + 2.0f);
        nuage.vitesse_x.push_back(0.5f);
        nuage.vitesse_y.push_back(0.25f);
        nuage.vitesse_z.push_back(0.125f);
        nuage.identifiant.push_back((std::uint32_t)indice);
        nuage.masse.push_back(1.0f + 0.25f * (float)(indice % 4));
    }
    return nuage;
}

std::size_t nombre_de(const Nuage &nuage) {
    return nuage.masse.size();
}

float position_y_de(const Nuage &nuage, std::size_t indice) {
    return nuage.position_y[indice];
}

std::uint32_t identifiant_de(const Nuage &nuage, std::size_t indice) {
    return nuage.identifiant[indice];
}

float masse_totale(const Nuage &nuage) {
    float total = 0.0f;
    for (const float masse : nuage.masse) {
        total += masse;
    }
    return total;
}

std::size_t octets_traverses_par_masse_totale(const Nuage &nuage) {
    return nuage.masse.size() * sizeof(float);
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
