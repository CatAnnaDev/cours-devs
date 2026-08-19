#include <numbers>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

struct Forme {
    virtual ~Forme() = default;

    virtual double aire() const { return 0.0; }
    virtual const char *nom() const { return "forme"; }
};

struct Cercle : Forme {
    double rayon;

    explicit Cercle(double r) : rayon(r) {}

    double aire() const override { return std::numbers::pi * rayon * rayon; }
    const char *nom() const override { return "cercle"; }
};

struct Carre : Forme {
    double cote;

    explicit Carre(double c) : cote(c) {}

    double aire() const override { return cote * cote; }
    const char *nom() const override { return "carre"; }
};

double aire_totale(const std::vector<const Forme *> &formes) {
    double total = 0.0;
    for (const Forme *forme : formes) {
        total += forme->aire();
    }
    return total;
}

int main() {
    const Cercle cercle(2.0);
    const Carre carre(3.0);

    const Forme *vue_cercle = &cercle;
    const Forme *vue_carre = &carre;

    VERIFIE_REEL(vue_cercle->aire(), std::numbers::pi * 4.0,
                 "un Cercle vu comme Forme donne l'aire du cercle");
    VERIFIE_REEL(vue_carre->aire(), 9.0, "un Carre vu comme Forme donne l'aire du carre");
    VERIFIE_TEXTE(vue_cercle->nom(), "cercle", "le nom suit lui aussi le type reel");
    VERIFIE_TEXTE(vue_carre->nom(), "carre", "et pour le carre aussi");

    const std::vector<const Forme *> formes = {vue_cercle, vue_carre};
    VERIFIE_REEL(aire_totale(formes), std::numbers::pi * 4.0 + 9.0,
                 "la somme parcourt des Forme * et trouve les vraies aires");
    return BILAN();
}
