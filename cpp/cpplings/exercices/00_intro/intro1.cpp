
#include "verif.hpp"

const bool PAS_FINI = true;

int main() {
    VERIFIE(1 + 1 == 2, "le compilateur sait compter");
    return BILAN();
}
