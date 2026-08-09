package exercices;

import java.util.Optional;
import java.util.OptionalDouble;

/**
 * Exercice 07 — Optional et exceptions (révise la leçon 8).
 */
public class Ex07Erreurs {

    /**
     * EX 7.1 — Renvoie le premier élément du tableau, ou Optional.empty() s'il est vide.
     * Indice : Optional.of(valeur) si non vide, sinon Optional.empty().
     */
    public static Optional<Integer> premier(int[] nombres) {
        throw new UnsupportedOperationException("à faire : teste si le tableau est vide");
    }

    /**
     * EX 7.2 — Divise a par b.
     *   - si b == 0 -> lève « throw new ArithmeticException("division par zéro") »
     *   - sinon     -> renvoie a / b
     */
    public static double diviser(double a, double b) {
        throw new UnsupportedOperationException("à faire : lève une exception si b == 0, sinon divise");
    }

    /**
     * EX 7.3 — Convertit un texte en entier.
     *   - "42"  -> 42
     *   - "abc" -> lève « throw new IllegalArgumentException("nombre invalide") »
     * Indice : entoure Integer.parseInt(texte) d'un try/catch (NumberFormatException).
     */
    public static int parserEntier(String texte) {
        throw new UnsupportedOperationException("à faire : parse, et convertis l'échec en IllegalArgumentException");
    }

    /**
     * EX 7.4 — Renvoie l'inverse (1/n), ou OptionalDouble.empty() si n vaut 0.
     * Indice : OptionalDouble.of(1.0 / n) sinon OptionalDouble.empty().
     */
    public static OptionalDouble inverse(double n) {
        throw new UnsupportedOperationException("à faire : renvoie empty si n == 0, sinon 1/n");
    }
}
