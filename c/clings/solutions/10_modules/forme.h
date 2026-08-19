#ifndef FORME_H
#define FORME_H

struct Forme {
    int largeur;
    int hauteur;
};

enum Orientation {
    ORIENTATION_PORTRAIT,
    ORIENTATION_PAYSAGE,
    ORIENTATION_CARRE
};

static inline int forme_aire(const struct Forme *forme) {
    return forme->largeur * forme->hauteur;
}

#endif
