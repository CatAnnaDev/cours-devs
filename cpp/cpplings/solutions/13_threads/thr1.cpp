#include <atomic>
#include <thread>
#include <type_traits>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

constexpr int TOURS = 200000;

template <typename T>
struct SansVerrou : std::false_type {};

template <typename T>
struct SansVerrou<std::atomic<T>> : std::bool_constant<std::atomic<T>::is_always_lock_free> {};

using Compteur = std::atomic<long>;

Compteur pieces_ramassees{0};

void ramasser() {
    for (int tour = 0; tour < TOURS; tour++) {
        pieces_ramassees++;
    }
}

}

int main() {
    std::thread premiere(ramasser);
    std::thread seconde(ramasser);
    premiere.join();
    seconde.join();

    VERIFIE_ENTIER(pieces_ramassees, 2 * TOURS,
                   "deux fils, deux cent mille tours chacun, quatre cent mille pieces");
    VERIFIE(SansVerrou<Compteur>::value,
            "le compteur partage est un atomique que le materiel traite sans verrou");
    return BILAN();
}
