#include <cstddef>
#include <new>
#include <stdexcept>
#include <utility>

#include "verif.hpp"

const bool PAS_FINI = true;

struct Capricieux {
    static inline int vivants = 0;
    static inline int copies_faites = 0;
    static inline int copies_avant_incident = -1;

    int valeur = 0;

    explicit Capricieux(int v) : valeur(v) { vivants++; }

    Capricieux(const Capricieux &autre) : valeur(autre.valeur) {
        if (copies_faites == copies_avant_incident) {
            throw std::runtime_error("cette copie refuse de se faire");
        }
        copies_faites++;
        vivants++;
    }

    ~Capricieux() { vivants--; }
};

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
        T *ancien = stockage_;
        const std::size_t ancienne_taille = taille_;
        stockage_ = static_cast<T *>(::operator new(capacite_voulue * sizeof(T)));
        capacite_ = capacite_voulue;
        taille_ = 0;
        for (std::size_t i = 0; i < ancienne_taille; i++) {
            new (stockage_ + i) T(std::move_if_noexcept(ancien[i]));
            taille_++;
            ancien[i].~T();
        }
        ::operator delete(ancien);
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

int main() {
    {
        Vecteur<Capricieux> vecteur;
        vecteur.reserve(4);
        vecteur.emplace_back(10);
        vecteur.emplace_back(20);
        vecteur.emplace_back(30);
        vecteur.emplace_back(40);

        Capricieux::copies_faites = 0;
        Capricieux::copies_avant_incident = 2;

        bool incident_attrape = false;
        try {
            vecteur.emplace_back(50);
        } catch (const std::runtime_error &) {
            incident_attrape = true;
        }

        Capricieux::copies_avant_incident = -1;

        VERIFIE(incident_attrape, "la troisieme copie a bien leve pendant la croissance");
        VERIFIE_ENTIER(Capricieux::copies_faites, 2, "deux copies avaient deja eu lieu");
        VERIFIE_ENTIER(vecteur.taille(), 4, "la taille n'a pas bouge");
        VERIFIE_ENTIER(vecteur.capacite(), 4, "la capacite non plus");

        long long somme = 0;
        for (std::size_t i = 0; i < vecteur.taille(); i++) {
            somme += vecteur[i].valeur;
        }

        VERIFIE_ENTIER(somme, 100, "les quatre valeurs d'origine sont toutes intactes");
        VERIFIE_ENTIER(vecteur[0].valeur, 10, "le premier element est lisible");
        VERIFIE_ENTIER(vecteur[1].valeur, 20, "le deuxieme aussi");
        VERIFIE_ENTIER(Capricieux::vivants, 4, "quatre objets vivants, ni plus ni moins");
    }

    VERIFIE_ENTIER(Capricieux::vivants, 0, "la croissance ratee n'a rien laisse fuir");
    return BILAN();
}
