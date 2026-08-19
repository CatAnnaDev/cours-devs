#include "verif.h"

const int PAS_FINI = 0;

typedef struct {
    char *nom;
    int score;
} Entree;

Entree entree_creer(const char *nom, int score) {
    char *propre = suivi_malloc(strlen(nom) + 1);
    strcpy(propre, nom);
    return (Entree){.nom = propre, .score = score};
}

Entree entree_copier(const Entree *source) {
    return entree_creer(source->nom, source->score);
}

void entree_detruire(Entree *entree) {
    suivi_free(entree->nom);
    entree->nom = NULL;
}

int main(void) {
    Entree original = entree_creer("anna", 10);
    Entree copie = entree_copier(&original);

    copie.score = 20;

    VERIFIE_TEXTE(copie.nom, "anna", "la copie porte le meme nom");
    VERIFIE(copie.nom != original.nom, "chacune possede son propre nom");
    VERIFIE_ENTIER(original.score, 10, "le score de l'original n'a pas bouge");

    entree_detruire(&copie);
    entree_detruire(&original);

    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
