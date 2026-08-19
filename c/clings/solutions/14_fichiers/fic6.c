#include "verif.h"

#include <unistd.h>

const int PAS_FINI = 0;

static const char ATTENDU[] = "premiere ligne\ndeuxieme ligne\nplus rien apres\n";

static char *lire_tout(const char *chemin, size_t *taille_rendue) {
    FILE *flux = fopen(chemin, "rb");
    if (flux == NULL) {
        return NULL;
    }
    if (fseek(flux, 0, SEEK_END) != 0) {
        fclose(flux);
        return NULL;
    }
    long taille = ftell(flux);
    if (taille < 0) {
        fclose(flux);
        return NULL;
    }
    rewind(flux);

    char *contenu = suivi_malloc((size_t)taille + 1);
    if (contenu == NULL) {
        fclose(flux);
        return NULL;
    }
    size_t lus = fread(contenu, 1, (size_t)taille, flux);
    int rate = ferror(flux);
    fclose(flux);
    if (rate) {
        suivi_free(contenu);
        return NULL;
    }
    contenu[lus] = '\0';
    *taille_rendue = lus;
    return contenu;
}

static int creer_fichier(char *chemin, size_t taille, const char *contenu) {
    const char *base = getenv("TMPDIR");
    if (base == NULL || base[0] == '\0') {
        base = "/tmp";
    }
    const char *separateur = base[strlen(base) - 1] == '/' ? "" : "/";
    int ecrits = snprintf(chemin, taille, "%s%sclings_fic6_XXXXXX", base, separateur);
    if (ecrits < 0 || (size_t)ecrits >= taille) {
        return -1;
    }
    int descripteur = mkstemp(chemin);
    if (descripteur < 0) {
        return -1;
    }
    FILE *flux = fdopen(descripteur, "wb");
    if (flux == NULL) {
        close(descripteur);
        remove(chemin);
        return -1;
    }
    int pose = fputs(contenu, flux);
    if (fclose(flux) != 0 || pose == EOF) {
        remove(chemin);
        return -1;
    }
    return 0;
}

int main(void) {
    char chemin[512];
    char vide[512];
    size_t taille = 0;
    size_t taille_vide = 42;
    size_t taille_absente = 42;

    VERIFIE_ENTIER(creer_fichier(chemin, sizeof chemin, ATTENDU), 0,
                   "un fichier de travail est cree dans TMPDIR");
    VERIFIE_ENTIER(creer_fichier(vide, sizeof vide, ""), 0, "un fichier vide aussi");

    char *contenu = lire_tout(chemin, &taille);
    char *rien = lire_tout(vide, &taille_vide);
    char *absent = lire_tout("/rien/ici/absolument/rien.txt", &taille_absente);
    remove(chemin);
    remove(vide);

    VERIFIE(contenu != NULL, "le fichier entier est charge");
    VERIFIE(rien != NULL, "un fichier vide se charge sans echouer");
    VERIFIE(absent == NULL, "un chemin absent rend NULL");
    if (contenu == NULL || rien == NULL) {
        suivi_free(contenu);
        suivi_free(rien);
        return BILAN();
    }

    VERIFIE_ENTIER(taille, (long long)sizeof ATTENDU - 1, "la taille rendue est celle du fichier");
    VERIFIE_TEXTE(contenu, ATTENDU, "et le bloc se lit comme une chaine terminee par un zero");
    VERIFIE_ENTIER(strlen(contenu), taille, "strlen retrouve exactement la taille rendue");
    VERIFIE_ENTIER(taille_vide, 0, "le fichier vide rend une taille nulle");
    VERIFIE_TEXTE(rien, "", "et une chaine vide, pas un bloc sans terminaison");

    suivi_free(contenu);
    suivi_free(rien);
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
