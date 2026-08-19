#include "caisse.h"

static int somme(const int *valeurs, int nombre) {
    int total = 0;
    for (int i = 0; i < nombre; i++) {
        total += valeurs[i];
    }
    return total;
}

int caisse_total(const int *prix, int nombre) {
    return somme(prix, nombre);
}
