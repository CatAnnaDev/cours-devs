#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

int compter_couches() { return 7; }

int couches_publiees();

int EPAISSEUR_TOTALE = couches_publiees() * 20;

int NOMBRE_DE_COUCHES = compter_couches();

int couches_publiees() { return NOMBRE_DE_COUCHES; }

struct Journal {
    int entrees;

    constexpr Journal() : entrees(0) {}
    ~Journal() { entrees = -1; }
};

constinit Journal JOURNAL{};

}

int main() {
    VERIFIE_ENTIER(NOMBRE_DE_COUCHES, 7, "la globale vaut ce que son initialiseur constant a mis");
    VERIFIE_ENTIER(EPAISSEUR_TOTALE, 140,
                   "toute initialisation statique precede toute initialisation dynamique");

    NOMBRE_DE_COUCHES = 9;
    VERIFIE_ENTIER(NOMBRE_DE_COUCHES, 9,
                   "constinit n'est pas const : la variable reste modifiable");

    JOURNAL.entrees = 4;
    VERIFIE_ENTIER(JOURNAL.entrees, 4,
                   "constinit accepte un destructeur non constexpr, ce que constexpr refuse");

    return BILAN();
}
