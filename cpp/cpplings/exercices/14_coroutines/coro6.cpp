#include <coroutine>
#include <cstddef>
#include <cstdlib>
#include <iterator>
#include <new>
#include <utility>

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

constexpr int NOMBRE_DE_TABLES = 12;
constexpr int LONGUEUR_DE_TABLE = 10;

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

Generateur multiples_de(int facteur, int combien) {
    for (int rang = 1; rang <= combien; rang++) {
        co_yield facteur * rang;
    }
}

long long somme_des_tables(int &valeurs_produites) {
    long long total = 0;
    for (int facteur = 1; facteur <= NOMBRE_DE_TABLES; facteur++) {
        for (int valeur : multiples_de(facteur, LONGUEUR_DE_TABLE)) {
            total += valeur;
            valeurs_produites++;
        }
    }
    return total;
}

}

int main() {
    suivi::Tas::remettre_a_zero();
    {
        Generateur temoin = multiples_de(2, 3);
        VERIFIE_ENTIER(suivi::Tas::allocations, 1,
                       "le cadre part sur le tas des l'appel, avant la moindre reprise");
        VERIFIE_ENTIER(suivi::Tas::liberations, 0, "et rien n'est rendu tant que le cadre vit");
    }
    VERIFIE_ENTIER(suivi::Tas::liberations, 1, "destroy() rend le cadre, meme jamais repris");

    suivi::Tas::remettre_a_zero();
    {
        Generateur parcouru = multiples_de(2, 3);
        long long total = 0;
        for (int valeur : parcouru) {
            total += valeur;
        }
        VERIFIE_ENTIER(total, 12, "deux, quatre, six");
        VERIFIE_ENTIER(suivi::Tas::allocations, 1,
                       "reprendre une coroutine n'alloue rien : tout tenait deja dans le cadre");
    }

    suivi::Tas::remettre_a_zero();
    int valeurs_produites = 0;
    const long long total = somme_des_tables(valeurs_produites);

    VERIFIE_ENTIER(valeurs_produites, NOMBRE_DE_TABLES * LONGUEUR_DE_TABLE,
                   "cent vingt valeurs produites");
    VERIFIE_ENTIER(total, 4290, "la somme des douze tables jusqu'a dix");
    VERIFIE_ENTIER(suivi::Tas::allocations, 1, "un seul cadre pour les cent vingt valeurs");
    VERIFIE_ENTIER(suivi::Tas::liberations, 1, "rendu une seule fois, a la fin du parcours");

    return BILAN();
}
