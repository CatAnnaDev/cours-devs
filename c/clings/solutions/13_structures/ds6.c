#include "verif.h"

const int PAS_FINI = 0;

#define CHARGE_MAX_NUMERATEUR 7
#define CHARGE_MAX_DENOMINATEUR 10

typedef enum {
    CASE_VIDE,
    CASE_OCCUPEE,
    CASE_SUPPRIMEE
} EtatCase;

typedef struct {
    EtatCase etat;
    int cle;
    int valeur;
} Case;

typedef struct {
    Case *cases;
    size_t capacite;
    size_t taille;
    size_t occupees;
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
    table->occupees = 0;
    return 1;
}

static void table_liberer(Table *table) {
    suivi_free(table->cases);
    table->cases = NULL;
    table->capacite = 0;
    table->taille = 0;
    table->occupees = 0;
}

static int table_placer(Table *table, int cle, int valeur) {
    size_t depart = hachage(cle) % table->capacite;
    size_t cible = table->capacite;
    int case_neuve = 1;
    for (size_t pas = 0; pas < table->capacite; pas++) {
        size_t indice = (depart + pas) % table->capacite;
        Case *emplacement = &table->cases[indice];
        if (emplacement->etat == CASE_VIDE) {
            if (cible == table->capacite) {
                cible = indice;
            }
            break;
        }
        if (emplacement->etat == CASE_SUPPRIMEE) {
            if (cible == table->capacite) {
                cible = indice;
                case_neuve = 0;
            }
            continue;
        }
        if (emplacement->cle == cle) {
            emplacement->valeur = valeur;
            return 1;
        }
    }
    if (cible == table->capacite) {
        return 0;
    }
    table->cases[cible].etat = CASE_OCCUPEE;
    table->cases[cible].cle = cle;
    table->cases[cible].valeur = valeur;
    table->taille++;
    if (case_neuve) {
        table->occupees++;
    }
    return 1;
}

static int table_grandir(Table *table) {
    size_t nouvelle_capacite = table->capacite * 2;
    Case *neuves = suivi_calloc(nouvelle_capacite, sizeof(Case));
    if (neuves == NULL) {
        return 0;
    }
    Case *anciennes = table->cases;
    size_t ancienne_capacite = table->capacite;
    table->cases = neuves;
    table->capacite = nouvelle_capacite;
    table->taille = 0;
    table->occupees = 0;
    for (size_t indice = 0; indice < ancienne_capacite; indice++) {
        if (anciennes[indice].etat == CASE_OCCUPEE) {
            table_placer(table, anciennes[indice].cle, anciennes[indice].valeur);
        }
    }
    suivi_free(anciennes);
    return 1;
}

static int table_inserer(Table *table, int cle, int valeur) {
    if ((table->occupees + 1) * CHARGE_MAX_DENOMINATEUR
        > table->capacite * CHARGE_MAX_NUMERATEUR && !table_grandir(table)) {
        return 0;
    }
    return table_placer(table, cle, valeur);
}

static int table_chercher(const Table *table, int cle, int *valeur) {
    size_t depart = hachage(cle) % table->capacite;
    for (size_t pas = 0; pas < table->capacite; pas++) {
        const Case *emplacement = &table->cases[(depart + pas) % table->capacite];
        if (emplacement->etat == CASE_VIDE) {
            return 0;
        }
        if (emplacement->etat == CASE_OCCUPEE && emplacement->cle == cle) {
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
        if (emplacement->etat == CASE_VIDE) {
            return 0;
        }
        if (emplacement->etat == CASE_OCCUPEE && emplacement->cle == cle) {
            emplacement->etat = CASE_SUPPRIMEE;
            table->taille--;
            return 1;
        }
    }
    return 0;
}

int main(void) {
    Table table;
    VERIFIE(table_creer(&table, 8) == 1, "la table part avec huit cases");

    int toutes_entrees = 1;
    for (int i = 0; i < 40; i++) {
        toutes_entrees = toutes_entrees && table_inserer(&table, i * 3 + 1, (i * 3 + 1) * 10);
    }
    VERIFIE(toutes_entrees, "quarante cles entrent dans une table nee avec huit cases");
    VERIFIE_ENTIER(table.taille, 40, "la table compte quarante cles");
    VERIFIE_ENTIER(table.capacite, 64, "la capacite a double jusqu'a soixante-quatre");
    VERIFIE(table.occupees * CHARGE_MAX_DENOMINATEUR <= table.capacite * CHARGE_MAX_NUMERATEUR,
            "le taux d'occupation est reste sous le seuil");

    int toutes_retrouvees = 1;
    for (int i = 0; i < 40; i++) {
        int valeur = 0;
        if (!table_chercher(&table, i * 3 + 1, &valeur) || valeur != (i * 3 + 1) * 10) {
            toutes_retrouvees = 0;
        }
    }
    VERIFIE(toutes_retrouvees, "les quarante cles se retrouvent toutes apres les rehachages");

    int valeur = 0;
    VERIFIE(table_chercher(&table, 2, &valeur) == 0, "une cle jamais inseree reste absente");
    VERIFIE(table_chercher(&table, 500, &valeur) == 0, "une cle hors de la plage reste absente");

    int toutes_retirees = 1;
    for (int i = 0; i < 40; i += 8) {
        toutes_retirees = toutes_retirees && table_supprimer(&table, i * 3 + 1);
    }
    VERIFIE(toutes_retirees, "cinq cles sont retirees");
    VERIFIE_ENTIER(table.taille, 35, "la table compte trente-cinq cles");
    VERIFIE_ENTIER(table.capacite, 64, "retirer des cles ne change pas la capacite");

    int survivantes_correctes = 1;
    for (int i = 0; i < 40; i++) {
        int trouvee = table_chercher(&table, i * 3 + 1, &valeur);
        if (i % 8 == 0) {
            if (trouvee) {
                survivantes_correctes = 0;
            }
        } else if (!trouvee || valeur != (i * 3 + 1) * 10) {
            survivantes_correctes = 0;
        }
    }
    VERIFIE(survivantes_correctes, "les trente-cinq survivantes repondent toutes");

    table_liberer(&table);
    VERIFIE_PAS_DE_FUITE();
    return BILAN();
}
