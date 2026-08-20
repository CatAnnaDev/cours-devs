#include "verif.h"

const int PAS_FINI = 0;

enum {
    NOMBRE = 4096,
    ACCUMULATEURS = 4,
    MAILLONS_DE_FUSION = 2
};

typedef struct {
    long total;
    int profondeur;
} Accumulateur;

static Accumulateur accumulateur_vide(void) {
    Accumulateur vide = {0, 0};
    return vide;
}

static void ajouter(Accumulateur *accumulateur, long valeur) {
    accumulateur->total += valeur;
    accumulateur->profondeur++;
}

static Accumulateur fusionner(Accumulateur gauche, Accumulateur droite) {
    Accumulateur resultat;
    resultat.total = gauche.total + droite.total;
    resultat.profondeur =
        (gauche.profondeur > droite.profondeur ? gauche.profondeur : droite.profondeur) + 1;
    return resultat;
}

static Accumulateur somme_en_serie(const long *valeurs, int nombre) {
    Accumulateur total = accumulateur_vide();
    for (int indice = 0; indice < nombre; indice++) {
        ajouter(&total, valeurs[indice]);
    }
    return total;
}

static Accumulateur somme_en_quatre(const long *valeurs, int nombre) {
    Accumulateur partiels[ACCUMULATEURS];
    for (int rang = 0; rang < ACCUMULATEURS; rang++) {
        partiels[rang] = accumulateur_vide();
    }
    for (int indice = 0; indice + ACCUMULATEURS <= nombre; indice += ACCUMULATEURS) {
        ajouter(&partiels[0], valeurs[indice]);
        ajouter(&partiels[1], valeurs[indice + 1]);
        ajouter(&partiels[2], valeurs[indice + 2]);
        ajouter(&partiels[3], valeurs[indice + 3]);
    }
    return fusionner(fusionner(partiels[0], partiels[1]), fusionner(partiels[2], partiels[3]));
}

int main(void) {
    static long valeurs[NOMBRE];
    for (int indice = 0; indice < NOMBRE; indice++) {
        valeurs[indice] = indice % 251 - 125;
    }

    Accumulateur serie = somme_en_serie(valeurs, NOMBRE);
    Accumulateur quatre = somme_en_quatre(valeurs, NOMBRE);

    VERIFIE_ENTIER(quatre.total, serie.total, "quatre accumulateurs donnent la meme somme entiere");
    VERIFIE_ENTIER(serie.profondeur, NOMBRE, "en serie, un maillon de dependance par element");
    VERIFIE_ENTIER(quatre.profondeur, NOMBRE / ACCUMULATEURS + MAILLONS_DE_FUSION,
                   "en quatre, la chaine est quatre fois plus courte");
    VERIFIE(serie.profondeur > 3 * quatre.profondeur, "le gain de chaine depasse un facteur trois");

    static const double MORCEAUX[3] = {0.1, 0.2, 0.3};
    double a_gauche = (MORCEAUX[0] + MORCEAUX[1]) + MORCEAUX[2];
    double a_droite = MORCEAUX[0] + (MORCEAUX[1] + MORCEAUX[2]);
    VERIFIE(a_gauche != a_droite, "sur des flottants, regrouper autrement change le resultat");
    VERIFIE(a_gauche > a_droite, "et c'est le regroupement de gauche qui rend le plus grand");
    return BILAN();
}
