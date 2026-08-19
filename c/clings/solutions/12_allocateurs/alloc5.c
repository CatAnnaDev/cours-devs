#include "verif.h"

#include <stdalign.h>
#include <stddef.h>
#include <stdint.h>

const int PAS_FINI = 0;

typedef union {
    size_t taille;
    max_align_t contrainte_d_alignement;
} En_tete;

void *mon_allouer(size_t taille) {
    if (taille > SIZE_MAX - sizeof(En_tete)) {
        return NULL;
    }
    En_tete *entete = suivi_malloc(sizeof(En_tete) + taille);
    if (entete == NULL) {
        return NULL;
    }
    entete->taille = taille;
    return entete + 1;
}

size_t mon_taille(const void *bloc) {
    return ((const En_tete *)bloc - 1)->taille;
}

void mon_liberer(void *bloc) {
    if (bloc == NULL) {
        return;
    }
    suivi_free((En_tete *)bloc - 1);
}

int main(void) {
    char *texte = mon_allouer(16);
    VERIFIE(texte != NULL, "l'allocation reussit");
    VERIFIE_ENTIER(mon_taille(texte), 16, "l'en-tete se relit juste avant le pointeur rendu");
    VERIFIE_ENTIER((uintptr_t)texte % alignof(max_align_t), 0,
                   "le pointeur rendu reste utilisable pour n'importe quel type");
    memcpy(texte, "seize octets ici", 16);
    VERIFIE(memcmp(texte, "seize octets ici", 16) == 0, "les seize octets se relisent");

    double *mesures = mon_allouer(4 * sizeof(double));
    VERIFIE(mesures != NULL, "la deuxieme allocation reussit");
    VERIFIE_ENTIER(mon_taille(mesures), 4 * sizeof(double), "chaque bloc porte sa propre taille");
    for (int i = 0; i < 4; i++) {
        mesures[i] = i * 0.25;
    }
    VERIFIE_REEL(mesures[3], 0.75, "les quatre doubles se relisent");
    VERIFIE_ENTIER(mon_taille(texte), 16, "et la taille du premier bloc n'a pas bouge");

    mon_liberer(texte);
    mon_liberer(mesures);
    mon_liberer(NULL);
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
