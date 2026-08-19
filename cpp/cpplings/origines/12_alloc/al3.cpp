#include <cstddef>
#include <cstdint>
#include <cstdlib>
#include <memory_resource>
#include <new>
#include <string>
#include <vector>

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

using Ligne = std::string;
using Journal = std::pmr::vector<Ligne>;

constexpr std::size_t NOMBRE_DE_LIGNES = 32;
constexpr std::size_t TAILLE_DU_TAMPON = 8192;

const char *const MODELES[4] = {
    "journal/2024-03-11/incident-refroidissement-secteur-nord",
    "journal/2024-03-12/verification-des-joints-de-la-coque",
    "journal/2024-03-13/etalonnage-du-sonar-de-proximite",
    "journal/2024-03-14/remplacement-du-filtre-a-particules",
};

bool dans_le_tampon(const void *adresse, const std::byte *tampon, std::size_t taille) {
    const std::uintptr_t position = reinterpret_cast<std::uintptr_t>(adresse);
    const std::uintptr_t debut = reinterpret_cast<std::uintptr_t>(tampon);
    return position >= debut && position < debut + taille;
}

}

int main() {
    suivi::Tas::remettre_a_zero();
    {
        const std::string courte = "vingt-deux caracteres!";
        VERIFIE_ENTIER(courte.size(), 22, "vingt-deux caracteres");
        VERIFIE_ENTIER(suivi::Tas::allocations, 0, "ils tiennent dans l'objet chaine lui-meme");
    }

    suivi::Tas::remettre_a_zero();
    {
        const std::string longue = "vingt-trois caracteres!";
        VERIFIE_ENTIER(longue.size(), 23, "vingt-trois caracteres");
        VERIFIE_ENTIER(suivi::Tas::allocations, 1, "un de plus, et il faut deja le tas");
    }

    alignas(std::max_align_t) std::byte tampon[TAILLE_DU_TAMPON];
    std::pmr::monotonic_buffer_resource arene(tampon, sizeof(tampon),
                                              std::pmr::null_memory_resource());

    suivi::Tas::remettre_a_zero();

    Journal lignes(&arene);
    lignes.reserve(NOMBRE_DE_LIGNES);
    for (std::size_t indice = 0; indice < NOMBRE_DE_LIGNES; indice++) {
        lignes.emplace_back(MODELES[indice % 4]);
    }

    std::size_t caracteres = 0;
    std::size_t dans_l_arene = 0;
    for (const Ligne &ligne : lignes) {
        caracteres += ligne.size();
        if (dans_le_tampon(ligne.data(), tampon, sizeof(tampon))) {
            dans_l_arene++;
        }
    }

    VERIFIE_ENTIER(lignes.size(), NOMBRE_DE_LIGNES, "trente-deux lignes de journal");
    VERIFIE_TEXTE(lignes[5], MODELES[1], "la sixieme ligne est la bonne");
    VERIFIE_ENTIER(caracteres, 1720, "le texte est complet");
    VERIFIE(dans_le_tampon(lignes.data(), tampon, sizeof(tampon)),
            "le tableau du vecteur sort bien de l'arene");
    VERIFIE_ENTIER(dans_l_arene, NOMBRE_DE_LIGNES, "et le texte de chaque ligne aussi");
    VERIFIE_ENTIER(suivi::Tas::allocations, 0, "aucune ligne n'est allee chercher le tas");
    return BILAN();
}
