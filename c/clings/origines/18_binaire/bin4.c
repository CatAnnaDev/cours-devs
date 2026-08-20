#include "verif.h"
#include <stdint.h>

const int PAS_FINI = 1;

#define MAGIE_OCTETS 4
#define EN_TETE_OCTETS 7
#define VERSION_COURANTE 2

typedef enum {
    EN_TETE_OK,
    EN_TETE_TRONQUE,
    EN_TETE_MAGIE,
    EN_TETE_VERSION
} EtatEnTete;

typedef struct {
    uint8_t version;
    uint16_t nombre;
} EnTete;

static const uint8_t MAGIE[MAGIE_OCTETS] = {'C', 'L', 'N', 'G'};

static EtatEnTete lire_en_tete(FILE *flux, EnTete *sortie) {
    uint8_t octets[EN_TETE_OCTETS];
    if (fread(octets, 1, sizeof octets, flux) != sizeof octets) {
        return EN_TETE_TRONQUE;
    }
    sortie->version = octets[4];
    sortie->nombre = (uint16_t)(((uint16_t)octets[5] << 8) | (uint16_t)octets[6]);
    return EN_TETE_OK;
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

static EtatEnTete etat_de(const uint8_t *octets, size_t taille, EnTete *sortie) {
    FILE *flux = flux_depuis(octets, taille);
    if (flux == NULL) {
        return EN_TETE_TRONQUE;
    }
    EtatEnTete etat = lire_en_tete(flux, sortie);
    fclose(flux);
    return etat;
}

int main(void) {
    static const uint8_t VALIDE[EN_TETE_OCTETS] = {'C', 'L', 'N', 'G', 2, 0x01, 0x2C};
    static const uint8_t ANCIEN[EN_TETE_OCTETS] = {'C', 'L', 'N', 'G', 1, 0x00, 0x03};
    static const uint8_t FUTUR[EN_TETE_OCTETS] = {'C', 'L', 'N', 'G', 9, 0x00, 0x03};
    static const uint8_t IMAGE[EN_TETE_OCTETS] = {'G', 'I', 'F', '8', '9', 'a', 0x10};
    static const uint8_t TEXTE[] = "version 2\n";
    static const uint8_t COUPE[3] = {'C', 'L', 'N'};

    EnTete en_tete = {0, 0};

    VERIFIE(memcmp(VALIDE, MAGIE, MAGIE_OCTETS) == 0,
            "le fichier de test porte la magie du format");

    VERIFIE_ENTIER(etat_de(VALIDE, sizeof VALIDE, &en_tete), EN_TETE_OK,
                   "un en-tete conforme est accepte");
    VERIFIE_ENTIER(en_tete.version, VERSION_COURANTE, "avec la version courante");
    VERIFIE_ENTIER(en_tete.nombre, 300, "et le nombre d'enregistrements annonce");

    VERIFIE_ENTIER(etat_de(IMAGE, sizeof IMAGE, &en_tete), EN_TETE_MAGIE,
                   "une image qui passait par la est refusee sur la magie");
    VERIFIE_ENTIER(etat_de(TEXTE, sizeof TEXTE - 1, &en_tete), EN_TETE_MAGIE,
                   "un fichier texte aussi, meme s'il parle de version");
    VERIFIE_ENTIER(etat_de(FUTUR, sizeof FUTUR, &en_tete), EN_TETE_VERSION,
                   "une version plus recente est refusee, et nommee");
    VERIFIE_ENTIER(etat_de(ANCIEN, sizeof ANCIEN, &en_tete), EN_TETE_VERSION,
                   "une version plus ancienne aussi, tant que personne ne l'a implementee");
    VERIFIE_ENTIER(etat_de(COUPE, sizeof COUPE, &en_tete), EN_TETE_TRONQUE,
                   "un en-tete incomplet est signale comme tel");
    VERIFIE_ENTIER(etat_de(VALIDE, 0, &en_tete), EN_TETE_TRONQUE, "un fichier vide egalement");

    return BILAN();
}
