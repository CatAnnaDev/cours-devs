#include <coroutine>
#include <cstdlib>
#include <iterator>
#include <string>
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

Generateur longueurs_des_morceaux(std::string texte, char separateur) {
    int longueur = 0;
    for (char lettre : texte) {
        if (lettre == separateur) {
            co_yield longueur;
            longueur = 0;
        } else {
            longueur++;
        }
    }
    co_yield longueur;
}

}

int main() {
    int lus[8] = {};
    int nombre = 0;
    long long total = 0;

    Generateur du_temporaire =
        longueurs_des_morceaux(std::string("granit,basalte,gneiss,schiste,obsidienne"), ',');

    for (int longueur : du_temporaire) {
        if (nombre < 8) {
            lus[nombre] = longueur;
        }
        nombre++;
        total += longueur;
    }

    VERIFIE_ENTIER(nombre, 5, "quatre virgules decoupent cinq morceaux");
    VERIFIE_ENTIER(lus[0], 6, "granit");
    VERIFIE_ENTIER(lus[1], 7, "basalte");
    VERIFIE_ENTIER(lus[4], 10, "obsidienne");
    VERIFIE_ENTIER(total, 36, "trente-six lettres en tout");

    std::string source = "malachite,cinabre,azurite";
    Generateur du_nomme = longueurs_des_morceaux(source, ',');
    source.assign("efface");

    int deuxieme[8] = {};
    int compte = 0;
    for (int longueur : du_nomme) {
        if (compte < 8) {
            deuxieme[compte] = longueur;
        }
        compte++;
    }

    VERIFIE_ENTIER(compte, 3, "le cadre garde sa propre copie du texte");
    VERIFIE_ENTIER(deuxieme[0], 9, "malachite, meme apres que la source ait change");
    VERIFIE_ENTIER(deuxieme[2], 7, "azurite, meme apres que la source ait change");

    return BILAN();
}
