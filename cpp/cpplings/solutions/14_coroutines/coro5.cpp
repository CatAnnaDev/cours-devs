#include <coroutine>
#include <cstddef>
#include <cstdlib>
#include <string_view>
#include <utility>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

constexpr std::size_t CAPACITE_DU_JOURNAL = 32;

char evenements[CAPACITE_DU_JOURNAL];
std::size_t evenements_notes = 0;

void noter(char etape) {
    if (evenements_notes < CAPACITE_DU_JOURNAL) {
        evenements[evenements_notes] = etape;
        evenements_notes++;
    }
}

std::string_view trace() { return std::string_view(evenements, evenements_notes); }

int suspensions() {
    int total = 0;
    for (std::size_t rang = 0; rang < evenements_notes; rang++) {
        if (evenements[rang] == 's') {
            total++;
        }
    }
    return total;
}

class Tache {
  public:
    struct promise_type {
        Tache get_return_object() {
            return Tache{std::coroutine_handle<promise_type>::from_promise(*this)};
        }
        std::suspend_always initial_suspend() noexcept { return {}; }
        std::suspend_always final_suspend() noexcept { return {}; }
        void return_void() noexcept {}
        void unhandled_exception() { std::abort(); }
    };

    Tache(const Tache &) = delete;
    Tache &operator=(const Tache &) = delete;

    Tache(Tache &&autre) noexcept : poignee_(std::exchange(autre.poignee_, {})) {}

    Tache &operator=(Tache &&autre) noexcept {
        if (this != &autre) {
            if (poignee_) {
                poignee_.destroy();
            }
            poignee_ = std::exchange(autre.poignee_, {});
        }
        return *this;
    }

    ~Tache() {
        if (poignee_) {
            poignee_.destroy();
        }
    }

    bool finie() const { return poignee_.done(); }

    void demarrer() {
        if (!poignee_.done()) {
            poignee_.resume();
        }
    }

  private:
    explicit Tache(std::coroutine_handle<promise_type> poignee) : poignee_(poignee) {}

    std::coroutine_handle<promise_type> poignee_;
};

struct Boite {
    int valeur = 0;
    bool remplie = false;
    std::coroutine_handle<> endormie{};

    void deposer(int nouvelle) {
        valeur = nouvelle;
        remplie = true;
    }

    void relancer() {
        if (endormie) {
            std::coroutine_handle<> reprise = std::exchange(endormie, {});
            if (!reprise.done()) {
                reprise.resume();
            }
        }
    }
};

struct Retrait {
    Boite &boite;

    bool await_ready() const noexcept {
        noter('r');
        return boite.remplie;
    }

    void await_suspend(std::coroutine_handle<> reprise) const noexcept {
        noter('s');
        boite.endormie = reprise;
    }

    int await_resume() const noexcept {
        noter('v');
        return boite.valeur;
    }
};

Tache additionner(Boite &gauche, Boite &droite, int &resultat) {
    const int premier = co_await Retrait{gauche};
    const int second = co_await Retrait{droite};
    resultat = premier + second;
}

}

int main() {
    Boite gauche;
    Boite droite;
    int resultat = 0;

    gauche.deposer(17);

    Tache somme = additionner(gauche, droite, resultat);
    VERIFIE_TEXTE(trace(), "", "creee, la tache n'a rien attendu du tout");

    somme.demarrer();

    VERIFIE_TEXTE(trace(), "rvrs",
                  "await_ready vrai saute await_suspend et va droit a await_resume");
    VERIFIE(!gauche.endormie, "une valeur deja prete ne suspend pas la coroutine");
    VERIFIE(static_cast<bool>(droite.endormie), "la boite vide, elle, a garde la poignee");
    VERIFIE_ENTIER(resultat, 0, "la tache est bloquee avant l'addition");
    VERIFIE(!somme.finie(), "et elle n'est pas terminee");

    for (int tour = 0; tour < 4 && !somme.finie(); tour++) {
        gauche.relancer();
        if (tour == 0) {
            droite.deposer(25);
        }
        droite.relancer();
    }

    VERIFIE_TEXTE(trace(), "rvrsv", "la reprise reprend a await_resume, pas a await_ready");
    VERIFIE_ENTIER(suspensions(), 1, "une seule suspension pour deux co_await");
    VERIFIE_ENTIER(resultat, 42, "dix-sept et vingt-cinq");
    VERIFIE(somme.finie(), "la tache est arrivee a son final_suspend");

    return BILAN();
}
