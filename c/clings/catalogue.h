#ifndef CATALOGUE_H
#define CATALOGUE_H

#include <stddef.h>

typedef struct {
    const char *id;
    const char *section;
    const char *fichier;
    const char *titre;
    const char *consigne;
    const char *indice;
    const char *annexes;
} Exercice;

typedef struct {
    const char *section;
    const char *enonce;
    const char *reponses[4];
    int bonne;
    const char *explication;
} Question;

extern const Exercice CATALOGUE[];
extern const int CATALOGUE_TAILLE;

extern const Question QUESTIONS[];
extern const int QUESTIONS_TAILLE;

#endif
