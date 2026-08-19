#include "verif.h"

const int PAS_FINI = 0;

typedef struct Noeud {
    int valeur;
    struct Noeud *suivant;
} Noeud;

static int liste_inserer_en_tete(Noeud **tete, int valeur) {
    Noeud *neuf = suivi_malloc(sizeof(Noeud));
    if (neuf == NULL) {
        return 0;
    }
    neuf->valeur = valeur;
    neuf->suivant = *tete;
    *tete = neuf;
    return 1;
}

static int liste_supprimer(Noeud **tete, int valeur) {
    for (Noeud **curseur = tete; *curseur != NULL; curseur = &(*curseur)->suivant) {
        if ((*curseur)->valeur == valeur) {
            Noeud *mort = *curseur;
            *curseur = mort->suivant;
            suivi_free(mort);
            return 1;
        }
    }
    return 0;
}

static void liste_liberer(Noeud **tete) {
    Noeud *courant = *tete;
    while (courant != NULL) {
        Noeud *suivant = courant->suivant;
        suivi_free(courant);
        courant = suivant;
    }
    *tete = NULL;
}

static void liste_en_texte(const Noeud *tete, char *tampon, size_t taille) {
    size_t ecrits = 0;
    tampon[0] = '\0';
    for (const Noeud *courant = tete; courant != NULL; courant = courant->suivant) {
        int longueur = snprintf(tampon + ecrits, taille - ecrits, "%s%d",
                                ecrits == 0 ? "" : ",", courant->valeur);
        if (longueur < 0 || (size_t)longueur >= taille - ecrits) {
            return;
        }
        ecrits += (size_t)longueur;
    }
}

int main(void) {
    Noeud *tete = NULL;
    char rendu[64];

    int construite = 1;
    for (int valeur = 5; valeur >= 1; valeur--) {
        construite = construite && liste_inserer_en_tete(&tete, valeur);
    }
    VERIFIE(construite, "la liste de cinq noeuds est construite");
    liste_en_texte(tete, rendu, sizeof(rendu));
    VERIFIE_TEXTE(rendu, "1,2,3,4,5", "elle se lit dans l'ordre croissant");

    VERIFIE_ENTIER(liste_supprimer(&tete, 3), 1, "supprimer un element du milieu reussit");
    liste_en_texte(tete, rendu, sizeof(rendu));
    VERIFIE_TEXTE(rendu, "1,2,4,5", "le milieu a disparu");

    VERIFIE_ENTIER(liste_supprimer(&tete, 1), 1, "supprimer la tete reussit");
    liste_en_texte(tete, rendu, sizeof(rendu));
    VERIFIE_TEXTE(rendu, "2,4,5", "la tete a disparu");

    VERIFIE_ENTIER(liste_supprimer(&tete, 5), 1, "supprimer le dernier reussit");
    liste_en_texte(tete, rendu, sizeof(rendu));
    VERIFIE_TEXTE(rendu, "2,4", "la fin a disparu");

    VERIFIE_ENTIER(liste_supprimer(&tete, 99), 0, "supprimer un absent ne reussit pas");
    liste_en_texte(tete, rendu, sizeof(rendu));
    VERIFIE_TEXTE(rendu, "2,4", "la liste n'a pas bouge");

    VERIFIE_ENTIER(liste_supprimer(&tete, 2), 1, "supprimer la nouvelle tete reussit");
    VERIFIE_ENTIER(liste_supprimer(&tete, 4), 1, "supprimer le dernier noeud reussit");
    VERIFIE(tete == NULL, "la liste est vide");
    VERIFIE_ENTIER(liste_supprimer(&tete, 4), 0, "supprimer dans une liste vide ne reussit pas");

    liste_liberer(&tete);
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
