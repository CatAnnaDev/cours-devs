import exercices.*;

import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.OptionalDouble;
import java.util.function.BooleanSupplier;

/**
 * Vérificateur d'exercices (l'équivalent de « cargo test » côté Rust).
 *
 * LANCER (depuis le dossier "java/") :  java src/Tests.java
 *
 * Pour chaque exercice, tu verras :
 *   [OK]   -> ta réponse est correcte
 *   [RATÉ] -> ta réponse compile mais donne un mauvais résultat
 *   [TODO] -> tu n'as pas encore remplacé le « throw ... UnsupportedOperationException »
 *   [ERR]  -> ton code a planté (une exception inattendue)
 *
 * Le but : tout passer en [OK] !
 */
public class Tests {

    static int reussis = 0;
    static int total = 0;

    public static void main(String[] args) {
        System.out.println("=== Exercices neni_learn (Java) ===");
        System.out.println("Objectif : tout passer en [OK].\n");

        ex01();
        ex02();
        ex03();
        ex04();
        ex05();
        ex06();
        ex07();
        ex08();
        ex09();

        System.out.println("\n-----------------------------------");
        System.out.printf("Résultat : %d / %d réussis%n", reussis, total);
        if (reussis == total) {
            System.out.println("Bravo, tout est au vert !");
        } else {
            System.out.println("Continue : ouvre src/exercices/ et complète les [TODO] puis corrige les [RATÉ].");
        }
    }

    private static void ex01() {
        titre("Exercice 01 — Variables et opérateurs");
        verifier("doubler(21) == 42", () -> Ex01VariablesOperateurs.doubler(21) == 42);
        verifier("doubler(-3) == -6", () -> Ex01VariablesOperateurs.doubler(-3) == -6);
        verifier("moyenne(10, 15) == 12.5", () -> Ex01VariablesOperateurs.moyenne(10, 15) == 12.5);
        verifier("estPair(4) est vrai", () -> Ex01VariablesOperateurs.estPair(4));
        verifier("estPair(7) est faux", () -> !Ex01VariablesOperateurs.estPair(7));
        verifier("estMajeur(18) est vrai", () -> Ex01VariablesOperateurs.estMajeur(18));
        verifier("estMajeur(17) est faux", () -> !Ex01VariablesOperateurs.estMajeur(17));
    }

    private static void ex02() {
        titre("Exercice 02 — Contrôle de flux");
        verifier("mention(18) == Très bien", () -> Ex02Controle.mention(18).equals("Très bien"));
        verifier("mention(14) == Bien", () -> Ex02Controle.mention(14).equals("Bien"));
        verifier("mention(13) == Assez bien", () -> Ex02Controle.mention(13).equals("Assez bien"));
        verifier("mention(11) == Passable", () -> Ex02Controle.mention(11).equals("Passable"));
        verifier("mention(5) == Insuffisant", () -> Ex02Controle.mention(5).equals("Insuffisant"));
        verifier("fizzbuzz(15) == FizzBuzz", () -> Ex02Controle.fizzbuzz(15).equals("FizzBuzz"));
        verifier("fizzbuzz(9) == Fizz", () -> Ex02Controle.fizzbuzz(9).equals("Fizz"));
        verifier("fizzbuzz(10) == Buzz", () -> Ex02Controle.fizzbuzz(10).equals("Buzz"));
        verifier("fizzbuzz(7) == 7", () -> Ex02Controle.fizzbuzz(7).equals("7"));
        verifier("sommeJusqua(5) == 15", () -> Ex02Controle.sommeJusqua(5) == 15);
        verifier("sommeJusqua(10) == 55", () -> Ex02Controle.sommeJusqua(10) == 55);
        verifier("factorielle(0) == 1", () -> Ex02Controle.factorielle(0) == 1);
        verifier("factorielle(5) == 120", () -> Ex02Controle.factorielle(5) == 120);
    }

    private static void ex03() {
        titre("Exercice 03 — Méthodes");
        verifier("maximum(3, 8) == 8", () -> Ex03Methodes.maximum(3, 8) == 8);
        verifier("maximum(-5, -1) == -1", () -> Ex03Methodes.maximum(-5, -1) == -1);
        verifier("carre(4) == 16", () -> Ex03Methodes.carre(4) == 16);
        verifier("carre(-3) == 9", () -> Ex03Methodes.carre(-3) == 9);
        verifier("valeurAbsolue(-7) == 7", () -> Ex03Methodes.valeurAbsolue(-7) == 7);
        verifier("valeurAbsolue(3) == 3", () -> Ex03Methodes.valeurAbsolue(3) == 3);
        verifier("estPremier(2) est vrai", () -> Ex03Methodes.estPremier(2));
        verifier("estPremier(13) est vrai", () -> Ex03Methodes.estPremier(13));
        verifier("estPremier(1) est faux", () -> !Ex03Methodes.estPremier(1));
        verifier("estPremier(9) est faux", () -> !Ex03Methodes.estPremier(9));
    }

