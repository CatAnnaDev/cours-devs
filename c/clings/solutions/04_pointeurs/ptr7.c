#include "verif.h"

const int PAS_FINI = 0;

int lire_entier(const void *source) {
    const int *entier = source;
    return *entier;
}

int main(void) {
    int valeur = 7;

    VERIFIE_ENTIER(lire_entier(&valeur), 7, "on relit 7 a travers un void*");
    VERIFIE_ENTIER(sizeof(void *), sizeof(int *), "tous les pointeurs de donnees font la meme taille");
    return BILAN();
}
