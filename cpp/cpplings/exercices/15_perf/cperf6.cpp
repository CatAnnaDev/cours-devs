#include <cstddef>
#include <cstdlib>
#include <functional>
#include <new>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace suivi {

struct Tas {
    static inline std::size_t allocations = 0;
    static inline std::size_t liberations = 0;

    static void remettre_a_zero() {
        allocations = 0;
        liberations = 0;
    }
};

}

void *operator new(std::size_t octets) {
    void *bloc = std::malloc(octets != 0 ? octets : 1);
    if (bloc == nullptr) {
        throw std::bad_alloc();
    }
    suivi::Tas::allocations++;
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
constexpr int TOURS = 100;
constexpr long long RETENUES_PAR_PASSE = 63;

struct Fenetre {
    long long minimum;
    long long maximum;
    long long pas;

    static inline long long copies = 0;

    Fenetre(long long debut, long long fin, long long ecart)
        : minimum(debut), maximum(fin), pas(ecart) {}

    Fenetre(const Fenetre &autre)
        : minimum(autre.minimum), maximum(autre.maximum), pas(autre.pas) {
        copies++;
    }

    Fenetre &operator=(const Fenetre &autre) {
        minimum = autre.minimum;
        maximum = autre.maximum;
        pas = autre.pas;
        copies++;
        return *this;
    }

    ~Fenetre() = default;

    bool operator()(long long valeur) const {
        return valeur >= minimum && valeur <= maximum && valeur % pas == 0;
    }
};

std::vector<long long> echantillon() {
    std::vector<long long> valeurs;
    valeurs.reserve(NOMBRE_DE_VALEURS);
    for (std::size_t rang = 0; rang < NOMBRE_DE_VALEURS; rang++) {
        valeurs.push_back((long long)rang);
    }
    return valeurs;
}

long long compter_avec_effacement(const std::vector<long long> &valeurs,
                                  std::function<bool(long long)> predicat) {
    long long retenues = 0;
    for (const long long valeur : valeurs) {
        if (predicat(valeur)) {
            retenues++;
        }
    }
    return retenues;
}

template <typename Predicat>
long long compter_avec_gabarit(const std::vector<long long> &valeurs, const Predicat &predicat) {
    long long retenues = 0;
    for (const long long valeur : valeurs) {
        if (predicat(valeur)) {
            retenues++;
        }
    }
    return retenues;
}

long long balayer(const std::vector<long long> &valeurs, const Fenetre &fenetre) {
    long long total = 0;
    for (int tour = 0; tour < TOURS; tour++) {
        total += compter_avec_effacement(valeurs, fenetre);
    }
    return total;
}

}

int main() {
    const std::vector<long long> valeurs = echantillon();
    const Fenetre fenetre(10, 200, 3);

    VERIFIE_ENTIER(sizeof(Fenetre), 24, "vingt-quatre octets d'etat capture");
    VERIFIE_ENTIER(sizeof(std::function<bool(long long)>), 32,
                   "trente-deux octets en tout pour la std::function elle-meme");

    suivi::Tas::remettre_a_zero();
    Fenetre::copies = 0;
    {
        const std::function<bool(long long)> efface = fenetre;
        const std::size_t allocations_d_une_fonction = suivi::Tas::allocations;
        const long long copies_d_une_fonction = Fenetre::copies;
        VERIFIE(efface(12), "le predicat efface repond comme le foncteur");
        VERIFIE_ENTIER(allocations_d_une_fonction, 1,
                       "le foncteur ne tient pas dans le tampon, donc il part sur le tas");
        VERIFIE_ENTIER(copies_d_une_fonction, 2,
                       "et il est copie en chemin : parametre par valeur, puis bloc alloue");
    }

    suivi::Tas::remettre_a_zero();
    Fenetre::copies = 0;
    long long total_efface = 0;
    for (int tour = 0; tour < TOURS; tour++) {
        total_efface += compter_avec_effacement(valeurs, fenetre);
    }
    const std::size_t allocations_effacees = suivi::Tas::allocations;
    const long long copies_effacees = Fenetre::copies;

    suivi::Tas::remettre_a_zero();
    Fenetre::copies = 0;
    const long long total_balaye = balayer(valeurs, fenetre);
    const std::size_t allocations_du_balayage = suivi::Tas::allocations;
    const long long copies_du_balayage = Fenetre::copies;

    VERIFIE_ENTIER(total_efface, (long long)TOURS * RETENUES_PAR_PASSE,
                   "soixante-trois multiples de trois entre dix et deux cents");
    VERIFIE_ENTIER(total_balaye, total_efface, "le balayage compte exactement pareil");

    VERIFIE_ENTIER(allocations_effacees, TOURS, "une std::function par tour, une allocation");
    VERIFIE_ENTIER(copies_effacees, 2 * TOURS, "et deux copies du foncteur par tour");

    VERIFIE_ENTIER(allocations_du_balayage, 0,
                   "un parametre gabarit n'efface rien, donc n'alloue rien");
    VERIFIE_ENTIER(copies_du_balayage, 0, "et ne copie pas le foncteur d'un seul tour");
    return BILAN();
}
