#ifndef DESSIN_H
#define DESSIN_H

#include "forme.h"

static inline enum Orientation dessin_orientation(const struct Forme *forme) {
    if (forme->largeur > forme->hauteur) {
        return ORIENTATION_PAYSAGE;
    }
    if (forme->largeur < forme->hauteur) {
        return ORIENTATION_PORTRAIT;
    }
    return ORIENTATION_CARRE;
}

#endif
