#include <cstddef>
#include <new>
#include <utility>

#include "verif.hpp"

const bool PAS_FINI = false;

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
            new (nouveau + i) T(std::move_if_noexcept(stockage_[i]));
        }
        detruire_les_elements();
        ::operator delete(stockage_);
        stockage_ = nouveau;
        capacite_ = capacite_voulue;
    }

    template <typename... Args>
    T &emplace_back(Args &&...arguments) {
        if (taille_ == capacite_) {
            reserve(capacite_ == 0 ? 1 : capacite_ * 2);
        }
        T *place = new (stockage_ + taille_) T(std::forward<Args>(arguments)...);
        taille_++;
        return *place;
    }

    std::size_t taille() const { return taille_; }
    std::size_t capacite() const { return capacite_; }

    T &operator[](std::size_t indice) { return stockage_[indice]; }
    const T &operator[](std::size_t indice) const { return stockage_[indice]; }

private:
    void detruire_les_elements() {
        for (std::size_t i = taille_; i > 0; i--) {
            stockage_[i - 1].~T();
        }
    }

    T *stockage_ = nullptr;
    std::size_t taille_ = 0;
    std::size_t capacite_ = 0;
};

long long somme_des_valeurs(const Vecteur<verif::Sonde> &vecteur) {
    long long somme = 0;
    for (std::size_t i = 0; i < vecteur.taille(); i++) {
        somme += vecteur[i].valeur;
    }
    return somme;
}

int main() {
    Vecteur<verif::Sonde> vecteur;
    vecteur.reserve(4);
    vecteur.emplace_back(1);
    vecteur.emplace_back(2);
    vecteur.emplace_back(3);

    VERIFIE_ENTIER(vecteur.taille(), 3, "trois elements construits");
    VERIFIE_ENTIER(vecteur.capacite(), 4, "quatre places disponibles");
    VERIFIE_ENTIER(somme_des_valeurs(vecteur), 6,
                   "le parcours ne visite que les elements construits");

    vecteur.emplace_back(4);

    VERIFIE_ENTIER(vecteur.taille(), 4, "le vecteur est plein");
    VERIFIE_ENTIER(vecteur.capacite(), 4, "sans avoir eu besoin de grandir");
    VERIFIE_ENTIER(somme_des_valeurs(vecteur), 10, "taille et capacite coincident enfin");

    const verif::Sonde *avant_l_ajout = &vecteur[0];
    vecteur.emplace_back(5);

    VERIFIE_ENTIER(vecteur.capacite(), 8, "l'ajout de trop a fait doubler le tampon");
    VERIFIE(avant_l_ajout != &vecteur[0],
            "le stockage a demenage : l'ancienne adresse ne designe plus rien de vivant");
    VERIFIE_ENTIER(vecteur[0].valeur, 1, "repasser par l'indice donne le bon element");

    VERIFIE(vecteur.capacite() > vecteur.taille(), "il reste de la place libre");

    const verif::Sonde *place_reservee = &vecteur[0];
    vecteur.emplace_back(6);

    VERIFIE(place_reservee == &vecteur[0],
            "tant qu'on ne depasse pas la capacite, le stockage ne demenage pas");
    VERIFIE_ENTIER(somme_des_valeurs(vecteur), 21, "les six valeurs sont la");
    return BILAN();
}
