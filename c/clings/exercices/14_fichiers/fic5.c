#include "verif.h"

#include <unistd.h>

const int PAS_FINI = 1;

#define LIGNES_NEUVES 4
#define SANS_ECHEC ((size_t)-1)

static const char ANCIEN[] = "ancien contenu, precieux, irremplacable\n";

static const char *const NEUVES[LIGNES_NEUVES] = {
    "premiere ligne\n",
    "deuxieme ligne\n",
    "troisieme ligne\n",
    "quatrieme ligne\n",
};

static int ecrire_lignes(FILE *flux, const char *const *lignes, size_t nombre,
                         size_t echec_apres) {
    for (size_t i = 0; i < nombre; i++) {
        if (i == echec_apres || fputs(lignes[i], flux) == EOF) {
            return -1;
        }
    }
    return 0;
}

static int remplacer_contenu(const char *chemin, const char *const *lignes, size_t nombre,
                             size_t echec_apres) {
    FILE *flux = fopen(chemin, "wb");
    if (flux == NULL) {
        return -1;
    }
    if (ecrire_lignes(flux, lignes, nombre, echec_apres) != 0) {
        fclose(flux);
        return -1;
    }
    if (fclose(flux) != 0) {
        return -1;
    }
    return 0;
}

static void concatener(char *destination, size_t taille, const char *const *lignes,
                       size_t nombre) {
    size_t ecrits = 0;
    destination[0] = '\0';
    for (size_t i = 0; i < nombre; i++) {
        int ajoutes = snprintf(destination + ecrits, taille - ecrits, "%s", lignes[i]);
        if (ajoutes < 0 || (size_t)ajoutes >= taille - ecrits) {
            return;
        }
        ecrits += (size_t)ajoutes;
    }
}

static long relire(const char *chemin, char *tampon, size_t taille) {
    FILE *flux = fopen(chemin, "rb");
    if (flux == NULL) {
        return -1;
    }
    size_t lus = fread(tampon, 1, taille - 1, flux);
    int rate = ferror(flux);
    fclose(flux);
    if (rate) {
        return -1;
    }
    tampon[lus] = '\0';
    return (long)lus;
}

static int chemin_temporaire(char *destination, size_t taille) {
    const char *base = getenv("TMPDIR");
    if (base == NULL || base[0] == '\0') {
        base = "/tmp";
    }
    const char *separateur = base[strlen(base) - 1] == '/' ? "" : "/";
    int ecrits = snprintf(destination, taille, "%s%sclings_fic5_XXXXXX", base, separateur);
    if (ecrits < 0 || (size_t)ecrits >= taille) {
        return -1;
    }
    int descripteur = mkstemp(destination);
    if (descripteur < 0) {
        return -1;
    }
    close(descripteur);
    FILE *flux = fopen(destination, "wb");
    if (flux == NULL) {
        remove(destination);
        return -1;
    }
    int pose = fputs(ANCIEN, flux);
    if (fclose(flux) != 0 || pose == EOF) {
        remove(destination);
        return -1;
    }
    return 0;
}

static int existe(const char *chemin) {
    FILE *flux = fopen(chemin, "rb");
    if (flux == NULL) {
        return 0;
    }
    fclose(flux);
    return 1;
}

int main(void) {
    char cible[512];
    char voisin[600];
    char attendu[256];
    char relu[256];

    VERIFIE_ENTIER(chemin_temporaire(cible, sizeof cible), 0,
                   "une cible portant l'ancien contenu existe dans TMPDIR");
    snprintf(voisin, sizeof voisin, "%s.neuf", cible);

    VERIFIE_ENTIER(remplacer_contenu(cible, NEUVES, LIGNES_NEUVES, 2), -1,
                   "un echec au milieu de l'ecriture est signale");
    VERIFIE_ENTIER(relire(cible, relu, sizeof relu), (long long)sizeof ANCIEN - 1,
                   "la cible a toujours la taille de l'ancien contenu");
    VERIFIE_TEXTE(relu, ANCIEN, "et l'ancien contenu, intact, mot pour mot");
    VERIFIE(!existe(voisin), "aucun fichier a demi ecrit n'a survecu a cote");

    concatener(attendu, sizeof attendu, NEUVES, LIGNES_NEUVES);
    VERIFIE_ENTIER(remplacer_contenu(cible, NEUVES, LIGNES_NEUVES, SANS_ECHEC), 0,
                   "un remplacement sans incident reussit");
    VERIFIE_ENTIER(relire(cible, relu, sizeof relu), (long long)strlen(attendu),
                   "la cible a la taille du nouveau contenu");
    VERIFIE_TEXTE(relu, attendu, "et les quatre lignes neuves y sont en entier");
    VERIFIE(!existe(voisin), "le fichier voisin a disparu, renomme et non copie");

    remove(cible);
    remove(voisin);
    return BILAN();
}
