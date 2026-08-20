#include <atomic>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

constexpr int JETONS = 3;
constexpr int PLAFOND_D_ESSAIS = 64;

struct Jeton {
    int valeur = 0;
    Jeton *dessous = nullptr;
};

struct Sommet {
    Jeton *jeton;
    unsigned long marque;
};

Jeton jetons[JETONS];
std::atomic<Sommet> sommet{Sommet{nullptr, 0}};
std::atomic<int> essais_epuises{0};

bool intercalation_faite = false;
Jeton *jeton_garde_par_l_autre_fil = nullptr;

void intercaler_l_autre_fil();

void empiler(Jeton *jeton) {
    Sommet ancien = sommet.load(std::memory_order_acquire);
    for (int essai = 0; essai < PLAFOND_D_ESSAIS; essai++) {
        jeton->dessous = ancien.jeton;
        const Sommet neuf{jeton, ancien.marque + 1};
        if (sommet.compare_exchange_strong(ancien, neuf, std::memory_order_acq_rel,
                                           std::memory_order_acquire)) {
            return;
        }
    }
    essais_epuises.fetch_add(1, std::memory_order_relaxed);
}

Jeton *depiler() {
    Sommet ancien = sommet.load(std::memory_order_acquire);
    for (int essai = 0; essai < PLAFOND_D_ESSAIS; essai++) {
        if (ancien.jeton == nullptr) {
            return nullptr;
        }
        const Sommet neuf{ancien.jeton->dessous, ancien.marque + 1};
        intercaler_l_autre_fil();
        if (sommet.compare_exchange_strong(ancien, neuf, std::memory_order_acq_rel,
                                           std::memory_order_acquire)) {
            return ancien.jeton;
        }
    }
    essais_epuises.fetch_add(1, std::memory_order_relaxed);
    return nullptr;
}

void intercaler_l_autre_fil() {
    if (intercalation_faite) {
        return;
    }
    intercalation_faite = true;
    Jeton *premier = depiler();
    Jeton *deuxieme = depiler();
    if (premier != nullptr) {
        empiler(premier);
    }
    jeton_garde_par_l_autre_fil = deuxieme;
}

}

int main() {
    for (int rang = 0; rang < JETONS; rang++) {
        jetons[rang].valeur = rang + 1;
        empiler(&jetons[rang]);
    }

    Jeton *jeton_pris_par_ce_fil = depiler();

    int presences[JETONS] = {};
    if (jeton_pris_par_ce_fil != nullptr) {
        presences[jeton_pris_par_ce_fil->valeur - 1]++;
    }
    if (jeton_garde_par_l_autre_fil != nullptr) {
        presences[jeton_garde_par_l_autre_fil->valeur - 1]++;
    }

    int hauteur = 0;
    for (Jeton *courant = sommet.load(std::memory_order_acquire).jeton;
         courant != nullptr && hauteur <= JETONS; courant = courant->dessous) {
        presences[courant->valeur - 1]++;
        hauteur++;
    }

    int jetons_en_double = 0;
    int jetons_perdus = 0;
    for (int rang = 0; rang < JETONS; rang++) {
        if (presences[rang] > 1) {
            jetons_en_double++;
        }
        if (presences[rang] == 0) {
            jetons_perdus++;
        }
    }

    VERIFIE(std::atomic<Sommet>::is_always_lock_free,
            "un pointeur et sa marque tiennent en seize octets, echanges d'un seul coup");
    VERIFIE_ENTIER(essais_epuises.load(std::memory_order_relaxed), 0,
                   "aucune boucle de compare_exchange n'a touche son plafond d'essais");
    VERIFIE(jeton_pris_par_ce_fil != nullptr && jeton_garde_par_l_autre_fil != nullptr,
            "les deux fils repartent chacun avec un jeton en main");
    VERIFIE_ENTIER(jetons_en_double, 0,
                   "aucun jeton n'est a la fois dans une main et sur la pile");
    VERIFIE_ENTIER(jetons_perdus, 0, "et aucun jeton n'a disparu du compte");
    VERIFIE_ENTIER(hauteur, 1, "et il reste exactement un jeton sur la pile");
    return BILAN();
}
