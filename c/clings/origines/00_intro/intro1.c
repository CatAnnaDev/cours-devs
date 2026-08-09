#include "verif.h"

const int PAS_FINI = 1;

int main(void) {
    VERIFIE(1 == 1, "le compilateur sait compter");
    return BILAN();
}
