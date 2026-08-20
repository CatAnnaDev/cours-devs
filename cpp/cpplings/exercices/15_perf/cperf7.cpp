#include <algorithm>
#include <cstddef>
#include <cstdint>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

constexpr std::size_t TAILLE_DE_LIGNE = 128;
constexpr std::size_t TAILLE_DE_LA_TABLE = 65536;
constexpr std::size_t NOMBRE_D_ENTREES = 1024;
constexpr std::size_t PAS_ENTRE_LES_ENTREES = TAILLE_DE_LIGNE / sizeof(std::uint16_t);
constexpr std::size_t LIGNES_DES_ENTREES = NOMBRE_D_ENTREES * sizeof(std::uint32_t) /
                                           TAILLE_DE_LIGNE;
constexpr std::size_t LIGNES_DE_LA_TABLE = TAILLE_DE_LA_TABLE * sizeof(std::uint16_t) /
                                           TAILLE_DE_LIGNE;

alignas(TAILLE_DE_LIGNE) std::uint16_t TABLE_DES_BITS[TAILLE_DE_LA_TABLE];
alignas(TAILLE_DE_LIGNE) std::uint32_t ENTREES[NOMBRE_D_ENTREES];

std::uintptr_t ligne_de(const void *adresse) {
    return reinterpret_cast<std::uintptr_t>(adresse) / TAILLE_DE_LIGNE;
}

std::size_t lignes_distinctes(std::vector<std::uintptr_t> lignes) {
    std::sort(lignes.begin(), lignes.end());
    return (std::size_t)(std::unique(lignes.begin(), lignes.end()) - lignes.begin());
}

std::uint16_t bits_par_calcul(std::uint32_t valeur) {
    valeur = valeur - ((valeur >> 1) & 0x5555u);
    valeur = (valeur & 0x3333u) + ((valeur >> 2) & 0x3333u);
    valeur = (valeur + (valeur >> 4)) & 0x0f0fu;
    return (std::uint16_t)((valeur + (valeur >> 8)) & 0x1fu);
}

void preparer() {
    for (std::size_t rang = 0; rang < TAILLE_DE_LA_TABLE; rang++) {
        TABLE_DES_BITS[rang] = bits_par_calcul((std::uint32_t)rang);
    }
    for (std::size_t rang = 0; rang < NOMBRE_D_ENTREES; rang++) {
        ENTREES[rang] = (std::uint32_t)(rang * PAS_ENTRE_LES_ENTREES);
    }
}

long long sommer_par_la_table(std::vector<std::uintptr_t> &lignes) {
    long long total = 0;
    for (std::size_t rang = 0; rang < NOMBRE_D_ENTREES; rang++) {
        lignes.push_back(ligne_de(&ENTREES[rang]));
        const std::uint32_t valeur = ENTREES[rang];
        lignes.push_back(ligne_de(&TABLE_DES_BITS[valeur]));
        total += TABLE_DES_BITS[valeur];
    }
    return total;
}

long long sommer_par_le_calcul(std::vector<std::uintptr_t> &lignes) {
    long long total = 0;
    for (std::size_t rang = 0; rang < NOMBRE_D_ENTREES; rang++) {
        lignes.push_back(ligne_de(&ENTREES[rang]));
        total += bits_par_calcul(ENTREES[rang]);
    }
    return total;
}

long long sommer_le_journal(std::vector<std::uintptr_t> &lignes) {
    return sommer_par_la_table(lignes);
}

}

int main() {
    preparer();

    std::vector<std::uintptr_t> lignes_de_la_table;
    lignes_de_la_table.reserve(2 * NOMBRE_D_ENTREES);
    const long long somme_par_la_table = sommer_par_la_table(lignes_de_la_table);
    const std::size_t touchees_par_la_table = lignes_distinctes(lignes_de_la_table);

    std::vector<std::uintptr_t> lignes_du_calcul;
    lignes_du_calcul.reserve(NOMBRE_D_ENTREES);
    const long long somme_par_le_calcul = sommer_par_le_calcul(lignes_du_calcul);
    const std::size_t touchees_par_le_calcul = lignes_distinctes(lignes_du_calcul);

    std::vector<std::uintptr_t> lignes_du_journal;
    lignes_du_journal.reserve(2 * NOMBRE_D_ENTREES);
    const long long somme_du_journal = sommer_le_journal(lignes_du_journal);
    const std::size_t touchees_par_le_journal = lignes_distinctes(lignes_du_journal);

    VERIFIE_ENTIER(somme_par_la_table, 5120,
                   "mille vingt-quatre entrees, cinq mille cent vingt bits");
    VERIFIE_ENTIER(somme_par_le_calcul, somme_par_la_table,
                   "la table et le calcul repondent exactement la meme chose");
    VERIFIE_ENTIER(somme_du_journal, somme_par_la_table, "le journal rend le meme total");

    VERIFIE_ENTIER(touchees_par_le_calcul, LIGNES_DES_ENTREES,
                   "le calcul ne traverse que les entrees : trente-deux lignes");
    VERIFIE_ENTIER(touchees_par_la_table, LIGNES_DES_ENTREES + LIGNES_DE_LA_TABLE,
                   "la table ajoute ses mille vingt-quatre lignes, entierement traversee");
    VERIFIE_ENTIER(touchees_par_le_calcul * TAILLE_DE_LIGNE, 4096,
                   "quatre kilo-octets remontes du cache pour le calcul");
    VERIFIE_ENTIER(touchees_par_la_table * TAILLE_DE_LIGNE, 135168,
                   "cent trente-deux kilo-octets pour la table reputee plus rapide");
    VERIFIE(touchees_par_la_table > 30 * touchees_par_le_calcul,
            "trente-trois fois plus de memoire traversee pour economiser quatre instructions");

    VERIFIE_ENTIER(touchees_par_le_journal, LIGNES_DES_ENTREES,
                   "le journal doit calculer, pas consulter la table");
    return BILAN();
}
