#ifndef POIGNEE_H
#define POIGNEE_H

typedef struct Poignee Poignee;

Poignee *poignee_creer(int capacite);
int poignee_ajouter(Poignee *poignee, int valeur);
int poignee_total(const Poignee *poignee);
void poignee_detruire(Poignee *poignee);

#endif
