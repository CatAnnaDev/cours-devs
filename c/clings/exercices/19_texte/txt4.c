#include "verif.h"

const int PAS_FINI = 1;

static size_t coupe_sure(const char *texte, size_t maximum) {
    size_t longueur = strlen(texte);

    if (longueur <= maximum) {
        return longueur;
    }
    return maximum;
}

static int coupe_propre(const char *texte, size_t coupe) {
    return ((unsigned char)texte[coupe] & 0xC0) != 0x80;
}

int main(void) {
    const char *phrase = "caf\xc3\xa9 chaud";
    const char *pouce = "\xf0\x9f\x91\x8d ok";
    char tampon[32];
    size_t coupe;

    VERIFIE_ENTIER(strlen(phrase), 11, "la phrase pese onze octets");

    coupe = coupe_sure(phrase, 20);
    VERIFIE_ENTIER(coupe, 11, "une limite plus large que la phrase ne coupe rien");
    VERIFIE(coupe_propre(phrase, coupe), "et ne tombe pas au milieu d'une sequence");

    coupe = coupe_sure(phrase, 5);
    VERIFIE_ENTIER(coupe, 5, "une limite de cinq octets tombe pile apres l'accent");
    VERIFIE(coupe_propre(phrase, coupe), "la coupe tombe sur un debut de sequence");
    snprintf(tampon, sizeof tampon, "%.*s", (int)coupe, phrase);
    VERIFIE_TEXTE(tampon, "caf\xc3\xa9", "et garde le mot accentue entier");

    coupe = coupe_sure(phrase, 4);
    VERIFIE_ENTIER(coupe, 3, "une limite de quatre octets doit reculer jusqu'a trois");
    VERIFIE(coupe_propre(phrase, coupe), "la coupe ne laisse pas une sequence a moitie");
    snprintf(tampon, sizeof tampon, "%.*s", (int)coupe, phrase);
    VERIFIE_TEXTE(tampon, "caf", "et abandonne la lettre accentuee entiere");

    VERIFIE_ENTIER(strlen(pouce), 7, "la seconde phrase pese sept octets");

    coupe = coupe_sure(pouce, 3);
    VERIFIE_ENTIER(coupe, 0, "trois octets ne suffisent pas au pouce leve");
    VERIFIE(coupe_propre(pouce, coupe), "mieux vaut ne rien garder qu'un demi caractere");

    coupe = coupe_sure(pouce, 4);
    VERIFIE_ENTIER(coupe, 4, "quatre octets le contiennent tout juste");
    VERIFIE(coupe_propre(pouce, coupe), "la coupe reste propre");

    coupe = coupe_sure(pouce, 6);
    VERIFIE_ENTIER(coupe, 6, "six octets ajoutent l'espace et le o");
    VERIFIE(coupe_propre(pouce, coupe), "la coupe reste propre sur de l'ASCII");
    snprintf(tampon, sizeof tampon, "%.*s", (int)coupe, pouce);
    VERIFIE_TEXTE(tampon, "\xf0\x9f\x91\x8d o", "et le resultat se relit sans surprise");
    return BILAN();
}
