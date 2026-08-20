#include <atomic>
#include <mutex>
#include <thread>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

constexpr int LECTRICES = 8;
constexpr int PAGES_DE_L_ANNUAIRE = 41;
constexpr int ANNUAIRES_POSSIBLES = 16;
constexpr long PLAFOND_D_ATTENTE = 3000000;

struct Annuaire {
    int pages = 0;
};

Annuaire annuaires_composes[ANNUAIRES_POSSIBLES];
std::atomic<int> compositions{0};
std::atomic<Annuaire *> annuaire_partage{nullptr};
std::mutex verrou_de_l_annuaire;

std::atomic<int> jalon_du_duo{0};
std::atomic<bool> duo_abandonne{false};
thread_local bool je_lis_lentement = false;

Annuaire *composer_l_annuaire() {
    const int rang = compositions.fetch_add(1, std::memory_order_acq_rel);
    Annuaire *neuf = &annuaires_composes[rang % ANNUAIRES_POSSIBLES];
    neuf->pages = PAGES_DE_L_ANNUAIRE;
    return neuf;
}

bool attendre_le_jalon(int seuil) {
    for (long essai = 0; essai < PLAFOND_D_ATTENTE; essai++) {
        if (jalon_du_duo.load(std::memory_order_acquire) >= seuil) {
            return true;
        }
        if ((essai & 1023) == 1023) {
            std::this_thread::yield();
        }
    }
    duo_abandonne.store(true, std::memory_order_relaxed);
    return false;
}

void laisser_passer_l_autre_fil() {
    if (!je_lis_lentement) {
        return;
    }
    int jalon_neuf = 0;
    if (!jalon_du_duo.compare_exchange_strong(jalon_neuf, 1, std::memory_order_acq_rel,
                                              std::memory_order_relaxed)) {
        return;
    }
    attendre_le_jalon(2);
}

Annuaire &obtenir_l_annuaire() {
    if (annuaire_partage.load(std::memory_order_acquire) == nullptr) {
        laisser_passer_l_autre_fil();
        std::lock_guard<std::mutex> garde(verrou_de_l_annuaire);
        annuaire_partage.store(composer_l_annuaire(), std::memory_order_release);
    }
    return *annuaire_partage.load(std::memory_order_acquire);
}

Annuaire *annuaires_vus[LECTRICES + 2] = {};

void consulter_lentement() {
    je_lis_lentement = true;
    annuaires_vus[0] = &obtenir_l_annuaire();
}

void consulter_vite() {
    attendre_le_jalon(1);
    annuaires_vus[1] = &obtenir_l_annuaire();
    jalon_du_duo.store(2, std::memory_order_release);
}

void consulter(int rang) { annuaires_vus[2 + rang] = &obtenir_l_annuaire(); }

}

int main() {
    {
        std::thread lente(consulter_lentement);
        std::thread rapide(consulter_vite);
        lente.join();
        rapide.join();
    }
    {
        std::vector<std::thread> lectrices;
        lectrices.reserve(LECTRICES);
        for (int rang = 0; rang < LECTRICES; rang++) {
            lectrices.emplace_back(consulter, rang);
        }
        for (std::thread &lectrice : lectrices) {
            lectrice.join();
        }
    }

    int annuaires_differents = 0;
    int pages_fausses = 0;
    for (Annuaire *vu : annuaires_vus) {
        if (vu != annuaires_vus[0]) {
            annuaires_differents++;
        }
        if (vu == nullptr || vu->pages != PAGES_DE_L_ANNUAIRE) {
            pages_fausses++;
        }
    }

    VERIFIE(!duo_abandonne.load(std::memory_order_relaxed),
            "l'orchestration a fonctionne : laisser_passer_l_autre_fil est reste dans l'acces");
    VERIFIE_ENTIER(compositions.load(std::memory_order_relaxed), 1,
                   "l'annuaire n'a ete compose qu'une fois, malgre les dix demandes");
    VERIFIE_ENTIER(annuaires_differents, 0, "et les dix lectrices tiennent toutes le meme");
    VERIFIE_ENTIER(pages_fausses, 0, "chacune y trouve bien les quarante et une pages");
    return BILAN();
}
