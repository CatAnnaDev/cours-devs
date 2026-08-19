#include <mutex>
#include <thread>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

constexpr int FILS = 4;
constexpr int PAR_FIL = 5000;

std::vector<int> registre_des_prises;
std::mutex verrou_du_registre;

void deposer(int rang) {
    const int marque = rang + 1;
    for (int tour = 0; tour < PAR_FIL; tour++) {
        const std::lock_guard<std::mutex> tenue(verrou_du_registre);
        registre_des_prises.push_back(marque);
    }
}

}

int main() {
    std::vector<std::thread> fils;
    fils.reserve(FILS);
    for (int rang = 0; rang < FILS; rang++) {
        fils.emplace_back(deposer, rang);
    }
    for (std::thread &fil : fils) {
        fil.join();
    }

    long somme_des_marques = 0;
    for (int marque : registre_des_prises) {
        somme_des_marques += marque;
    }

    VERIFIE_ENTIER(registre_des_prises.size(), FILS * PAR_FIL,
                   "les vingt mille depots sont tous arrives");
    VERIFIE_ENTIER(somme_des_marques, (long)PAR_FIL * FILS * (FILS + 1) / 2,
                   "et aucune marque n'a ete ecrasee par celle d'un autre fil");
    return BILAN();
}
