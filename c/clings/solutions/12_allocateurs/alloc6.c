#include "verif.h"

#include <stdalign.h>

const int PAS_FINI = 0;

#define ETIQUETTES_MAX 8
#define ARENE_OCTETS 256

static int appels_allocateur_systeme = 0;

void *reserver_octets(size_t taille) {
    appels_allocateur_systeme++;
    return suivi_malloc(taille);
}

typedef struct {
    unsigned char *octets;
    size_t capacite;
    size_t utilise;
} Arene;

int arene_creer(Arene *cible, size_t capacite) {
    cible->octets = reserver_octets(capacite);
    cible->capacite = cible->octets == NULL ? 0 : capacite;
    cible->utilise = 0;
    return cible->octets != NULL;
}

void arene_detruire(Arene *cible) {
    suivi_free(cible->octets);
    cible->octets = NULL;
    cible->capacite = 0;
    cible->utilise = 0;
}

void *arene_allouer(Arene *cible, size_t taille, size_t alignement) {
    size_t depart = (cible->utilise + alignement - 1) & ~(alignement - 1);
    if (depart > cible->capacite || taille > cible->capacite - depart) {
        return NULL;
    }
    cible->utilise = depart + taille;
    return cible->octets + depart;
}

typedef struct {
    Arene arene;
    char *textes[ETIQUETTES_MAX];
    size_t nombre;
} Etiquettes;

static int etiquettes_remplir(Etiquettes *sortie, const char *const *mots, size_t nombre) {
    for (size_t i = 0; i < nombre; i++) {
        size_t longueur = strlen(mots[i]);
        if (longueur == 0) {
            return 0;
        }
        char *copie = arene_allouer(&sortie->arene, longueur + 3, alignof(char));
        if (copie == NULL) {
            return 0;
        }
        copie[0] = '[';
        memcpy(copie + 1, mots[i], longueur);
        copie[longueur + 1] = ']';
        copie[longueur + 2] = '\0';
        sortie->textes[i] = copie;
        sortie->nombre = i + 1;
    }
    return 1;
}

void etiquettes_detruire(Etiquettes *cible) {
    cible->nombre = 0;
    arene_detruire(&cible->arene);
}

int etiquettes_construire(Etiquettes *sortie, const char *const *mots, size_t nombre) {
    sortie->arene.octets = NULL;
    sortie->arene.capacite = 0;
    sortie->arene.utilise = 0;
    sortie->nombre = 0;
    if (nombre > ETIQUETTES_MAX || !arene_creer(&sortie->arene, ARENE_OCTETS)) {
        return 0;
    }
    if (!etiquettes_remplir(sortie, mots, nombre)) {
        etiquettes_detruire(sortie);
        return 0;
    }
    return 1;
}

int main(void) {
    const char *const mots[] = {"lire", "trier", "ecrire"};
    Etiquettes bonnes;
    VERIFIE(etiquettes_construire(&bonnes, mots, 3) == 1, "la construction nominale reussit");
    VERIFIE_TEXTE(bonnes.textes[0], "[lire]", "la premiere etiquette est formee");
    VERIFIE_TEXTE(bonnes.textes[1], "[trier]", "la deuxieme aussi");
    VERIFIE_TEXTE(bonnes.textes[2], "[ecrire]", "et la derniere");
    VERIFIE_ENTIER(appels_allocateur_systeme, 1, "une seule prise a l'allocateur systeme pour tout le lot");
    etiquettes_detruire(&bonnes);

    appels_allocateur_systeme = 0;
    const char *const bancals[] = {"lire", "", "ecrire"};
    Etiquettes ratees;
    VERIFIE(etiquettes_construire(&ratees, bancals, 3) == 0, "le mot vide fait echouer la construction");
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
