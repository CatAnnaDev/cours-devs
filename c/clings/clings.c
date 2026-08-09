#include <dirent.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/stat.h>
#include <sys/wait.h>
#include <time.h>
#include <unistd.h>

#include "catalogue.h"

#define ROUGE "\033[31m"
#define VERT "\033[32m"
#define JAUNE "\033[33m"
#define BLEU "\033[34m"
#define GRIS "\033[90m"
#define GRAS "\033[1m"
#define FIN "\033[0m"

#define TRAVAIL ".travail"
#define CHEMIN_MAX 1024
#define COMMANDE_MAX 4096

typedef struct {
    int compile;
    int code;
    int signal_recu;
    char *journal;
    char *sortie;
} Resultat;

static char *lire_fichier(const char *chemin) {
    FILE *fichier = fopen(chemin, "rb");
    if (fichier == NULL) {
        return NULL;
    }
    fseek(fichier, 0, SEEK_END);
    long taille = ftell(fichier);
    fseek(fichier, 0, SEEK_SET);
    if (taille < 0) {
        fclose(fichier);
        return NULL;
    }
    char *contenu = malloc((size_t)taille + 1);
    if (contenu == NULL) {
        fclose(fichier);
        return NULL;
    }
    size_t lus = fread(contenu, 1, (size_t)taille, fichier);
    contenu[lus] = '\0';
    fclose(fichier);
    return contenu;
}

static void separateur(void) {
    printf(GRIS);
    for (int i = 0; i < 72; i++) {
        putchar('-');
    }
    printf(FIN "\n");
}

static void bloc(const char *titre, const char *corps, const char *couleur) {
    printf("\n%s%s%s%s\n", couleur, GRAS, titre, FIN);
    const char *debut = corps;
    while (*debut != '\0') {
        const char *fin_ligne = strchr(debut, '\n');
        int longueur = fin_ligne ? (int)(fin_ligne - debut) : (int)strlen(debut);
        printf("  %.*s\n", longueur, debut);
        if (fin_ligne == NULL) {
            break;
        }
        debut = fin_ligne + 1;
    }
}

static void message(const char *couleur, const char *texte) {
    printf("\n%s%s%s%s\n", couleur, GRAS, texte, FIN);
}

static void chemin_de(char *tampon, const char *dossier, const Exercice *exercice) {
    snprintf(tampon, CHEMIN_MAX, "%s/%s/%s", dossier, exercice->section, exercice->fichier);
}

static int est_termine(const char *chemin) {
    char *contenu = lire_fichier(chemin);
    if (contenu == NULL) {
        return 0;
    }
    const char *marque = strstr(contenu, "PAS_FINI");
    int termine = 0;
    if (marque != NULL) {
        const char *curseur = marque + strlen("PAS_FINI");
        while (*curseur == ' ' || *curseur == '\t' || *curseur == '=') {
            curseur++;
        }
        termine = (*curseur == '0');
    }
    free(contenu);
    return termine;
}

static long date_modification(const char *chemin) {
    struct stat infos;
    if (stat(chemin, &infos) != 0) {
        return -1;
    }
    return (long)infos.st_mtime;
}

static void liberer_resultat(Resultat *resultat) {
    free(resultat->journal);
    free(resultat->sortie);
    resultat->journal = NULL;
    resultat->sortie = NULL;
}

static Resultat compiler_et_lancer(const char *source, const char *nom) {
    Resultat resultat = {0, -1, 0, NULL, NULL};
    char commande[COMMANDE_MAX];
    char binaire[CHEMIN_MAX];
    char journal[CHEMIN_MAX];
    char sortie[CHEMIN_MAX];

    mkdir(TRAVAIL, 0755);
    snprintf(binaire, sizeof binaire, "%s/%s", TRAVAIL, nom);
    snprintf(journal, sizeof journal, "%s/%s.compilation", TRAVAIL, nom);
    snprintf(sortie, sizeof sortie, "%s/%s.execution", TRAVAIL, nom);

    const char *compilateur = getenv("CLINGS_CC");
    if (compilateur == NULL) {
        compilateur = "cc";
    }

    snprintf(commande, sizeof commande,
             "\"%s\" -std=c17 -g -O0 -fno-omit-frame-pointer "
             "-fsanitize=address,undefined -fno-sanitize-recover=undefined "
             "-Wall -Wextra -Wno-unused-function -I. "
             "\"%s\" -o \"%s\" > \"%s\" 2>&1",
             compilateur, source, binaire, journal);

    int etat = system(commande);
    resultat.journal = lire_fichier(journal);
    if (resultat.journal == NULL) {
        resultat.journal = calloc(1, 1);
    }

    if (!WIFEXITED(etat) || WEXITSTATUS(etat) != 0) {
        resultat.compile = 0;
        return resultat;
    }

    resultat.compile = 1;
    snprintf(commande, sizeof commande,
             "ASAN_OPTIONS=detect_stack_use_after_return=1 \"%s\" > \"%s\" 2>&1", binaire,
             sortie);
    etat = system(commande);
    resultat.sortie = lire_fichier(sortie);
    if (resultat.sortie == NULL) {
        resultat.sortie = calloc(1, 1);
    }

    if (WIFEXITED(etat)) {
        resultat.code = WEXITSTATUS(etat);
    } else if (WIFSIGNALED(etat)) {
        resultat.code = -1;
        resultat.signal_recu = WTERMSIG(etat);
    }
    return resultat;
}

