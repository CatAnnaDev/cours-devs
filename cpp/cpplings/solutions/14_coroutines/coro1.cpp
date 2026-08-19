#include <coroutine>
#include <cstdlib>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

class Compte {
  public:
    struct promise_type {
        int courant = 0;

        Compte get_return_object() {
            return Compte{std::coroutine_handle<promise_type>::from_promise(*this)};
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

    Compte(const Compte &) = delete;
    Compte &operator=(const Compte &) = delete;

    Compte(Compte &&autre) noexcept : poignee_(autre.poignee_) { autre.poignee_ = {}; }

    Compte &operator=(Compte &&autre) noexcept {
        if (this != &autre) {
            if (poignee_) {
                poignee_.destroy();
            }
            poignee_ = autre.poignee_;
            autre.poignee_ = {};
        }
        return *this;
    }

    ~Compte() {
        if (poignee_) {
            poignee_.destroy();
        }
    }

    bool terminee() const { return poignee_.done(); }

    int valeur() const { return poignee_.promise().courant; }

    void reprendre() {
        if (!poignee_.done()) {
            poignee_.resume();
        }
    }

  private:
    explicit Compte(std::coroutine_handle<promise_type> poignee) : poignee_(poignee) {}

    std::coroutine_handle<promise_type> poignee_;
};

Compte compter_a_rebours(int depart) {
    for (int reste = depart; reste > 0; reste--) {
        co_yield reste;
    }
}

}

int main() {
    Compte compte = compter_a_rebours(3);

    VERIFIE(!compte.terminee(), "creee, la coroutine est suspendue a initial_suspend");

    compte.reprendre();
    VERIFIE_ENTIER(compte.valeur(), 3, "la premiere reprise mene au premier co_yield");

    compte.reprendre();
    VERIFIE_ENTIER(compte.valeur(), 2, "la deuxieme reprise mene au deuxieme");

    compte.reprendre();
    VERIFIE_ENTIER(compte.valeur(), 1, "la troisieme reprise mene au troisieme");
    VERIFIE(!compte.terminee(), "le corps n'a pas encore quitte la boucle");

    compte.reprendre();
    VERIFIE(compte.terminee(), "le corps fini, final_suspend garde le cadre en vie");
    VERIFIE_ENTIER(compte.valeur(), 1, "et la promesse reste lisible apres la fin");

    return BILAN();
}
