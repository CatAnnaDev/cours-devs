#include <map>
#include <string>
#include <unordered_map>

#include "verif.hpp"

const bool PAS_FINI = false;

int main() {
    std::map<std::string, int> trie = {{"c", 3}, {"a", 1}, {"b", 2}};
    std::unordered_map<std::string, int> rapide = {{"c", 3}, {"a", 1}, {"b", 2}};

    std::string ordre;
    for (const auto &[cle, valeur] : trie) {
        ordre += cle;
    }

    VERIFIE_TEXTE(ordre, "abc", "map parcourt dans l'ordre des cles");
    VERIFIE_ENTIER(rapide.at("b"), 2, "unordered_map retrouve aussi bien");
    VERIFIE_ENTIER(rapide.count("z"), 0, "et repond bien sur une cle absente");
    return BILAN();
}
