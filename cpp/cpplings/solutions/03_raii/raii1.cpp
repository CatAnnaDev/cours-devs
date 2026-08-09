
#include "verif.hpp"

const bool PAS_FINI = false;

int main() {
    verif::Compteur::remettre_a_zero();

    {
        verif::Sonde temporaire(1);
        VERIFIE_ENTIER(verif::Compteur::destructions, 0, "pas encore detruite");
    }

    VERIFIE_ENTIER(verif::Compteur::destructions, 1, "detruite a la sortie de la portee");
    return BILAN();
}
