package exercices;

/**
 * Exercice 03 — Méthodes (révise la leçon 4).
 */
public class Ex03Methodes {

    /** EX 3.1 — Renvoie le plus grand des deux entiers. */
    public static int maximum(int a, int b) {
        throw new UnsupportedOperationException("à faire : compare a et b");
    }

    /** EX 3.2 — Renvoie le carré d'un nombre. */
    public static int carre(int n) {
        throw new UnsupportedOperationException("à faire : multiplie n par lui-même");
    }

    /**
     * EX 3.3 — Renvoie la valeur absolue (toujours >= 0).
     * Exemple : valeurAbsolue(-7) -> 7. Fais-le avec un if (sans Math.abs).
     */
    public static int valeurAbsolue(int n) {
        throw new UnsupportedOperationException("à faire : si n < 0 renvoie -n, sinon n");
    }

    /**
     * EX 3.4 — Renvoie true si n est un nombre premier.
     * Rappel : premier = >= 2 et divisible uniquement par 1 et lui-même.
     * Indice : teste les diviseurs de 2 à n-1 ; si l'un divise n (reste 0), ce n'est pas premier.
     */
    public static boolean estPremier(int n) {
        throw new UnsupportedOperationException("à faire : gère n < 2, puis teste les diviseurs");
    }
}
