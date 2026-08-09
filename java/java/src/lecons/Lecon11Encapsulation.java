package lecons;

/**
 * Leçon 11 — Bien structurer une classe : static / instance, public / private,
 * final / constantes, getters / setters.
 *
 * C'est le cœur de la « bonne structure » d'un projet en Java (et donc d'un mod Hytale).
 * L'idée maîtresse s'appelle l'ENCAPSULATION : on PROTÈGE les données d'un objet (champs
 * privés) et on n'autorise que des accès CONTRÔLÉS (méthodes publiques). Ainsi, personne
 * ne peut mettre l'objet dans un état incohérent.
 */
public class Lecon11Encapsulation {

    /** Un compte bancaire : un bon exemple d'objet bien encapsulé. */
    static class CompteBancaire {

        // --- CONSTANTE de classe : static final ---
        // static  = partagée par TOUTE la classe (pas une par objet).
        // final   = ne change JAMAIS.
        // Convention : en MAJUSCULES.
        public static final String DEVISE = "EUR";

        // --- Champ STATIC : une SEULE valeur pour toute la classe ---
        // Ici, un compteur du nombre total de comptes créés.
        private static int nombreDeComptes = 0;

        // --- Champs d'INSTANCE : une valeur PAR objet ---
        // private = invisibles de l'extérieur (c'est ça, l'encapsulation).
        private final String titulaire; // final : fixé à la création, ne bouge plus
        private double solde;

        public CompteBancaire(String titulaire) {
            this.titulaire = titulaire;
            this.solde = 0.0;
            nombreDeComptes++; // on touche au champ static partagé
        }

        // --- GETTERS : lire un champ privé depuis l'extérieur, en lecture seule ---
        public String getTitulaire() {
            return titulaire;
        }

        public double getSolde() {
            return solde;
        }

        // --- Pas de setSolde() ! ---
        // On NE laisse PAS modifier le solde directement. Il ne change qu'à travers des
        // opérations qui VÉRIFIENT les règles (c'est tout l'intérêt de l'encapsulation).
        public void deposer(double montant) {
            if (montant <= 0) {
                return; // règle métier : on refuse un dépôt nul ou négatif
            }
            this.solde += montant;
        }

        public boolean retirer(double montant) {
            if (montant <= 0 || montant > solde) {
                return false; // pas de découvert autorisé
            }
            this.solde -= montant;
            return true;
        }

        // --- Méthode STATIC : appartient à la CLASSE, pas à un objet ---
        // On l'appelle sur la classe (CompteBancaire.getNombreDeComptes()), sans objet.
        public static int getNombreDeComptes() {
            return nombreDeComptes;
        }
    }

    public static void demo() {
        // Une méthode static s'appelle SUR LA CLASSE, pas sur un objet :
        System.out.println("comptes au départ : " + CompteBancaire.getNombreDeComptes());

        CompteBancaire c1 = new CompteBancaire("Neniri");
        CompteBancaire c2 = new CompteBancaire("anna");
        // Le compteur est partagé : il vaut 2, peu importe l'objet.
        System.out.println("après 2 créations : " + CompteBancaire.getNombreDeComptes());

        // On passe par les méthodes publiques. Impossible d'écrire « c1.solde = 1000 »
        // depuis ici : le champ est private. C'est voulu !
        c1.deposer(100);
        c1.retirer(30);
        c1.deposer(-5); // ignoré (la règle refuse les montants négatifs)
        System.out.println(c1.getTitulaire() + " a " + c1.getSolde() + " " + CompteBancaire.DEVISE);

        boolean ok = c2.retirer(50); // c2 est vide -> refusé proprement
        System.out.println("retrait sur compte vide réussi ? " + ok);

        // --- Côté Rust (comparaison) ---
        // Rust n'a pas de "classe" : on utilise une `struct` (les champs) + un bloc `impl`
        // (les méthodes). La visibilité se gère avec `pub` — et tout est privé PAR DÉFAUT,
        // l'inverse de Java. Une constante = `const`. Le compteur partagé existe aussi,
        // mais Rust encadre plus strictement les données globales modifiables.
        // Même idée partout : on protège les données, on expose des opérations sûres.
    }
}
