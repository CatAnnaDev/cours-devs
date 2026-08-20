#include <atomic>
#include <thread>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

constexpr int MANCHES = 500;
constexpr long PAS_DU_TEMOIN = 13;
constexpr long PLAFOND_D_ATTENTE = 20000000;
constexpr long TOTAL_ATTENDU = PAS_DU_TEMOIN * ((long)MANCHES * (MANCHES + 1) / 2);

constexpr std::memory_order ORDRE_DE_LA_POSE = std::memory_order_release;
constexpr std::memory_order ORDRE_DE_LA_PRISE = std::memory_order_acquire;

constexpr bool paire_de_temoin(std::memory_order pose, std::memory_order prise) {
    return (pose == std::memory_order_release || pose == std::memory_order_seq_cst) &&
           (prise == std::memory_order_acquire || prise == std::memory_order_seq_cst);
}

std::atomic<int> temoin_demande{0};
std::atomic<int> temoin_pose{0};
std::atomic<long> gravure_du_temoin{0};
std::atomic<bool> relais_abandonne{false};

bool attendre_au_moins(const std::atomic<int> &jauge, int seuil, std::memory_order ordre) {
    for (long essai = 0; essai < PLAFOND_D_ATTENTE; essai++) {
        if (jauge.load(ordre) >= seuil) {
            return true;
        }
        if (relais_abandonne.load(std::memory_order_relaxed)) {
            return false;
        }
        if ((essai & 4095) == 4095) {
            std::this_thread::yield();
        }
    }
    relais_abandonne.store(true, std::memory_order_relaxed);
    return false;
}

void porter_le_temoin() {
    for (int manche = 1; manche <= MANCHES; manche++) {
        if (!attendre_au_moins(temoin_demande, manche, std::memory_order_acquire)) {
            return;
        }
        gravure_du_temoin.store(manche * PAS_DU_TEMOIN, std::memory_order_relaxed);
        temoin_pose.store(manche, ORDRE_DE_LA_POSE);
    }
}

long recevoir_le_temoin(int manche) {
    temoin_demande.store(manche, std::memory_order_release);
    if (!attendre_au_moins(temoin_pose, manche, ORDRE_DE_LA_PRISE)) {
        return -1;
    }
    return gravure_du_temoin.load(std::memory_order_relaxed);
}

}

int main() {
    long total_recu = 0;
    int manches_recues = 0;
    int gravures_perimees = 0;

    std::thread porteuse(porter_le_temoin);
    for (int manche = 1; manche <= MANCHES; manche++) {
        const long gravure = recevoir_le_temoin(manche);
        if (gravure < 0) {
            break;
        }
        manches_recues++;
        total_recu += gravure;
        if (gravure != manche * PAS_DU_TEMOIN) {
            gravures_perimees++;
        }
    }
    porteuse.join();

    VERIFIE(!relais_abandonne.load(std::memory_order_relaxed),
            "le relais s'est deroule sans qu'aucune attente touche son plafond");
    VERIFIE_ENTIER(manches_recues, MANCHES, "les cinq cents temoins ont bien ete pris");
    VERIFIE_ENTIER(gravures_perimees, 0,
                   "aucune gravure lue n'etait celle de la manche precedente");
    VERIFIE_ENTIER(total_recu, TOTAL_ATTENDU,
                   "et la somme des gravures est celle que la porteuse a ecrites");
    VERIFIE(paire_de_temoin(ORDRE_DE_LA_POSE, ORDRE_DE_LA_PRISE),
            "la pose est en release et la prise en acquire : c'est la paire qui cree l'ordre");
    return BILAN();
}
