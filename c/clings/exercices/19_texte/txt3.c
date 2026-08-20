#include "verif.h"
#include <stdint.h>

const int PAS_FINI = 1;

static int utf8_valide(const unsigned char *octets, size_t taille) {
    size_t position = 0;

    while (position < taille) {
        unsigned char tete = octets[position];
        size_t longueur;

        if (tete < 0x80) {
            longueur = 1;
        } else if (tete < 0xE0) {
            longueur = 2;
        } else if (tete < 0xF0) {
            longueur = 3;
        } else {
            longueur = 4;
        }
        if (taille - position < longueur) {
            return 0;
        }
        position += longueur;
    }
    return 1;
}

static int valide(const char *texte) {
    return utf8_valide((const unsigned char *)texte, strlen(texte));
}

int main(void) {
    VERIFIE(valide(""), "la chaine vide est de l'UTF-8 valide");
    VERIFIE(valide("sel et poivre"), "l'ASCII pur est de l'UTF-8 valide, octet pour octet");
    VERIFIE(valide("caf\xc3\xa9"), "une sequence de deux octets bien formee passe");
    VERIFIE(valide("\xe2\x82\xac"), "une sequence de trois octets aussi");
    VERIFIE(valide("\xf0\x9f\x91\x8d"), "une sequence de quatre octets aussi");
    VERIFIE(valide("\xef\xbb\xbf" "ok"), "un BOM est laid mais reste valide");

    VERIFIE(!valide("\xc3"), "une sequence coupee en fin de tampon est refusee");
    VERIFIE(!valide("caf\xc3\x28"), "un second octet qui n'est pas une continuation est refuse");
    VERIFIE(!valide("a\x80z"), "une continuation orpheline est refusee");
    VERIFIE(!valide("\xc0\xaf"), "la surlongue du slash sur deux octets est refusee");
    VERIFIE(!valide("\xe0\x80\xaf"), "la meme surlongue sur trois octets aussi");
    VERIFIE(!valide("\xc1\xbf"), "toute surlongue de deux octets est refusee");
    VERIFIE(!valide("\xed\xa0\x80"), "le substitut U+D800 n'a pas le droit d'etre encode");
    VERIFIE(!valide("\xed\xbf\xbf"), "ni aucun autre substitut jusqu'a U+DFFF");
    VERIFIE(!valide("\xf4\x90\x80\x80"), "au-dela de U+10FFFF il n'y a plus de points de code");
    VERIFIE(!valide("\xf8\x88\x80\x80\x80"), "et la vieille forme a cinq octets n'existe plus");
    return BILAN();
}
