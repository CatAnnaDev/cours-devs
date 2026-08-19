#include <atomic>
#include <thread>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

constexpr int FILS = 8;
constexpr int STOCK_INITIAL = 2000;
constexpr int ESSAIS_PAR_FIL = 300;
constexpr int MANCHES = 60;

std::atomic<int> lanternes_en_rayon{0};
std::atomic<long> lanternes_vendues{0};

bool prendre_une_lanterne() {
    if (lanternes_en_rayon.load(std::memory_order_relaxed) > 0) {
        lanternes_en_rayon.fetch_sub(1, std::memory_order_relaxed);
        return true;
    }
    return false;
}

void acheter() {
    for (int essai = 0; essai < ESSAIS_PAR_FIL; essai++) {
        if (prendre_une_lanterne()) {
            lanternes_vendues.fetch_add(1, std::memory_order_relaxed);
        }
    }
}

}

int main() {
    int pire_rayon = 0;
    std::vector<std::thread> fils;
    fils.reserve(FILS);

    for (int manche = 0; manche < MANCHES; manche++) {
        lanternes_en_rayon.store(STOCK_INITIAL, std::memory_order_relaxed);
        fils.clear();
        for (int rang = 0; rang < FILS; rang++) {
            fils.emplace_back(acheter);
        }
        for (std::thread &fil : fils) {
            fil.join();
        }
        const int reste = lanternes_en_rayon.load(std::memory_order_relaxed);
        if (reste < pire_rayon) {
            pire_rayon = reste;
        }
    }

    VERIFIE_ENTIER(pire_rayon, 0, "le rayon n'est jamais descendu sous zero lanterne");
    VERIFIE_ENTIER(lanternes_vendues.load(std::memory_order_relaxed),
                   (long)MANCHES * STOCK_INITIAL,
                   "on n'a jamais vendu plus de lanternes qu'il n'y en avait");
    VERIFIE_ENTIER(lanternes_en_rayon.load(std::memory_order_relaxed), 0,
                   "et la derniere manche a bien tout ecoule");
    return BILAN();
}
