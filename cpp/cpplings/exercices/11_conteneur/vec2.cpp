#include <cstddef>
#include <new>
#include <utility>

#include "verif.hpp"

const bool PAS_FINI = true;

template <typename T>
class Vecteur {
public:
    Vecteur() = default;

    Vecteur(const Vecteur &) = delete;
    Vecteur &operator=(const Vecteur &) = delete;

    ~Vecteur() {
        detruire_les_elements();
        ::operator delete(stockage_);
    }

    void reserve(std::size_t capacite_voulue) {
        if (capacite_voulue <= capacite_) {
            return;
        }
        T *nouveau = static_cast<T *>(::operator new(capacite_voulue * sizeof(T)));
        for (std::size_t i = 0; i < taille_; i++) {
            new (nouveau + i) T(std::move(stockage_[i]));
        }
        ::operator delete(stockage_);
        stockage_ = nouveau;
        capacite_ = capacite_voulue;
    }

    void push_back(const T &valeur) {
        faire_de_la_place();
        new (stockage_ + taille_) T(valeur);
        taille_++;
    }

    void push_back(T &&valeur) {
        faire_de_la_place();
        new (stockage_ + taille_) T(std::move(valeur));
        taille_++;
    }

    std::size_t taille() const { return taille_; }
    std::size_t capacite() const { return capacite_; }

    T &operator[](std::size_t indice) { return stockage_[indice]; }
    const T &operator[](std::size_t indice) const { return stockage_[indice]; }

private:
    void faire_de_la_place() {
        if (taille_ == capacite_) {
            reserve(capacite_ == 0 ? 1 : capacite_ * 2);
        }
    }

    void detruire_les_elements() {
        for (std::size_t i = taille_; i > 0; i--) {
            stockage_[i - 1].~T();
        }
    }

    T *stockage_ = nullptr;
    std::size_t taille_ = 0;
    std::size_t capacite_ = 0;
};

int objets_vivants() {
    return verif::Compteur::constructions + verif::Compteur::copies +
           verif::Compteur::deplacements - verif::Compteur::destructions;
}

int main() {
    verif::Compteur::remettre_a_zero();

    {
        Vecteur<verif::Sonde> vecteur;

        VERIFIE_ENTIER(vecteur.capacite(), 0, "un vecteur neuf ne possede aucune place");

        for (int i = 1; i <= 5; i++) {
            vecteur.push_back(verif::Sonde(i));
        }

        VERIFIE_ENTIER(vecteur.taille(), 5, "cinq elements");
        VERIFIE_ENTIER(vecteur.capacite(), 8, "la capacite a double jusqu'a huit");
        VERIFIE_ENTIER(verif::Compteur::copies, 0, "la croissance deplace, elle ne copie pas");
        VERIFIE_ENTIER(vecteur[0].valeur, 1, "le premier element a survecu aux demenagements");
        VERIFIE_ENTIER(vecteur[4].valeur, 5, "le dernier aussi");
        VERIFIE_ENTIER(objets_vivants(), 5, "cinq objets vivants, pas un de plus");
    }

    VERIFIE_ENTIER(objets_vivants(), 0, "chaque objet ne a fini par etre detruit");
    VERIFIE_ENTIER(verif::Compteur::destructions,
                   verif::Compteur::constructions + verif::Compteur::deplacements,
                   "constructions plus deplacements egale destructions");
    return BILAN();
}
