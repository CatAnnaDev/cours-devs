#include <numbers>
#include <type_traits>

#include "verif.hpp"

const bool PAS_FINI = true;

template <typename Derivee>
struct FormeStatique {
    virtual ~FormeStatique() = default;

    virtual double aire() const = 0;
    double aire_doublee() const { return 2.0 * aire(); }
};

struct CercleStatique : FormeStatique<CercleStatique> {
    double rayon;

    explicit CercleStatique(double r) : rayon(r) {}

    double aire() const override { return std::numbers::pi * rayon * rayon; }
};

struct CarreStatique : FormeStatique<CarreStatique> {
    double cote;

    explicit CarreStatique(double c) : cote(c) {}

    double aire() const override { return cote * cote; }
};

struct FormeVirtuelle {
    virtual ~FormeVirtuelle() = default;

    virtual double aire() const = 0;
    double aire_doublee() const { return 2.0 * aire(); }
};

struct CercleVirtuel : FormeVirtuelle {
    double rayon;

    explicit CercleVirtuel(double r) : rayon(r) {}

    double aire() const override { return std::numbers::pi * rayon * rayon; }
};

int main() {
    const CercleStatique cercle_statique(2.0);
    const CarreStatique carre_statique(3.0);
    const CercleVirtuel cercle_virtuel(2.0);

    VERIFIE_REEL(cercle_statique.aire(), cercle_virtuel.aire(), "meme aire des deux cotes");
    VERIFIE_REEL(cercle_statique.aire_doublee(), cercle_virtuel.aire_doublee(),
                 "la base reutilise l'implementation de la derivee");
    VERIFIE_REEL(carre_statique.aire_doublee(), 18.0, "la meme base sert aussi au carre");

    VERIFIE(std::is_empty_v<FormeStatique<CercleStatique>>, "la base CRTP ne pese rien");
    VERIFIE_ENTIER(sizeof(CercleStatique), sizeof(double), "le CRTP ne porte que ses donnees");
    VERIFIE_ENTIER(sizeof(CercleVirtuel), sizeof(double) + sizeof(void *),
                   "la version virtuelle porte en plus un pointeur de table virtuelle");
    VERIFIE_ENTIER(sizeof(CercleVirtuel) - sizeof(CercleStatique), 8,
                   "l'ecart est exactement un pointeur, soit 8 octets");
    return BILAN();
}
