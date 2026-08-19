#include <atomic>
#include <cstddef>
#include <cstdint>
#include <functional>
#include <thread>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

constexpr std::size_t TAILLE_LIGNE_CACHE = 64;
constexpr std::uint64_t TOURS = 200000;

struct Compteurs {
    alignas(TAILLE_LIGNE_CACHE) std::atomic<std::uint64_t> premier{0};
    alignas(TAILLE_LIGNE_CACHE) std::atomic<std::uint64_t> second{0};
};

static_assert(sizeof(Compteurs) == 2 * TAILLE_LIGNE_CACHE,
              "chaque compteur doit occuper sa propre ligne de cache");

void marteler(std::atomic<std::uint64_t> &compteur) {
    for (std::uint64_t tour = 0; tour < TOURS; tour++) {
        compteur.fetch_add(1, std::memory_order_relaxed);
    }
}

}

int main() {
    Compteurs compteurs;

    std::thread premier_fil(marteler, std::ref(compteurs.premier));
    std::thread second_fil(marteler, std::ref(compteurs.second));
    premier_fil.join();
    second_fil.join();

    const auto adresse_premier = reinterpret_cast<std::uintptr_t>(&compteurs.premier);
    const auto adresse_second = reinterpret_cast<std::uintptr_t>(&compteurs.second);
    const std::size_t ecart = (std::size_t)(adresse_second - adresse_premier);

    VERIFIE_ENTIER(compteurs.premier.load(), TOURS, "le premier fil a compte jusqu'au bout");
    VERIFIE_ENTIER(compteurs.second.load(), TOURS, "le second fil aussi");
    VERIFIE_ENTIER(compteurs.premier.load() + compteurs.second.load(), 2 * TOURS,
                   "aucune incrementation perdue");

    VERIFIE(ecart >= TAILLE_LIGNE_CACHE, "les deux compteurs ne partagent plus la meme ligne");
    VERIFIE_ENTIER(ecart, TAILLE_LIGNE_CACHE, "exactement une ligne de cache d'ecart");
    VERIFIE_ENTIER(offsetof(Compteurs, second) % TAILLE_LIGNE_CACHE, 0,
                   "le second compteur demarre pile au debut d'une ligne");
    VERIFIE_ENTIER(alignof(Compteurs), TAILLE_LIGNE_CACHE, "la struct s'aligne sur une ligne");
    VERIFIE_ENTIER(sizeof(Compteurs), 2 * TAILLE_LIGNE_CACHE,
                   "deux lignes de 64 octets (Apple Silicon en fait 128)");
    return BILAN();
}
