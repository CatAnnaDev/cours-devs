#include "verif.h"

const int PAS_FINI = 0;

enum {
    DIVISEUR = 8,
    DECALAGE = 3,
    BORNE = 64,
    DESACCORDS_ATTENDUS = 56,
    VALEURS = 40
};

static int diviser_par_huit(int valeur) {
    return valeur / DIVISEUR;
}

static int reste_de_huit(int valeur) {
    return valeur % DIVISEUR;
}

static unsigned diviser_par_huit_non_signe(unsigned valeur) {
    return valeur / DIVISEUR;
}

static long somme_simple(const int *valeurs, int nombre) {
    long somme = 0;
    for (int indice = 0; indice < nombre; indice++) {
        somme += valeurs[indice];
    }
    return somme;
}

static long somme_deroulee_a_la_main(const int *valeurs, int nombre) {
    long somme = 0;
    int indice = 0;
    for (; indice + 4 <= nombre; indice += 4) {
        somme += valeurs[indice];
        somme += valeurs[indice + 1];
        somme += valeurs[indice + 2];
        somme += valeurs[indice + 3];
    }
    for (; indice < nombre; indice++) {
        somme += valeurs[indice];
    }
    return somme;
}

int main(void) {
    VERIFIE_ENTIER(diviser_par_huit(24), 3, "sur un positif, division et decalage coincident");
    VERIFIE_ENTIER(24 >> DECALAGE, 3, "le decalage donne la meme chose sur un positif");
    VERIFIE_ENTIER(diviser_par_huit(-9), -1, "la division entiere tronque vers zero");
    VERIFIE_ENTIER(-9 >> DECALAGE, -2, "le decalage arithmetique, lui, arrondit vers le bas");
    VERIFIE_ENTIER(reste_de_huit(-9), -1, "le reste garde le signe du dividende");
    VERIFIE_ENTIER(-9 & (DIVISEUR - 1), 7, "le masque binaire rend toujours un positif");

    int desaccords = 0;
    for (int valeur = -BORNE; valeur <= BORNE; valeur++) {
        if (diviser_par_huit(valeur) != valeur >> DECALAGE) {
            desaccords++;
        }
    }
    VERIFIE_ENTIER(desaccords, DESACCORDS_ATTENDUS,
                   "sur cent vingt-neuf valeurs, cinquante-six desaccords");

    unsigned desaccords_non_signes = 0;
    for (unsigned valeur = 0; valeur <= 2u * BORNE; valeur++) {
        if (diviser_par_huit_non_signe(valeur) != valeur >> DECALAGE) {
            desaccords_non_signes++;
        }
    }
    VERIFIE_ENTIER(desaccords_non_signes, 0,
                   "sur des non signes, decalage et division sont interchangeables");

    static int echantillon[VALEURS];
    for (int indice = 0; indice < VALEURS; indice++) {
        echantillon[indice] = indice % 7 * 13 - 41;
    }
    VERIFIE_ENTIER(somme_deroulee_a_la_main(echantillon, VALEURS),
                   somme_simple(echantillon, VALEURS),
                   "derouler la boucle a la main ne change pas la somme");
    VERIFIE_ENTIER(somme_deroulee_a_la_main(echantillon, VALEURS - 3),
                   somme_simple(echantillon, VALEURS - 3),
                   "meme quand le nombre d'elements n'est pas un multiple de quatre");
    return BILAN();
}
