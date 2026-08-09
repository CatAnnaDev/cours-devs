#include "verif.h"

const int PAS_FINI = 0;

#define LARGEUR 4
#define HAUTEUR 3

int lire(const int grille[HAUTEUR][LARGEUR], int ligne, int colonne) {
    return grille[ligne][colonne];
}

int lire_a_plat(const int *donnees, int ligne, int colonne) {
    return donnees[ligne * LARGEUR + colonne];
}

int main(void) {
    int grille[HAUTEUR][LARGEUR] = {
        {0, 1, 2, 3},
        {4, 5, 6, 7},
        {8, 9, 10, 11},
    };

    VERIFIE_ENTIER(lire(grille, 1, 2), 6, "la case (1,2) vaut 6");
    VERIFIE_ENTIER(lire_a_plat(&grille[0][0], 2, 1), 9, "la meme case a plat");
    VERIFIE_ENTIER(sizeof(grille), 48, "la grille fait 48 octets");
    return BILAN();
}
