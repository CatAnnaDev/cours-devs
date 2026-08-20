#include "verif.h"
#include <stdint.h>

const int PAS_FINI = 0;

#define MAGIE_OCTETS 4
#define EN_TETE_OCTETS 7
#define VERSION_COURANTE 3
#define NOM_MAX 15
#define ENREG_OCTETS_MIN 5

typedef enum {
    LECTURE_OK,
    LECTURE_VIDE,
    LECTURE_MAGIE,
    LECTURE_VERSION,
    LECTURE_TRONQUEE,
    LECTURE_LONGUEUR,
    LECTURE_MEMOIRE
} EtatLecture;

typedef struct {
    uint32_t identifiant;
    char nom[NOM_MAX + 1];
} Enregistrement;

typedef struct {
    Enregistrement *enregistrements;
    uint16_t nombre;
} Document;

static const uint8_t MAGIE[MAGIE_OCTETS] = {'C', 'L', 'N', 'G'};

static void liberer_document(Document *document) {
    suivi_free(document->enregistrements);
    document->enregistrements = NULL;
    document->nombre = 0;
}

static long octets_restants(FILE *flux) {
    long ici = ftell(flux);
    if (ici < 0 || fseek(flux, 0, SEEK_END) != 0) {
        return -1;
    }
    long fin = ftell(flux);
    if (fin < 0 || fseek(flux, ici, SEEK_SET) != 0) {
        return -1;
    }
    return fin - ici;
}

static EtatLecture lire_enregistrements(FILE *flux, Enregistrement *tableau, uint16_t nombre) {
    for (uint16_t i = 0; i < nombre; i++) {
        uint8_t champs[ENREG_OCTETS_MIN];
        if (fread(champs, 1, sizeof champs, flux) != sizeof champs) {
            return LECTURE_TRONQUEE;
        }
        uint8_t longueur = champs[4];
        if (longueur > NOM_MAX) {
            return LECTURE_LONGUEUR;
        }
        if (fread(tableau[i].nom, 1, longueur, flux) != longueur) {
            return LECTURE_TRONQUEE;
        }
        tableau[i].identifiant = ((uint32_t)champs[0] << 24) | ((uint32_t)champs[1] << 16) |
                                 ((uint32_t)champs[2] << 8) | (uint32_t)champs[3];
        tableau[i].nom[longueur] = '\0';
    }
    return LECTURE_OK;
}

static EtatLecture lire_document(FILE *flux, Document *sortie) {
    uint8_t entete[EN_TETE_OCTETS];
    sortie->enregistrements = NULL;
    sortie->nombre = 0;

    size_t lus = fread(entete, 1, sizeof entete, flux);
    if (lus == 0) {
        return LECTURE_VIDE;
    }
    if (lus != sizeof entete) {
        return LECTURE_TRONQUEE;
    }
    if (memcmp(entete, MAGIE, MAGIE_OCTETS) != 0) {
        return LECTURE_MAGIE;
    }
    if (entete[4] != VERSION_COURANTE) {
        return LECTURE_VERSION;
    }

    uint16_t nombre = (uint16_t)(((uint16_t)entete[5] << 8) | (uint16_t)entete[6]);
    long reste = octets_restants(flux);
    if (reste < 0) {
        return LECTURE_TRONQUEE;
    }
    if ((unsigned long)nombre > (unsigned long)reste / ENREG_OCTETS_MIN) {
        return LECTURE_TRONQUEE;
    }

    Enregistrement *tableau = NULL;
    if (nombre > 0) {
        tableau = suivi_calloc(nombre, sizeof *tableau);
        if (tableau == NULL) {
            return LECTURE_MEMOIRE;
        }
    }

    EtatLecture etat = lire_enregistrements(flux, tableau, nombre);
    if (etat != LECTURE_OK) {
        suivi_free(tableau);
        return etat;
    }

    sortie->enregistrements = tableau;
    sortie->nombre = nombre;
    return LECTURE_OK;
}

static FILE *flux_depuis(const uint8_t *octets, size_t taille) {
    FILE *flux = tmpfile();
    if (flux == NULL) {
        return NULL;
    }
    if (taille > 0 && fwrite(octets, 1, taille, flux) != taille) {
        fclose(flux);
        return NULL;
    }
    if (fflush(flux) != 0) {
        fclose(flux);
        return NULL;
    }
    rewind(flux);
    return flux;
}

