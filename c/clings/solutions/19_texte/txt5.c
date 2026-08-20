#include "verif.h"

const int PAS_FINI = 0;

static size_t octets_modifies = 0;

static void en_majuscules_ascii(char *texte) {
    for (size_t indice = 0; texte[indice] != '\0'; indice++) {
        unsigned char octet = (unsigned char)texte[indice];

        if (octet >= 'a' && octet <= 'z') {
            texte[indice] = (char)(octet - ('a' - 'A'));
            octets_modifies++;
        }
    }
}

int main(void) {
    char tampon[64];

    octets_modifies = 0;
    snprintf(tampon, sizeof tampon, "%s", "sel et poivre");
    en_majuscules_ascii(tampon);
    VERIFIE_TEXTE(tampon, "SEL ET POIVRE", "l'ASCII passe en majuscules sans probleme");
    VERIFIE_ENTIER(octets_modifies, 11, "onze lettres touchees, les espaces non");

    octets_modifies = 0;
    snprintf(tampon, sizeof tampon, "%s", "caf\xc3\xa9 cr\xc3\xa8me");
    en_majuscules_ascii(tampon);
    VERIFIE_TEXTE(tampon, "CAF\xc3\xa9 CR\xc3\xa8ME", "les lettres accentuees sortent inchangees");
    VERIFIE_ENTIER(octets_modifies, 7, "seuls les sept octets ASCII minuscules ont bouge");

    octets_modifies = 0;
    snprintf(tampon, sizeof tampon, "%s", "\xe2\x82\xac 5 euros");
    en_majuscules_ascii(tampon);
    VERIFIE_TEXTE(tampon, "\xe2\x82\xac 5 EUROS", "le signe euro traverse la conversion intact");
    VERIFIE_ENTIER((unsigned char)tampon[0], 0xE2, "son octet de tete n'est pas une lettre");
    VERIFIE_ENTIER(octets_modifies, 5, "cinq lettres touchees, pas une de plus");

    octets_modifies = 0;
    snprintf(tampon, sizeof tampon, "%s", "\xf0\x9f\x91\x8d ok");
    en_majuscules_ascii(tampon);
    VERIFIE_TEXTE(tampon, "\xf0\x9f\x91\x8d OK", "un emoji sort sans une egratignure");
    VERIFIE_ENTIER((unsigned char)tampon[0], 0xF0, "son octet de tete est reste un octet de tete");
    VERIFIE_ENTIER(octets_modifies, 2, "deux lettres touchees");
    return BILAN();
}