    private static void ex04() {
        titre("Exercice 04 — Tableaux et listes");
        verifier("somme({1,2,3}) == 6", () -> Ex04TableauxListes.somme(new int[]{1, 2, 3}) == 6);
        verifier("somme({}) == 0", () -> Ex04TableauxListes.somme(new int[]{}) == 0);
        verifier("plusGrand({3,9,5}) == 9", () -> Ex04TableauxListes.plusGrand(new int[]{3, 9, 5}) == 9);
        verifier("plusGrand({-4,-2,-8}) == -2", () -> Ex04TableauxListes.plusGrand(new int[]{-4, -2, -8}) == -2);
        verifier("pairs({1,2,3,4}) == [2, 4]", () -> Ex04TableauxListes.pairs(new int[]{1, 2, 3, 4}).equals(List.of(2, 4)));
        verifier("pairs({1,3,5}) == []", () -> Ex04TableauxListes.pairs(new int[]{1, 3, 5}).equals(List.of()));
        verifier("compter({1,2,2,3,2}, 2) == 3", () -> Ex04TableauxListes.compter(new int[]{1, 2, 2, 3, 2}, 2) == 3);
        verifier("compter({1,2,3}, 9) == 0", () -> Ex04TableauxListes.compter(new int[]{1, 2, 3}, 9) == 0);
    }

    private static void ex05() {
        titre("Exercice 05 — Chaînes de caractères");
        verifier("longueur(\"Java\") == 4", () -> Ex05Chaines.longueur("Java") == 4);
        verifier("longueur(\"\") == 0", () -> Ex05Chaines.longueur("") == 0);
        verifier("crier(\"salut\") == SALUT", () -> Ex05Chaines.crier("salut").equals("SALUT"));
        verifier("inverser(\"Java\") == avaJ", () -> Ex05Chaines.inverser("Java").equals("avaJ"));
        verifier("estPalindrome(\"kayak\") est vrai", () -> Ex05Chaines.estPalindrome("kayak"));
        verifier("estPalindrome(\"Kayak\") est vrai", () -> Ex05Chaines.estPalindrome("Kayak"));
        verifier("estPalindrome(\"java\") est faux", () -> !Ex05Chaines.estPalindrome("java"));
    }

    private static void ex06() {
        titre("Exercice 06 — Classes, objets et enums");
        verifier("Rectangle(5,3).aire() == 15", () -> new Ex06Classes.Rectangle(5, 3).aire() == 15.0);
        verifier("Rectangle(5,3).perimetre() == 16", () -> new Ex06Classes.Rectangle(5, 3).perimetre() == 16.0);
        verifier("Rectangle(4,4).estCarre() est vrai", () -> new Ex06Classes.Rectangle(4, 4).estCarre());
        verifier("Rectangle(4,5).estCarre() est faux", () -> !new Ex06Classes.Rectangle(4, 5).estCarre());
        verifier("calculer(ADDITION, 6, 4) == 10", () -> Ex06Classes.calculer(Ex06Classes.Operation.ADDITION, 6, 4) == 10.0);
        verifier("calculer(SOUSTRACTION, 6, 4) == 2", () -> Ex06Classes.calculer(Ex06Classes.Operation.SOUSTRACTION, 6, 4) == 2.0);
        verifier("calculer(MULTIPLICATION, 6, 4) == 24", () -> Ex06Classes.calculer(Ex06Classes.Operation.MULTIPLICATION, 6, 4) == 24.0);
        verifier("calculer(DIVISION, 6, 4) == 1.5", () -> Ex06Classes.calculer(Ex06Classes.Operation.DIVISION, 6, 4) == 1.5);
        verifier("calculer(DIVISION, 6, 0) == 0", () -> Ex06Classes.calculer(Ex06Classes.Operation.DIVISION, 6, 0) == 0.0);
    }

