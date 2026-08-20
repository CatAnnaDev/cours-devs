#include <cstddef>
#include <memory>
#include <utility>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

constexpr std::size_t NOMBRE_DE_MESURES = 64;
constexpr int TOURS = 1000;

struct Mesures {
    std::vector<int> valeurs;
};

struct Poignee {
    std::shared_ptr<Mesures> partagee;

    static inline long long incrementations = 0;

    explicit Poignee(std::shared_ptr<Mesures> source) : partagee(std::move(source)) {}

    Poignee(const Poignee &autre) : partagee(autre.partagee) { incrementations++; }

    Poignee &operator=(const Poignee &autre) {
        partagee = autre.partagee;
        incrementations++;
        return *this;
    }

    Poignee(Poignee &&autre) noexcept = default;
    Poignee &operator=(Poignee &&autre) noexcept = default;
    ~Poignee() = default;
};

Poignee construire() {
    auto mesures = std::make_shared<Mesures>();
    mesures->valeurs.reserve(NOMBRE_DE_MESURES);
    for (std::size_t rang = 0; rang < NOMBRE_DE_MESURES; rang++) {
        mesures->valeurs.push_back((int)(rang * 3));
    }
    return Poignee{std::move(mesures)};
}

long long somme(const Poignee &poignee) {
    long long total = 0;
    for (const int valeur : poignee.partagee->valeurs) {
        total += valeur;
    }
    return total;
}

int maximum(const Mesures &mesures) {
    int plus_grand = mesures.valeurs.front();
    for (const int valeur : mesures.valeurs) {
        if (valeur > plus_grand) {
            plus_grand = valeur;
        }
    }
    return plus_grand;
}

std::size_t combien(const Poignee &poignee) { return poignee.partagee->valeurs.size(); }

}

int main() {
    const Poignee poignee = construire();

    VERIFIE_ENTIER(sizeof(std::unique_ptr<Mesures>), sizeof(Mesures *),
                   "un unique_ptr ne pese pas plus qu'un pointeur nu");
    VERIFIE_ENTIER(sizeof(std::shared_ptr<Mesures>), 2 * sizeof(std::unique_ptr<Mesures>),
                   "un shared_ptr en pese deux : l'objet, et le bloc de controle");
    VERIFIE_ENTIER(sizeof(std::shared_ptr<Mesures>), 16, "seize octets contre huit");

    Poignee::incrementations = 0;
    {
        const Poignee doublon = poignee;
        VERIFIE_ENTIER(Poignee::incrementations, 1, "copier une poignee touche le compteur");
        VERIFIE_ENTIER(poignee.partagee.use_count(), 2, "deux proprietaires, le temps du bloc");
        VERIFIE_ENTIER(doublon.partagee->valeurs.size(), NOMBRE_DE_MESURES,
                       "le doublon designe bien les memes mesures");
    }
    VERIFIE_ENTIER(poignee.partagee.use_count(), 1, "et un seul de nouveau apres le bloc");

    Poignee::incrementations = 0;
    long long total = 0;
    int plus_grand = 0;
    std::size_t elements = 0;
    for (int tour = 0; tour < TOURS; tour++) {
        total += somme(poignee);
        plus_grand = maximum(*poignee.partagee);
        elements = combien(poignee);
    }
    const long long incrementations_de_la_boucle = Poignee::incrementations;

    VERIFIE_ENTIER(total, (long long)TOURS * 6048, "mille tours sur la meme somme");
    VERIFIE_ENTIER(plus_grand, 189, "soixante-trois fois trois");
    VERIFIE_ENTIER(elements, NOMBRE_DE_MESURES, "soixante-quatre mesures");
    VERIFIE_ENTIER(poignee.partagee.use_count(), 1,
                   "personne n'a pris de part de propriete dans la boucle");
    VERIFIE_ENTIER(incrementations_de_la_boucle, 0,
                   "trois mille incrementations atomiques evitees par trois esperluettes");
    return BILAN();
}
