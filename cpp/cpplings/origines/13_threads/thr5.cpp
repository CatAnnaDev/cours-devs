#include <future>
#include <stdexcept>
#include <string>
#include <thread>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

int inventorier_le_coffre(int rang) {
    if (rang < 0) {
        throw std::out_of_range("rang negatif");
    }
    return rang * 2;
}

}

int main() {
    int pieces_comptees = 0;
    std::string plainte;

    try {
        std::thread porteuse([&pieces_comptees] { pieces_comptees = inventorier_le_coffre(4); });
        porteuse.join();
    } catch (const std::out_of_range &) {
        plainte = "le coffre de rang quatre ne devait pas se plaindre";
    }
    VERIFIE_ENTIER(pieces_comptees, 8, "le coffre de rang quatre contient huit pieces");

    try {
        std::thread porteuse([&pieces_comptees] { pieces_comptees = inventorier_le_coffre(-1); });
        porteuse.join();
    } catch (const std::out_of_range &refus) {
        plainte = refus.what();
    }
    VERIFIE_TEXTE(plainte, "rang negatif", "l'exception levee dans le fil est revenue jusqu'ici");
    VERIFIE_ENTIER(pieces_comptees, 8,
                   "et le compte tenu ici n'a pas ete touche par le coffre refuse");
    return BILAN();
}
