#include "verif.hpp"

const bool PAS_FINI = true;

struct Ressource {
    int identifiant;

    explicit Ressource(int id) : identifiant(id) {}
    ~Ressource() = default;

    virtual int taille() const { return 0; }
};

struct FichierOuvert : Ressource {
    verif::Sonde poignee;

    explicit FichierOuvert(int id) : Ressource(id), poignee(id) {}

    int taille() const override { return poignee.valeur; }
};

int main() {
    verif::Compteur::remettre_a_zero();

    Ressource *ressource = new FichierOuvert(7);

    VERIFIE_ENTIER(ressource->taille(), 7, "l'appel virtuel atteint bien la derivee");
    VERIFIE_ENTIER(verif::Compteur::constructions, 1,
                   "la Sonde de la derivee a bien ete construite");

    delete ressource;

    VERIFIE_ENTIER(verif::Compteur::destructions, 1,
                   "le destructeur de la derivee a detruit la Sonde");
    return BILAN();
}
