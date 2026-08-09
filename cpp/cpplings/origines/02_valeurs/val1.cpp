
#include "verif.hpp"

const bool PAS_FINI = true;

int lire(verif::Sonde sonde) {
    return sonde.valeur;
}

int main() {
    verif::Sonde sonde(42);
    verif::Compteur::remettre_a_zero();

    VERIFIE_ENTIER(lire(sonde), 42, "on lit la bonne valeur");
    VERIFIE_ENTIER(verif::Compteur::copies, 0, "sans aucune copie");
    return BILAN();
}
