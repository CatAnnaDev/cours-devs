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

constexpr std::size_t TAILLE_DU_TAMPON = 1024;
constexpr std::size_t NOMBRE_DE_DRAPEAUX = 13;
constexpr std::size_t NOMBRE_DE_REELS = 8;

class ArenePlate : public std::pmr::memory_resource {
  public:
    ArenePlate(std::byte *tampon, std::size_t capacite) : tampon_(tampon), capacite_(capacite) {}

    std::size_t position() const { return position_; }

    void *servir(std::size_t octets, std::size_t alignement) {
        const std::size_t depart = arrondir(position_, alignement);
        if (depart + octets > capacite_) {
            throw std::bad_alloc();
        }
        position_ = depart + octets;
        return tampon_ + depart;
    }

  private:
    static std::size_t arrondir(std::size_t position, std::size_t alignement) {
        return (position + alignement - 1) & ~(alignement - 1);
    }

    void *do_allocate(std::size_t octets, std::size_t alignement) override {
        return servir(octets, alignement);
    }

    void do_deallocate(void *, std::size_t, std::size_t) override {}

    bool do_is_equal(const std::pmr::memory_resource &autre) const noexcept override {
        return this == &autre;
    }

    std::byte *tampon_;
    std::size_t capacite_;
    std::size_t position_ = 0;
};

bool bien_aligne(const void *adresse, std::size_t alignement) {
    return reinterpret_cast<std::uintptr_t>(adresse) % alignement == 0;
}

}

int main() {
    alignas(std::max_align_t) std::byte tampon_sonde[64];
    ArenePlate sonde(tampon_sonde, sizeof(tampon_sonde));
    sonde.servir(1, 1);

    VERIFIE(bien_aligne(sonde.servir(sizeof(double), alignof(double)), alignof(double)),
            "apres un octet, servir recule jusqu'au prochain multiple de huit");

    alignas(std::max_align_t) std::byte tampon[TAILLE_DU_TAMPON];
    ArenePlate arene(tampon, sizeof(tampon));

    suivi::Tas::remettre_a_zero();

    std::pmr::vector<char> drapeaux(&arene);
    drapeaux.reserve(NOMBRE_DE_DRAPEAUX);
    for (std::size_t indice = 0; indice < NOMBRE_DE_DRAPEAUX; indice++) {
        drapeaux.push_back('a' + static_cast<char>(indice));
    }

    VERIFIE_ENTIER(arene.position(), 13, "treize octets pris, sans le moindre remplissage");

    std::pmr::vector<double> reels(&arene);
    reels.reserve(NOMBRE_DE_REELS);
    for (std::size_t indice = 0; indice < NOMBRE_DE_REELS; indice++) {
        reels.push_back(0.5 * static_cast<double>(indice));
    }

    VERIFIE(bien_aligne(reels.data(), alignof(double)),
            "l'arene a recule jusqu'au prochain multiple de huit");
    VERIFIE_ENTIER(arene.position(), 16 + NOMBRE_DE_REELS * sizeof(double),
                   "trois octets de remplissage perdus, et soixante-quatre utiles");

    double total = 0.0;
    for (double reel : reels) {
        total += reel;
    }
    VERIFIE_REEL(total, 14.0, "les huit reels sont lisibles et justes");
    VERIFIE_ENTIER(drapeaux[12], 'm', "les drapeaux n'ont pas bouge");

    const std::size_t position_avant = arene.position();
    {
        std::pmr::vector<double> passager(&arene);
        passager.reserve(4);
    }
    VERIFIE_ENTIER(arene.position(), position_avant + 32,
                   "do_deallocate ne fait rien : l'arene ne recule jamais");

    VERIFIE_ENTIER(suivi::Tas::allocations, 0, "et rien de tout cela n'est passe par le tas");
    return BILAN();
}
