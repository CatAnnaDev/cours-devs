#include "verif.h"

const int PAS_FINI = 1;

typedef struct {
    int points;
    int niveau;
} Joueur;

void gagner(Joueur joueur, int points) {
    joueur.points += points;
    if (joueur.points >= 100) {
        joueur.niveau++;
    }
}

int main(void) {
    Joueur joueur = {.points = 90, .niveau = 1};

    gagner(joueur, 20);

    VERIFIE_ENTIER(joueur.points, 110, "les vingt points sont arrives dans l'original");
    VERIFIE_ENTIER(joueur.niveau, 2, "passer la barre des cent fait monter d'un niveau");
    return BILAN();
}