    private static void ex07() {
        titre("Exercice 07 — Optional et exceptions");
        verifier("premier({7,8,9}) == Optional[7]", () -> Ex07Erreurs.premier(new int[]{7, 8, 9}).equals(Optional.of(7)));
        verifier("premier({}) est vide", () -> Ex07Erreurs.premier(new int[]{}).isEmpty());
        verifier("diviser(10, 2) == 5", () -> Ex07Erreurs.diviser(10, 2) == 5.0);
        verifier("diviser(1, 0) lève une erreur « division par zéro »", () -> {
            try {
                Ex07Erreurs.diviser(1, 0);
                return false; // aurait dû lever une exception
            } catch (ArithmeticException e) {
                return e.getMessage() != null && e.getMessage().contains("division par zéro");
            }
        });
        verifier("parserEntier(\"42\") == 42", () -> Ex07Erreurs.parserEntier("42") == 42);
        verifier("parserEntier(\"abc\") lève une erreur « nombre invalide »", () -> {
            try {
                Ex07Erreurs.parserEntier("abc");
                return false;
            } catch (IllegalArgumentException e) {
                return e.getMessage() != null && e.getMessage().contains("nombre invalide");
            }
        });
        verifier("inverse(4) == OptionalDouble[0.25]", () -> Ex07Erreurs.inverse(4).equals(OptionalDouble.of(0.25)));
        verifier("inverse(0) est vide", () -> Ex07Erreurs.inverse(0).isEmpty());
    }

    private static void ex08() {
        titre("Exercice 08 — Collections en profondeur");
        verifier("nbValeursUniques({1,2,2,3,3,3}) == 3", () -> Ex08Collections.nbValeursUniques(new int[]{1, 2, 2, 3, 3, 3}) == 3);
        verifier("nbValeursUniques({}) == 0", () -> Ex08Collections.nbValeursUniques(new int[]{}) == 0);
        verifier("contientDoublon({1,2,2}) est vrai", () -> Ex08Collections.contientDoublon(new int[]{1, 2, 2}));
        verifier("contientDoublon({1,2,3}) est faux", () -> !Ex08Collections.contientDoublon(new int[]{1, 2, 3}));
        verifier("compterOccurrences({a,b,a}) == {a=2, b=1}", () -> Ex08Collections.compterOccurrences(new String[]{"a", "b", "a"}).equals(Map.of("a", 2, "b", 1)));
        verifier("intersection({1,2,3,4},{2,4,6}) == [2, 4]", () -> Ex08Collections.intersection(new int[]{1, 2, 3, 4}, new int[]{2, 4, 6}).equals(List.of(2, 4)));
        verifier("intersection({1,2},{3,4}) == []", () -> Ex08Collections.intersection(new int[]{1, 2}, new int[]{3, 4}).equals(List.of()));
    }

    private static void ex09() {
        titre("Exercice 09 — Programmation orientée objet");
        verifier("Personne.getNom() == Neniri", () -> new Ex09Poo.Personne("Neniri", 25).getNom().equals("Neniri"));
        verifier("Personne.getAge() == 25", () -> new Ex09Poo.Personne("Neniri", 25).getAge() == 25);
        verifier("setAge ignore le négatif, accepte le positif", () -> {
            Ex09Poo.Personne p = new Ex09Poo.Personne("x", 25);
            p.setAge(-3);
            boolean refus = p.getAge() == 25; // -3 ignoré
            p.setAge(30);
            return refus && p.getAge() == 30;
        });
        verifier("Rectangle(5,3).aire() == 15", () -> new Ex09Poo.Rectangle(5, 3).aire() == 15.0);
        verifier("Cercle(2).aire() ≈ π·4", () -> Math.abs(new Ex09Poo.Cercle(2).aire() - Math.PI * 4) < 1e-9);
        verifier("aireTotale([Rect(2,3), Cercle(1)]) ≈ 6 + π", () -> {
            Ex09Poo.Forme[] formes = { new Ex09Poo.Rectangle(2, 3), new Ex09Poo.Cercle(1) };
            return Math.abs(Ex09Poo.aireTotale(formes) - (6 + Math.PI)) < 1e-9;
        });
    }

    // ---- Petits utilitaires d'affichage ----

    private static void titre(String texte) {
        System.out.println("\n# " + texte);
    }

    /** Exécute un test ; affiche [OK]/[RATÉ]/[TODO]/[ERR] et tient les compteurs à jour. */
    private static void verifier(String nom, BooleanSupplier test) {
        total++;
        try {
            if (test.getAsBoolean()) {
                reussis++;
                System.out.println("  [OK]   " + nom);
            } else {
                System.out.println("  [RATÉ] " + nom);
            }
        } catch (UnsupportedOperationException e) {
            System.out.println("  [TODO] " + nom);
        } catch (Throwable t) {
            System.out.println("  [ERR]  " + nom + "  -> " + t);
        }
    }
}
