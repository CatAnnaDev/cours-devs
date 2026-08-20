#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

constexpr int TOURS_DE_CAPTURE = 4;

struct Releve {
    verif::Sonde temoin;
    int mesure = 0;
};

int poids(const verif::Sonde &sonde) { return sonde.valeur * 2; }

const verif::Sonde &temoin_de(const Releve &releve) { return releve.temoin; }

int somme_capturee(const verif::Sonde &sonde, int tours) {
    const auto accumuler = [&sonde](int total) { return total + sonde.valeur; };
    int total = 0;
    for (int tour = 0; tour < tours; tour++) {
        total = accumuler(total);
    }
    return total;
}

}

int main() {
    Releve releve;
    releve.temoin.valeur = 21;
    releve.mesure = 21;

    verif::Compteur::remettre_a_zero();
    const int double_de_la_mesure = poids(releve.temoin);
    const int copies_du_parametre = verif::Compteur::copies;

    verif::Compteur::remettre_a_zero();
    const verif::Sonde &temoin = temoin_de(releve);
    const int copies_du_retour = verif::Compteur::copies;

    verif::Compteur::remettre_a_zero();
    const int somme = somme_capturee(releve.temoin, TOURS_DE_CAPTURE);
    const int copies_de_la_capture = verif::Compteur::copies;

    const int copies_totales = copies_du_parametre + copies_du_retour + copies_de_la_capture;

    VERIFIE_ENTIER(double_de_la_mesure, 42, "la fonction lit bien la sonde qu'on lui donne");
    VERIFIE_ENTIER(copies_du_parametre, 0,
                   "un parametre pris par reference constante ne copie rien");

    VERIFIE_ENTIER(temoin.valeur, 21, "le temoin rendu est bien celui du releve");
    VERIFIE(&temoin == &releve.temoin, "c'est le meme objet, pas un jumeau");
    VERIFIE_ENTIER(releve.mesure, temoin.valeur, "le releve porte sa mesure et son temoin");
    VERIFIE_ENTIER(copies_du_retour, 0, "rendre une reference constante ne copie rien");

    VERIFIE_ENTIER(somme, 84, "quatre tours a vingt et un");
    VERIFIE_ENTIER(copies_de_la_capture, 0, "une lambda qui capture par reference ne copie rien");

    VERIFIE_ENTIER(copies_totales, 0, "trois copies invisibles, trois copies de moins");
    return BILAN();
}
