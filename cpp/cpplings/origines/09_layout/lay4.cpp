#include <cstring>
#include <string>
#include <type_traits>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

struct Point3 {
    float x;
    float y;
    float z;
};

template <typename T>
T copier_octets(const T &source) {
    T destination;
    std::memcpy(&destination, &source, sizeof(T));
    return destination;
}

}

int main() {
    const Point3 origine{1.5f, 2.5f, 3.5f};
    const Point3 copie = copier_octets(origine);

    VERIFIE(std::is_trivially_copyable_v<Point3>, "Point3 est trivialement copiable");
    VERIFIE_ENTIER(sizeof(Point3), 12, "trois float colles, douze octets");
    VERIFIE_REEL(copie.x, 1.5, "memcpy a bien recopie x");
    VERIFIE_REEL(copie.y, 2.5, "memcpy a bien recopie y");
    VERIFIE_REEL(copie.z, 3.5, "memcpy a bien recopie z");

    const std::string etiquette =
        "une etiquette bien trop longue pour tenir dans la petite optimisation de chaine";
    const std::string doublon = copier_octets(etiquette);

    VERIFIE(!std::is_trivially_copyable_v<std::string>, "std::string ne l'est pas, lui");
    VERIFIE_TEXTE(doublon, etiquette, "la chaine est copiee, pas dupliquee octet par octet");
    return BILAN();
}
