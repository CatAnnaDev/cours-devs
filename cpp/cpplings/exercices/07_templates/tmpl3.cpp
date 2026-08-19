#include <cstddef>
#include <string>
#include <type_traits>

#include "verif.hpp"

const bool PAS_FINI = true;

template <typename T>
std::size_t longueur(const T &valeur) {
    if (std::is_integral_v<T>) {
        return sizeof(T);
    } else {
        return valeur.size();
    }
}

int main() {
    VERIFIE_ENTIER(longueur(42), 4, "pour un entier, c'est sa taille en octets");
    VERIFIE_ENTIER(longueur(std::string("coucou")), 6,
                   "pour une chaine, c'est son nombre de caracteres");
    return BILAN();
}
