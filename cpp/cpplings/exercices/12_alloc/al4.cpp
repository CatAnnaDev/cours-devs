#include <cstddef>
#include <cstdlib>
#include <new>
#include <utility>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace suivi {

struct Tas {
    static inline std::size_t allocations = 0;
    static inline std::size_t liberations = 0;
    static inline std::size_t octets = 0;

    static void remettre_a_zero() {
        allocations = 0;
        liberations = 0;
        octets = 0;
    }
};

}

void *operator new(std::size_t octets) {
    void *bloc = std::malloc(octets != 0 ? octets : 1);
    if (bloc == nullptr) {
        throw std::bad_alloc();
    }
    suivi::Tas::allocations++;
    suivi::Tas::octets += octets;
    return bloc;
}

void *operator new[](std::size_t octets) { return ::operator new(octets); }

void operator delete(void *bloc) noexcept {
    if (bloc != nullptr) {
        suivi::Tas::liberations++;
        std::free(bloc);
    }
}

void operator delete[](void *bloc) noexcept { ::operator delete(bloc); }

void operator delete(void *bloc, std::size_t) noexcept { ::operator delete(bloc); }

void operator delete[](void *bloc, std::size_t) noexcept { ::operator delete(bloc); }

namespace {

template <typename T, std::size_t CAPACITE_INTERNE>
class PetitVecteur {
  public:
    PetitVecteur() : donnees_(nullptr), taille_(0), capacite_(0) {}

    PetitVecteur(const PetitVecteur &) = delete;
    PetitVecteur &operator=(const PetitVecteur &) = delete;

    ~PetitVecteur() {
        detruire_les_elements();
        liberer_si_sur_le_tas();
    }

    void ajouter(const T &valeur) {
        reserver_une_place_de_plus();
        new (donnees_ + taille_) T(valeur);
        taille_++;
    }

    void ajouter(T &&valeur) {
        reserver_une_place_de_plus();
        new (donnees_ + taille_) T(std::move(valeur));
        taille_++;
    }

    const T &operator[](std::size_t indice) const { return donnees_[indice]; }

    std::size_t taille() const { return taille_; }
    std::size_t capacite() const { return capacite_; }
    bool sur_le_tas() const { return donnees_ != interne(); }

  private:
    T *interne() { return reinterpret_cast<T *>(tampon_interne_); }
    const T *interne() const { return reinterpret_cast<const T *>(tampon_interne_); }

    void reserver_une_place_de_plus() {
        if (taille_ < capacite_) {
            return;
        }
        const std::size_t nouvelle_capacite = capacite_ != 0 ? capacite_ * 2 : 1;
        T *nouveau = static_cast<T *>(::operator new(nouvelle_capacite * sizeof(T)));
        for (std::size_t indice = 0; indice < taille_; indice++) {
            new (nouveau + indice) T(std::move(donnees_[indice]));
            donnees_[indice].~T();
        }
        liberer_si_sur_le_tas();
        donnees_ = nouveau;
        capacite_ = nouvelle_capacite;
    }

    void detruire_les_elements() {
        for (std::size_t restants = taille_; restants > 0; restants--) {
            donnees_[restants - 1].~T();
        }
        taille_ = 0;
    }

    void liberer_si_sur_le_tas() {
        if (sur_le_tas()) {
            ::operator delete(donnees_, capacite_ * sizeof(T));
        }
    }

    T *donnees_;
    std::size_t taille_;
    std::size_t capacite_;
    alignas(T) std::byte tampon_interne_[CAPACITE_INTERNE * sizeof(T)];
};

}

int main() {
    VERIFIE_ENTIER(sizeof(std::vector<int>), 24, "un std::vector ne pese que trois pointeurs");
    VERIFIE_ENTIER(sizeof(PetitVecteur<int, 8>), 56,
                   "huit entiers loges dans l'objet, c'est trente-deux octets de plus");

    suivi::Tas::remettre_a_zero();
    {
        PetitVecteur<int, 8> petit;
        for (int valeur = 0; valeur < 8; valeur++) {
            petit.ajouter(valeur * 3);
        }

        VERIFIE_ENTIER(petit.taille(), 8, "huit elements ranges");
        VERIFIE_ENTIER(petit[7], 21, "le dernier vaut vingt et un");
        VERIFIE(!petit.sur_le_tas(), "sous le seuil, tout reste dans l'objet");
        VERIFIE_ENTIER(suivi::Tas::allocations, 0, "et le tas n'a jamais ete sollicite");

        petit.ajouter(24);

        VERIFIE_ENTIER(suivi::Tas::allocations, 1, "le neuvieme coute une allocation, une seule");
        VERIFIE(petit.sur_le_tas(), "au-dela du seuil, le contenu demenage sur le tas");
        VERIFIE_ENTIER(petit.capacite(), 16, "la capacite a double");

        bool contenu_intact = true;
        for (std::size_t indice = 0; indice < petit.taille(); indice++) {
            if (petit[indice] != static_cast<int>(indice) * 3) {
                contenu_intact = false;
            }
        }
        VERIFIE(contenu_intact, "les huit premiers ont suivi le demenagement");
    }
    VERIFIE_ENTIER(suivi::Tas::liberations, 1, "le bloc du tas est rendu a la destruction");

    verif::Compteur::remettre_a_zero();
    suivi::Tas::remettre_a_zero();
    {
        PetitVecteur<verif::Sonde, 4> sondes;
        for (int valeur = 0; valeur < 4; valeur++) {
            sondes.ajouter(verif::Sonde(valeur));
        }

        VERIFIE_ENTIER(verif::Compteur::deplacements, 4, "quatre ajouts, quatre deplacements");
        VERIFIE_ENTIER(suivi::Tas::allocations, 0, "toujours rien sur le tas");

        sondes.ajouter(verif::Sonde(4));

        VERIFIE_ENTIER(suivi::Tas::allocations, 1, "la bascule coute une allocation");
        VERIFIE_ENTIER(verif::Compteur::deplacements, 9,
                       "les quatre anciens deplaces, plus le nouvel arrivant");
        VERIFIE_ENTIER(verif::Compteur::copies, 0, "la bascule deplace, elle ne copie jamais");
    }
    return BILAN();
}
