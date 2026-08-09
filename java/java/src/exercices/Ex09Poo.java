package exercices;

/**
 * Exercice 09 — Programmation orientée objet (révise les leçons 11 et 12).
 *
 * Remplace les « throw new UnsupportedOperationException(...) » par ton code,
 * puis lance `java src/Tests.java`.
 */
public class Ex09Poo {

    // ============================================================
    // 9.1 — Encapsulation : complète la classe Personne
    // ============================================================
    public static class Personne {
        private final String nom;
        private int age;

        public Personne(String nom, int age) {
            this.nom = nom;
            this.age = Math.max(age, 0); // un âge négatif au départ devient 0
        }

        /** EX 9.1a — Renvoie le nom. */
        public String getNom() {
            throw new UnsupportedOperationException("à faire : renvoie le champ nom");
        }

        /** EX 9.1b — Renvoie l'âge. */
        public int getAge() {
            throw new UnsupportedOperationException("à faire : renvoie le champ age");
        }

        /** EX 9.1c — Met à jour l'âge SEULEMENT s'il est >= 0 (sinon ne change rien). */
        public void setAge(int nouvelAge) {
            throw new UnsupportedOperationException("à faire : ignore les valeurs négatives");
        }
    }

    // ============================================================
    // 9.2 — Héritage : complète aire() dans Rectangle et Cercle
    // ============================================================
    /** Une forme géométrique. Chaque forme concrète DOIT savoir calculer son aire. */
    public static abstract class Forme {
        public abstract double aire();
    }

    public static class Rectangle extends Forme {
        private final double largeur;
        private final double hauteur;

        public Rectangle(double largeur, double hauteur) {
            this.largeur = largeur;
            this.hauteur = hauteur;
        }

        @Override
        public double aire() {
            throw new UnsupportedOperationException("à faire : largeur * hauteur");
        }
    }

    public static class Cercle extends Forme {
        private final double rayon;

        public Cercle(double rayon) {
            this.rayon = rayon;
        }

        @Override
        public double aire() {
            throw new UnsupportedOperationException("à faire : Math.PI * rayon * rayon");
        }
    }

    // ============================================================
    // 9.3 — Polymorphisme : somme des aires de formes variées
    // ============================================================
    /** EX 9.3 — Renvoie la somme des aires de toutes les formes du tableau. */
    public static double aireTotale(Forme[] formes) {
        throw new UnsupportedOperationException("à faire : additionne f.aire() pour chaque forme");
    }
}
