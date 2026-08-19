#include "verif.h"

#include <sys/resource.h>
#include <unistd.h>

const int PAS_FINI = 1;

#define DESCRIPTEURS_MAX 48
#define OUVERTURES_REPETEES 300

static int chemin_temporaire(char *destination, size_t taille) {
    const char *base = getenv("TMPDIR");
    if (base == NULL || base[0] == '\0') {
        base = "/tmp";
    }
    size_t longueur = strlen(base);
    const char *separateur = base[longueur - 1] == '/' ? "" : "/";
    int ecrits = snprintf(destination, taille, "%s%sclings_fic1_XXXXXX", base, separateur);
    if (ecrits < 0 || (size_t)ecrits >= taille) {
        return -1;
    }
    return mkstemp(destination);
}

static int creer_fichier(char *chemin, size_t taille, const char *contenu) {
    int descripteur = chemin_temporaire(chemin, taille);
    if (descripteur < 0) {
        return -1;
    }
    FILE *flux = fdopen(descripteur, "wb");
    if (flux == NULL) {
        close(descripteur);
        remove(chemin);
        return -1;
    }
    int ecrit = fputs(contenu, flux);
    if (fclose(flux) != 0 || ecrit == EOF) {
        remove(chemin);
        return -1;
    }
    return 0;
}

static void limiter_descripteurs(rlim_t maximum) {
    struct rlimit limite;
    if (getrlimit(RLIMIT_NOFILE, &limite) != 0 || limite.rlim_cur <= maximum) {
        return;
    }
    limite.rlim_cur = maximum;
    setrlimit(RLIMIT_NOFILE, &limite);
}

static int lire_entete(const char *chemin, char *tampon, size_t taille) {
    FILE *flux = fopen(chemin, "rb");
    size_t lus = fread(tampon, 1, taille, flux);
    if (lus == taille) {
        return -1;
    }
    fclose(flux);
    tampon[lus] = '\0';
    return (int)lus;
}

int main(void) {
    char court[512];
    char grand[512];
    char tampon[64];
    char etroit[8];

    VERIFIE_ENTIER(creer_fichier(court, sizeof court, "entete"), 0,
                   "un fichier court est cree dans TMPDIR");
    VERIFIE_ENTIER(creer_fichier(grand, sizeof grand, "trente-deux octets bien comptes."), 0,
                   "un fichier trop long pour un tampon de huit est cree");

    VERIFIE_ENTIER(lire_entete(court, tampon, sizeof tampon), 6, "la lecture rend six octets");
    VERIFIE_TEXTE(tampon, "entete", "et le contenu attendu");

    VERIFIE_ENTIER(lire_entete(grand, etroit, sizeof etroit), -1,
                   "un fichier plus grand que le tampon rend un code d'erreur");

    VERIFIE_ENTIER(lire_entete("/rien/ici/absolument/rien.txt", tampon, sizeof tampon), -1,
                   "un chemin absent rend un code d'erreur au lieu de planter");

    limiter_descripteurs(DESCRIPTEURS_MAX);

    int tenu = 1;
    for (int essai = 0; essai < OUVERTURES_REPETEES && tenu; essai++) {
        tenu = lire_entete(grand, etroit, sizeof etroit) == -1;
    }
    VERIFIE(tenu, "trois cents refus d'affilee ne perdent pas un seul descripteur");

    remove(court);
    remove(grand);
    return BILAN();
}
