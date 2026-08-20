#include "verif.h"

const int PAS_FINI = 1;

enum {
    CHAMPS_MAXIMUM = 8
};

typedef struct {
    const char *debut;
    size_t longueur;
} Champ;

static size_t decouper(char *ligne, char separateur, Champ *champs, size_t maximum) {
    char separateurs[2];
    size_t nombre = 0;

    separateurs[0] = separateur;
    separateurs[1] = '\0';

    for (char *morceau = strtok(ligne, separateurs); morceau != NULL && nombre < maximum;
         morceau = strtok(NULL, separateurs)) {
        size_t longueur = strlen(morceau);
        char *copie = suivi_malloc(longueur + 1);

        if (copie == NULL) {
            return nombre;
        }
        memcpy(copie, morceau, longueur + 1);
        champs[nombre].debut = copie;
        champs[nombre].longueur = longueur;
        nombre++;
    }
    return nombre;
}

static int champ_egal(Champ champ, const char *attendu) {
    size_t longueur = strlen(attendu);

    return champ.debut != NULL && champ.longueur == longueur &&
           memcmp(champ.debut, attendu, longueur) == 0;
}

int main(void) {
    char ligne[64];
    Champ champs[CHAMPS_MAXIMUM];

    snprintf(ligne, sizeof ligne, "%s", "nom,pr\xc3\xa9nom,,ville");
    memset(champs, 0, sizeof champs);

    size_t nombre = decouper(ligne, ',', champs, CHAMPS_MAXIMUM);

    VERIFIE_ENTIER(nombre, 4, "quatre champs, celui du milieu etant vide");
    VERIFIE(champ_egal(champs[0], "nom"), "le premier champ");
    VERIFIE(champ_egal(champs[1], "pr\xc3\xa9nom"), "le deuxieme, accent compris");
    VERIFIE(champ_egal(champs[2], ""), "le troisieme est vide, et il existe quand meme");
    VERIFIE(champ_egal(champs[3], "ville"), "le quatrieme champ");
    VERIFIE_TEXTE(ligne, "nom,pr\xc3\xa9nom,,ville", "la ligne d'origine n'a pas ete touchee");
    VERIFIE(champs[0].debut == ligne, "le premier champ pointe dans la ligne, sans copie");
    VERIFIE(champs[3].debut == ligne + 13, "le dernier aussi, a l'octet pres");
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
