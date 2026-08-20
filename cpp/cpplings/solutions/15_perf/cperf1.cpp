#include <cstddef>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

constexpr std::size_t NOMBRE_DE_MESURES = 100;

struct Trace {
    int deplacements;
    int copies;
    int constructions;
    std::size_t taille;
    std::size_t capacite;
    int derniere_valeur;
};

Trace relever(const std::vector<verif::Sonde> &mesures) {
    return Trace{verif::Compteur::deplacements, verif::Compteur::copies,
                 verif::Compteur::constructions, mesures.size(), mesures.capacity(),
                 mesures.back().valeur};
}

Trace remplir_naivement() {
    verif::Compteur::remettre_a_zero();
    std::vector<verif::Sonde> mesures;
    for (std::size_t rang = 0; rang < NOMBRE_DE_MESURES; rang++) {
        mesures.emplace_back((int)rang);
    }
    return relever(mesures);
}

Trace remplir_en_reservant() {
    verif::Compteur::remettre_a_zero();
    std::vector<verif::Sonde> mesures;
    mesures.reserve(NOMBRE_DE_MESURES);
    for (std::size_t rang = 0; rang < NOMBRE_DE_MESURES; rang++) {
        mesures.emplace_back((int)rang);
    }
    return relever(mesures);
}

}

int main() {
    const Trace naif = remplir_naivement();
    const Trace reserve = remplir_en_reservant();

    VERIFIE_ENTIER(naif.taille, NOMBRE_DE_MESURES, "cent mesures rangees sans reserve");
    VERIFIE_ENTIER(reserve.taille, NOMBRE_DE_MESURES, "cent mesures rangees avec reserve");
    VERIFIE_ENTIER(naif.derniere_valeur, 99, "la derniere mesure vaut quatre-vingt-dix-neuf");
    VERIFIE_ENTIER(reserve.derniere_valeur, 99, "des deux cotes, le contenu est le meme");
    VERIFIE_ENTIER(naif.constructions, NOMBRE_DE_MESURES, "cent emplace_back, cent constructions");
    VERIFIE_ENTIER(reserve.constructions, NOMBRE_DE_MESURES, "cent aussi avec reserve");
    VERIFIE_ENTIER(naif.copies, 0, "une reallocation deplace, elle ne copie pas");
    VERIFIE_ENTIER(reserve.copies, 0, "et reserver n'y change rien");

    VERIFIE_ENTIER(naif.capacite, 128, "la capacite double jusqu'a depasser cent");
    VERIFIE_ENTIER(naif.deplacements, 127,
                   "un plus deux plus quatre jusqu'a soixante-quatre : sept reallocations");
    VERIFIE_ENTIER(reserve.capacite, NOMBRE_DE_MESURES, "reserve demande la taille exacte");
    VERIFIE_ENTIER(reserve.deplacements, 0,
                   "plus une seule reallocation, plus un seul deplacement");
    VERIFIE_ENTIER(naif.deplacements - reserve.deplacements, 127,
                   "cent vingt-sept objets remues pour cent ranges, faute d'un appel");
    return BILAN();
}