static const Exercice *par_id(const char *id) {
    for (int i = 0; i < CATALOGUE_TAILLE; i++) {
        if (strcmp(CATALOGUE[i].id, id) == 0) {
            return &CATALOGUE[i];
        }
    }
    return NULL;
}

static int position_de(const Exercice *exercice) {
    for (int i = 0; i < CATALOGUE_TAILLE; i++) {
        if (&CATALOGUE[i] == exercice) {
            return i + 1;
        }
    }
    return 0;
}

static void entete(const Exercice *exercice) {
    separateur();
    printf("%s%s%s%s  %s%s%s   %s%d / %d%s\n", GRAS, BLEU, exercice->section, FIN, GRAS,
           exercice->titre, FIN, GRIS, position_de(exercice), CATALOGUE_TAILLE, FIN);
    printf("%s%s%s\n", GRIS, exercice->id, FIN);
    separateur();
}

static void afficher_resultat(const Resultat *resultat, const Exercice *exercice, int termine) {
    if (!resultat->compile) {
        message(ROUGE, "ca ne compile pas");
        printf("%s\n", resultat->journal);
        printf("%sindice : ./clings hint %s%s\n", GRIS, exercice->id, FIN);
        return;
    }

    if (resultat->journal != NULL && resultat->journal[0] != '\0') {
        bloc("avertissements du compilateur", resultat->journal, JAUNE);
    }

    if (resultat->sortie != NULL && resultat->sortie[0] != '\0') {
        printf("\n%s", resultat->sortie);
    }

    if (resultat->signal_recu != 0) {
        char texte[128];
        snprintf(texte, sizeof texte, "le programme a ete tue par le signal %d",
                 resultat->signal_recu);
        message(ROUGE, texte);
        printf("%sun signal 6 vient presque toujours d'un sanitizer : lis le rapport ci-dessus%s\n",
               GRIS, FIN);
        printf("%sindice : ./clings hint %s%s\n", GRIS, exercice->id, FIN);
        return;
    }

    if (resultat->code == 0) {
        if (termine) {
            message(VERT, "tout passe");
        } else {
            message(JAUNE, "tout passe : mets PAS_FINI a 0 et sauvegarde");
        }
    } else if (resultat->code == 3) {
        message(JAUNE, "tout passe : mets PAS_FINI a 0 et sauvegarde");
    } else {
        char texte[128];
        snprintf(texte, sizeof texte, "des verifications ont rate (code %d)", resultat->code);
        message(ROUGE, texte);
        printf("%sindice : ./clings hint %s%s\n", GRIS, exercice->id, FIN);
    }
}

static void commande_list(void) {
    const char *section = "";
    int termines = 0;

    for (int i = 0; i < CATALOGUE_TAILLE; i++) {
        const Exercice *exercice = &CATALOGUE[i];
        if (strcmp(exercice->section, section) != 0) {
            section = exercice->section;
            printf("\n%s%s%s%s\n", GRAS, BLEU, section, FIN);
        }
        char chemin[CHEMIN_MAX];
        chemin_de(chemin, "exercices", exercice);
        int fini = est_termine(chemin);
        termines += fini;
        printf("  %s%s%s  %-14s %s\n", fini ? VERT : GRIS, fini ? "fait" : "....", FIN,
               exercice->id, exercice->titre);
    }

    printf("\n%s%d / %d exercices termines%s\n", GRAS, termines, CATALOGUE_TAILLE, FIN);
}

static void commande_hint(const char *id) {
    const Exercice *exercice = par_id(id);
    if (exercice == NULL) {
        message(ROUGE, "exercice inconnu");
        return;
    }
    bloc("indice", exercice->indice, JAUNE);
}

