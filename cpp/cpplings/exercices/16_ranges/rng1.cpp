#include <algorithm>
#include <initializer_list>
#include <iterator>
#include <ranges>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

class Sondage {
  public:
    Sondage(std::initializer_list<int> profondeurs) {
        for (int profondeur : profondeurs) {
            if (nombre_ < CAPACITE) {
                valeurs_[nombre_] = profondeur;
                nombre_++;
            }
        }
    }

    int *begin() { return valeurs_; }
    int *end() { return valeurs_ + nombre_; }

  private:
    static constexpr int CAPACITE = 8;

    int valeurs_[CAPACITE] = {};
    int nombre_ = 0;
};

struct FinDuTexte {};

bool operator==(const char *position, FinDuTexte) { return *position == '\0'; }

class Mot {
  public:
    explicit Mot(const char *texte) : texte_(texte) {}

    const char *begin() const { return texte_; }
    FinDuTexte end() const { return {}; }

  private:
    const char *texte_;
};

long long somme(const Sondage &sondage) {
    long long total = 0;
    for (int profondeur : sondage) {
        total += profondeur;
    }
    return total;
}

}

static_assert(std::ranges::range<Sondage>);
static_assert(std::ranges::range<const Sondage>);
static_assert(std::ranges::contiguous_range<Sondage>);
static_assert(std::ranges::sized_range<Sondage>);
static_assert(std::ranges::range<Mot>);
static_assert(std::ranges::input_range<Mot>);
static_assert(!std::ranges::sized_range<Mot>);
static_assert(!std::ranges::common_range<Mot>);

int main() {
    Sondage sondage{40, 12, 75, 12, 3, 61};

    VERIFIE_ENTIER(somme(sondage), 203, "un for parcourt le type par begin et end");
    VERIFIE_ENTIER(std::ranges::size(sondage), 6, "ranges::size deduit la taille de end - begin");

    VERIFIE_ENTIER(std::ranges::count(sondage, 12), 2, "ranges::count prend l'objet entier");
    VERIFIE_ENTIER(*std::ranges::max_element(sondage), 75, "et ranges::max_element aussi");
    VERIFIE(std::ranges::find(sondage, 61) != sondage.end(), "ranges::find rend un iterateur");
    VERIFIE(std::ranges::find(sondage, 99) == sondage.end(), "et end() quand rien ne correspond");

    std::ranges::sort(sondage);
    VERIFIE_ENTIER(*sondage.begin(), 3, "un range aleatoire mutable se trie sur place");
    VERIFIE(std::ranges::is_sorted(sondage), "et le resultat est trie");

    const Sondage &lecture_seule = sondage;
    VERIFIE_ENTIER(std::ranges::count_if(lecture_seule, [](int p) { return p > 20; }), 3,
                   "les surcharges const rendent l'objet const parcourable");

    Mot mot{"basalte"};
    VERIFIE_ENTIER(std::ranges::distance(mot), 7, "end() peut etre une sentinelle d'un autre type");
    VERIFIE_ENTIER(std::ranges::count(mot, 'a'), 2,
                   "un range est un debut et une condition d'arret");
    VERIFIE(std::ranges::find(mot, 'z') == FinDuTexte{}, "la sentinelle se compare a l'iterateur");

    return BILAN();
}
