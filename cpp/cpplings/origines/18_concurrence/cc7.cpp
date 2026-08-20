#include <atomic>
#include <barrier>
#include <latch>
#include <thread>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

constexpr int SONNEUSES = 3;
constexpr int PHASES = 4;
constexpr int VEILLEUSES = 6;
constexpr int COUPS_PAR_VEILLEUSE = 500;
constexpr long CARILLON_ATTENDU = (long)SONNEUSES * PHASES * (PHASES + 1) / 2;
constexpr long GUET_ATTENDU = (long)COUPS_PAR_VEILLEUSE * (1 + 3 + 5);

std::atomic<int> phases_achevees{0};
std::atomic<long> carillon{0};
std::atomic<long> guet{0};
std::atomic<int> rondes_ecourtees{0};
std::atomic<int> descentes_du_portail{0};

struct FinDePhase {
    void operator()() noexcept { phases_achevees.fetch_add(1, std::memory_order_acq_rel); }
};

std::barrier<FinDePhase> releve(SONNEUSES, FinDePhase{});
std::latch portail(VEILLEUSES);

void sonner() {
    for (int phase = 1; phase <= PHASES; phase++) {
        carillon.fetch_add(phase, std::memory_order_relaxed);
        releve.arrive_and_wait();
    }
}

void veiller(int rang) {
    if (rang % 2 == 1) {
        rondes_ecourtees.fetch_add(1, std::memory_order_relaxed);
        return;
    }
    long coups = 0;
    for (int coup = 0; coup < COUPS_PAR_VEILLEUSE; coup++) {
        coups += rang + 1;
    }
    guet.fetch_add(coups, std::memory_order_relaxed);
    descentes_du_portail.fetch_add(1, std::memory_order_relaxed);
    portail.count_down();
}

}

int main() {
    {
        std::vector<std::jthread> sonneuses;
        sonneuses.reserve(SONNEUSES);
        for (int rang = 0; rang < SONNEUSES; rang++) {
            sonneuses.emplace_back(sonner);
        }
    }

    VERIFIE_ENTIER(phases_achevees.load(std::memory_order_relaxed), PHASES,
                   "la barriere a referme quatre phases, une action de fin par phase");
    VERIFIE_ENTIER(carillon.load(std::memory_order_relaxed), CARILLON_ATTENDU,
                   "et les trois sonneuses ont frappe chaque phase avant de la refermer");

    {
        std::vector<std::jthread> veilleuses;
        veilleuses.reserve(VEILLEUSES);
        for (int rang = 0; rang < VEILLEUSES; rang++) {
            veilleuses.emplace_back(veiller, rang);
        }
    }

    VERIFIE_ENTIER(guet.load(std::memory_order_relaxed), GUET_ATTENDU,
                   "les trois veilleuses de rang pair ont fait leur tour de guet");
    VERIFIE_ENTIER(rondes_ecourtees.load(std::memory_order_relaxed), VEILLEUSES / 2,
                   "et les trois autres ont ecourte leur ronde, ce qui est prevu");
    VERIFIE_ENTIER(descentes_du_portail.load(std::memory_order_relaxed), VEILLEUSES,
                   "les six veilleuses ont fait descendre le portail, une fois chacune");
    VERIFIE(portail.try_wait(),
            "le portail est donc a zero : try_wait le constate sans jamais attendre");
    return BILAN();
}
