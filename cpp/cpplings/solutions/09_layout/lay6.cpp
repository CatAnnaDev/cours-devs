#include <cstdint>
#include <memory>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

struct alignas(32) Bloc {
    double valeurs[4];

    double somme() const { return valeurs[0] + valeurs[1] + valeurs[2] + valeurs[3]; }
};

static_assert(alignof(Bloc) == 32, "Bloc reclame trente-deux octets d'alignement");
static_assert(alignof(Bloc) > __STDCPP_DEFAULT_NEW_ALIGNMENT__,
              "c'est bien la surcharge alignee de new qui doit servir");

}

int main() {
    const std::unique_ptr<Bloc> bloc = std::make_unique<Bloc>();

    const auto adresse = reinterpret_cast<std::uintptr_t>(bloc.get());

    VERIFIE_ENTIER(alignof(Bloc), 32, "le type reclame trente-deux octets d'alignement");
    VERIFIE_ENTIER(sizeof(Bloc), 32, "et il en occupe trente-deux");
    VERIFIE(alignof(Bloc) > __STDCPP_DEFAULT_NEW_ALIGNMENT__,
            "plus que l'alignement que new donne par defaut");
    VERIFIE_ENTIER(adresse % alignof(Bloc), 0, "l'adresse rendue est un multiple de trente-deux");

    bloc->valeurs[0] = 1.0;
    bloc->valeurs[1] = 2.0;
    bloc->valeurs[2] = 4.0;
    bloc->valeurs[3] = 8.0;

    VERIFIE_REEL(bloc->somme(), 15.0, "et le bloc est utilisable");

    return BILAN();
}
