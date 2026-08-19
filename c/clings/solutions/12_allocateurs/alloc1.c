#include "verif.h"

const int PAS_FINI = 0;

#define BUMP_CAPACITE 64

typedef struct {
    unsigned char *octets;
    size_t capacite;
    size_t utilise;
} Bump;

static unsigned char zone[BUMP_CAPACITE];
static Bump bump = {zone, BUMP_CAPACITE, 0};

void *bump_allouer(Bump *tampon, size_t taille) {
    size_t depart = tampon->utilise;
    if (taille > tampon->capacite - depart) {
        return NULL;
    }
    tampon->utilise = depart + taille;
    return tampon->octets + depart;
}

void remplir(unsigned char *bloc, size_t taille, unsigned char motif) {
    for (size_t i = 0; i < taille; i++) {
        bloc[i] = (unsigned char)(motif + i);
    }
}

int main(void) {
    unsigned char *premier = bump_allouer(&bump, 40);
    VERIFIE(premier != NULL, "les quarante premiers octets sont servis");
    remplir(premier, 40, 1);

    unsigned char *second = bump_allouer(&bump, 20);
    VERIFIE(second != NULL, "les vingt suivants tiennent encore");
    remplir(second, 20, 100);

    unsigned char *trop = bump_allouer(&bump, 8);
    VERIFIE(trop == NULL, "la demande qui deborde du tampon renvoie NULL");
    if (trop != NULL) {
        remplir(trop, 8, 200);
    }

    VERIFIE_ENTIER(premier[0], 1, "le premier bloc commence sur son motif");
    VERIFIE_ENTIER(premier[39], 40, "et se relit jusqu'a son dernier octet");
    VERIFIE_ENTIER(second[0], 100, "le deuxieme bloc garde le sien");
    VERIFIE_ENTIER(second[19], 119, "et n'a pas ete recouvert");
    VERIFIE_ENTIER(bump.utilise, 60, "seules les demandes servies consomment le tampon");
    return BILAN();
}
