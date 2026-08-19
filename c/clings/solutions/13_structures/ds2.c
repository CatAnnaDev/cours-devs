#include "verif.h"
#include <stdint.h>

const int PAS_FINI = 0;

typedef struct {
    int *donnees;
    size_t taille;
    size_t capacite;
} Suite;

static int suite_ajouter(Suite *suite, int valeur) {
    if (suite->taille == suite->capacite) {
        size_t nouvelle = suite->capacite == 0 ? 2 : suite->capacite * 2;
        int *agrandi = suivi_realloc(suite->donnees, nouvelle * sizeof(int));
        if (agrandi == NULL) {
            return 0;
        }
        suite->donnees = agrandi;
        suite->capacite = nouvelle;
    }
    suite->donnees[suite->taille] = valeur;
    suite->taille++;
    return 1;
}

static void suite_liberer(Suite *suite) {
    suivi_free(suite->donnees);
    suite->donnees = NULL;
    suite->taille = 0;
    suite->capacite = 0;
}

static int somme_des_bornes(Suite *suite, int a_ajouter) {
    size_t premier = 0;
    size_t dernier = suite->taille - 1;
    for (int i = 0; i < a_ajouter; i++) {
        if (!suite_ajouter(suite, i)) {
            return 0;
        }
    }
    return suite->donnees[premier] + suite->donnees[dernier];
}

int main(void) {
    Suite suite = {NULL, 0, 0};
    VERIFIE(suite_ajouter(&suite, 10) == 1, "la suite accepte un premier element");
    VERIFIE(suite_ajouter(&suite, 20) == 1, "la suite accepte un second element");
    VERIFIE_ENTIER(suite.capacite, 2, "la capacite de depart est volontairement minuscule");

    uintptr_t adresse_avant = (uintptr_t)suite.donnees;
    int somme = somme_des_bornes(&suite, 64);
    uintptr_t adresse_apres = (uintptr_t)suite.donnees;

    VERIFIE(adresse_avant != adresse_apres, "les reallocations ont bien deplace le bloc");
    VERIFIE_ENTIER(suite.taille, 66, "la suite compte soixante-six elements");
    VERIFIE_ENTIER(suite.capacite, 128, "la capacite a double six fois");
    VERIFIE_ENTIER(somme, 30, "les deux bornes valent encore 10 et 20");
    VERIFIE_ENTIER(suite.donnees[0], 10, "le premier element est intact");
    VERIFIE_ENTIER(suite.donnees[1], 20, "le second element est intact");
    VERIFIE_ENTIER(suite.donnees[65], 63, "le dernier ajout est en place");

    suite_liberer(&suite);
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
