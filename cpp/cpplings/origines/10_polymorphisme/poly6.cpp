#include <array>
#include <functional>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

int somme_transformee(const std::vector<int> &valeurs, std::function<int(int)> operation) {
    int total = 0;
    for (int valeur : valeurs) {
        total += operation(valeur);
    }
    return total;
}

int main() {
    const std::vector<int> valeurs = {1, 2, 3, 4};

    const verif::Sonde facteur(10);
    const std::array<long long, 8> lest{};
    const auto operation = [facteur, lest](int valeur) {
        return valeur * facteur.valeur + static_cast<int>(lest[0]);
    };

    VERIFIE_ENTIER(sizeof(std::function<int(int)>), 32,
                   "std::function pese 32 octets quel que soit le callable range dedans");
    VERIFIE_ENTIER(sizeof(operation), 72, "la lambda et ses captures pesent 72 octets");
    VERIFIE(sizeof(operation) > sizeof(std::function<int(int)>),
            "72 > 32 : la lambda ne tient pas dans std::function, qui devra allouer");

    verif::Compteur::remettre_a_zero();
    const int total = somme_transformee(valeurs, operation);

    VERIFIE_ENTIER(total, 100, "la somme transformee vaut 100");
    VERIFIE_ENTIER(verif::Compteur::copies, 0, "la Sonde capturee n'a pas ete recopiee");
    VERIFIE_ENTIER(verif::Compteur::constructions, 0, "aucune Sonde supplementaire construite");
    return BILAN();
}
