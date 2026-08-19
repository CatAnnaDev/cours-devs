#include <cstddef>
#include <cstdlib>
#include <list>
#include <memory_resource>
#include <new>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace suivi {

struct Tas {
    static inline std::size_t allocations = 0;
    static inline std::size_t liberations = 0;
    static inline std::size_t octets = 0;

    static void remettre_a_zero() {
        allocations = 0;
        liberations = 0;
        octets = 0;
    }
};

}

void *operator new(std::size_t octets) {
    void *bloc = std::malloc(octets != 0 ? octets : 1);
    if (bloc == nullptr) {
        throw std::bad_alloc();
    }
    suivi::Tas::allocations++;
    suivi::Tas::octets += octets;
    return bloc;
}

void *operator new[](std::size_t octets) { return ::operator new(octets); }

void operator delete(void *bloc) noexcept {
    if (bloc != nullptr) {
        suivi::Tas::liberations++;
        std::free(bloc);
    }
}

void operator delete[](void *bloc) noexcept { ::operator delete(bloc); }

void operator delete(void *bloc, std::size_t) noexcept { ::operator delete(bloc); }

void operator delete[](void *bloc, std::size_t) noexcept { ::operator delete(bloc); }

namespace {

constexpr std::size_t NOMBRE_DE_MESURES = 2000;

struct Mesure {
    int capteur;
    float profondeur;
};

using Releve = std::pmr::list<Mesure>;

void remplir(Releve &releve) {
    for (std::size_t indice = 0; indice < NOMBRE_DE_MESURES; indice++) {
        releve.push_back(Mesure{static_cast<int>(indice % 8), 0.5f * static_cast<float>(indice)});
    }
}

double profondeur_totale(const Releve &releve) {
    double total = 0.0;
    for (const Mesure &mesure : releve) {
        total += mesure.profondeur;
    }
    return total;
}

}

int main() {
    suivi::Tas::remettre_a_zero();
    std::size_t allocations_sans_pool = 0;
    std::size_t octets_sans_pool = 0;
    {
        Releve temoin(std::pmr::new_delete_resource());
        remplir(temoin);
        allocations_sans_pool = suivi::Tas::allocations;
        octets_sans_pool = suivi::Tas::octets;
        VERIFIE_REEL(profondeur_totale(temoin), 999500.0, "le temoin porte les bonnes mesures");
    }
    VERIFIE_ENTIER(allocations_sans_pool, NOMBRE_DE_MESURES,
                   "sans pool : un aller-retour au tas par mesure");

    std::pmr::unsynchronized_pool_resource pool;
    std::pmr::memory_resource *ressource = std::pmr::new_delete_resource();

    suivi::Tas::remettre_a_zero();
    std::size_t allocations_avec_pool = 0;
    std::size_t octets_avec_pool = 0;
    {
        Releve releve(ressource);
        remplir(releve);
        allocations_avec_pool = suivi::Tas::allocations;
        octets_avec_pool = suivi::Tas::octets;

        VERIFIE_ENTIER(releve.size(), NOMBRE_DE_MESURES, "deux mille mesures enregistrees");
        VERIFIE_REEL(profondeur_totale(releve), 999500.0, "et les memes valeurs qu'au temoin");
        VERIFIE_ENTIER(releve.front().capteur, 0, "la premiere vient du capteur zero");
    }

    VERIFIE(allocations_avec_pool >= 1, "le pool va bien chercher sa matiere en amont");
    VERIFIE(allocations_avec_pool * 50 < NOMBRE_DE_MESURES,
            "moins d'une allocation en amont pour cinquante mesures rangees");
    VERIFIE(octets_avec_pool > octets_sans_pool,
            "il prend plus d'octets au total : gros morceaux decoupes, et de l'avance");
    return BILAN();
}
