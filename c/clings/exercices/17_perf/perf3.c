#include "verif.h"
#include <stdint.h>

const int PAS_FINI = 1;

enum {
    LIGNES = 64,
    COLONNES = 64,
    OCTETS_PAR_LIGNE_DE_CACHE = 64
};

static long lignes_chargees = 0;
static uintptr_t ligne_en_cache = 0;
static int cache_amorce = 0;

static void vider_le_cache(void) {
    lignes_chargees = 0;
    ligne_en_cache = 0;
    cache_amorce = 0;
}

static int lire_case(const int *adresse) {
    uintptr_t ligne = (uintptr_t)adresse / OCTETS_PAR_LIGNE_DE_CACHE;
    if (cache_amorce == 0 || ligne != ligne_en_cache) {
        ligne_en_cache = ligne;
        cache_amorce = 1;
        lignes_chargees++;
    }
    return *adresse;
}

static long somme_en_colonnes(int grille[LIGNES][COLONNES]) {
    long somme = 0;
    for (int colonne = 0; colonne < COLONNES; colonne++) {
        for (int ligne = 0; ligne < LIGNES; ligne++) {
            somme += lire_case(&grille[ligne][colonne]);
        }
    }
    return somme;
}

static long somme_en_lignes(int grille[LIGNES][COLONNES]) {
    long somme = 0;
    for (int colonne = 0; colonne < COLONNES; colonne++) {
        for (int ligne = 0; ligne < LIGNES; ligne++) {
            somme += lire_case(&grille[ligne][colonne]);
        }
    }
    return somme;
}

int main(void) {
    static _Alignas(OCTETS_PAR_LIGNE_DE_CACHE) int grille[LIGNES][COLONNES];
    for (int ligne = 0; ligne < LIGNES; ligne++) {
        for (int colonne = 0; colonne < COLONNES; colonne++) {
            grille[ligne][colonne] = ligne * COLONNES + colonne;
        }
    }

    VERIFIE_ENTIER((uintptr_t)grille % OCTETS_PAR_LIGNE_DE_CACHE, 0,
                   "la grille demarre au debut d'une ligne de cache");
    VERIFIE_ENTIER(sizeof grille[0], COLONNES * sizeof(int), "une rangee occupe ses colonnes");

    vider_le_cache();
    long somme_lente = somme_en_colonnes(grille);
    long chargements_lents = lignes_chargees;

    vider_le_cache();
    long somme_rapide = somme_en_lignes(grille);
    long chargements_rapides = lignes_chargees;

    long cases = (long)LIGNES * COLONNES;
    long cases_par_ligne_de_cache = OCTETS_PAR_LIGNE_DE_CACHE / (long)sizeof(int);

    VERIFIE_ENTIER(somme_rapide, somme_lente, "les deux parcours somment les memes cases");
    VERIFIE_ENTIER(chargements_lents, cases, "en colonnes, chaque case tombe dans une autre ligne");
    VERIFIE_ENTIER(chargements_rapides, cases / cases_par_ligne_de_cache,
                   "en rangees, une ligne de cache sert seize cases");
    VERIFIE_ENTIER(chargements_lents / chargements_rapides, cases_par_ligne_de_cache,
                   "le mauvais sens charge seize fois plus de lignes");
    return BILAN();
}
