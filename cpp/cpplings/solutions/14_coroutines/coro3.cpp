#include <coroutine>
#include <cstdlib>
#include <iterator>
#include <type_traits>
#include <utility>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

class Generateur {
  public:
    struct promise_type {
        int courant = 0;

        Generateur get_return_object() {
            return Generateur{std::coroutine_handle<promise_type>::from_promise(*this)};
        }
        std::suspend_always initial_suspend() noexcept { return {}; }
        std::suspend_always final_suspend() noexcept { return {}; }
        std::suspend_always yield_value(int valeur) noexcept {
            courant = valeur;
            return {};
        }
        void return_void() noexcept {}
        void unhandled_exception() { std::abort(); }
    };

    class Iterateur {
      public:
        explicit Iterateur(std::coroutine_handle<promise_type> poignee) : poignee_(poignee) {}

        int operator*() const { return poignee_.promise().courant; }

        Iterateur &operator++() {
            poignee_.resume();
            if (poignee_.done()) {
                poignee_ = {};
            }
            return *this;
        }

        bool operator!=(std::default_sentinel_t) const { return static_cast<bool>(poignee_); }

      private:
        std::coroutine_handle<promise_type> poignee_;
    };

    Generateur(const Generateur &) = delete;
    Generateur &operator=(const Generateur &) = delete;

    Generateur(Generateur &&autre) noexcept : poignee_(std::exchange(autre.poignee_, {})) {}

    Generateur &operator=(Generateur &&autre) noexcept {
        if (this != &autre) {
            if (poignee_) {
                poignee_.destroy();
            }
            poignee_ = std::exchange(autre.poignee_, {});
        }
        return *this;
    }

    ~Generateur() {
        if (poignee_) {
            poignee_.destroy();
        }
    }

    Iterateur begin() {
        if (!poignee_.done()) {
            poignee_.resume();
        }
        if (poignee_.done()) {
            return Iterateur{std::coroutine_handle<promise_type>{}};
        }
        return Iterateur{poignee_};
    }

    std::default_sentinel_t end() const noexcept { return {}; }

  private:
    explicit Generateur(std::coroutine_handle<promise_type> poignee) : poignee_(poignee) {}

    std::coroutine_handle<promise_type> poignee_;
};

Generateur inventaire(int combien) {
    verif::Sonde marqueur(100);
    for (int rang = 0; rang < combien; rang++) {
        co_yield marqueur.valeur + rang;
    }
}

}

int main() {
    VERIFIE(!std::is_copy_constructible_v<Generateur>,
            "un generateur ne se copie pas : deux poignees detruiraient le meme cadre");
    VERIFIE(std::is_move_constructible_v<Generateur>, "mais il se deplace, en volant la poignee");
    VERIFIE(std::is_nothrow_move_constructible_v<Generateur>, "et son deplacement ne leve pas");

    verif::Compteur::remettre_a_zero();
    {
        Generateur flux = inventaire(5);
        int lus = 0;
        long long somme = 0;
        for (int valeur : flux) {
            somme += valeur;
            lus++;
            if (lus == 2) {
                break;
            }
        }
        VERIFIE_ENTIER(lus, 2, "on abandonne le parcours au deuxieme element");
        VERIFIE_ENTIER(somme, 201, "cent, puis cent un");
        VERIFIE_ENTIER(verif::Compteur::constructions, 1,
                       "la Sonde du corps a bien ete construite");
        VERIFIE_ENTIER(verif::Compteur::destructions, 0,
                       "et elle vit toujours : la coroutine est suspendue dans sa boucle");
    }
    VERIFIE_ENTIER(verif::Compteur::destructions, 1,
                   "le destructeur du generateur appelle destroy(), qui detruit les locales");

    verif::Compteur::remettre_a_zero();
    {
        Generateur source = inventaire(3);
        Generateur destination = std::move(source);
        long long somme = 0;
        int lus = 0;
        for (int valeur : destination) {
            somme += valeur;
            lus++;
            if (lus == 2) {
                break;
            }
        }
        VERIFIE_ENTIER(somme, 201, "le cadre a survecu au deplacement, intact");
    }
    VERIFIE_ENTIER(verif::Compteur::constructions, 1, "un seul cadre construit");
    VERIFIE_ENTIER(verif::Compteur::destructions, 1,
                   "un seul detruit : la source deplacee ne tient plus la poignee");

    verif::Compteur::remettre_a_zero();
    {
        Generateur jamais_lu = inventaire(4);
        (void)jamais_lu;
    }
    VERIFIE_ENTIER(verif::Compteur::constructions, 0, "un corps jamais repris ne construit rien");
    VERIFIE_ENTIER(verif::Compteur::destructions, 0, "et il n'y a donc rien a detruire");

    return BILAN();
}
