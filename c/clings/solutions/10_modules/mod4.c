#include "forme.h"
#include "dessin.h"
#include "verif.h"

const int PAS_FINI = 0;

int main(void) {
    const struct Forme carte = {.largeur = 7, .hauteur = 5};
    const struct Forme tuile = {.largeur = 4, .hauteur = 4};

    VERIFIE_ENTIER(forme_aire(&carte), 35, "forme.h calcule l'aire");
    VERIFIE_ENTIER(forme_aire(&tuile), 16, "l'aire d'une tuile carree");
    VERIFIE_ENTIER(dessin_orientation(&carte), ORIENTATION_PAYSAGE, "dessin.h lit la meme forme");
    VERIFIE_ENTIER(ORIENTATION_CARRE, 2, "l'enum n'est declare qu'une fois");
    return BILAN();
}
