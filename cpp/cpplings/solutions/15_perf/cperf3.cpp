#include <cstddef>
#include <cstdlib>
#include <new>
#include <string>
#include <string_view>

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

constexpr std::size_t NOMBRE_DE_CHAMPS = 6;
constexpr char SEPARATEUR = ';';

const char *const LIGNE_DE_JOURNAL = "identifiant-du-capteur-de-proximite;"
                                     "horodatage-2024-03-11T04-17-52Z;"
                                     "profondeur-mesuree-en-millimetres;"
                                     "temperature-du-fluide-caloporteur;"
                                     "pression-absolue-au-point-de-mesure;"
                                     "code-de-controle-de-redondance";

std::size_t decouper_en_chaines(const std::string &source, std::string sortie[],
                                std::size_t capacite) {
    std::size_t compte = 0;
    std::size_t debut = 0;
    while (debut <= source.size() && compte < capacite) {
        const std::size_t fin = source.find(SEPARATEUR, debut);
        const std::size_t longueur = (fin == std::string::npos ? source.size() : fin) - debut;
        sortie[compte] = source.substr(debut, longueur);
        compte++;
        if (fin == std::string::npos) {
            break;
        }
        debut = fin + 1;
    }
    return compte;
}

std::size_t decouper_en_vues(std::string_view source, std::string_view sortie[],
                             std::size_t capacite) {
    std::size_t compte = 0;
    std::size_t debut = 0;
    while (debut <= source.size() && compte < capacite) {
        const std::size_t fin = source.find(SEPARATEUR, debut);
        const std::size_t longueur = (fin == std::string_view::npos ? source.size() : fin) - debut;
        sortie[compte] = source.substr(debut, longueur);
        compte++;
        if (fin == std::string_view::npos) {
            break;
        }
        debut = fin + 1;
    }
    return compte;
}

bool pointe_dans(std::string_view vue, const std::string &source) {
    return vue.data() >= source.data() && vue.data() + vue.size() <= source.data() + source.size();
}

}

int main() {
    const std::string source(LIGNE_DE_JOURNAL);
    std::string champs[NOMBRE_DE_CHAMPS];
    std::string_view vues[NOMBRE_DE_CHAMPS];

    suivi::Tas::remettre_a_zero();
    const std::size_t nombre_de_chaines = decouper_en_chaines(source, champs, NOMBRE_DE_CHAMPS);
    const std::size_t allocations_des_chaines = suivi::Tas::allocations;

    suivi::Tas::remettre_a_zero();
    const std::size_t nombre_de_vues = decouper_en_vues(source, vues, NOMBRE_DE_CHAMPS);
    const std::size_t allocations_des_vues = suivi::Tas::allocations;

    std::size_t vues_dans_la_source = 0;
    for (std::size_t rang = 0; rang < nombre_de_vues; rang++) {
        if (pointe_dans(vues[rang], source)) {
            vues_dans_la_source++;
        }
    }

    VERIFIE_ENTIER(source.size(), 202,
                   "deux cent deux caracteres, six champs, cinq points-virgules");
    VERIFIE_ENTIER(nombre_de_chaines, NOMBRE_DE_CHAMPS, "six champs decoupes en chaines");
    VERIFIE_ENTIER(nombre_de_vues, NOMBRE_DE_CHAMPS, "six champs decoupes en vues");

    VERIFIE_TEXTE(champs[0], "identifiant-du-capteur-de-proximite", "le premier champ est entier");
    VERIFIE_TEXTE(champs[5], "code-de-controle-de-redondance", "le dernier aussi");
    VERIFIE_TEXTE(vues[0], champs[0], "la vue dit exactement la meme chose que la chaine");
    VERIFIE_TEXTE(vues[3], champs[3], "au milieu comme au bord");
    VERIFIE_TEXTE(vues[5], champs[5], "et jusqu'au dernier");

    VERIFIE_ENTIER(allocations_des_chaines, NOMBRE_DE_CHAMPS,
                   "substr sur une chaine alloue une fois par champ");
    VERIFIE_ENTIER(allocations_des_vues, 0, "substr sur une vue ne fait que bouger deux pointeurs");
    VERIFIE_ENTIER(vues_dans_la_source, NOMBRE_DE_CHAMPS,
                   "chaque vue regarde dans la ligne d'origine, elle ne possede rien");
    return BILAN();
}
