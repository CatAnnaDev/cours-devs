package exercices;

/**
 * Exercice 06 — Classes, objets et enums (révise la leçon 7).
 */
public class Ex06Classes {

    /** Un rectangle défini par sa largeur et sa hauteur. */
    public static class Rectangle {
        public double largeur;
        public double hauteur;

        public Rectangle(double largeur, double hauteur) {
            this.largeur = largeur;
            this.hauteur = hauteur;
        }

        /** EX 6.1 — Renvoie l'aire (largeur x hauteur). */
        public double aire() {
            throw new UnsupportedOperationException("à faire : largeur * hauteur");
        }

        /** EX 6.2 — Renvoie le périmètre : 2 x (largeur + hauteur). */
        public double perimetre() {
            throw new UnsupportedOperationException("à faire : 2 * (largeur + hauteur)");
        }

        /** EX 6.3 — Renvoie true si le rectangle est un carré. */
        public boolean estCarre() {
            throw new UnsupportedOperationException("à faire : largeur == hauteur ?");
        }
    }

    /** Les opérations d'une mini-calculatrice. */
    public enum Operation {
        ADDITION, SOUSTRACTION, MULTIPLICATION, DIVISION
    }

    /**
     * EX 6.4 — Applique l'opération aux deux nombres.
     * Pour la division par zéro, renvoie 0.0 (choix simplifié pour l'exercice).
     * Indice : un switch sur l'opération.
     */
    public static double calculer(Operation operation, double a, double b) {
        throw new UnsupportedOperationException("à faire : switch sur l'opération");
    }
}
