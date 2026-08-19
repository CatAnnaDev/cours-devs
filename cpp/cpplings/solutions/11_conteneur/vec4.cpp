#include <cstddef>
#include <new>
#include <type_traits>
#include <utility>

#include "verif.hpp"

const bool PAS_FINI = false;

struct Fragile {
    static inline int copies = 0;
    static inline int deplacements = 0;

    int valeur = 0;

    explicit Fragile(int v) : valeur(v) {}

    Fragile(const Fragile &autre) : valeur(autre.valeur) { copies++; }
    Fragile(Fragile &&autre) : valeur(autre.valeur) { deplacements++; }
};

struct Solide {
    static inline int copies = 0;
    static inline int deplacements = 0;

    int valeur = 0;

    explicit Solide(int v) : valeur(v) {}

    Solide(const Solide &autre) : valeur(autre.valeur) { copies++; }
    Solide(Solide &&autre) noexcept : valeur(autre.valeur) { deplacements++; }
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

int main() {
    VERIFIE(std::is_nothrow_move_constructible_v<Solide>,
            "le deplacement de Solide ne peut jamais lever");
    VERIFIE(!std::is_nothrow_move_constructible_v<Fragile>,
            "celui de Fragile peut lever");

    {
        Vecteur<Solide> solides;
        solides.reserve(4);
        for (int i = 1; i <= 4; i++) {
            solides.emplace_back(i);
        }

        Solide::copies = 0;
        Solide::deplacements = 0;
        solides.emplace_back(5);

        VERIFIE_ENTIER(solides.capacite(), 8, "le tampon des Solide a double");
        VERIFIE_ENTIER(Solide::deplacements, 4, "les quatre Solide ont ete deplaces");
        VERIFIE_ENTIER(Solide::copies, 0, "aucun Solide n'a ete copie");
        VERIFIE_ENTIER(solides[0].valeur, 1, "les valeurs ont suivi");
        VERIFIE_ENTIER(solides[4].valeur, 5, "y compris la derniere ajoutee");
    }

    {
        Vecteur<Fragile> fragiles;
        fragiles.reserve(4);
        for (int i = 1; i <= 4; i++) {
            fragiles.emplace_back(i);
        }

        Fragile::copies = 0;
        Fragile::deplacements = 0;
        fragiles.emplace_back(5);

        VERIFIE_ENTIER(fragiles.capacite(), 8, "le tampon des Fragile a double aussi");
        VERIFIE_ENTIER(Fragile::copies, 4, "les quatre Fragile ont ete copies");
        VERIFIE_ENTIER(Fragile::deplacements, 0, "aucun Fragile n'a ete deplace");
        VERIFIE_ENTIER(fragiles[0].valeur, 1, "les valeurs ont suivi la aussi");
        VERIFIE_ENTIER(fragiles[4].valeur, 5, "y compris la derniere ajoutee");
    }

    return BILAN();
}