static void commande_solution(const char *id) {
    const Exercice *exercice = par_id(id);
    if (exercice == NULL) {
        message(ROUGE, "exercice inconnu");
        return;
    }
    char chemin[CHEMIN_MAX];
    chemin_de(chemin, "solutions", exercice);
    char *contenu = lire_fichier(chemin);
    if (contenu == NULL) {
        message(ROUGE, "pas de solution pour cet exercice");
        return;
    }
    separateur();
    printf("%s%s%s\n", GRIS, chemin, FIN);
    separateur();
    printf("%s\n", contenu);
    free(contenu);
}

static void commande_reset(const char *id) {
    const Exercice *exercice = par_id(id);
    if (exercice == NULL) {
        message(ROUGE, "exercice inconnu");
        return;
    }
    char origine[CHEMIN_MAX];
    char cible[CHEMIN_MAX];
    chemin_de(origine, "origines", exercice);
    chemin_de(cible, "exercices", exercice);

    char *contenu = lire_fichier(origine);
    if (contenu == NULL) {
        message(ROUGE, "aucune copie d'origine trouvee");
        return;
    }
    FILE *fichier = fopen(cible, "wb");
    if (fichier == NULL) {
        message(ROUGE, "impossible d'ecrire l'exercice");
        free(contenu);
        return;
    }
    fputs(contenu, fichier);
    fclose(fichier);
    free(contenu);
    message(VERT, "exercice remis a zero");
}

static void commande_run(const char *id) {
    const Exercice *exercice = par_id(id);
    if (exercice == NULL) {
        message(ROUGE, "exercice inconnu");
        return;
    }
    char chemin[CHEMIN_MAX];
    chemin_de(chemin, "exercices", exercice);
    entete(exercice);
    bloc("consigne", exercice->consigne, BLEU);
    Resultat resultat = compiler_et_lancer(chemin, exercice->id);
    afficher_resultat(&resultat, exercice, est_termine(chemin));
    liberer_resultat(&resultat);
}

static int commande_verify(void) {
    int casses = 0;

    for (int i = 0; i < CATALOGUE_TAILLE; i++) {
        const Exercice *exercice = &CATALOGUE[i];
        char chemin[CHEMIN_MAX];
        char nom[256];
        chemin_de(chemin, "solutions", exercice);
        snprintf(nom, sizeof nom, "solution_%s", exercice->id);

        Resultat resultat = compiler_et_lancer(chemin, nom);
        int ok = resultat.compile && resultat.code == 0 && resultat.signal_recu == 0;

        printf("  %s%s%s  %s\n", ok ? VERT : ROUGE, ok ? "ok  " : "RATE", FIN, exercice->id);
        if (!ok) {
            casses++;
            printf("%s\n", resultat.compile ? resultat.sortie : resultat.journal);
        }
        liberer_resultat(&resultat);
    }

    if (casses == 0) {
        char texte[128];
        snprintf(texte, sizeof texte, "les %d solutions passent", CATALOGUE_TAILLE);
        message(VERT, texte);
        return 0;
    }
    message(ROUGE, "des solutions sont en echec");
    return 1;
}

static void commande_quiz(const char *filtre) {
    int total = 0;
    int justes = 0;
    char ligne[64];

    for (int i = 0; i < QUESTIONS_TAILLE; i++) {
        const Question *question = &QUESTIONS[i];
        if (filtre != NULL && strcmp(question->section, filtre) != 0) {
            continue;
        }
        total++;

        separateur();
        printf("%s%s  question %d%s\n\n", GRIS, question->section, total, FIN);
        printf("%s%s%s\n\n", GRAS, question->enonce, FIN);
        for (int r = 0; r < 4; r++) {
            printf("  %d. %s\n", r + 1, question->reponses[r]);
        }

        printf("\nta reponse (1-4, q pour quitter) : ");
        fflush(stdout);
        if (fgets(ligne, sizeof ligne, stdin) == NULL || ligne[0] == 'q') {
            break;
        }

        int choix = atoi(ligne) - 1;
        if (choix == question->bonne) {
            justes++;
            message(VERT, "juste");
        } else {
            char texte[64];
            snprintf(texte, sizeof texte, "faux, la bonne reponse est la %d", question->bonne + 1);
            message(ROUGE, texte);
        }
        bloc("pourquoi", question->explication, GRIS);
        printf("\n");
    }

    separateur();
    printf("\n%s%d / %d bonnes reponses%s\n", GRAS, justes, total, FIN);
}

