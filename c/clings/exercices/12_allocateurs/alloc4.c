#include "verif.h"

#include <stdalign.h>

const int PAS_FINI = 1;

#define POOL_BLOCS 8
#define POOL_TAILLE_BLOC 32

typedef struct {
    unsigned char *octets;
    size_t taille_bloc;
    size_t nombre_blocs;
    void *premier_libre;
} Pool;

static alignas(16) unsigned char reserve[POOL_BLOCS * POOL_TAILLE_BLOC];
static Pool pool = {reserve, POOL_TAILLE_BLOC, POOL_BLOCS, NULL};

void pool_initialiser(Pool *cible) {
    cible->premier_libre = NULL;
    for (size_t i = cible->nombre_blocs; i > 0; i--) {
        unsigned char *bloc = cible->octets + (i - 1) * cible->taille_bloc;
        memcpy(bloc, &cible->premier_libre, sizeof(void *));
        cible->premier_libre = bloc;
    }
}

void *pool_allouer(Pool *cible) {
    void *bloc = cible->premier_libre;
    if (bloc == NULL) {
        return NULL;
    }
    memcpy(&cible->premier_libre, bloc, sizeof(void *));
    return bloc;
}

void pool_liberer(Pool *cible, void *bloc) {
    if (bloc == NULL) {
        return;
    }
    memset(bloc, 0, cible->taille_bloc);
}

int main(void) {
    VERIFIE(POOL_TAILLE_BLOC >= sizeof(void *), "un bloc est assez grand pour porter le chainage");

    pool_initialiser(&pool);
    unsigned char *tete = pool.premier_libre;
    VERIFIE(tete >= reserve && tete < reserve + sizeof reserve,
            "la tete de la liste libre est un bloc du pool, pas une case a part");

    unsigned char *blocs[POOL_BLOCS];
    int servis = 0;
    for (int i = 0; i < POOL_BLOCS; i++) {
        blocs[i] = pool_allouer(&pool);
        if (blocs[i] == NULL) {
            break;
        }
        memset(blocs[i], 'a' + i, POOL_TAILLE_BLOC);
        servis++;
    }
    VERIFIE_ENTIER(servis, POOL_BLOCS, "le pool sert ses huit blocs");
    VERIFIE(pool_allouer(&pool) == NULL, "et refuse le neuvieme");

    pool_liberer(&pool, blocs[3]);
    VERIFIE(pool_allouer(&pool) == blocs[3], "le bloc rendu repart en tete de la liste libre");
    VERIFIE_ENTIER(blocs[0][0], 'a', "le bloc encore pris devant n'a pas ete touche");
    VERIFIE_ENTIER(blocs[7][POOL_TAILLE_BLOC - 1], 'h', "celui de derriere non plus");

    for (int i = 0; i < POOL_BLOCS; i++) {
        pool_liberer(&pool, blocs[i]);
    }
    int reservis = 0;
    for (int i = 0; i < POOL_BLOCS; i++) {
        unsigned char *bloc = pool_allouer(&pool);
        if (bloc == NULL || bloc < reserve || bloc >= reserve + sizeof reserve) {
            break;
        }
        reservis++;
    }
    VERIFIE_ENTIER(reservis, POOL_BLOCS, "tout rendre puis tout reprendre marche encore");
    VERIFIE(pool_allouer(&pool) == NULL, "et le pool est de nouveau plein");
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
