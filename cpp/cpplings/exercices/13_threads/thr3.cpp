#include <atomic>
#include <stop_token>
#include <thread>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

constexpr int LAMPES_DU_COULOIR = 64;

std::atomic<int> lampes_allumees{0};
std::atomic<bool> ronde_interrompue{false};

void faire_la_ronde(std::stop_token jeton) {
    while (!jeton.stop_requested()) {
        std::this_thread::yield();
    }
    ronde_interrompue.store(true, std::memory_order_release);
}

void allumer_le_couloir() {
    for (int lampe = 0; lampe < LAMPES_DU_COULOIR; lampe++) {
        lampes_allumees.fetch_add(1, std::memory_order_relaxed);
    }
}

}

int main() {
    {
        std::jthread veilleur(faire_la_ronde);
        veilleur.request_stop();
    }
    VERIFIE(ronde_interrompue.load(std::memory_order_acquire),
            "request_stop reveille le fil, et le destructeur de jthread l'attend");

    {
        std::thread allumeuse(allumer_le_couloir);
    }
    VERIFIE_ENTIER(lampes_allumees.load(std::memory_order_relaxed), LAMPES_DU_COULOIR,
                   "les soixante-quatre lampes sont allumees avant qu'on les compte");
    return BILAN();
}
