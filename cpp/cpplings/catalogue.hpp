#pragma once

#include <string_view>
#include <vector>

struct Exercice {
    std::string_view id;
    std::string_view section;
    std::string_view fichier;
    std::string_view titre;
    std::string_view consigne;
    std::string_view indice;
    std::string_view norme;
};

struct Question {
    std::string_view section;
    std::string_view enonce;
    std::string_view reponses[4];
    int bonne;
    std::string_view explication;
};

const std::vector<Exercice> &catalogue();
const std::vector<Question> &questions();
