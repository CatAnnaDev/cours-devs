#include <chrono>
#include <cstdlib>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <sstream>
#include <string>
#include <thread>

#include <sys/wait.h>

#include "catalogue.hpp"

namespace {

constexpr std::string_view ROUGE = "\033[31m";
constexpr std::string_view VERT = "\033[32m";
constexpr std::string_view JAUNE = "\033[33m";
constexpr std::string_view BLEU = "\033[34m";
constexpr std::string_view GRIS = "\033[90m";
constexpr std::string_view GRAS = "\033[1m";
constexpr std::string_view FIN = "\033[0m";

const std::filesystem::path TRAVAIL = ".travail";

struct Resultat {
    bool compile = false;
    int code = -1;
    int signal_recu = 0;
    std::string journal;
    std::string sortie;

    bool reussi() const { return compile && code == 0 && signal_recu == 0; }
};

std::string lire_fichier(const std::filesystem::path &chemin) {
    std::ifstream fichier(chemin, std::ios::binary);
    if (!fichier) {
        return {};
    }
    std::ostringstream tampon;
    tampon << fichier.rdbuf();
    return tampon.str();
}

void separateur() {
    std::cout << GRIS << std::string(72, '-') << FIN << '\n';
}

void bloc(std::string_view titre, std::string_view corps, std::string_view couleur) {
    std::cout << '\n' << couleur << GRAS << titre << FIN << '\n';
    size_t debut = 0;
    while (debut <= corps.size()) {
        size_t fin = corps.find('\n', debut);
        if (fin == std::string_view::npos) {
            std::cout << "  " << corps.substr(debut) << '\n';
            break;
        }
        std::cout << "  " << corps.substr(debut, fin - debut) << '\n';
        debut = fin + 1;
    }
}

void message(std::string_view couleur, std::string_view texte) {
    std::cout << '\n' << couleur << GRAS << texte << FIN << '\n';
}

std::filesystem::path chemin_de(std::string_view dossier, const Exercice &exercice) {
    return std::filesystem::path(std::string(dossier)) / std::string(exercice.section) /
           std::string(exercice.fichier);
}

bool est_termine(const std::filesystem::path &chemin) {
    const std::string contenu = lire_fichier(chemin);
    const size_t marque = contenu.find("PAS_FINI");
    if (marque == std::string::npos) {
        return false;
    }
    size_t curseur = marque + 8;
    while (curseur < contenu.size() &&
           (contenu[curseur] == ' ' || contenu[curseur] == '\t' || contenu[curseur] == '=')) {
        curseur++;
    }
    return curseur < contenu.size() && contenu.compare(curseur, 5, "false") == 0;
}

std::string compilateur() {
    const char *choisi = std::getenv("CPPLINGS_CXX");
    return choisi != nullptr ? choisi : "c++";
}

Resultat compiler_et_lancer(const std::filesystem::path &source, std::string_view nom,
                            std::string_view norme) {
    Resultat resultat;
    std::filesystem::create_directories(TRAVAIL);

    const std::filesystem::path binaire = TRAVAIL / std::string(nom);
    const std::filesystem::path journal = TRAVAIL / (std::string(nom) + ".compilation");
    const std::filesystem::path sortie = TRAVAIL / (std::string(nom) + ".execution");

    std::ostringstream commande;
    commande << '"' << compilateur() << "\" -std=" << norme
             << " -g -O0 -fno-omit-frame-pointer"
                " -fsanitize=address,undefined -fno-sanitize-recover=undefined"
                " -Wall -Wextra -I. \""
             << source.string() << "\" -o \"" << binaire.string() << "\" > \"" << journal.string()
             << "\" 2>&1";

    const int etat_compilation = std::system(commande.str().c_str());
    resultat.journal = lire_fichier(journal);

    if (!WIFEXITED(etat_compilation) || WEXITSTATUS(etat_compilation) != 0) {
        return resultat;
    }
    resultat.compile = true;

    std::ostringstream execution;
    execution << "ASAN_OPTIONS=detect_stack_use_after_return=1 \"" << binaire.string() << "\" > \""
              << sortie.string() << "\" 2>&1";

    const int etat = std::system(execution.str().c_str());
    resultat.sortie = lire_fichier(sortie);

    if (WIFEXITED(etat)) {
        resultat.code = WEXITSTATUS(etat);
    } else if (WIFSIGNALED(etat)) {
        resultat.signal_recu = WTERMSIG(etat);
    }
    return resultat;
}

const Exercice *par_id(std::string_view id) {
    for (const Exercice &exercice : catalogue()) {
        if (exercice.id == id) {
            return &exercice;
        }
    }
    return nullptr;
}

int position_de(const Exercice &recherche) {
    int position = 1;
    for (const Exercice &exercice : catalogue()) {
        if (exercice.id == recherche.id) {
            return position;
        }
        position++;
    }
    return 0;
}

void entete(const Exercice &exercice) {
    separateur();
    std::cout << GRAS << BLEU << exercice.section << FIN << "  " << GRAS << exercice.titre << FIN
              << "   " << GRIS << position_de(exercice) << " / " << catalogue().size() << FIN
              << '\n';
    std::cout << GRIS << exercice.id << "   " << exercice.norme << FIN << '\n';
    separateur();
}

void afficher_resultat(const Resultat &resultat, const Exercice &exercice, bool termine) {
    if (!resultat.compile) {
        message(ROUGE, "ca ne compile pas");
        std::cout << resultat.journal << '\n';
        std::cout << GRIS << "indice : ./cpplings hint " << exercice.id << FIN << '\n';
        return;
    }

    if (!resultat.journal.empty()) {
        bloc("avertissements du compilateur", resultat.journal, JAUNE);
    }
    if (!resultat.sortie.empty()) {
        std::cout << '\n' << resultat.sortie;
    }

    if (resultat.signal_recu != 0) {
        message(ROUGE, "le programme a ete tue par le signal " + std::to_string(resultat.signal_recu));
        std::cout << GRIS
                  << "un signal 6 vient presque toujours d'un sanitizer : lis le rapport ci-dessus"
                  << FIN << '\n';
        std::cout << GRIS << "indice : ./cpplings hint " << exercice.id << FIN << '\n';
        return;
    }

    if (resultat.code == 0 && termine) {
        message(VERT, "tout passe");
    } else if (resultat.code == 0 || resultat.code == 3) {
        message(JAUNE, "tout passe : mets PAS_FINI a false et sauvegarde");
    } else {
        message(ROUGE, "des verifications ont rate (code " + std::to_string(resultat.code) + ")");
        std::cout << GRIS << "indice : ./cpplings hint " << exercice.id << FIN << '\n';
    }
}

void commande_list() {
    std::string_view section;
    int termines = 0;

    for (const Exercice &exercice : catalogue()) {
        if (exercice.section != section) {
            section = exercice.section;
            std::cout << '\n' << GRAS << BLEU << section << FIN << '\n';
        }
        const bool fini = est_termine(chemin_de("exercices", exercice));
        termines += fini ? 1 : 0;
        std::cout << "  " << (fini ? VERT : GRIS) << (fini ? "fait" : "....") << FIN << "  "
                  << exercice.id << std::string(16 - exercice.id.size(), ' ') << exercice.titre
                  << '\n';
    }

    std::cout << '\n'
              << GRAS << termines << " / " << catalogue().size() << " exercices termines" << FIN
              << '\n';
}

int commande_verify() {
    int casses = 0;

    for (const Exercice &exercice : catalogue()) {
        const Resultat resultat = compiler_et_lancer(chemin_de("solutions", exercice),
                                                     "solution_" + std::string(exercice.id),
                                                     exercice.norme);
        const bool ok = resultat.reussi();
        std::cout << "  " << (ok ? VERT : ROUGE) << (ok ? "ok  " : "RATE") << FIN << "  "
                  << exercice.id << '\n';
        if (!ok) {
            casses++;
            std::cout << (resultat.compile ? resultat.sortie : resultat.journal) << '\n';
        }
    }

    if (casses == 0) {
        message(VERT, "les " + std::to_string(catalogue().size()) + " solutions passent");
        return 0;
    }
    message(ROUGE, "des solutions sont en echec");
    return 1;
}

void commande_quiz(std::string_view filtre) {
    int total = 0;
    int justes = 0;

    for (const Question &question : questions()) {
        if (!filtre.empty() && question.section != filtre) {
            continue;
        }
        total++;
        separateur();
        std::cout << GRIS << question.section << "  question " << total << FIN << "\n\n";
        std::cout << GRAS << question.enonce << FIN << "\n\n";
        for (int r = 0; r < 4; r++) {
            std::cout << "  " << (r + 1) << ". " << question.reponses[r] << '\n';
        }

        std::cout << "\nta reponse (1-4, q pour quitter) : " << std::flush;
        std::string ligne;
        if (!std::getline(std::cin, ligne) || ligne == "q") {
            break;
        }

        const int choix = std::atoi(ligne.c_str()) - 1;
        if (choix == question.bonne) {
            justes++;
            message(VERT, "juste");
        } else {
            message(ROUGE, "faux, la bonne reponse est la " + std::to_string(question.bonne + 1));
        }
        bloc("pourquoi", question.explication, GRIS);
        std::cout << '\n';
    }

    separateur();
    std::cout << '\n' << GRAS << justes << " / " << total << " bonnes reponses" << FIN << '\n';
}

void commande_quiz_list() {
    std::string_view section;
    int compte = 0;
    for (const Question &question : questions()) {
        if (question.section != section) {
            if (compte > 0) {
                std::cout << "  " << section << "  " << compte << " question(s)\n";
            }
            section = question.section;
            compte = 0;
        }
        compte++;
    }
    if (compte > 0) {
        std::cout << "  " << section << "  " << compte << " question(s)\n";
    }
}

void aide() {
    std::cout << '\n' << GRAS << "cpplings" << FIN << " — apprendre le C++ en reparant du code casse\n\n";
    std::cout << "  ./cpplings                reprend au premier exercice non termine\n";
    std::cout << "  ./cpplings list           ou j'en suis\n";
    std::cout << "  ./cpplings run <id>       relancer un exercice precis\n";
    std::cout << "  ./cpplings hint <id>      un indice\n";
    std::cout << "  ./cpplings solution <id>  la correction\n";
    std::cout << "  ./cpplings reset <id>     remettre l'exercice dans son etat d'origine\n";
    std::cout << "  ./cpplings verify         verifier que toutes les solutions passent\n";
    std::cout << "  ./cpplings quiz           le questionnaire\n";
    std::cout << "  ./cpplings quiz list      combien de questions par section\n\n";
}

void boucle_principale() {
    while (true) {
        const Exercice *courant = nullptr;
        for (const Exercice &exercice : catalogue()) {
            if (!est_termine(chemin_de("exercices", exercice))) {
                courant = &exercice;
                break;
            }
        }

        if (courant == nullptr) {
            std::cout << "\033[2J\033[H";
            message(VERT, "tous les exercices sont termines.");
            std::cout << GRIS << "lance maintenant : ./cpplings quiz" << FIN << '\n';
            return;
        }

        const std::filesystem::path source = chemin_de("exercices", *courant);
        const auto empreinte = std::filesystem::last_write_time(source);

        std::cout << "\033[2J\033[H";
        entete(*courant);
        std::cout << GRIS << source.string() << FIN << '\n';
        bloc("consigne", courant->consigne, BLEU);

        const Resultat resultat = compiler_et_lancer(source, courant->id, courant->norme);
        const bool termine = est_termine(source);
        afficher_resultat(resultat, *courant, termine);

        if (resultat.reussi() && termine) {
            message(VERT, "exercice termine, on passe au suivant");
            std::this_thread::sleep_for(std::chrono::milliseconds(900));
            continue;
        }

        std::cout << '\n'
                  << GRIS << "sauvegarde le fichier pour relancer   —   ctrl-c pour arreter" << FIN
                  << '\n';

        while (std::filesystem::last_write_time(source) == empreinte) {
            std::this_thread::sleep_for(std::chrono::milliseconds(200));
        }
    }
}

}

