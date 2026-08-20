#include "verif.h"

const int PAS_FINI = 0;

enum {
    CHAMPS_MAXIMUM = 8
};

typedef struct {
    const char *debut;
    size_t longueur;
} Champ;

static size_t decouper(const char *ligne, char separateur, Champ *champs, size_t maximum) {
    const char *debut = ligne;
    size_t nombre = 0;

    while (nombre < maximum) {
        const char *fin = strchr(debut, separateur);

        champs[nombre].debut = debut;
        if (fin == NULL) {
            champs[nombre].longueur = strlen(debut);
            return nombre + 1;
        }
        champs[nombre].longueur = (size_t)(fin - debut);
        nombre++;
        debut = fin + 1;
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
