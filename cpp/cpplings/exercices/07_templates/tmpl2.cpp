#include <cstddef>

#include "verif.hpp"

const bool PAS_FINI = true;

struct Pile {
    int elements[4] = {};
    std::size_t hauteur = 0;

    bool empiler(const int &valeur) {
        if (hauteur == 4) {
            return false;
        }
        elements[hauteur] = valeur;
        hauteur++;
        return true;
    }

    int depiler() {
        hauteur--;
        return elements[hauteur];
    }
};

int main() {
    Pile<int, 2> entiers;

    VERIFIE(entiers.empiler(1), "le premier empilement passe");
    VERIFIE(entiers.empiler(2), "le second aussi");
    VERIFIE(!entiers.empiler(3), "la pile est pleine, le troisieme est refuse");
    VERIFIE_ENTIER(entiers.depiler(), 2, "le depilement rend le dernier empile");

    Pile<double, 3> reels;

    VERIFIE(reels.empiler(1.5), "la meme pile marche pour des reels");
    VERIFIE_REEL(reels.depiler(), 1.5, "et elle rend bien un reel");

    VERIFIE_ENTIER(sizeof(Pile<int, 2>), 16, "deux int et la hauteur, rien de plus");
    return BILAN();
}
