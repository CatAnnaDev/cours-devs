#include <string>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

struct Tarif {
    int prix_unitaire;

    explicit Tarif(int prix) : prix_unitaire(prix) {}
    virtual ~Tarif() = default;

    virtual std::string libelle() const { return "plein"; }
    virtual int montant(int quantite) const { return prix_unitaire * quantite; }
};

struct TarifDegressif : Tarif {
    int seuil;

    TarifDegressif(int prix, int quantite_seuil) : Tarif(prix), seuil(quantite_seuil) {}

    std::string libelle() const { return "degressif"; }

    using Tarif::montant;
    int montant(int quantite) {
        return quantite >= seuil ? prix_unitaire * quantite / 2 : prix_unitaire * quantite;
    }
};

int facture(const std::vector<const Tarif *> &lignes, int quantite) {
    int total = 0;
    for (const Tarif *tarif : lignes) {
        total += tarif->montant(quantite);
    }
    return total;
}

int main() {
    const Tarif plein(10);
    const TarifDegressif degressif(10, 4);

    const Tarif *vue_degressif = &degressif;

    VERIFIE_TEXTE(vue_degressif->libelle(), "degressif", "le libelle passe bien par la derivee");
    VERIFIE_ENTIER(vue_degressif->montant(6), 30,
                   "au-dela du seuil, 6 unites sont a moitie prix");
    VERIFIE_ENTIER(vue_degressif->montant(2), 20, "sous le seuil, c'est le plein tarif");
    VERIFIE_ENTIER(facture({&plein, vue_degressif}, 6), 90, "la facture melange les deux tarifs");
    return BILAN();
}
