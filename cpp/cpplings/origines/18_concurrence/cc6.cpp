#include <atomic>
#include <thread>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

constexpr int OUVRIERES = 4;
constexpr int TACHES = 96;
constexpr long PAS_PAR_TACHE = 64;
constexpr long SOMME_DES_PAS = PAS_PAR_TACHE * (PAS_PAR_TACHE + 1) / 2;
constexpr long OUVRAGE_ATTENDU = SOMME_DES_PAS * ((long)TACHES * (TACHES + 1) / 2);

struct Tache {
    long graine = 0;
};

Tache carnet[TACHES];
long ouvrages[TACHES] = {};
int executions[TACHES] = {};

std::atomic<int> prochaine_tache{0};
std::atomic<bool> carnet_epuise{false};
std::atomic<int> taches_executees{0};

void executer(int rang) {
    long ouvrage = 0;
    for (long pas = 1; pas <= PAS_PAR_TACHE; pas++) {
        ouvrage += carnet[rang].graine * pas;
    }
    ouvrages[rang] = ouvrage;
    executions[rang]++;
    taches_executees.fetch_add(1, std::memory_order_relaxed);
}

void tenir_l_etabli() {
    while (!carnet_epuise.load(std::memory_order_acquire)) {
        const int rang = prochaine_tache.fetch_add(1, std::memory_order_acq_rel);
        if (rang >= TACHES - 1) {
            carnet_epuise.store(true, std::memory_order_release);
            break;
        }
        executer(rang);
    }
}

}

int main() {
    for (int rang = 0; rang < TACHES; rang++) {
        carnet[rang].graine = rang + 1;
    }

    {
        std::vector<std::jthread> ouvrieres;
        ouvrieres.reserve(OUVRIERES);
        for (int rang = 0; rang < OUVRIERES; rang++) {
            ouvrieres.emplace_back(tenir_l_etabli);
        }
    }

    int taches_jamais_faites = 0;
    int taches_faites_deux_fois = 0;
    long ouvrage_total = 0;
    for (int rang = 0; rang < TACHES; rang++) {
        if (executions[rang] == 0) {
            taches_jamais_faites++;
        }
        if (executions[rang] > 1) {
            taches_faites_deux_fois++;
        }
        ouvrage_total += ouvrages[rang];
    }

    VERIFIE(prochaine_tache.load(std::memory_order_relaxed) >= TACHES,
            "le carnet a bien ete distribue jusqu'a sa derniere page");
    VERIFIE_ENTIER(taches_jamais_faites, 0, "aucune tache du carnet n'est restee en plan");
    VERIFIE_ENTIER(taches_faites_deux_fois, 0, "et aucune n'a ete faite deux fois");
    VERIFIE_ENTIER(taches_executees.load(std::memory_order_relaxed), TACHES,
                   "les quatre ouvrieres ont execute les quatre-vingt-seize taches");
    VERIFIE_ENTIER(ouvrage_total, OUVRAGE_ATTENDU, "et l'ouvrage rendu est complet");
    return BILAN();
}
