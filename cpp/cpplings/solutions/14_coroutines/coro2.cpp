#include <coroutine>
#include <cstdlib>
#include <iterator>
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

int entrees_dans_le_corps = 0;

Generateur premiers_carres(int combien) {
    entrees_dans_le_corps++;
    for (int rang = 1; rang <= combien; rang++) {
        co_yield rang * rang;
    }
}

}

int main() {
    Generateur carres = premiers_carres(5);

    VERIFIE_ENTIER(entrees_dans_le_corps, 0,
                   "creer une coroutine n'execute pas une seule ligne de son corps");

    int lus[8] = {};
    int nombre_lus = 0;
    long long somme = 0;
    for (int carre : carres) {
        if (nombre_lus < 8) {
            lus[nombre_lus] = carre;
        }
        nombre_lus++;
        somme += carre;
    }

    VERIFIE_ENTIER(entrees_dans_le_corps, 1, "le corps n'a demarre qu'a la premiere iteration");
    VERIFIE_ENTIER(nombre_lus, 5, "cinq co_yield, cinq tours de boucle");
    VERIFIE_ENTIER(lus[0], 1, "le premier co_yield n'est pas saute");
    VERIFIE_ENTIER(lus[1], 4, "puis deux au carre");
    VERIFIE_ENTIER(lus[4], 25, "et le dernier vaut cinq au carre");
    VERIFIE_ENTIER(somme, 55, "un plus quatre plus neuf plus seize plus vingt-cinq");

    Generateur vide = premiers_carres(0);
    int tours_a_vide = 0;
    for (int carre : vide) {
        (void)carre;
        tours_a_vide++;
    }
    VERIFIE_ENTIER(tours_a_vide, 0, "un generateur sans co_yield boucle zero fois");

    return BILAN();
}
