#include "verif.h"
#include <stdint.h>

const int PAS_FINI = 1;

#define TRAME_TAILLE 9

static const uint8_t TRAME_RECUE[TRAME_TAILLE] = {
    0x07,
    0x78, 0x56, 0x34, 0x12,
    0x2A, 0x00, 0x00, 0x00,
};

static uint32_t lire_u32_petit_boutiste(const uint8_t *octets) {
    return *(const uint32_t *)octets;
}

int main(void) {
    uint8_t *trame = suivi_malloc(TRAME_TAILLE);
    VERIFIE(trame != NULL, "le tampon de reception est alloue");
    if (trame == NULL) {
        return BILAN();
    }
    memcpy(trame, TRAME_RECUE, TRAME_TAILLE);

    VERIFIE_ENTIER(trame[0], 0x07, "l'octet de type ouvre la trame");
    VERIFIE_ENTIER(lire_u32_petit_boutiste(trame + 1), 0x12345678u,
                   "l'identifiant occupe les quatre octets suivants");
    VERIFIE_ENTIER(lire_u32_petit_boutiste(trame + 5), 0x0000002Au,
                   "puis vient le compteur");

    VERIFIE_ENTIER(trame[1], 0x78, "la trame n'a pas ete modifiee par la lecture");

    suivi_free(trame);
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