static void commande_quiz_list(void) {
    const char *section = "";
    int compte = 0;

    for (int i = 0; i < QUESTIONS_TAILLE; i++) {
        if (strcmp(QUESTIONS[i].section, section) != 0) {
            if (compte > 0) {
                printf("  %-16s %d question(s)\n", section, compte);
            }
            section = QUESTIONS[i].section;
            compte = 0;
        }
        compte++;
    }
    if (compte > 0) {
        printf("  %-16s %d question(s)\n", section, compte);
    }
}

static void aide(void) {
    printf("\n%sclings%s — apprendre le C en reparant du code casse\n\n", GRAS, FIN);
    printf("  %s./clings%s                reprend au premier exercice non termine\n", GRAS, FIN);
    printf("  %s./clings list%s           ou j'en suis\n", GRAS, FIN);
    printf("  %s./clings run <id>%s       relancer un exercice precis\n", GRAS, FIN);
    printf("  %s./clings hint <id>%s      un indice\n", GRAS, FIN);
    printf("  %s./clings solution <id>%s  la correction\n", GRAS, FIN);
    printf("  %s./clings reset <id>%s     remettre l'exercice dans son etat d'origine\n", GRAS, FIN);
    printf("  %s./clings verify%s         verifier que toutes les solutions passent\n", GRAS, FIN);
    printf("  %s./clings quiz%s           le questionnaire\n", GRAS, FIN);
    printf("  %s./clings quiz list%s      combien de questions par section\n\n", GRAS, FIN);
}

static void boucle_principale(void) {
    while (1) {
        const Exercice *courant = NULL;
        char chemin[CHEMIN_MAX];

        for (int i = 0; i < CATALOGUE_TAILLE; i++) {
            chemin_de(chemin, "exercices", &CATALOGUE[i]);
            if (!est_termine(chemin)) {
                courant = &CATALOGUE[i];
                break;
            }
        }

        if (courant == NULL) {
            printf("\033[2J\033[H");
            message(VERT, "tous les exercices sont termines.");
            printf("%slance maintenant : ./clings quiz%s\n", GRIS, FIN);
            return;
        }

        chemin_de(chemin, "exercices", courant);
        long empreinte = date_modification(chemin);

        printf("\033[2J\033[H");
        entete(courant);
        printf("%s%s%s\n", GRIS, chemin, FIN);
        bloc("consigne", courant->consigne, BLEU);

        Resultat resultat = compiler_et_lancer(chemin, courant->id);
        int termine = est_termine(chemin);
        afficher_resultat(&resultat, courant, termine);
        int reussi = resultat.compile && resultat.code == 0 && resultat.signal_recu == 0;
        liberer_resultat(&resultat);

        if (reussi && termine) {
            message(VERT, "exercice termine, on passe au suivant");
            struct timespec pause = {0, 900000000L};
            nanosleep(&pause, NULL);
            continue;
        }

        printf("\n%ssauvegarde le fichier pour relancer   —   ctrl-c pour arreter%s\n", GRIS, FIN);

        while (1) {
            struct timespec pause = {0, 200000000L};
            nanosleep(&pause, NULL);
            long actuelle = date_modification(chemin);
            if (actuelle != empreinte && actuelle != -1) {
                break;
            }
        }
    }
}

int main(int argc, char **argv) {
    struct stat infos;
    if (stat("exercices", &infos) != 0) {
        fprintf(stderr, "lance clings depuis le dossier qui contient 'exercices'\n");
        return 2;
    }

    if (argc < 2) {
        boucle_principale();
        return 0;
    }

    const char *commande = argv[1];
    const char *argument = argc > 2 ? argv[2] : NULL;

    if (strcmp(commande, "list") == 0) {
        commande_list();
    } else if (strcmp(commande, "run") == 0 && argument != NULL) {
        commande_run(argument);
    } else if (strcmp(commande, "hint") == 0 && argument != NULL) {
        commande_hint(argument);
    } else if (strcmp(commande, "solution") == 0 && argument != NULL) {
        commande_solution(argument);
    } else if (strcmp(commande, "reset") == 0 && argument != NULL) {
        commande_reset(argument);
    } else if (strcmp(commande, "verify") == 0) {
        return commande_verify();
    } else if (strcmp(commande, "quiz") == 0) {
        if (argument != NULL && strcmp(argument, "list") == 0) {
            commande_quiz_list();
        } else {
            commande_quiz(argument);
        }
    } else {
        aide();
    }

    return 0;
}
