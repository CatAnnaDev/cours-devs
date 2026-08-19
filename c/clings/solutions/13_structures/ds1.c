#include "verif.h"

const int PAS_FINI = 0;

typedef struct {
    void *donnees;
    size_t taille;
    size_t capacite;
    size_t taille_element;
} Vecteur;

typedef struct {
    int identifiant;
    short largeur;
    short hauteur;
} Echantillon;

static void vecteur_init(Vecteur *vecteur, size_t taille_element) {
    vecteur->donnees = NULL;
    vecteur->taille = 0;
    vecteur->capacite = 0;
    vecteur->taille_element = taille_element;
}

static void *vecteur_element(const Vecteur *vecteur, size_t indice) {
    return (char *)vecteur->donnees + indice * vecteur->taille_element;
}

static int vecteur_ajouter(Vecteur *vecteur, const void *element) {
    if (vecteur->taille == vecteur->capacite) {
        size_t nouvelle = vecteur->capacite == 0 ? 4 : vecteur->capacite * 2;
        void *agrandi = suivi_realloc(vecteur->donnees, nouvelle * vecteur->taille_element);
        if (agrandi == NULL) {
            return 0;
        }
        vecteur->donnees = agrandi;
        vecteur->capacite = nouvelle;
    }
    memcpy(vecteur_element(vecteur, vecteur->taille), element, vecteur->taille_element);
    vecteur->taille++;
    return 1;
}

static void vecteur_lire(const Vecteur *vecteur, size_t indice, void *destination) {
    memcpy(destination, vecteur_element(vecteur, indice), vecteur->taille_element);
}

static void vecteur_liberer(Vecteur *vecteur) {
    suivi_free(vecteur->donnees);
    vecteur->donnees = NULL;
    vecteur->taille = 0;
    vecteur->capacite = 0;
}

int main(void) {
    Vecteur entiers;
    vecteur_init(&entiers, sizeof(int));
    VERIFIE_ENTIER(entiers.taille_element, sizeof(int), "le vecteur retient la taille d'un int");

    int tous_ajoutes = 1;
    for (int i = 0; i < 20; i++) {
        int valeur = i * 3 + 1;
        tous_ajoutes = tous_ajoutes && vecteur_ajouter(&entiers, &valeur);
    }
    VERIFIE(tous_ajoutes, "vingt entiers sont ajoutes");
    VERIFIE_ENTIER(entiers.taille, 20, "le vecteur compte vingt elements");

    int tous_relus = 1;
    for (size_t i = 0; i < entiers.taille; i++) {
        int lu = 0;
        vecteur_lire(&entiers, i, &lu);
        if (lu != (int)i * 3 + 1) {
            tous_relus = 0;
        }
    }
    VERIFIE(tous_relus, "les vingt entiers se relisent tous");

    int premier = 0;
    vecteur_lire(&entiers, 0, &premier);
    VERIFIE_ENTIER(premier, 1, "le premier entier vaut encore 1");
    int dernier = 0;
    vecteur_lire(&entiers, entiers.taille - 1, &dernier);
    VERIFIE_ENTIER(dernier, 58, "le dernier entier vaut encore 58");

    Vecteur echantillons;
    vecteur_init(&echantillons, sizeof(Echantillon));
    VERIFIE_ENTIER(echantillons.taille_element, sizeof(Echantillon),
                   "le meme code retient la taille d'un Echantillon");

    int tous_stockes = 1;
    for (int i = 0; i < 6; i++) {
        Echantillon echantillon = {100 + i * 10, (short)(i + 1), (short)(i * 2 + 1)};
        tous_stockes = tous_stockes && vecteur_ajouter(&echantillons, &echantillon);
    }
    VERIFIE(tous_stockes, "six echantillons sont ajoutes");

    Echantillon lu;
    vecteur_lire(&echantillons, 0, &lu);
    VERIFIE_ENTIER(lu.identifiant, 100, "le premier echantillon a garde son identifiant");
    VERIFIE_ENTIER(lu.largeur, 1, "il a garde sa largeur");
    vecteur_lire(&echantillons, 4, &lu);
    VERIFIE_ENTIER(lu.identifiant, 140, "le cinquieme echantillon a garde son identifiant");
    VERIFIE_ENTIER(lu.hauteur, 9, "il a garde sa hauteur");

    vecteur_liberer(&entiers);
    vecteur_liberer(&echantillons);
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
