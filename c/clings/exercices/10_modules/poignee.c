#include <stdlib.h>

#include "poignee.h"

struct Poignee {
    int capacite;
    int nombre;
    int total;
};

Poignee *poignee_creer(int capacite) {
    if (capacite <= 0) {
        return NULL;
    }
    Poignee *poignee = malloc(sizeof *poignee);
    if (poignee == NULL) {
        return NULL;
    }
    poignee->capacite = capacite;
    poignee->nombre = 0;
    poignee->total = 0;
    return poignee;
}

int poignee_ajouter(Poignee *poignee, int valeur) {
    if (poignee == NULL || poignee->nombre >= poignee->capacite) {
        return 0;
    }
    poignee->nombre++;
    poignee->total += valeur;
    return 1;
}

int poignee_total(const Poignee *poignee) {
    return poignee == NULL ? 0 : poignee->total;
}

void poignee_detruire(Poignee *poignee) {
    free(poignee);
}
