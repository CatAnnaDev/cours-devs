#include "verif.hpp"

const bool PAS_FINI = true;

int somme() {
    return 0;
}

int somme(int a) {
    return a;
}

int somme(int a, int b) {
    return a + b;
}

int main() {
    VERIFIE_ENTIER(somme(), 0, "aucune valeur, donc zero");
    VERIFIE_ENTIER(somme(1), 1, "une seule valeur");
    VERIFIE_ENTIER(somme(1, 2, 3, 4), 10, "quatre valeurs d'un coup");
    VERIFIE_REEL(somme(1.5, 2.5), 4.0, "et ca marche aussi pour des reels");
    return BILAN();
}
