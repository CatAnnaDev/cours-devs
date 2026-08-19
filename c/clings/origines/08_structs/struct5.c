#include "verif.h"

const int PAS_FINI = 1;

typedef enum {
    VALEUR_ENTIER,
    VALEUR_REEL,
    VALEUR_TEXTE
} Genre;

typedef struct {
    Genre genre;
    union {
        int entier;
        double reel;
        const char *texte;
    } comme;
} Valeur;

void ecrire(const Valeur *valeur, char *tampon, size_t taille) {
    snprintf(tampon, taille, "%d", valeur->comme.entier);
}

int main(void) {
    Valeur entier = {.genre = VALEUR_ENTIER, .comme = {.entier = 42}};
    Valeur reel = {.genre = VALEUR_REEL, .comme = {.reel = 1.5}};
    Valeur texte = {.genre = VALEUR_TEXTE, .comme = {.texte = "coucou"}};
    char tampon[32];

    ecrire(&entier, tampon, sizeof(tampon));
    VERIFIE_TEXTE(tampon, "42", "l'alternative entiere s'ecrit 42");

    ecrire(&reel, tampon, sizeof(tampon));
    VERIFIE_TEXTE(tampon, "1.5", "l'alternative reelle s'ecrit 1.5");

    ecrire(&texte, tampon, sizeof(tampon));
    VERIFIE_TEXTE(tampon, "coucou", "l'alternative texte s'ecrit coucou");

    VERIFIE_ENTIER(sizeof(Valeur), 16, "le tag plus la plus grande alternative");
    return BILAN();
}