static EtatLecture etat_de(const uint8_t *octets, size_t taille, Document *sortie) {
    sortie->enregistrements = NULL;
    sortie->nombre = 0;
    FILE *flux = flux_depuis(octets, taille);
    if (flux == NULL) {
        return LECTURE_MEMOIRE;
    }
    EtatLecture etat = lire_document(flux, sortie);
    fclose(flux);
    return etat;
}

int main(void) {
    static const uint8_t COMPLET[] = {
        'C', 'L', 'N', 'G', VERSION_COURANTE, 0x00, 0x02,
        0x00, 0x00, 0x00, 0x2A, 5, 'a', 'l', 'p', 'h', 'a',
        0x00, 0x00, 0x01, 0x00, 4, 'b', 'e', 't', 'a',
    };
    static const uint8_t AUTRE_FORMAT[] = {
        'C', 'L', 'N', 'H', VERSION_COURANTE, 0x00, 0x01,
        0x00, 0x00, 0x00, 0x01, 3, 'a', 'b', 'c',
    };
    static const uint8_t VIEUX[] = {
        'C', 'L', 'N', 'G', 1, 0x00, 0x01,
        0x00, 0x00, 0x00, 0x01, 3, 'a', 'b', 'c',
    };
    static const uint8_t COUPE[] = {
        'C', 'L', 'N', 'G', VERSION_COURANTE, 0x00, 0x02,
        0x00, 0x00, 0x00, 0x2A, 5, 'a', 'l', 'p', 'h', 'a',
        0x00, 0x00,
    };
    static uint8_t NOM_ENORME[EN_TETE_OCTETS + ENREG_OCTETS_MIN + 200] = {
        'C', 'L', 'N', 'G', VERSION_COURANTE, 0x00, 0x01,
        0x00, 0x00, 0x00, 0x01, 200,
    };
    memset(NOM_ENORME + EN_TETE_OCTETS + ENREG_OCTETS_MIN, 'z', 200);

    Document document = {NULL, 0};

    VERIFIE(memcmp(COMPLET, MAGIE, MAGIE_OCTETS) == 0, "le document de test porte la magie");

    VERIFIE_ENTIER(etat_de(COMPLET, sizeof COMPLET, &document), LECTURE_OK,
                   "un document conforme est lu");
    VERIFIE(document.enregistrements != NULL && document.nombre == 2,
            "avec ses deux enregistrements");
    if (document.enregistrements != NULL && document.nombre == 2) {
        VERIFIE_ENTIER(document.enregistrements[0].identifiant, 42u, "le premier identifiant");
        VERIFIE_TEXTE(document.enregistrements[0].nom, "alpha", "le premier nom");
        VERIFIE_ENTIER(document.enregistrements[1].identifiant, 256u, "le second identifiant");
        VERIFIE_TEXTE(document.enregistrements[1].nom, "beta", "le second nom");
    }
    liberer_document(&document);
    VERIFIE_PAS_DE_FUITE();

    VERIFIE_ENTIER(etat_de(COMPLET, 0, &document), LECTURE_VIDE, "un fichier vide est nomme vide");
    liberer_document(&document);
    VERIFIE_PAS_DE_FUITE();

    VERIFIE_ENTIER(etat_de(AUTRE_FORMAT, sizeof AUTRE_FORMAT, &document), LECTURE_MAGIE,
                   "un fichier d'un autre format est refuse sur la magie");
    liberer_document(&document);
    VERIFIE_PAS_DE_FUITE();

    VERIFIE_ENTIER(etat_de(VIEUX, sizeof VIEUX, &document), LECTURE_VERSION,
                   "une version que ce lecteur ne connait pas est refusee");
    liberer_document(&document);
    VERIFIE_PAS_DE_FUITE();

    VERIFIE_ENTIER(etat_de(COUPE, sizeof COUPE, &document), LECTURE_TRONQUEE,
                   "un fichier coupe au milieu d'un champ est signale tronque");
    liberer_document(&document);
    VERIFIE_PAS_DE_FUITE();

    VERIFIE_ENTIER(etat_de(NOM_ENORME, sizeof NOM_ENORME, &document), LECTURE_LONGUEUR,
                   "un nom de 200 octets pour un champ de 15 est refuse, pas copie");
    liberer_document(&document);
    VERIFIE_PAS_DE_FUITE();

    return BILAN();
}
