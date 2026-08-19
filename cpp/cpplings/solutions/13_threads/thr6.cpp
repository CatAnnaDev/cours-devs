#include <atomic>
#include <cstddef>
#include <thread>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

constexpr int FILS = 8;
constexpr int TOURS = 25000;
constexpr std::size_t LIGNE_DE_CACHE = 64;

struct alignas(LIGNE_DE_CACHE) CasePrivee {
    long gerbes = 0;
};

std::atomic<long> grange{0};
std::atomic<long> passages_par_la_grange{0};
CasePrivee cases[FILS];

void moissonner(int rang) {
    const long gerbe = rang + 1;
    long charrette = 0;
    for (int tour = 0; tour < TOURS; tour++) {
        charrette += gerbe;
    }
    cases[rang].gerbes = charrette;
    grange.fetch_add(charrette, std::memory_order_relaxed);
    passages_par_la_grange.fetch_add(1, std::memory_order_relaxed);
}

}

int main() {
    std::vector<std::thread> fils;
    fils.reserve(FILS);
    for (int rang = 0; rang < FILS; rang++) {
        fils.emplace_back(moissonner, rang);
    }
    for (std::thread &fil : fils) {
        fil.join();
    }

    long somme_des_cases = 0;
    for (const CasePrivee &case_privee : cases) {
        somme_des_cases += case_privee.gerbes;
    }

    constexpr long RECOLTE_ATTENDUE = (long)TOURS * FILS * (FILS + 1) / 2;

    VERIFIE_ENTIER(grange.load(std::memory_order_relaxed), RECOLTE_ATTENDUE,
                   "la grange contient neuf cent mille gerbes, comme dans les deux versions");
    VERIFIE_ENTIER(somme_des_cases, RECOLTE_ATTENDUE, "et chaque fil a bien compte sa part");
    VERIFIE_ENTIER(passages_par_la_grange.load(std::memory_order_relaxed), FILS,
                   "un seul passage par la grange et par fil, pas un par gerbe");
    VERIFIE_ENTIER(sizeof(CasePrivee), LIGNE_DE_CACHE,
                   "chaque case tient une ligne de cache a elle seule : faux partage, lecon 09");
    return BILAN();
}
