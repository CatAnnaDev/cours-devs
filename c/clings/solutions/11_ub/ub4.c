#include "verif.h"

const int PAS_FINI = 0;

typedef union {
    int entiers[3];
    unsigned char octets[12];
} Trame;

int lire_entier(const unsigned char *depart) {
    int valeur;
    memcpy(&valeur, depart, sizeof valeur);
    return valeur;
}

int main(void) {
    Trame trame;
    memset(trame.octets, 0, sizeof trame.octets);

    int identifiant = 1000;
    int longueur = 2026;
    memcpy(trame.octets, &identifiant, sizeof identifiant);
    memcpy(trame.octets + 5, &longueur, sizeof longueur);

    VERIFIE_ENTIER(lire_entier(trame.octets), 1000, "le champ pose a l'octet 0 est aligne");
    VERIFIE_ENTIER(lire_entier(trame.octets + 5), 2026, "le champ pose a l'octet 5 ne l'est pas");
    return BILAN();
}
