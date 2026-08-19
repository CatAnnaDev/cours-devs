#include <cstddef>
#include <cstdlib>
#include <new>
#include <string>
#include <string_view>
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

bool debute_par_temoin(std::string texte, std::string debut) {
    if (texte.size() < debut.size()) {
        return false;
    }
    return texte.substr(0, debut.size()) == debut;
}

bool debute_par(std::string texte, std::string debut) {
    if (texte.size() < debut.size()) {
        return false;
    }
    return texte.substr(0, debut.size()) == debut;
}

std::size_t compter_sous_le_prefixe(const std::vector<std::string> &chemins,
                                    const std::string &prefixe) {
    std::size_t total = 0;
    for (const std::string &chemin : chemins) {
        if (debute_par(chemin, prefixe)) {
            total++;
        }
    }
    return total;
}

}

int main() {
    const std::vector<std::string> chemins = {
        "ressources/textures/pierre/mousse_humide.png",
        "ressources/textures/pierre/dalle_fissuree.png",
        "ressources/sons/ambiance/goutte_lointaine.wav",
        "ressources/textures/pierre/veine_de_quartz.png",
    };
    const std::string prefixe = "ressources/textures/pierre/";

    suivi::Tas::remettre_a_zero();
    {
        std::vector<int> mesure;
        mesure.reserve(100);
        VERIFIE_ENTIER(suivi::Tas::allocations, 1, "reserve(100) demande le tas une seule fois");
        VERIFIE_ENTIER(suivi::Tas::octets, 100 * sizeof(int),
                       "quatre cents octets, ni plus ni moins");
    }
    VERIFIE_ENTIER(suivi::Tas::liberations, 1, "et le vecteur detruit rend son bloc");

    suivi::Tas::remettre_a_zero();
    const bool temoin = debute_par_temoin(chemins[0], prefixe);
    VERIFIE(temoin, "le temoin repond juste");
    VERIFIE_ENTIER(suivi::Tas::allocations, 3,
                   "le temoin alloue trois fois : deux parametres copies, un substr");

    suivi::Tas::remettre_a_zero();
    const std::size_t trouves = compter_sous_le_prefixe(chemins, prefixe);

    VERIFIE_ENTIER(trouves, 3, "trois chemins sous le prefixe");
    VERIFIE_ENTIER(suivi::Tas::allocations, 0, "lire un texte ne demande aucune allocation");
    VERIFIE_ENTIER(suivi::Tas::octets, 0, "pas un octet pris au tas");
    return BILAN();
}
