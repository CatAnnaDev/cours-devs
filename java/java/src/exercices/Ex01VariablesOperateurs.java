package exercices;

/**
 * Exercice 01 — Variables et opérateurs (révise les leçons 1 et 2).
 *
 * Remplace chaque « throw new UnsupportedOperationException(...) » par du vrai code,
 * puis lance `java src/Tests.java` pour vérifier.
 */
public class Ex01VariablesOperateurs {

    /** EX 1.1 — Renvoie le double du nombre. Exemple : doubler(21) -> 42. */
    public static int doubler(int n) {
        throw new UnsupportedOperationException("à faire : multiplie n par 2");
    }

    /** EX 1.2 — Renvoie la moyenne de deux notes. Exemple : moyenne(10, 15) -> 12.5. */
    public static double moyenne(double note1, double note2) {
        throw new UnsupportedOperationException("à faire : additionne puis divise par 2");
    }

    /** EX 1.3 — Renvoie true si n est pair. Indice : utilise le modulo % et une comparaison. */
    public static boolean estPair(int n) {
        throw new UnsupportedOperationException("à faire : n % 2 == 0 ?");
    }

    /** EX 1.4 — Renvoie true si la personne est majeure (18 ans ou plus). */
    public static boolean estMajeur(int age) {
        throw new UnsupportedOperationException("à faire : compare age à 18");
    }
}
