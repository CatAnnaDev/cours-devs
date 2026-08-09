#include <memory>

#include "verif.hpp"

const bool PAS_FINI = false;

int main() {
    auto partage = std::make_shared<verif::Sonde>(7);
    VERIFIE_ENTIER(partage.use_count(), 1, "un seul proprietaire");

    {
        auto second = partage;
        VERIFIE_ENTIER(partage.use_count(), 2, "deux proprietaires");
    }

    VERIFIE_ENTIER(partage.use_count(), 1, "le second est parti");
    VERIFIE_ENTIER(partage->valeur, 7, "l'objet est toujours la");
    return BILAN();
}
