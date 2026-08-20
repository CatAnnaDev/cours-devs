#include "verif.h"
#include <stdint.h>

const int PAS_FINI = 0;

enum {
    MOTS = 5
};

static size_t decoder_une_sequence(const unsigned char *octets, uint32_t *point) {
    unsigned char tete = octets[0];

    if (tete < 0x80) {
        *point = tete;
        return 1;
    }
    if ((tete & 0xE0) == 0xC0) {
        *point = ((uint32_t)(tete & 0x1F) << 6) | (octets[1] & 0x3F);
        return 2;
    }
    if ((tete & 0xF0) == 0xE0) {
        *point = ((uint32_t)(tete & 0x0F) << 12) | ((uint32_t)(octets[1] & 0x3F) << 6) |
                 (octets[2] & 0x3F);
        return 3;
    }
    *point = ((uint32_t)(tete & 0x07) << 18) | ((uint32_t)(octets[1] & 0x3F) << 12) |
             ((uint32_t)(octets[2] & 0x3F) << 6) | (octets[3] & 0x3F);
    return 4;
}

static unsigned char lettre_de_base(uint32_t point) {
    uint32_t minuscule = point >= 0x00C0 && point <= 0x00DE ? point + 0x20 : point;

    if (minuscule >= 'A' && minuscule <= 'Z') {
        return (unsigned char)(minuscule - 'A' + 'a');
    }
    if (minuscule >= 'a' && minuscule <= 'z') {
        return (unsigned char)minuscule;
    }
    switch (minuscule) {
    case 0x00E0: case 0x00E1: case 0x00E2: case 0x00E3: case 0x00E4: case 0x00E5:
        return 'a';
    case 0x00E7:
        return 'c';
    case 0x00E8: case 0x00E9: case 0x00EA: case 0x00EB:
        return 'e';
    case 0x00EC: case 0x00ED: case 0x00EE: case 0x00EF:
        return 'i';
    case 0x00F1:
        return 'n';
    case 0x00F2: case 0x00F3: case 0x00F4: case 0x00F5: case 0x00F6:
        return 'o';
    case 0x00F9: case 0x00FA: case 0x00FB: case 0x00FC:
        return 'u';
    case 0x00FD: case 0x00FF:
        return 'y';
    default:
        return 0;
    }
}

static unsigned char lettre_suivante(const unsigned char **curseur) {
    while (**curseur != '\0') {
        uint32_t point = 0;
        unsigned char lettre;

        *curseur += decoder_une_sequence(*curseur, &point);
        lettre = lettre_de_base(point);
        if (lettre != 0) {
            return lettre;
        }
    }
    return 0;
}

static int comparer_au_dictionnaire(const void *gauche, const void *droite) {
    const char *const *a = gauche;
    const char *const *b = droite;
    const unsigned char *curseur_a = (const unsigned char *)*a;
    const unsigned char *curseur_b = (const unsigned char *)*b;

    for (;;) {
        unsigned char lettre_a = lettre_suivante(&curseur_a);
        unsigned char lettre_b = lettre_suivante(&curseur_b);

        if (lettre_a != lettre_b) {
            return lettre_a < lettre_b ? -1 : 1;
        }
        if (lettre_a == 0) {
            return strcmp(*a, *b);
        }
    }
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
