package lecons;

/**
 * Leçon 3 — Le contrôle de flux.
 *
 * « Contrôler le flux » = décider quelles instructions exécuter, et combien de fois.
 * Outils : les conditions (if/else, switch) et les boucles (for, while, do-while).
 */
public class Lecon03Controle {

    public static void demo() {
        // --- if / else if / else ---
        int note = 14;
        if (note >= 16) {
            System.out.println("Très bien");
        } else if (note >= 12) {
            System.out.println("Bien");            // c'est cette branche qui s'exécute ici
        } else if (note >= 10) {
            System.out.println("Passable");
        } else {
            System.out.println("Insuffisant");
        }

        // L'opérateur ternaire : un if/else condensé en une expression.
        // syntaxe :  condition ? valeurSiVrai : valeurSiFaux
        String parite = (note % 2 == 0) ? "pair" : "impair";
        System.out.println("la note est " + parite);

        // --- switch : comparer une valeur à plusieurs cas ---
        int jour = 3;
        String nom = switch (jour) {
            case 1 -> "lundi";
            case 2 -> "mardi";
            case 3 -> "mercredi";
            case 6, 7 -> "week-end";       // plusieurs valeurs pour un même cas
            default -> "autre jour";       // « default » = tous les autres cas
        };
        System.out.println("jour " + jour + " = " + nom);

        // --- for : répéter un nombre connu de fois ---
        // (départ ; condition de continuation ; pas à chaque tour)
        System.out.print("compte à rebours : ");
        for (int i = 5; i >= 1; i--) {     // i-- veut dire « i = i - 1 »
            System.out.print(i + " ");
        }
        System.out.println("partez !");

        // --- while : répéter TANT QUE la condition est vraie ---
        int energie = 3;
        while (energie > 0) {
            System.out.println("énergie restante : " + energie);
            energie--;
        }

        // --- do-while : comme while, mais exécute AU MOINS UNE FOIS avant de tester ---
        int n = 1;
        do {
            n++;
        } while (n % 7 != 0 || n % 3 != 0);
        System.out.println("premier multiple commun de 3 et 7 : " + n);
    }
}
