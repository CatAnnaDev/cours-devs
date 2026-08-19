#include <cstddef>
#include <cstdint>
#include <cstdlib>
#include <memory_resource>
#include <new>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

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

constexpr std::size_t NOMBRE_DE_VALEURS = 256;
constexpr std::size_t TAILLE_DU_TAMPON = 4096;

bool dans_le_tampon(const void *adresse, const std::byte *tampon, std::size_t taille) {
    const std::uintptr_t position = reinterpret_cast<std::uintptr_t>(adresse);
    const std::uintptr_t debut = reinterpret_cast<std::uintptr_t>(tampon);
    return position >= debut && position < debut + taille;
}

}

int main() {
    alignas(std::max_align_t) std::byte tampon[TAILLE_DU_TAMPON];
    std::pmr::monotonic_buffer_resource arene(tampon, sizeof(tampon),
                                              std::pmr::null_memory_resource());

    suivi::Tas::remettre_a_zero();

    std::pmr::vector<int> valeurs(&arene);
    for (std::size_t indice = 0; indice < NOMBRE_DE_VALEURS; indice++) {
        valeurs.push_back(static_cast<int>(indice * indice));
    }

    long long total = 0;
    for (int valeur : valeurs) {
        total += valeur;
    }

    VERIFIE_ENTIER(valeurs.size(), NOMBRE_DE_VALEURS, "deux cent cinquante-six valeurs rangees");
    VERIFIE_ENTIER(total, 5559680, "la somme des carres est intacte");
    VERIFIE_ENTIER(valeurs[100], 10000, "et chaque case aussi");

    VERIFIE_ENTIER(suivi::Tas::allocations, 0, "le remplissage n'a pas touche le tas");
    VERIFIE_ENTIER(suivi::Tas::liberations, 0, "et il n'a rien rendu non plus");
    VERIFIE(dans_le_tampon(valeurs.data(), tampon, sizeof(tampon)),
            "les elements vivent dans le tableau local, sur la pile");
    return BILAN();
}
