#include "verif.h"

const int PAS_FINI = 1;

enum {
    NOMBRE = 4096,
    CHAMPS_PAR_CORPS = 5
};

typedef struct {
    int identifiant;
    int masse;
    int position_x;
    int position_y;
    int position_z;
} CorpsGroupes;

typedef struct {
    int identifiant[NOMBRE];
    int masse[NOMBRE];
    int position_x[NOMBRE];
    int position_y[NOMBRE];
    int position_z[NOMBRE];
} CorpsSepares;

static long octets_traverses = 0;

static int lire_avec_pas(const void *depart, size_t pas, int indice) {
    octets_traverses += (long)pas;
    int valeur = 0;
    memcpy(&valeur, (const unsigned char *)depart + (size_t)indice * pas, sizeof valeur);
    return valeur;
}

static long somme_des_masses(const void *depart, size_t pas, int nombre) {
    long somme = 0;
    for (int indice = 0; indice < nombre; indice++) {
        somme += lire_avec_pas(depart, pas, indice);
    }
    return somme;
}

int main(void) {
    static CorpsGroupes groupes[NOMBRE];
    static CorpsSepares separes;

    for (int indice = 0; indice < NOMBRE; indice++) {
        int masse = 1 + indice % 97;
        groupes[indice].identifiant = indice;
        groupes[indice].masse = masse;
        groupes[indice].position_x = 2 * indice;
        groupes[indice].position_y = 3 * indice;
        groupes[indice].position_z = 5 * indice;
        separes.identifiant[indice] = indice;
        separes.masse[indice] = masse;
        separes.position_x[indice] = 2 * indice;
        separes.position_y[indice] = 3 * indice;
        separes.position_z[indice] = 5 * indice;
    }

    VERIFIE_ENTIER(sizeof(CorpsGroupes), CHAMPS_PAR_CORPS * sizeof(int),
                   "la structure groupee pese ses cinq champs");

    octets_traverses = 0;
    long somme_dispersee = somme_des_masses(&groupes[0].masse, sizeof groupes[0], NOMBRE);
    long octets_disperses = octets_traverses;

    octets_traverses = 0;
    long somme_serree = somme_des_masses(&groupes[0].masse, sizeof groupes[0], NOMBRE);
    long octets_serres = octets_traverses;

    VERIFIE_ENTIER(somme_serree, somme_dispersee, "les deux dispositions donnent la meme somme");
    VERIFIE_ENTIER(octets_disperses, (long)NOMBRE * (long)sizeof(CorpsGroupes),
                   "le tableau de structures traine les cinq champs a chaque tour");
    VERIFIE_ENTIER(octets_serres, (long)NOMBRE * (long)sizeof(int),
                   "la structure de tableaux ne traverse que les masses");
    VERIFIE_ENTIER(octets_disperses / octets_serres, CHAMPS_PAR_CORPS,
                   "le rapport des octets vaut le nombre de champs");
    return BILAN();
}
