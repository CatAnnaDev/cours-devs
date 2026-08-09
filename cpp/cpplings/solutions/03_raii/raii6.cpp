#include <string>

#include "verif.hpp"

const bool PAS_FINI = false;

struct Trace {
    std::string *journal;
    char nom;

    Trace(std::string *cible, char lettre) : journal(cible), nom(lettre) {}
    ~Trace() { journal->push_back(nom); }
};

int main() {
    std::string journal;

    {
        Trace a(&journal, 'a');
        Trace b(&journal, 'b');
        Trace c(&journal, 'c');
    }

    VERIFIE_TEXTE(journal, "cba", "detruits dans l'ordre inverse");
    return BILAN();
}
