#include "verif.h"

const int PAS_FINI = 0;

#define TAMPON_LIGNE 16

typedef enum {
    LIGNE_LUE,
    LIGNE_TRONQUEE,
    LIGNE_FIN
} StatutLigne;

static void sauter_fin_de_ligne(FILE *flux) {
    int caractere;
    do {
        caractere = fgetc(flux);
    } while (caractere != '\n' && caractere != EOF);
}

static StatutLigne lire_ligne(FILE *flux, char *tampon, size_t taille) {
    if (fgets(tampon, (int)taille, flux) == NULL) {
        tampon[0] = '\0';
        return LIGNE_FIN;
    }

    size_t longueur = strlen(tampon);
    if (longueur > 0 && tampon[longueur - 1] == '\n') {
        tampon[longueur - 1] = '\0';
        return LIGNE_LUE;
    }
    if (feof(flux)) {
        return LIGNE_LUE;
    }

    sauter_fin_de_ligne(flux);
    return LIGNE_TRONQUEE;
}

static FILE *flux_de_test(const char *contenu) {
    FILE *flux = tmpfile();
    if (flux == NULL) {
        return NULL;
    }
    size_t longueur = strlen(contenu);
    if (fwrite(contenu, 1, longueur, flux) != longueur || fflush(flux) != 0) {
        fclose(flux);
        return NULL;
    }
    rewind(flux);
    return flux;
}

int main(void) {
    static const char CONTENU[] = "alpha\n"
                                  "beta\n"
                                  "une ligne beaucoup trop longue pour le tampon\n"
                                  "gamma\n"
                                  "fin";
    char ligne[TAMPON_LIGNE];

    FILE *flux = flux_de_test(CONTENU);
    VERIFIE(flux != NULL, "le flux de test est pret");
    if (flux == NULL) {
        return BILAN();
    }

    VERIFIE_ENTIER(lire_ligne(flux, ligne, sizeof ligne), LIGNE_LUE, "la premiere ligne est lue");
    VERIFIE_TEXTE(ligne, "alpha", "sans le saut de ligne final");

    VERIFIE_ENTIER(lire_ligne(flux, ligne, sizeof ligne), LIGNE_LUE, "la deuxieme ligne est lue");
    VERIFIE_TEXTE(ligne, "beta", "sans le saut de ligne non plus");

    VERIFIE_ENTIER(lire_ligne(flux, ligne, sizeof ligne), LIGNE_TRONQUEE,
                   "une ligne plus longue que le tampon est signalee coupee");
    VERIFIE_TEXTE(ligne, "une ligne beauc", "et le tampon ne contient que son debut");

    VERIFIE_ENTIER(lire_ligne(flux, ligne, sizeof ligne), LIGNE_LUE,
                   "la lecture reprend au debut de la ligne suivante");
    VERIFIE_TEXTE(ligne, "gamma", "et non sur la fin de la ligne coupee");

    VERIFIE_ENTIER(lire_ligne(flux, ligne, sizeof ligne), LIGNE_LUE,
                   "une derniere ligne sans saut de ligne est une ligne comme une autre");
    VERIFIE_TEXTE(ligne, "fin", "avec son contenu entier");

    VERIFIE_ENTIER(lire_ligne(flux, ligne, sizeof ligne), LIGNE_FIN, "puis c'est la fin du flux");

    fclose(flux);
    return BILAN();
}
