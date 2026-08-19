#include "poignee.h"
#include "verif.h"

const int PAS_FINI = 1;

int main(void) {
    Poignee *sac = poignee_creer(3);
    VERIFIE(sac != NULL, "la poignee est creee");

    VERIFIE_ENTIER(poignee_ajouter(sac, 10), 1, "le premier ajout passe");
    sac->total += 5;
    VERIFIE_ENTIER(poignee_ajouter(sac, 7), 1, "le troisieme ajout passe");

    VERIFIE_ENTIER(poignee_total(sac), 22, "le total suit les trois ajouts");
    VERIFIE_ENTIER(poignee_ajouter(sac, 100), 0, "le quatrieme ajout est refuse");
    VERIFIE_ENTIER(poignee_total(sac), 22, "un ajout refuse ne change pas le total");

    poignee_detruire(sac);
    return BILAN();
}
