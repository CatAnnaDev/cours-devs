#include "verif.h"

const int PAS_FINI = 1;

enum { TAILLE = 8 };

static void copier_disjoint(int *restrict destination, const int *restrict source, int nombre) {
    for (int indice = 0; indice < nombre; indice++) {
        destination[indice] = source[indice];
    }
}

static void copier_chevauchant(int *destination, const int *source, int nombre) {
    for (int indice = 0; indice < nombre; indice++) {
        destination[indice] = source[indice];
    }
}

int main(void) {
    static const int ORIGINE[TAILLE] = {10, 20, 30, 40, 50, 60, 70, 80};
    static const int ATTENDU_A_DROITE[TAILLE] = {10, 10, 20, 30, 40, 50, 60, 70};
    static const int ATTENDU_A_GAUCHE[TAILLE] = {20, 30, 40, 50, 60, 70, 80, 80};

    int copie[TAILLE] = {0};
    copier_disjoint(copie, ORIGINE, TAILLE);
    VERIFIE(memcmp(copie, ORIGINE, sizeof ORIGINE) == 0,
            "entre deux zones disjointes, la copie avant marche");

    int par_le_general[TAILLE] = {0};
    copier_chevauchant(par_le_general, ORIGINE, TAILLE);
    VERIFIE(memcmp(par_le_general, ORIGINE, sizeof ORIGINE) == 0,
            "la version qui gere le recouvrement copie aussi le cas disjoint");

    int vers_la_droite[TAILLE];
    memcpy(vers_la_droite, ORIGINE, sizeof ORIGINE);
    copier_chevauchant(vers_la_droite + 1, vers_la_droite, TAILLE - 1);
    VERIFIE(memcmp(vers_la_droite, ATTENDU_A_DROITE, sizeof ATTENDU_A_DROITE) == 0,
            "decaler d'une case vers la droite n'ecrase pas ce qui reste a lire");

    int vers_la_gauche[TAILLE];
    memcpy(vers_la_gauche, ORIGINE, sizeof ORIGINE);
    copier_chevauchant(vers_la_gauche, vers_la_gauche + 1, TAILLE - 1);
    VERIFIE(memcmp(vers_la_gauche, ATTENDU_A_GAUCHE, sizeof ATTENDU_A_GAUCHE) == 0,
            "et decaler vers la gauche marche dans l'autre sens");

    int sur_place[TAILLE];
    memcpy(sur_place, ORIGINE, sizeof ORIGINE);
    copier_chevauchant(sur_place, sur_place, TAILLE);
    VERIFIE(memcmp(sur_place, ORIGINE, sizeof ORIGINE) == 0,
            "copier un tableau sur lui-meme ne change rien");
    return BILAN();
}
