#include "verif.h"

const int PAS_FINI = 1;

static size_t longueur_en_octets(const char *texte) {
    return strlen(texte);
}

static size_t longueur_en_points_de_code(const char *texte) {
    return strlen(texte);
}

int main(void) {
    const char *cafe = "caf\xc3\xa9";
    const char *ete = "\xc3\xa9t\xc3\xa9";
    const char *pouce = "\xf0\x9f\x91\x8d";
    const char *melange = "na\xc3\xafve \xe2\x82\xac 3";

    VERIFIE_ENTIER(longueur_en_octets(""), 0, "la chaine vide ne pese aucun octet");
    VERIFIE_ENTIER(longueur_en_points_de_code(""), 0, "et ne contient aucun caractere");

    VERIFIE_ENTIER(longueur_en_octets("sel"), 3, "trois lettres ASCII pesent trois octets");
    VERIFIE_ENTIER(longueur_en_points_de_code("sel"), 3, "et valent trois points de code");

    VERIFIE_ENTIER(longueur_en_octets(cafe), 5, "cafe accentue pese cinq octets");
    VERIFIE_ENTIER(longueur_en_points_de_code(cafe), 4, "et ne vaut que quatre points de code");

    VERIFIE_ENTIER(longueur_en_octets(ete), 5, "ete accentue pese cinq octets");
    VERIFIE_ENTIER(longueur_en_points_de_code(ete), 3, "et ne vaut que trois points de code");

    VERIFIE_ENTIER(longueur_en_octets(pouce), 4, "un pouce leve pese quatre octets");
    VERIFIE_ENTIER(longueur_en_points_de_code(pouce), 1, "et ne vaut qu'un seul point de code");

    VERIFIE_ENTIER(longueur_en_octets(melange), 12, "la phrase melangee pese douze octets");
    VERIFIE_ENTIER(longueur_en_points_de_code(melange), 9, "et ne vaut que neuf points de code");
    return BILAN();
}
