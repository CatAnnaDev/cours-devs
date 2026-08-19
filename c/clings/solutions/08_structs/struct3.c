#include "verif.h"

const int PAS_FINI = 0;

typedef struct {
    double poids;
    double vitesse;
    int identifiant;
    char actif;
    char type;
} Particule;

int main(void) {
    Particule particule = {
        .actif = 1,
        .poids = 1.5,
        .type = 'a',
        .vitesse = 2.5,
        .identifiant = 7,
    };

    VERIFIE_ENTIER(sizeof(Particule), 24, "bien ranges, les memes champs tiennent en 24 octets");
    VERIFIE_ENTIER(particule.actif, 1, "actif vaut toujours 1");
    VERIFIE_ENTIER(particule.type, 'a', "type vaut toujours la lettre a");
    VERIFIE_ENTIER(particule.identifiant, 7, "identifiant vaut toujours 7");
    VERIFIE_REEL(particule.poids, 1.5, "poids vaut toujours 1.5");
    VERIFIE_REEL(particule.vitesse, 2.5, "vitesse vaut toujours 2.5");
    return BILAN();
}
