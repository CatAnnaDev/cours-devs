#include "verif.h"

const int PAS_FINI = 0;

typedef struct {
    int *donnees;
    size_t taille;
    size_t capacite;
} Vecteur;

int ajouter(Vecteur *vecteur, int valeur) {
    if (vecteur->taille == vecteur->capacite) {
        size_t nouvelle = vecteur->capacite == 0 ? 1 : vecteur->capacite * 2;
        int *agrandi = suivi_realloc(vecteur->donnees, nouvelle * sizeof(int));
        if (agrandi == NULL) {
            return 0;
        }
        vecteur->donnees = agrandi;
        vecteur->capacite = nouvelle;
    }
    vecteur->donnees[vecteur->taille] = valeur;
    vecteur->taille++;
    return 1;
}

int main(void) {
    Vecteur vecteur = {NULL, 0, 0};

    for (int i = 0; i < 100; i++) {
        VERIFIE(ajouter(&vecteur, i * 2) == 1, i == 0 ? "premier ajout" : "ajouts suivants");
        if (i > 0) {
            verif_reussites--;
        }
    }

    VERIFIE_ENTIER(vecteur.taille, 100, "cent elements");
    VERIFIE(vecteur.capacite >= 100, "la capacite a suivi");
    VERIFIE_ENTIER(vecteur.donnees[99], 198, "le dernier element est correct");

    suivi_free(vecteur.donnees);
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
