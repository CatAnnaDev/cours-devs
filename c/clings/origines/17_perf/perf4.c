#include "verif.h"

const int PAS_FINI = 1;

enum {
    TOURS = 2000,
    TAILLE_TAMPON = 64,
    NOMBRE_DE_MOTS = 6
};

static long appels_a_l_allocateur = 0;
static long octets_demandes = 0;

static void *allouer(size_t taille) {
    appels_a_l_allocateur++;
    octets_demandes += (long)taille;
    return suivi_malloc(taille);
}

static const char *const MOTS[NOMBRE_DE_MOTS] = {
    "tampon", "boucle", "chaude", "allouer", "liberer", "reutiliser"
};

static unsigned long empreinte(const char *texte) {
    unsigned long valeur = 14695981039346656037UL;
    for (size_t indice = 0; texte[indice] != '\0'; indice++) {
        valeur ^= (unsigned char)texte[indice];
        valeur *= 1099511628211UL;
    }
    return valeur;
}

static void renverser(char *destination, const char *source) {
    size_t longueur = strlen(source);
    for (size_t indice = 0; indice < longueur; indice++) {
        destination[indice] = source[longueur - 1 - indice];
    }
    destination[longueur] = '\0';
}

static unsigned long empreinte_des_mots_renverses(int tours) {
    unsigned long total = 0;
    for (int tour = 0; tour < tours; tour++) {
        char *tampon = allouer(TAILLE_TAMPON);
        if (tampon == NULL) {
            return total;
        }
        renverser(tampon, MOTS[tour % NOMBRE_DE_MOTS]);
        total += empreinte(tampon);
        suivi_free(tampon);
    }
    return total;
}

int main(void) {
    char reference[TAILLE_TAMPON];
    unsigned long attendu = 0;
    for (int tour = 0; tour < TOURS; tour++) {
        renverser(reference, MOTS[tour % NOMBRE_DE_MOTS]);
        attendu += empreinte(reference);
    }

    renverser(reference, "boucle");
    VERIFIE_TEXTE(reference, "elcuob", "renverser fait bien son travail");

    appels_a_l_allocateur = 0;
    octets_demandes = 0;
    unsigned long obtenu = empreinte_des_mots_renverses(TOURS);

    VERIFIE(obtenu == attendu, "le tampon reutilise donne exactement le meme resultat");
    VERIFIE_ENTIER(appels_a_l_allocateur, 1, "une seule allocation pour les deux mille tours");
    VERIFIE_ENTIER(octets_demandes, TAILLE_TAMPON, "un seul tampon demande a l'allocateur");
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
