package lecons;

/**
 * Leçon 7 — Classes, objets et enums.
 *
 * Une CLASSE est un modèle (un plan) qui regroupe des données (les "champs") et
 * des comportements (les "méthodes"). À partir d'une classe, on crée des OBJETS
 * concrets avec « new ». C'est le cœur de la programmation orientée objet.
 *
 * Une ENUM représente un choix parmi un nombre fixe de valeurs possibles.
 */
public class Lecon07Classes {

    // --- Une classe imbriquée : un compte bancaire ---
    static class Compte {
        // Les champs : les données que possède chaque compte.
        String titulaire;
        double solde;

        // Le constructeur : appelé avec « new » pour fabriquer un objet.
        Compte(String titulaire) {
            this.titulaire = titulaire; // "this.titulaire" = le champ ; "titulaire" = le paramètre
            this.solde = 0.0;
        }

        // Une méthode qui modifie l'objet.
        void deposer(double montant) {
            this.solde += montant;
        }

        // Une méthode qui lit l'objet et affiche son état.
        void afficher() {
            System.out.printf("Compte de %s : %.2f €%n", titulaire, solde); // %n = saut de ligne
        }
    }

    // --- Une enum : un feu tricolore ne peut être que dans UN de ces états ---
    enum Feu {
        ROUGE, ORANGE, VERT
    }

    /** Renvoie la consigne associée à l'état du feu. */
    static String consigne(Feu feu) {
        return switch (feu) {
            case ROUGE -> "Stop";
            case ORANGE -> "Ralentis";
            case VERT -> "Passe";
        };
    }

    public static void demo() {
        // Créer et utiliser un objet :
        Compte compte = new Compte("Anna");   // « new » fabrique l'objet
        compte.deposer(150.0);
        compte.deposer(49.99);
        compte.afficher();

        // Utiliser l'enum :
        Feu[] feux = {Feu.VERT, Feu.ORANGE, Feu.ROUGE};
        for (Feu feu : feux) {
            System.out.println("feu -> " + consigne(feu));
        }
    }
}
