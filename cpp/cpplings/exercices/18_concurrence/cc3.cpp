#include <atomic>
#include <thread>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

constexpr int BUCHERONNES = 8;
constexpr int BUCHES_PAR_BUCHERONNE = 2000;
constexpr int BUCHES_ORCHESTREES = 2;
constexpr int BUCHES_TOTALES = BUCHERONNES * BUCHES_PAR_BUCHERONNE + BUCHES_ORCHESTREES;
constexpr long PLAFOND_D_ATTENTE = 3000000;
constexpr long SOMME_ATTENDUE = (long)BUCHES_TOTALES * (BUCHES_TOTALES + 1) / 2;

struct Buche {
    int marque = 0;
    Buche *dessous = nullptr;
};

Buche buches[BUCHES_TOTALES];
std::atomic<Buche *> sommet{nullptr};
std::atomic<int> poussees_abandonnees{0};

std::atomic<int> jalon_du_duo{0};
std::atomic<bool> duo_abandonne{false};
thread_local bool je_pose_lentement = false;

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
    if (!je_pose_lentement) {
        return;
    }
    int jalon_neuf = 0;
    if (!jalon_du_duo.compare_exchange_strong(jalon_neuf, 1, std::memory_order_acq_rel,
                                              std::memory_order_relaxed)) {
        return;
    }
    attendre_le_jalon(2);
}

void empiler(Buche *buche) {
    Buche *ancien_sommet = sommet.load(std::memory_order_relaxed);
    buche->dessous = ancien_sommet;
    laisser_passer_l_autre_fil();
    sommet.store(buche, std::memory_order_release);
}

void poser_lentement(Buche *buche) {
    je_pose_lentement = true;
    empiler(buche);
}

void poser_vite(Buche *buche) {
    attendre_le_jalon(1);
    empiler(buche);
    jalon_du_duo.store(2, std::memory_order_release);
}

void abattre(int rang) {
    const int premiere = BUCHES_ORCHESTREES + rang * BUCHES_PAR_BUCHERONNE;
    for (int pas = 0; pas < BUCHES_PAR_BUCHERONNE; pas++) {
        empiler(&buches[premiere + pas]);
    }
}

}

int main() {
    for (int rang = 0; rang < BUCHES_TOTALES; rang++) {
        buches[rang].marque = rang + 1;
    }

    {
        std::thread lente(poser_lentement, &buches[0]);
        std::thread rapide(poser_vite, &buches[1]);
        lente.join();
        rapide.join();
    }

    int hauteur_du_duo = 0;
    for (Buche *courant = sommet.load(std::memory_order_acquire);
         courant != nullptr && hauteur_du_duo <= BUCHES_ORCHESTREES; courant = courant->dessous) {
        hauteur_du_duo++;
    }

    VERIFIE(!duo_abandonne.load(std::memory_order_relaxed),
            "l'orchestration a fonctionne : laisser_passer_l_autre_fil est reste dans empiler");
    VERIFIE_ENTIER(hauteur_du_duo, BUCHES_ORCHESTREES,
                   "les deux buches du duo sont sur le tas : celle posee pendant l'attente aussi");

    {
        std::vector<std::thread> bucheronnes;
        bucheronnes.reserve(BUCHERONNES);
        for (int rang = 0; rang < BUCHERONNES; rang++) {
            bucheronnes.emplace_back(abattre, rang);
        }
        for (std::thread &bucheronne : bucheronnes) {
            bucheronne.join();
        }
    }

    int hauteur = 0;
    long somme_des_marques = 0;
    for (Buche *courant = sommet.load(std::memory_order_acquire);
         courant != nullptr && hauteur <= BUCHES_TOTALES; courant = courant->dessous) {
        hauteur++;
        somme_des_marques += courant->marque;
    }

    VERIFIE_ENTIER(poussees_abandonnees.load(std::memory_order_relaxed), 0,
                   "aucune poussee n'a touche son plafond d'essais");
    VERIFIE_ENTIER(hauteur, BUCHES_TOTALES, "les seize mille deux buches sont toutes sur le tas");
    VERIFIE_ENTIER(somme_des_marques, SOMME_ATTENDUE,
                   "et chaque marque n'y figure qu'une fois : aucune poussee n'en a ecrase une");
    return BILAN();
}
