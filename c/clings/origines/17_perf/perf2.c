#include "verif.h"

const int PAS_FINI = 1;

#define PHRASE "le compilateur ne devine pas ce que tu n'as pas dit"

enum {
    VOYELLES_ATTENDUES = 18,
    LONGUEUR_ATTENDUE = 51
};

static long appels_a_longueur = 0;
static long caracteres_examines = 0;

static size_t longueur_du_texte(const char *texte) {
    appels_a_longueur++;
    size_t longueur = 0;
    while (texte[longueur] != '\0') {
        caracteres_examines++;
        longueur++;
    }
    return longueur;
}

static int est_une_voyelle(char lettre) {
    return lettre == 'a' || lettre == 'e' || lettre == 'i' || lettre == 'o' || lettre == 'u'
           || lettre == 'y';
}

static int compter_les_voyelles(const char *texte) {
    int total = 0;
    for (size_t indice = 0; indice < longueur_du_texte(texte); indice++) {
        if (est_une_voyelle(texte[indice])) {
            total++;
        }
    }
    return total;
}

int main(void) {
    static const char TEXTE_COURT[] = PHRASE;
    static const char TEXTE_LONG[] = PHRASE PHRASE;

    VERIFIE_ENTIER(sizeof TEXTE_COURT - 1, LONGUEUR_ATTENDUE, "le texte court fait sa longueur");
    VERIFIE_ENTIER(sizeof TEXTE_LONG - 1, 2 * LONGUEUR_ATTENDUE, "le texte long est le double");

    appels_a_longueur = 0;
    caracteres_examines = 0;
    int voyelles_court = compter_les_voyelles(TEXTE_COURT);
    long appels_court = appels_a_longueur;
    long examines_court = caracteres_examines;

    appels_a_longueur = 0;
    caracteres_examines = 0;
    int voyelles_long = compter_les_voyelles(TEXTE_LONG);
    long appels_long = appels_a_longueur;
    long examines_long = caracteres_examines;

    VERIFIE_ENTIER(voyelles_court, VOYELLES_ATTENDUES, "le compte de voyelles reste juste");
    VERIFIE_ENTIER(voyelles_long, 2 * VOYELLES_ATTENDUES, "et il double avec le texte");
    VERIFIE_ENTIER(appels_court, 1, "la longueur du texte court se calcule une seule fois");
    VERIFIE_ENTIER(appels_long, 1, "celle du texte long aussi");
    VERIFIE_ENTIER(examines_court, LONGUEUR_ATTENDUE, "le texte court n'est traverse qu'une fois");
    VERIFIE_ENTIER(examines_long, 2 * examines_court, "doubler le texte double le travail");
    return BILAN();
}
