#include <string>

#include "verif.hpp"

const bool PAS_FINI = true;

bool est_dans_l_objet(const std::string &texte) {
    const char *debut = reinterpret_cast<const char *>(&texte);
    return texte.data() >= debut && texte.data() < debut + sizeof(std::string);
}

int main() {
    std::string courte = "abc";
    std::string longue(200, 'x');

    VERIFIE(!est_dans_l_objet(courte), "une chaine courte ne va pas sur le tas");
    VERIFIE(!est_dans_l_objet(longue), "une chaine longue, si");
    VERIFIE_ENTIER(courte.size(), 3, "trois caracteres");
    VERIFIE_ENTIER(longue.size(), 200, "deux cents caracteres");
    return BILAN();
}