int main(int argc, char **argv) {
    if (!std::filesystem::exists("exercices")) {
        std::cerr << "lance cpplings depuis le dossier qui contient 'exercices'\n";
        return 2;
    }

    if (argc < 2) {
        boucle_principale();
        return 0;
    }

    const std::string commande = argv[1];
    const std::string argument = argc > 2 ? argv[2] : "";

    if (commande == "list") {
        commande_list();
    } else if (commande == "verify") {
        return commande_verify();
    } else if (commande == "quiz") {
        if (argument == "list") {
            commande_quiz_list();
        } else {
            commande_quiz(argument);
        }
    } else if (argument.empty()) {
        aide();
    } else if (const Exercice *exercice = par_id(argument); exercice == nullptr) {
        message(ROUGE, "exercice inconnu");
    } else if (commande == "hint") {
        bloc("indice", exercice->indice, JAUNE);
    } else if (commande == "solution") {
        const auto chemin = chemin_de("solutions", *exercice);
        separateur();
        std::cout << GRIS << chemin.string() << FIN << '\n';
        separateur();
        std::cout << lire_fichier(chemin) << '\n';
    } else if (commande == "reset") {
        std::filesystem::copy_file(chemin_de("origines", *exercice),
                                   chemin_de("exercices", *exercice),
                                   std::filesystem::copy_options::overwrite_existing);
        message(VERT, "exercice remis a zero");
    } else if (commande == "run") {
        const auto chemin = chemin_de("exercices", *exercice);
        entete(*exercice);
        bloc("consigne", exercice->consigne, BLEU);
        const Resultat resultat = compiler_et_lancer(chemin, exercice->id, exercice->norme);
        afficher_resultat(resultat, *exercice, est_termine(chemin));
    } else {
        aide();
    }

    return 0;
}
