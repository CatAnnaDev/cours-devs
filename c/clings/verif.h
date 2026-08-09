#ifndef VERIF_H
#define VERIF_H

#include <math.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static int verif_reussites = 0;
static int verif_echecs = 0;

static void verif_resultat(int ok, const char *message, const char *fichier, int ligne) {
    if (ok) {
        verif_reussites++;
        printf("  \033[32mok\033[0m    %s\n", message);
    } else {
        verif_echecs++;
        printf("  \033[31mRATE\033[0m  %s\n", message);
        printf("        %s:%d\n", fichier, ligne);
    }
}

static void verif_resultat_entier(long long obtenu, long long attendu, const char *message,
                                  const char *fichier, int ligne) {
    if (obtenu == attendu) {
        verif_reussites++;
        printf("  \033[32mok\033[0m    %s\n", message);
    } else {
        verif_echecs++;
        printf("  \033[31mRATE\033[0m  %s\n", message);
        printf("        attendu %lld, obtenu %lld\n", attendu, obtenu);
        printf("        %s:%d\n", fichier, ligne);
    }
}

static void verif_resultat_reel(double obtenu, double attendu, double tolerance,
                                const char *message, const char *fichier, int ligne) {
    if (fabs(obtenu - attendu) <= tolerance) {
        verif_reussites++;
        printf("  \033[32mok\033[0m    %s\n", message);
    } else {
        verif_echecs++;
        printf("  \033[31mRATE\033[0m  %s\n", message);
        printf("        attendu %g, obtenu %g\n", attendu, obtenu);
        printf("        %s:%d\n", fichier, ligne);
    }
}

static void verif_resultat_texte(const char *obtenu, const char *attendu, const char *message,
                                 const char *fichier, int ligne) {
    int ok = obtenu != NULL && attendu != NULL && strcmp(obtenu, attendu) == 0;
    if (ok) {
        verif_reussites++;
        printf("  \033[32mok\033[0m    %s\n", message);
    } else {
        verif_echecs++;
        printf("  \033[31mRATE\033[0m  %s\n", message);
        printf("        attendu \"%s\", obtenu \"%s\"\n",
               attendu ? attendu : "(null)", obtenu ? obtenu : "(null)");
        printf("        %s:%d\n", fichier, ligne);
    }
}

static int verif_bilan(int pas_fini) {
    printf("\n  %d verification(s) passee(s), %d ratee(s)\n", verif_reussites, verif_echecs);
    if (verif_echecs > 0) {
        return 1;
    }
    if (pas_fini) {
        return 3;
    }
    return 0;
}

static long verif_blocs_alloues = 0;

static void *suivi_malloc(size_t taille) {
    void *bloc = malloc(taille);
    if (bloc != NULL) {
        verif_blocs_alloues++;
    }
    return bloc;
}

static void *suivi_calloc(size_t nombre, size_t taille) {
    void *bloc = calloc(nombre, taille);
    if (bloc != NULL) {
        verif_blocs_alloues++;
    }
    return bloc;
}

static void *suivi_realloc(void *bloc, size_t taille) {
    void *neuf = realloc(bloc, taille);
    if (bloc == NULL && neuf != NULL) {
        verif_blocs_alloues++;
    }
    return neuf;
}

static void suivi_free(void *bloc) {
    if (bloc != NULL) {
        verif_blocs_alloues--;
    }
    free(bloc);
}

static long long verif_a_faire(const char *fichier, int ligne) {
    printf("  \033[31mRATE\033[0m  un A_FAIRE n'a pas ete remplace\n");
    printf("        %s:%d\n", fichier, ligne);
    verif_echecs++;
    return 0;
}

#define VERIFIE(condition, message) verif_resultat((condition) ? 1 : 0, message, __FILE__, __LINE__)
#define VERIFIE_ENTIER(obtenu, attendu, message) \
    verif_resultat_entier((long long)(obtenu), (long long)(attendu), message, __FILE__, __LINE__)
#define VERIFIE_REEL(obtenu, attendu, message) \
    verif_resultat_reel((double)(obtenu), (double)(attendu), 1e-6, message, __FILE__, __LINE__)
#define VERIFIE_PROCHE(obtenu, attendu, tolerance, message) \
    verif_resultat_reel((double)(obtenu), (double)(attendu), (double)(tolerance), message, __FILE__, __LINE__)
#define VERIFIE_TEXTE(obtenu, attendu, message) \
    verif_resultat_texte((obtenu), (attendu), message, __FILE__, __LINE__)

#define VERIFIE_PAS_DE_FUITE() \
    verif_resultat_entier(verif_blocs_alloues, 0, "aucun bloc laisse en memoire", __FILE__, __LINE__)

#define A_FAIRE verif_a_faire(__FILE__, __LINE__)
#define BILAN() verif_bilan(PAS_FINI)

#endif
