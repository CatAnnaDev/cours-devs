#include <algorithm>
#include <cstddef>
#include <cstdlib>
#include <iterator>
#include <new>
#include <ranges>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

long long allocations = 0;
long long octets_alloues = 0;

void remettre_les_compteurs_a_zero() {
    allocations = 0;
    octets_alloues = 0;
}

bool paire(int valeur) { return valeur % 2 == 0; }

int fois_dix(int valeur) { return valeur * 10; }

long long somme_paresseuse(const std::vector<int> &source) {
    long long total = 0;
    for (int valeur : source | std::views::filter(paire) | std::views::transform(fois_dix) |
                          std::views::take(4)) {
        total += valeur;
    }
    return total;
}

long long somme_materialisee(const std::vector<int> &source) {
    std::vector<int> retenus;
    retenus.reserve(source.size());
    std::ranges::copy_if(source, std::back_inserter(retenus), paire);

    std::vector<int> transformes;
    transformes.reserve(retenus.size());
    std::ranges::transform(retenus, std::back_inserter(transformes), fois_dix);

    std::vector<int> tete;
    tete.reserve(4);
    std::ranges::copy_n(transformes.begin(), 4, std::back_inserter(tete));

    long long total = 0;
    for (int valeur : tete) {
        total += valeur;
    }
    return total;
}

}

void *operator new(std::size_t taille) {
    allocations++;
    octets_alloues += static_cast<long long>(taille);
    void *bloc = std::malloc(taille == 0 ? 1 : taille);
    if (bloc == nullptr) {
        throw std::bad_alloc{};
    }
    return bloc;
}

void *operator new[](std::size_t taille) { return ::operator new(taille); }

void operator delete(void *bloc) noexcept { std::free(bloc); }

void operator delete[](void *bloc) noexcept { std::free(bloc); }

void operator delete(void *bloc, std::size_t) noexcept { std::free(bloc); }

void operator delete[](void *bloc, std::size_t) noexcept { std::free(bloc); }

int main() {
    const std::vector<int> source{1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16};

    remettre_les_compteurs_a_zero();
    const long long resultat_paresseux = somme_paresseuse(source);
    const long long allocations_paresseuses = allocations;
    const long long octets_paresseux = octets_alloues;

    remettre_les_compteurs_a_zero();
    const long long resultat_materialise = somme_materialisee(source);
    const long long allocations_materialisees = allocations;
    const long long octets_materialises = octets_alloues;

    VERIFIE_ENTIER(resultat_paresseux, 200, "20 + 40 + 60 + 80");
    VERIFIE_ENTIER(resultat_materialise, resultat_paresseux,
                   "les deux versions donnent la meme chose");

    VERIFIE_ENTIER(allocations_paresseuses, 0, "trois adaptateurs composes n'allouent rien");
    VERIFIE_ENTIER(octets_paresseux, 0, "et ne recopient pas un seul element");

    VERIFIE_ENTIER(allocations_materialisees, 3, "un vecteur intermediaire par etape");
    VERIFIE_ENTIER(octets_materialises, 112, "64 + 32 + 16 octets recopies pour rien");

    auto chaine = source | std::views::filter(paire) | std::views::transform(fois_dix) |
                  std::views::take(4);
    VERIFIE(sizeof(chaine) < 3 * sizeof(std::vector<int>),
            "la chaine entiere tient dans moins de place que trois vecteurs vides");

    remettre_les_compteurs_a_zero();
    long long relecture = 0;
    for (int valeur : chaine) {
        relecture += valeur;
    }
    for (int valeur : chaine) {
        relecture += valeur;
    }
    VERIFIE_ENTIER(relecture, 400, "deux parcours de la meme chaine");
    VERIFIE_ENTIER(allocations, 0, "et toujours aucune allocation");

    return BILAN();
}
