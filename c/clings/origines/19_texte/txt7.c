#include "verif.h"
#include <stdint.h>

const int PAS_FINI = 1;

enum {
    MOTS = 5
};

static int comparer_au_dictionnaire(const void *gauche, const void *droite) {
    const char *const *a = gauche;
    const char *const *b = droite;

    return strcmp(*a, *b);
}

static void trier_les_mots(const char **mots, size_t nombre) {
    qsort(mots, nombre, sizeof *mots, comparer_au_dictionnaire);
}

int main(void) {
    const char *mots[MOTS] = {
        "zebre",
        "\xc3\xa9lan",
        "Ananas",
        "abricot",
        "\xc3\xa9tage"
    };

    VERIFIE(strcmp("Zoe", "abricot") < 0,
            "l'ordre des octets met toutes les majuscules avant les minuscules");
    VERIFIE(strcmp("\xc3\xa9lan", "zebre") > 0,
            "et rejette les lettres accentuees derriere le z");
    VERIFIE(strcmp("\xc3\xa9lan", "elan") > 0,
            "elan et son homographe accentue ne se suivent meme pas");
    VERIFIE(strcmp("cote", "cote") == 0, "en revanche l'egalite exacte se decide bien en octets");

    trier_les_mots(mots, MOTS);

    VERIFIE_TEXTE(mots[0], "abricot", "abricot vient en tete du dictionnaire");
    VERIFIE_TEXTE(mots[1], "Ananas", "puis Ananas, dont la majuscule ne decide de rien");
    VERIFIE_TEXTE(mots[2], "\xc3\xa9lan", "puis elan, dont l'accent ne decide de rien non plus");
    VERIFIE_TEXTE(mots[3], "\xc3\xa9tage", "puis etage, qui ne differe qu'a la deuxieme lettre");
    VERIFIE_TEXTE(mots[4], "zebre", "et zebre ferme la marche");
    return BILAN();
}
