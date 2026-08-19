#include "verif.h"

#include <unistd.h>

const int PAS_FINI = 0;

#define TAILLE_TAMPON 1024

static const char MESSAGE[] = "le contenu publie";

static int chemin_temporaire(char *destination, size_t taille) {
    const char *base = getenv("TMPDIR");
    if (base == NULL || base[0] == '\0') {
        base = "/tmp";
    }
    const char *separateur = base[strlen(base) - 1] == '/' ? "" : "/";
    int ecrits = snprintf(destination, taille, "%s%sclings_fic4_XXXXXX", base, separateur);
    if (ecrits < 0 || (size_t)ecrits >= taille) {
        return -1;
    }
    int descripteur = mkstemp(destination);
    if (descripteur < 0) {
        return -1;
    }
    close(descripteur);
    return 0;
}

static long taille_sur_disque(const char *chemin) {
    FILE *flux = fopen(chemin, "rb");
    if (flux == NULL) {
        return -1;
    }
    long taille = -1;
    if (fseek(flux, 0, SEEK_END) == 0) {
        taille = ftell(flux);
    }
    fclose(flux);
    return taille;
}

static long seuil_de_vidage(const char *chemin) {
    char espace[TAILLE_TAMPON];
    FILE *sortie = fopen(chemin, "wb");
    if (sortie == NULL) {
        return -1;
    }
    if (setvbuf(sortie, espace, _IOFBF, sizeof espace) != 0) {
        fclose(sortie);
        return -1;
    }

    long seuil = -1;
    for (long ecrits = 1; ecrits <= (long)sizeof espace * 2 && seuil < 0; ecrits++) {
        if (fputc('x', sortie) == EOF) {
            break;
        }
        if (taille_sur_disque(chemin) > 0) {
            seuil = ecrits;
        }
    }
    fclose(sortie);
    return seuil;
}

static int publier_et_relire(const char *chemin, const char *message, char *tampon,
                             size_t taille) {
    char espace[TAILLE_TAMPON];
    FILE *sortie = fopen(chemin, "wb");
    if (sortie == NULL) {
        return -1;
    }
    if (setvbuf(sortie, espace, _IOFBF, sizeof espace) != 0 || fputs(message, sortie) == EOF ||
        fflush(sortie) != 0) {
        fclose(sortie);
        return -1;
    }

    FILE *entree = fopen(chemin, "rb");
    if (entree == NULL) {
        fclose(sortie);
        return -1;
    }
    size_t lus = fread(tampon, 1, taille - 1, entree);
    int rate = ferror(entree);
    fclose(entree);
    fclose(sortie);
    if (rate) {
        return -1;
    }
    tampon[lus] = '\0';
    return (int)lus;
}

int main(void) {
    char chemin[512];
    char relu[64];

    VERIFIE_ENTIER(chemin_temporaire(chemin, sizeof chemin), 0,
                   "un fichier de travail est cree dans TMPDIR");

    long seuil = seuil_de_vidage(chemin);
    VERIFIE(seuil > 0, "le seuil de vidage du tampon a ete mesure");
    VERIFIE(seuil >= TAILLE_TAMPON,
            "aucun octet n'atteint le disque avant que le tampon de 1024 soit plein");

    VERIFIE_ENTIER(publier_et_relire(chemin, MESSAGE, relu, sizeof relu),
                   (long long)sizeof MESSAGE - 1, "le second descripteur voit tout le message");
    VERIFIE_TEXTE(relu, MESSAGE, "et le lit a l'identique");
    VERIFIE_ENTIER(taille_sur_disque(chemin), (long long)sizeof MESSAGE - 1,
                   "le fichier fait la taille du message une fois le flux ferme");

    remove(chemin);
    VERIFIE_ENTIER(taille_sur_disque(chemin), -1, "et le fichier de travail est efface");
    return BILAN();
}
