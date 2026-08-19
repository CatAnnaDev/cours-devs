#include <memory>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

struct Employe {
    int salaire_base;

    explicit Employe(int base) : salaire_base(base) {}
    virtual ~Employe() = default;

    virtual int salaire() const { return salaire_base; }
};

struct Commercial : Employe {
    int prime_par_vente;
    int ventes;

    Commercial(int base, int prime, int nombre_ventes)
        : Employe(base), prime_par_vente(prime), ventes(nombre_ventes) {}

    int salaire() const override { return salaire_base + prime_par_vente * ventes; }
};

int masse_salariale(const std::vector<Employe> &equipe) {
    int total = 0;
    for (const Employe &membre : equipe) {
        total += membre.salaire();
    }
    return total;
}

int main() {
    VERIFIE_ENTIER(sizeof(Employe), 16, "un Employe pese 16 octets");
    VERIFIE_ENTIER(sizeof(Commercial), 24, "un Commercial en pese 24");
    VERIFIE_ENTIER(sizeof(Commercial) - sizeof(Employe), 8,
                   "8 octets perdus : prime_par_vente et ventes en plus de la base");

    std::vector<Employe> equipe;
    equipe.reserve(2);
    equipe.push_back(Employe(1000));
    equipe.push_back(Commercial(1000, 50, 4));

    VERIFIE_ENTIER(equipe[1].salaire(), 1200, "le commercial touche sa prime");
    VERIFIE_ENTIER(masse_salariale(equipe), 2200, "la masse salariale tient compte des primes");
    return BILAN();
}
