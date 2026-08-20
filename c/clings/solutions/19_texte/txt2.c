#include "verif.h"
#include <stdint.h>

const int PAS_FINI = 0;

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

static uint32_t point_a(const char *texte, size_t position, size_t *longueur) {
    uint32_t point = 0;
    *longueur = decoder_une_sequence((const unsigned char *)texte + position, &point);
    return point;
}

int main(void) {
    size_t longueur = 0;

    VERIFIE_ENTIER(point_a("A", 0, &longueur), 0x41, "la lettre A vaut U+0041");
    VERIFIE_ENTIER(longueur, 1, "et tient sur un octet");

    VERIFIE_ENTIER(point_a("caf\xc3\xa9", 3, &longueur), 0xE9, "le e accent aigu vaut U+00E9");
    VERIFIE_ENTIER(longueur, 2, "et tient sur deux octets");

    VERIFIE_ENTIER(point_a("\xe2\x82\xac", 0, &longueur), 0x20AC, "le signe euro vaut U+20AC");
    VERIFIE_ENTIER(longueur, 3, "et tient sur trois octets");

    VERIFIE_ENTIER(point_a("\xf0\x9f\x91\x8d", 0, &longueur), 0x1F44D,
                   "le pouce leve vaut U+1F44D");
    VERIFIE_ENTIER(longueur, 4, "et tient sur quatre octets");

    const char *phrase = "\xc3\xa9t\xc3\xa9 \xe2\x82\xac";
    uint32_t somme = 0;
    size_t nombre = 0;
    for (size_t position = 0; phrase[position] != '\0';) {
        uint32_t point = 0;
        position += decoder_une_sequence((const unsigned char *)phrase + position, &point);
        somme += point;
        nombre++;
    }

    VERIFIE_ENTIER(nombre, 5, "la phrase compte cinq points de code");
    VERIFIE_ENTIER(somme, 0xE9 + 0x74 + 0xE9 + 0x20 + 0x20AC,
                   "et la somme de leurs valeurs se retrouve exactement");
    return BILAN();
}
