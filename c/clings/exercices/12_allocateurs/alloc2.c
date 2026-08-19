#include "verif.h"

#include <stdalign.h>
#include <stdint.h>

const int PAS_FINI = 1;

#define BUMP_CAPACITE 64

typedef struct {
    unsigned char *octets;
    size_t capacite;
    size_t utilise;
} Bump;

static alignas(16) unsigned char zone[BUMP_CAPACITE];
static Bump bump = {zone, BUMP_CAPACITE, 0};

void *bump_allouer(Bump *tampon, size_t taille, size_t alignement) {
    if (alignement == 0 || (alignement & (alignement - 1)) != 0) {
        return NULL;
    }
    size_t depart = tampon->utilise;
    if (taille > tampon->capacite - depart) {
        return NULL;
    }
    tampon->utilise = depart + taille;
    return tampon->octets + depart;
}

int main(void) {
    char *drapeau = bump_allouer(&bump, sizeof(char), alignof(char));
    VERIFIE(drapeau != NULL, "l'octet de tete est servi");
    *drapeau = 'A';

    double *mesure = bump_allouer(&bump, sizeof(double), alignof(double));
    VERIFIE(mesure != NULL, "le double est servi lui aussi");
    VERIFIE_ENTIER((uintptr_t)mesure % alignof(double), 0,
                   "son adresse est un multiple de son alignement");
    *mesure = 1.5;
    VERIFIE_REEL(*mesure, 1.5, "le double se relit tel qu'il a ete ecrit");

    int *compteur = bump_allouer(&bump, sizeof(int), alignof(int));
    VERIFIE(compteur != NULL, "le int passe apres");
    VERIFIE_ENTIER((uintptr_t)compteur % alignof(int), 0, "et son adresse tombe juste aussi");
    *compteur = 7;
    VERIFIE_ENTIER(*compteur, 7, "le int se relit");

    VERIFIE_ENTIER(*drapeau, 'A', "l'octet de tete n'a pas ete recouvert");
    VERIFIE_ENTIER(bump.utilise, 20, "le remplissage d'alignement se voit dans le compteur");
    return BILAN();
}
