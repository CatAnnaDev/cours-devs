#include <array>
#include <cstddef>
#include <string_view>
#include <type_traits>

#include "verif.hpp"

const bool PAS_FINI = true;

namespace {

int tables_construites_a_l_execution = 0;

using TableCrc = std::array<unsigned, 256>;

constexpr TableCrc construire_table() {
    if (!std::is_constant_evaluated()) {
        tables_construites_a_l_execution++;
    }
    TableCrc table{};
    for (std::size_t octet = 0; octet < table.size(); octet++) {
        unsigned reste = static_cast<unsigned>(octet);
        for (int bit = 0; bit < 8; bit++) {
            reste = (reste & 1u) != 0u ? (0xEDB88320u ^ (reste >> 1)) : (reste >> 1);
        }
        table[octet] = reste;
    }
    return table;
}

constexpr TableCrc TABLE = construire_table();

constexpr unsigned crc32(std::string_view texte) {
    TableCrc table = construire_table();
    unsigned reste = 0xFFFFFFFFu;
    for (char lettre : texte) {
        reste = table[(reste ^ static_cast<unsigned char>(lettre)) & 0xFFu] ^ (reste >> 8);
    }
    return reste ^ 0xFFFFFFFFu;
}

std::string_view carotte_lue() { return "gneiss"; }

}

static_assert(TABLE[1] == 0x77073096u, "la case 1 de la table CRC-32 est deja calculee");
static_assert(TABLE[255] == 0x2D02EF8Du, "la derniere aussi");
static_assert(crc32("basalte") != 0u, "et le CRC d'un litteral se calcule a la compilation");

int main() {
    VERIFIE_ENTIER(TABLE[1], 0x77073096u, "les 2048 decalages sont dans le binaire, pas dans main");
    VERIFIE_ENTIER(crc32("basalte"), crc32("basalte"), "le calcul est deterministe");
    VERIFIE(crc32(carotte_lue()) != crc32("basalte"), "sur un texte d'execution, seul crc32 tourne");
    VERIFIE_ENTIER(tables_construites_a_l_execution, 0,
                   "aucune table construite a l'execution : la table est une constante");

    TableCrc refaite = construire_table();
    VERIFIE_ENTIER(tables_construites_a_l_execution, 1,
                   "appelee a l'execution, la meme fonction construit vraiment");
    VERIFIE(refaite == TABLE, "et rend exactement la table figee a la compilation");

    return BILAN();
}
