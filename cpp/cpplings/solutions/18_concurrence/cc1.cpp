#include <atomic>
#include <thread>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

constexpr int PORTEUSES = 4;
constexpr int SEAUX_PAR_PORTEUSE = 25000;
constexpr int NIVEAU_DU_PUITS = 77;
constexpr long PLAFOND_D_ECOUTE = 20000000;

constexpr std::memory_order ORDRE_DU_COMPTEUR = std::memory_order_relaxed;
constexpr std::memory_order ORDRE_DE_L_ANNONCE = std::memory_order_release;
constexpr std::memory_order ORDRE_DE_L_ECOUTE = std::memory_order_acquire;

constexpr bool suffit_pour_compter(std::memory_order ordre) {
    return ordre == std::memory_order_relaxed;
}

constexpr bool publie_vraiment(std::memory_order ordre) {
    return ordre == std::memory_order_release || ordre == std::memory_order_seq_cst;
}

constexpr bool recoit_vraiment(std::memory_order ordre) {
    return ordre == std::memory_order_acquire || ordre == std::memory_order_seq_cst;
}

std::atomic<long> seaux_verses{0};
std::atomic<int> niveau_du_bassin{0};
std::atomic<int> bassin_annonce{0};

void porter_de_l_eau() {
    for (int seau = 0; seau < SEAUX_PAR_PORTEUSE; seau++) {
        seaux_verses.fetch_add(1, ORDRE_DU_COMPTEUR);
    }
}

void annoncer_le_bassin() {
    niveau_du_bassin.store(NIVEAU_DU_PUITS, std::memory_order_relaxed);
    bassin_annonce.store(1, ORDRE_DE_L_ANNONCE);
}

}

int main() {
    std::vector<std::thread> porteuses;
    porteuses.reserve(PORTEUSES);
    for (int rang = 0; rang < PORTEUSES; rang++) {
        porteuses.emplace_back(porter_de_l_eau);
    }
    for (std::thread &porteuse : porteuses) {
        porteuse.join();
    }

    VERIFIE_ENTIER(seaux_verses.load(std::memory_order_seq_cst),
                   (long)PORTEUSES * SEAUX_PAR_PORTEUSE,
                   "les cent mille seaux sont tous comptes : relaxed suffit pour un total");
    VERIFIE(suffit_pour_compter(ORDRE_DU_COMPTEUR),
            "et le compteur reste en relaxed : personne ne lit son ordre, seulement sa somme");

    bool ecoute_abandonnee = false;
    int niveau_vu = -1;
    std::thread annonceuse(annoncer_le_bassin);
    for (long essai = 0; bassin_annonce.load(ORDRE_DE_L_ECOUTE) == 0; essai++) {
        if (essai >= PLAFOND_D_ECOUTE) {
            ecoute_abandonnee = true;
            break;
        }
    }
    if (!ecoute_abandonnee) {
        niveau_vu = niveau_du_bassin.load(std::memory_order_relaxed);
    }
    annonceuse.join();

    VERIFIE(!ecoute_abandonnee, "l'annonce du bassin est arrivee avant le plafond d'essais");
    VERIFIE_ENTIER(niveau_vu, NIVEAU_DU_PUITS,
                   "le niveau lu apres l'annonce est celui que l'annonceuse avait verse");
    VERIFIE(publie_vraiment(ORDRE_DE_L_ANNONCE),
            "l'annonce publie le bassin : elle demande release, pas relaxed");
    VERIFIE(recoit_vraiment(ORDRE_DE_L_ECOUTE),
            "et l'ecoute recoit le bassin : elle demande acquire, pas relaxed");
    return BILAN();
}
