#include <cstddef>
#include <cstdint>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

struct Echantillon {
    double mesure;
    std::uint32_t identifiant;
    std::uint16_t sequence;
    std::uint8_t canal;
    bool valide;
};

static_assert(sizeof(Echantillon) == 16, "Echantillon doit tenir en seize octets");
static_assert(alignof(Echantillon) == 8, "Echantillon garde l'alignement du double");

}

int main() {
    constexpr std::size_t OCTETS_UTILES =
        sizeof(Echantillon::mesure) + sizeof(Echantillon::identifiant) +
        sizeof(Echantillon::sequence) + sizeof(Echantillon::canal) + sizeof(Echantillon::valide);

    Echantillon echantillon{};
    echantillon.valide = true;
    echantillon.mesure = 2.5;
    echantillon.canal = 7;
    echantillon.identifiant = 4242;
    echantillon.sequence = 900;

    VERIFIE_ENTIER(OCTETS_UTILES, 16, "les cinq membres pesent seize octets a eux tous");
    VERIFIE_ENTIER(sizeof(Echantillon), 16, "la struct doit peser seize octets, pas trente-deux");
    VERIFIE_ENTIER(sizeof(Echantillon), OCTETS_UTILES, "donc plus un seul octet de remplissage");
    VERIFIE_ENTIER(alignof(Echantillon), 8, "l'alignement reste celui du double");

    VERIFIE(echantillon.valide, "le membre valide est toujours la");
    VERIFIE_REEL(echantillon.mesure, 2.5, "le membre mesure est toujours la");
    VERIFIE_ENTIER(echantillon.canal, 7, "le membre canal est toujours la");
    VERIFIE_ENTIER(echantillon.identifiant, 4242, "le membre identifiant est toujours la");
    VERIFIE_ENTIER(echantillon.sequence, 900, "le membre sequence est toujours la");
    return BILAN();
}
