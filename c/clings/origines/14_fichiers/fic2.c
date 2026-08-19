#include "verif.h"

#include <stdint.h>
#include <unistd.h>

const int PAS_FINI = 1;

#define MESURES_ECRITES 3
#define MESURES_DEMANDEES 5
#define SENTINELLE (-777)

typedef enum {
    LECTURE_COMPLETE,
    LECTURE_FIN,
    LECTURE_ERREUR
} ResultatLecture;

static ResultatLecture lire_exactement(FILE *flux, void *destination, size_t octets) {
    if (fread(destination, 1, octets, flux) == 0) {
        return LECTURE_FIN;
    }
    return LECTURE_COMPLETE;
}

static int chemin_temporaire(char *destination, size_t taille) {
    const char *base = getenv("TMPDIR");
    if (base == NULL || base[0] == '\0') {
        base = "/tmp";
    }
    const char *separateur = base[strlen(base) - 1] == '/' ? "" : "/";
    int ecrits = snprintf(destination, taille, "%s%sclings_fic2_XXXXXX", base, separateur);
    if (ecrits < 0 || (size_t)ecrits >= taille) {
        return -1;
    }
    return mkstemp(destination);
}

static FILE *flux_en_ecriture_seule(char *chemin, size_t taille) {
    int descripteur = chemin_temporaire(chemin, taille);
    if (descripteur < 0) {
        return NULL;
    }
    close(descripteur);
    FILE *flux = fopen(chemin, "wb");
    if (flux == NULL) {
        remove(chemin);
    }
    return flux;
}

int main(void) {
    const int32_t source[MESURES_ECRITES] = {11, 22, 33};
    int32_t mesures[MESURES_DEMANDEES];
    char chemin[512];

    FILE *donnees = tmpfile();
    VERIFIE(donnees != NULL, "un flux temporaire anonyme est ouvert");
    if (donnees == NULL) {
        return BILAN();
    }
    VERIFIE_ENTIER(fwrite(source, sizeof source[0], MESURES_ECRITES, donnees), MESURES_ECRITES,
                   "trois mesures y sont ecrites");

    rewind(donnees);
    for (size_t i = 0; i < MESURES_DEMANDEES; i++) {
        mesures[i] = SENTINELLE;
    }
    VERIFIE_ENTIER(lire_exactement(donnees, mesures, sizeof source), LECTURE_COMPLETE,
                   "relire exactement ce qui a ete ecrit reussit");
    VERIFIE_ENTIER(mesures[2], 33, "la derniere mesure ecrite est bien la");

    rewind(donnees);
    for (size_t i = 0; i < MESURES_DEMANDEES; i++) {
        mesures[i] = SENTINELLE;
    }
    VERIFIE_ENTIER(lire_exactement(donnees, mesures, sizeof mesures), LECTURE_FIN,
                   "demander cinq mesures quand il n'y en a que trois signale la fin");
    VERIFIE(feof(donnees), "le drapeau de fin de fichier est leve");
    VERIFIE(!ferror(donnees), "et le drapeau d'erreur ne l'est pas");
    VERIFIE_ENTIER(mesures[0], 11, "les mesures presentes ont ete lues");
    VERIFIE_ENTIER(mesures[MESURES_DEMANDEES - 1], SENTINELLE,
                   "et la fin du tampon n'a pas ete remplie");
    fclose(donnees);

    FILE *interdit = flux_en_ecriture_seule(chemin, sizeof chemin);
    VERIFIE(interdit != NULL, "un flux ouvert en ecriture seule est pret");
    if (interdit == NULL) {
        return BILAN();
    }
    VERIFIE_ENTIER(lire_exactement(interdit, mesures, sizeof mesures), LECTURE_ERREUR,
                   "lire sur un flux en ecriture seule signale une erreur, pas une fin");
    VERIFIE(ferror(interdit), "le drapeau d'erreur est leve");
    VERIFIE(!feof(interdit), "et le drapeau de fin de fichier ne l'est pas");
    fclose(interdit);
    remove(chemin);

    return BILAN();
}
