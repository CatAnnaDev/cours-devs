#include <stddef.h>

#include "verif.h"

const int PAS_FINI = 0;

typedef struct {
    char drapeau;
    int compte;
    char lettre;
} Desordre;

int main(void) {
    VERIFIE_ENTIER(sizeof(Desordre), 12, "la struct fait 12 octets et pas 6");
    VERIFIE_ENTIER(offsetof(Desordre, drapeau), 0, "le premier champ commence a l'octet 0");
    VERIFIE_ENTIER(offsetof(Desordre, compte), 4, "le int saute jusqu'au prochain multiple de 4");
    VERIFIE_ENTIER(offsetof(Desordre, lettre), 8, "le second char suit le int sans remplissage");
    VERIFIE_ENTIER(_Alignof(Desordre), 4, "la struct s'aligne comme son champ le plus exigeant");
    return BILAN();
}
