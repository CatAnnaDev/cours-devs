#include "verif.h"

const int PAS_FINI = 0;

typedef struct {
    int occupee;
    int cle;
    int valeur;
} Case;

typedef struct {
    Case *cases;
    size_t capacite;
    size_t taille;
} Table;

static size_t hachage(int cle) {
    return (size_t)(unsigned int)cle * 2654435761u;
}

static int table_creer(Table *table, size_t capacite) {
    table->cases = suivi_calloc(capacite, sizeof(Case));
    if (table->cases == NULL) {
        return 0;
    }
    table->capacite = capacite;
    table->taille = 0;
    return 1;
}

static void table_liberer(Table *table) {
    suivi_free(table->cases);
    table->cases = NULL;
    table->capacite = 0;
    table->taille = 0;
}

static int table_inserer(Table *table, int cle, int valeur) {
    size_t depart = hachage(cle) % table->capacite;
    for (size_t pas = 0; pas < table->capacite; pas++) {
        Case *emplacement = &table->cases[(depart + pas) % table->capacite];
        if (!emplacement->occupee) {
            emplacement->occupee = 1;
            emplacement->cle = cle;
            emplacement->valeur = valeur;
            table->taille++;
            return 1;
        }
        if (emplacement->cle == cle) {
            emplacement->valeur = valeur;
            return 1;
        }
    }
    return 0;
}

static int table_chercher(const Table *table, int cle, int *valeur) {
    size_t depart = hachage(cle) % table->capacite;
    for (size_t pas = 0; pas < table->capacite; pas++) {
        const Case *emplacement = &table->cases[(depart + pas) % table->capacite];
        if (!emplacement->occupee) {
            return 0;
        }
        if (emplacement->cle == cle) {
            *valeur = emplacement->valeur;
            return 1;
        }
    }
    return 0;
}

int main(void) {
    Table table;
    VERIFIE(table_creer(&table, 8) == 1, "la table de huit cases est creee");

    VERIFIE(table_inserer(&table, 7, 700) == 1, "la cle 7 se loge dans la derniere case");
    VERIFIE(table_inserer(&table, 15, 1500) == 1, "la cle 15 vise la meme case et doit repartir du debut");
    VERIFIE(table_inserer(&table, 23, 2300) == 1, "la cle 23 vise encore la meme case");
    VERIFIE(table_inserer(&table, 2, 200) == 1, "la cle 2 vise une case libre");
    VERIFIE(table_inserer(&table, 10, 1000) == 1, "la cle 10 entre en collision avec 2");
    VERIFIE_ENTIER(table.taille, 5, "la table compte cinq cles");

    int valeur = 0;
    VERIFIE(table_chercher(&table, 7, &valeur) == 1 && valeur == 700, "la cle 7 se retrouve");
    VERIFIE(table_chercher(&table, 15, &valeur) == 1 && valeur == 1500, "la cle 15 se retrouve");
    VERIFIE(table_chercher(&table, 23, &valeur) == 1 && valeur == 2300, "la cle 23 se retrouve");
    VERIFIE(table_chercher(&table, 2, &valeur) == 1 && valeur == 200, "la cle 2 se retrouve");
    VERIFIE(table_chercher(&table, 10, &valeur) == 1 && valeur == 1000, "la cle 10 se retrouve");
    VERIFIE(table_chercher(&table, 99, &valeur) == 0, "une cle absente ne se retrouve pas");

    VERIFIE(table_inserer(&table, 15, 1501) == 1, "reinserer une cle presente la met a jour");
    VERIFIE(table_chercher(&table, 15, &valeur) == 1 && valeur == 1501, "elle porte sa nouvelle valeur");
    VERIFIE_ENTIER(table.taille, 5, "la table compte toujours cinq cles");

    table_liberer(&table);
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
