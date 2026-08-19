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

    ~Vecteur() { delete[] stockage_; }

    void reserve(std::size_t capacite_voulue) {
        if (capacite_voulue <= capacite_) {
            return;
        }
        T *nouveau = new T[capacite_voulue];
        for (std::size_t i = 0; i < taille_; i++) {
            nouveau[i] = std::move(stockage_[i]);
        }
        delete[] stockage_;
        stockage_ = nouveau;
        capacite_ = capacite_voulue;
    }

    void push_back(const T &valeur) {
        faire_de_la_place();
        stockage_[taille_] = valeur;
        taille_++;
    }

    void push_back(T &&valeur) {
        faire_de_la_place();
        stockage_[taille_] = std::move(valeur);
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
        vecteur.reserve(10);

        VERIFIE_ENTIER(vecteur.capacite(), 10, "dix places sont reservees");
        VERIFIE_ENTIER(vecteur.taille(), 0, "mais le vecteur est encore vide");
        VERIFIE_ENTIER(verif::Compteur::constructions, 0,
                       "reserver de la place ne construit aucun element");
        VERIFIE_ENTIER(verif::Compteur::destructions, 0, "et n'en detruit aucun");

        vecteur.push_back(verif::Sonde(1));
        vecteur.push_back(verif::Sonde(2));
        vecteur.push_back(verif::Sonde(3));

        VERIFIE_ENTIER(vecteur.taille(), 3, "trois elements ajoutes");
        VERIFIE_ENTIER(vecteur.capacite(), 10, "la capacite n'a pas bouge");
        VERIFIE_ENTIER(objets_vivants(), 3, "exactement trois objets vivants");
        VERIFIE_ENTIER(vecteur[0].valeur, 1, "le premier element est le bon");
        VERIFIE_ENTIER(vecteur[2].valeur, 3, "le dernier element aussi");
    }

    VERIFIE_ENTIER(objets_vivants(), 0, "le destructeur n'a rien laisse derriere lui");
    VERIFIE_ENTIER(verif::Compteur::copies, 0, "aucune copie du debut a la fin");
    VERIFIE_ENTIER(verif::Compteur::destructions,
                   verif::Compteur::constructions + verif::Compteur::deplacements,
                   "autant de destructions que d'objets nes");
    return BILAN();
}
