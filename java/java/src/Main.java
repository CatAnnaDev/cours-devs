import lecons.*;

/**
 * neni_learn — ta base pour apprendre Java. (Je t'ai fait ça, Neniri. — anna)
 *
 * COMMENT LANCER (depuis le dossier "java/") :
 *   java src/Main.java        -> exécute TOUTES les leçons
 *   java src/Main.java 3      -> exécute UNIQUEMENT la leçon n°3
 *   java src/Tests.java       -> lance les exercices (à compléter dans src/exercices/)
 *
 * Pas besoin de compiler à la main : depuis Java 22, la commande `java` sait compiler et
 * exécuter directement un programme réparti en plusieurs fichiers. Lis le GUIDE.md pour le détail.
 */
public class Main {

    public static void main(String[] args) {
        // Si un numéro de leçon est passé en argument, on n'exécute que celle-ci.
        if (args.length >= 1) {
            try {
                int numero = Integer.parseInt(args[0]);
                lancer(numero);
            } catch (NumberFormatException e) {
                System.out.println("Argument invalide : « " + args[0]
                        + " ». Donne un numéro de leçon, ex. : java src/Main.java 3");
            }
            return;
        }

        // Sinon, on déroule toutes les leçons dans l'ordre.
        System.out.println("===========================================");
        System.out.println("  neni_learn (Java) — apprendre à programmer");
        System.out.println("===========================================");
        for (int n = 1; n <= 12; n++) {
            lancer(n);
        }
        System.out.println("\nFini ! Lance maintenant `java src/Tests.java` pour t'entraîner.");
    }

    /** Affiche le titre puis exécute la leçon demandée. */
    private static void lancer(int numero) {
        switch (numero) {
            case 1 -> { titre(1, "Variables et types"); Lecon01Variables.demo(); }
            case 2 -> { titre(2, "Opérateurs"); Lecon02Operateurs.demo(); }
            case 3 -> { titre(3, "Contrôle de flux (if / switch / boucles)"); Lecon03Controle.demo(); }
            case 4 -> { titre(4, "Méthodes (les fonctions en Java)"); Lecon04Methodes.demo(); }
            case 5 -> { titre(5, "Tableaux et listes"); Lecon05TableauxListes.demo(); }
            case 6 -> { titre(6, "Chaînes de caractères"); Lecon06Chaines.demo(); }
            case 7 -> { titre(7, "Classes, objets et enums"); Lecon07Classes.demo(); }
            case 8 -> { titre(8, "Gestion des erreurs (exceptions / Optional)"); Lecon08Erreurs.demo(); }
            case 9 -> { titre(9, "Valeurs, références et null"); Lecon09References.demo(); }
            case 10 -> { titre(10, "Collections en profondeur (ArrayList, HashMap, HashSet, mémoire)"); Lecon10Collections.demo(); }
            case 11 -> { titre(11, "Structurer une classe (static, public/private, final, getters/setters)"); Lecon11Encapsulation.demo(); }
            case 12 -> { titre(12, "Héritage et interfaces (extends, implements)"); Lecon12HeritageInterfaces.demo(); }
            default -> System.out.println("Leçon " + numero + " introuvable. Leçons disponibles : 1 à 12.");
        }
    }

    private static void titre(int numero, String texte) {
        System.out.println("\n--- Leçon " + numero + " : " + texte + " ---");
    }
}
