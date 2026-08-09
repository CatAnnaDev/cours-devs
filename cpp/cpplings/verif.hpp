#pragma once

#include <cmath>
#include <cstdio>
#include <cstdlib>
#include <string>
#include <string_view>

extern const bool PAS_FINI;

namespace verif {

inline int reussites = 0;
inline int echecs = 0;

inline void resultat(bool ok, std::string_view message, const char *fichier, int ligne) {
    if (ok) {
        reussites++;
        std::printf("  \033[32mok\033[0m    %.*s\n", (int)message.size(), message.data());
    } else {
        echecs++;
        std::printf("  \033[31mRATE\033[0m  %.*s\n", (int)message.size(), message.data());
        std::printf("        %s:%d\n", fichier, ligne);
    }
}

template <typename A, typename B>
void egal(const A &obtenu, const B &attendu, std::string_view message, const char *fichier,
          int ligne) {
    if (obtenu == attendu) {
        reussites++;
        std::printf("  \033[32mok\033[0m    %.*s\n", (int)message.size(), message.data());
    } else {
        echecs++;
        std::printf("  \033[31mRATE\033[0m  %.*s\n", (int)message.size(), message.data());
        std::printf("        %s:%d\n", fichier, ligne);
    }
}

inline void egal_entier(long long obtenu, long long attendu, std::string_view message,
                        const char *fichier, int ligne) {
    if (obtenu == attendu) {
        reussites++;
        std::printf("  \033[32mok\033[0m    %.*s\n", (int)message.size(), message.data());
    } else {
        echecs++;
        std::printf("  \033[31mRATE\033[0m  %.*s\n", (int)message.size(), message.data());
        std::printf("        attendu %lld, obtenu %lld\n", attendu, obtenu);
        std::printf("        %s:%d\n", fichier, ligne);
    }
}

inline void egal_texte(std::string_view obtenu, std::string_view attendu, std::string_view message,
                       const char *fichier, int ligne) {
    if (obtenu == attendu) {
        reussites++;
        std::printf("  \033[32mok\033[0m    %.*s\n", (int)message.size(), message.data());
    } else {
        echecs++;
        std::printf("  \033[31mRATE\033[0m  %.*s\n", (int)message.size(), message.data());
        std::printf("        attendu \"%.*s\", obtenu \"%.*s\"\n", (int)attendu.size(),
                    attendu.data(), (int)obtenu.size(), obtenu.data());
        std::printf("        %s:%d\n", fichier, ligne);
    }
}

inline void proche(double obtenu, double attendu, double tolerance, std::string_view message,
                   const char *fichier, int ligne) {
    if (std::fabs(obtenu - attendu) <= tolerance) {
        reussites++;
        std::printf("  \033[32mok\033[0m    %.*s\n", (int)message.size(), message.data());
    } else {
        echecs++;
        std::printf("  \033[31mRATE\033[0m  %.*s\n", (int)message.size(), message.data());
        std::printf("        attendu %g, obtenu %g\n", attendu, obtenu);
        std::printf("        %s:%d\n", fichier, ligne);
    }
}

inline int bilan(bool pas_fini) {
    std::printf("\n  %d verification(s) passee(s), %d ratee(s)\n", reussites, echecs);
    if (echecs > 0) {
        return 1;
    }
    return pas_fini ? 3 : 0;
}

inline long long a_faire(const char *fichier, int ligne) {
    std::printf("  \033[31mRATE\033[0m  un A_FAIRE n'a pas ete remplace\n");
    std::printf("        %s:%d\n", fichier, ligne);
    echecs++;
    return 0;
}

struct Compteur {
    static inline int constructions = 0;
    static inline int copies = 0;
    static inline int deplacements = 0;
    static inline int destructions = 0;

    static void remettre_a_zero() {
        constructions = 0;
        copies = 0;
        deplacements = 0;
        destructions = 0;
    }
};

struct Sonde {
    int valeur = 0;

    Sonde() { Compteur::constructions++; }
    explicit Sonde(int v) : valeur(v) { Compteur::constructions++; }

    Sonde(const Sonde &autre) : valeur(autre.valeur) { Compteur::copies++; }
    Sonde(Sonde &&autre) noexcept : valeur(autre.valeur) {
        autre.valeur = -1;
        Compteur::deplacements++;
    }

    Sonde &operator=(const Sonde &autre) {
        valeur = autre.valeur;
        Compteur::copies++;
        return *this;
    }
    Sonde &operator=(Sonde &&autre) noexcept {
        valeur = autre.valeur;
        autre.valeur = -1;
        Compteur::deplacements++;
        return *this;
    }

    ~Sonde() { Compteur::destructions++; }
};

}

#define VERIFIE(condition, message) verif::resultat((condition), message, __FILE__, __LINE__)
#define VERIFIE_EGAL(obtenu, attendu, message) \
    verif::egal((obtenu), (attendu), message, __FILE__, __LINE__)
#define VERIFIE_ENTIER(obtenu, attendu, message) \
    verif::egal_entier((long long)(obtenu), (long long)(attendu), message, __FILE__, __LINE__)
#define VERIFIE_TEXTE(obtenu, attendu, message) \
    verif::egal_texte((obtenu), (attendu), message, __FILE__, __LINE__)
#define VERIFIE_REEL(obtenu, attendu, message) \
    verif::proche((double)(obtenu), (double)(attendu), 1e-6, message, __FILE__, __LINE__)
#define VERIFIE_PROCHE(obtenu, attendu, tolerance, message) \
    verif::proche((double)(obtenu), (double)(attendu), (double)(tolerance), message, __FILE__, \
                  __LINE__)

#define A_FAIRE verif::a_faire(__FILE__, __LINE__)
#define BILAN() verif::bilan(PAS_FINI)
