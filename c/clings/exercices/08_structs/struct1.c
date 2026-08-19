#include "verif.h"

const int PAS_FINI = 1;

typedef struct {
    int x;
    int y;
} Point;

void deplacer(Point *point, int dx, int dy) {
    point.x += dx;
    point.y += dy;
}

int main(void) {
    Point point = {.x = 1, .y = 2};

    deplacer(&point, 3, 4);

    VERIFIE_ENTIER(point.x, 4, "x a avance de 3");
    VERIFIE_ENTIER(point.y, 6, "y a avance de 4");
    return BILAN();
}
