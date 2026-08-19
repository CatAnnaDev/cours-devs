#include "caisse.h"
#include "verif.h"

const int PAS_FINI = 0;

static int somme(const int *valeurs, int nombre) {
    int total = 0;
    for (int i = 0; i < nombre; i++) {
        total += valeurs[i] * valeurs[i];
    }
    return total;
}

int main(void) {
    const int prix[4] = {3, 5, 7, 9};

    VERIFIE_ENTIER(caisse_total(prix, 4), 24, "caisse.c additionne les prix");
    VERIFIE_ENTIER(caisse_total(prix, 2), 8, "caisse.c s'arrete ou on lui dit");
    VERIFIE_ENTIER(somme(prix, 4), 164, "mod2.c additionne les carres");
    VERIFIE_ENTIER(somme(prix, 2), 34, "les deux auxiliaires coexistent sans se marcher dessus");
    return BILAN();
}
