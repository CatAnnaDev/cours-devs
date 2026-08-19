#include <stddef.h>

#include "verif.h"

const int PAS_FINI = 1;

typedef struct {
    char drapeau;
    int compte;
    char lettre;
} Desordre;

int main(void) {
    VERIFIE_ENTIER(sizeof(Desordre), A_FAIRE, "la struct fait 12 octets et pas 6");
    VERIFIE_ENTIER(offsetof(Desordre, drapeau), A_FAIRE, "le premier champ commence a l'octet 0");
    VERIFIE_ENTIER(offsetof(Desordre, compte), A_FAIRE, "le int saute jusqu'au prochain multiple de 4");
    VERIFIE_ENTIER(offsetof(Desordre, lettre), A_FAIRE, "le second char suit le int sans remplissage");
    VERIFIE_ENTIER(_Alignof(Desordre), A_FAIRE, "la struct s'aligne comme son champ le plus exigeant");
    return BILAN();
}
