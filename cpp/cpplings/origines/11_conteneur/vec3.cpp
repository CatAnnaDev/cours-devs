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
        detruire_les_elements();
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

    template <typename... Args>
    T &emplace_back(Args &&...arguments) {
        push_back(T(std::forward<Args>(arguments)...));
        return stockage_[taille_ - 1];
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
    {
        Vecteur<verif::Sonde> vecteur;
        vecteur.reserve(8);

        verif::Compteur::remettre_a_zero();
        vecteur.emplace_back(1);

        VERIFIE_ENTIER(verif::Compteur::constructions, 1,
                       "emplace_back construit l'element une seule fois");
        VERIFIE_ENTIER(verif::Compteur::copies, 0, "emplace_back ne copie rien");
        VERIFIE_ENTIER(verif::Compteur::deplacements, 0, "et ne deplace rien non plus");

        verif::Sonde source(2);
        verif::Compteur::remettre_a_zero();
        vecteur.push_back(source);

        VERIFIE_ENTIER(verif::Compteur::copies, 1, "push_back d'une lvalue fait une copie");
        VERIFIE_ENTIER(verif::Compteur::deplacements, 0, "et rien de plus");

        verif::Compteur::remettre_a_zero();
        vecteur.push_back(verif::Sonde(3));

        VERIFIE_ENTIER(verif::Compteur::deplacements, 1,
                       "push_back d'une rvalue fait un deplacement");
        VERIFIE_ENTIER(verif::Compteur::copies, 0, "et aucune copie");

        VERIFIE_ENTIER(vecteur.taille(), 3, "trois elements en tout");
        VERIFIE_ENTIER(vecteur[0].valeur, 1, "celui construit sur place");
        VERIFIE_ENTIER(vecteur[1].valeur, 2, "celui copie depuis une lvalue");
        VERIFIE_ENTIER(vecteur[2].valeur, 3, "celui deplace depuis une rvalue");
    }

    verif::Compteur::remettre_a_zero();

    {
        Vecteur<verif::Sonde> vecteur;
        for (int i = 0; i < 5; i++) {
            vecteur.emplace_back(i);
        }

        VERIFIE_ENTIER(vecteur.taille(), 5, "cinq elements construits sur place");
        VERIFIE_ENTIER(vecteur[4].valeur, 4, "le dernier porte la bonne valeur");
    }

    VERIFIE_ENTIER(objets_vivants(), 0, "rien ne survit au vecteur");
    return BILAN();
}
