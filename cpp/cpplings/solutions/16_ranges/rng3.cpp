#include <ranges>
#include <vector>

#include "verif.hpp"

const bool PAS_FINI = false;

namespace {

int appels_du_filtre = 0;
int appels_de_la_transformation = 0;

void remettre_les_compteurs_a_zero() {
    appels_du_filtre = 0;
    appels_de_la_transformation = 0;
}

bool multiple_de_trois(int valeur) {
    appels_du_filtre++;
    return valeur % 3 == 0;
}

int au_carre(int valeur) {
    appels_de_la_transformation++;
    return valeur * valeur;
}

auto chaine(const std::vector<int> &source) {
    return source | std::views::filter(multiple_de_trois) | std::views::transform(au_carre);
}

}

int main() {
    const std::vector<int> donnees{1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12};

    remettre_les_compteurs_a_zero();
    auto vue = chaine(donnees);

    VERIFIE_ENTIER(appels_du_filtre, 0, "construire la chaine n'appelle pas le predicat");
    VERIFIE_ENTIER(appels_de_la_transformation, 0, "ni la transformation");

    VERIFIE_ENTIER(*vue.begin(), 9, "le premier element demande vaut 3 au carre");
    VERIFIE_ENTIER(appels_du_filtre, 3,
                   "begin() a teste 1, 2 et 3 : juste assez pour trouver le premier retenu");
    VERIFIE_ENTIER(appels_de_la_transformation, 1, "et n'a transforme que celui-la");

    VERIFIE_ENTIER(*vue.begin(), 9, "relire le premier element redonne la meme valeur");
    VERIFIE_ENTIER(appels_du_filtre, 3,
                   "filter_view garde son begin() en cache : pas un test de predicat de plus");
    VERIFIE_ENTIER(appels_de_la_transformation, 2, "mais la transformation, elle, est refaite");

    remettre_les_compteurs_a_zero();
    long long total = 0;
    for (int valeur : vue) {
        total += valeur;
    }

    VERIFIE_ENTIER(total, 270, "9 + 36 + 81 + 144");
    VERIFIE_ENTIER(appels_du_filtre, 9, "le parcours reprend au cache et teste les neuf restants");
    VERIFIE_ENTIER(appels_de_la_transformation, 4, "un appel par element effectivement lu");

    remettre_les_compteurs_a_zero();
    auto arretee_tot = chaine(donnees);
    long long partiel = 0;
    for (int valeur : arretee_tot) {
        partiel += valeur;
        if (valeur > 20) {
            break;
        }
    }

    VERIFIE_ENTIER(partiel, 45, "9 + 36, le parcours s'arrete la");
    VERIFIE_ENTIER(appels_du_filtre, 6,
                   "le predicat n'a vu que 1 a 6 : rien n'est calcule d'avance");
    VERIFIE_ENTIER(appels_de_la_transformation, 2, "deux elements lus, deux transformations");

    remettre_les_compteurs_a_zero();
    auto deux_premiers = chaine(donnees) | std::views::take(2);
    long long tete = 0;
    for (int valeur : deux_premiers) {
        tete += valeur;
    }

    VERIFIE_ENTIER(tete, 45, "take(2) donne les deux memes valeurs");
    VERIFIE_ENTIER(appels_de_la_transformation, 2, "et ne transforme que deux elements");
    VERIFIE_ENTIER(appels_du_filtre, 9,
                   "mais l'increment de filter_view cherche le suivant tout de suite : 9 tests");

    remettre_les_compteurs_a_zero();
    auto relue = chaine(donnees);
    long long premier_passage = 0;
    for (int valeur : relue) {
        premier_passage += valeur;
    }
    long long second_passage = 0;
    for (int valeur : relue) {
        second_passage += valeur;
    }

    VERIFIE_ENTIER(premier_passage, second_passage, "une vue relue donne le meme resultat");
    VERIFIE_ENTIER(appels_du_filtre, 21, "mais elle recalcule tout : 12 tests puis 9");
    VERIFIE_ENTIER(appels_de_la_transformation, 8, "une vue ne memorise rien, elle recalcule");

    return BILAN();
}
