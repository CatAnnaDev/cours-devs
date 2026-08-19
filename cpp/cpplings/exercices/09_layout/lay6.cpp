#include <cstddef>
#include <cstdint>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

constexpr std::size_t TAILLE_ENTETE = 8;

struct alignas(32) Bloc {
    double valeurs[4];

    double somme() const { return valeurs[0] + valeurs[1] + valeurs[2] + valeurs[3]; }
};

}

int main() {
    char *tampon = new char[TAILLE_ENTETE + sizeof(Bloc)];
    Bloc *bloc = reinterpret_cast<Bloc *>(tampon + TAILLE_ENTETE);

    const auto adresse = reinterpret_cast<std::uintptr_t>(bloc);

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

    delete[] tampon;
    return BILAN();
}
