#include <functional>
#include <utility>

#include "verif.hpp"

const bool PAS_FINI = true;

class Garde {
public:
    explicit Garde(std::function<void()> action) : action_(std::move(action)) {}
    ~Garde() {}

    Garde(const Garde &) = delete;
    Garde &operator=(const Garde &) = delete;

private:
    std::function<void()> action_;
};

int main() {
    int ferme = 0;

    {
        Garde garde([&ferme] { ferme = 1; });
        VERIFIE_ENTIER(ferme, 0, "pas encore ferme");
    }

    VERIFIE_ENTIER(ferme, 1, "ferme a la sortie de la portee");
    return BILAN();
}
