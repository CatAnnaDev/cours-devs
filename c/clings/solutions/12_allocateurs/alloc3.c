#include "verif.h"

#include <stdalign.h>

const int PAS_FINI = 0;

#define ARENE_OCTETS 256

typedef struct {
    unsigned char *octets;
    size_t capacite;
    size_t utilise;
} Arene;

static alignas(16) unsigned char zone[ARENE_OCTETS];
static Arene arene = {zone, ARENE_OCTETS, 0};

size_t arrondir_au_multiple(size_t valeur, size_t alignement) {
    return (valeur + alignement - 1) & ~(alignement - 1);
}

void *arene_allouer(Arene *cible, size_t taille, size_t alignement) {
    size_t depart = arrondir_au_multiple(cible->utilise, alignement);
    if (depart > cible->capacite || taille > cible->capacite - depart) {
        return NULL;
    }
    cible->utilise = depart + taille;
    return cible->octets + depart;
}

size_t arene_marquer(const Arene *cible) {
    return cible->utilise;
}

void arene_restaurer(Arene *cible, size_t marque) {
    if (marque > cible->utilise) {
        return;
    }
    cible->utilise = marque;
}

int main(void) {
    int *permanent = arene_allouer(&arene, 4 * sizeof(int), alignof(int));
    VERIFIE(permanent != NULL, "le bloc permanent est servi");
    for (int i = 0; i < 4; i++) {
        permanent[i] = i * 11;
    }

    size_t marque = arene_marquer(&arene);

    int *temporaire = arene_allouer(&arene, 8 * sizeof(int), alignof(int));
    VERIFIE(temporaire != NULL, "le bloc temporaire est servi apres la marque");
    for (int i = 0; i < 8; i++) {
        temporaire[i] = i;
    }
    VERIFIE_ENTIER(temporaire[7], 7, "il se relit tant qu'il est vivant");

    arene_restaurer(&arene, marque);
    VERIFIE_ENTIER(arene.utilise, marque, "la restauration ramene le curseur sur la marque");

    int *reprise = arene_allouer(&arene, 2 * sizeof(int), alignof(int));
    VERIFIE(reprise == temporaire, "la prochaine allocation rend exactement la meme adresse");

    arene_restaurer(&arene, marque);
    int *attendu = NULL;
    int tours_servis = 0;
    long somme = 0;
    for (int tour = 0; tour < 1000; tour++) {
        int *travail = arene_allouer(&arene, 16 * sizeof(int), alignof(int));
        if (travail == NULL || (attendu != NULL && travail != attendu)) {
            break;
        }
        attendu = travail;
        travail[15] = tour;
        somme += travail[15];
        tours_servis++;
        arene_restaurer(&arene, marque);
    }
    VERIFIE_ENTIER(tours_servis, 1000, "mille tours de travail recyclent la meme adresse");
    VERIFIE_ENTIER(somme, 499500, "et chaque tour a bien pu ecrire dans son bloc");

    VERIFIE_ENTIER(permanent[3], 33, "le bloc pris avant la marque n'a jamais bouge");
    VERIFIE_ENTIER(arene.utilise, marque, "et le curseur est reste a la marque");
    return BILAN();
}
