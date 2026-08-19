#include "verif.h"

const int PAS_FINI = 1;

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

static int table_supprimer(Table *table, int cle) {
    size_t depart = hachage(cle) % table->capacite;
    for (size_t pas = 0; pas < table->capacite; pas++) {
        Case *emplacement = &table->cases[(depart + pas) % table->capacite];
        if (!emplacement->occupee) {
            return 0;
        }
        if (emplacement->cle == cle) {
            emplacement->occupee = 0;
            table->taille--;
            return 1;
        }
    }
    return 0;
}

int main(void) {
    Table table;
    VERIFIE(table_creer(&table, 8) == 1, "la table de huit cases est creee");

    VERIFIE(table_inserer(&table, 7, 700) == 1, "la cle 7 ouvre la chaine de sondage");
    VERIFIE(table_inserer(&table, 15, 1500) == 1, "la cle 15 se decale d'un cran");
    VERIFIE(table_inserer(&table, 23, 2300) == 1, "la cle 23 se decale de deux crans");

    int valeur = 0;
    VERIFIE(table_supprimer(&table, 15) == 1, "la cle du milieu de la chaine est supprimee");
    VERIFIE_ENTIER(table.taille, 2, "la table compte deux cles");
    VERIFIE(table_chercher(&table, 15, &valeur) == 0, "la cle supprimee est introuvable");
    VERIFIE(table_chercher(&table, 7, &valeur) == 1 && valeur == 700, "la cle d'avant repond encore");
    VERIFIE(table_chercher(&table, 23, &valeur) == 1 && valeur == 2300,
            "la cle d'apres repond encore malgre le trou");

    VERIFIE(table_inserer(&table, 15, 1501) == 1, "la cle supprimee peut revenir");
    VERIFIE(table_chercher(&table, 15, &valeur) == 1 && valeur == 1501, "elle porte sa nouvelle valeur");
    VERIFIE_ENTIER(table.taille, 3, "la table compte trois cles");
    VERIFIE(table_supprimer(&table, 99) == 0, "supprimer une cle absente ne reussit pas");
    table_liberer(&table);

    Table pleine;
    VERIFIE(table_creer(&pleine, 8) == 1, "une seconde table de huit cases est creee");
    int toutes_entrees = 1;
    for (int cle = 0; cle < 8; cle++) {
        toutes_entrees = toutes_entrees && table_inserer(&pleine, cle, cle * 100);
    }
    VERIFIE(toutes_entrees, "huit cles remplissent la table");
    VERIFIE(table_inserer(&pleine, 100, 1) == 0, "une neuvieme cle ne rentre pas");
    VERIFIE(table_supprimer(&pleine, 3) == 1, "une cle est retiree");
    VERIFIE(table_inserer(&pleine, 100, 10000) == 1, "la case retiree est reutilisee");
    VERIFIE(table_chercher(&pleine, 100, &valeur) == 1 && valeur == 10000, "la nouvelle cle repond");

    int restantes_correctes = 1;
    for (int cle = 0; cle < 8; cle++) {
        int trouvee = table_chercher(&pleine, cle, &valeur);
        if (cle == 3) {
            if (trouvee) {
                restantes_correctes = 0;
            }
        } else if (!trouvee || valeur != cle * 100) {
            restantes_correctes = 0;
        }
    }
    VERIFIE(restantes_correctes, "les sept cles restantes repondent toutes");

    table_liberer(&pleine);
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
