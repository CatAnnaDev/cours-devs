package lecons;

/**
 * Leçon 4 — Les méthodes (ce qu'on appelle « fonctions » dans d'autres langages).
 *
 * Une méthode est un bloc de code nommé et réutilisable. On lui donne des
 * ENTRÉES (les paramètres) et elle peut renvoyer une SORTIE (la valeur de retour).
 *
 * Forme générale :
 *   [static] <typeDeRetour> <nom>(<type> param1, <type> param2) { ... return ...; }
 *   - "void" comme type de retour = la méthode ne renvoie rien.
 *   - "static" = la méthode appartient à la classe (pas besoin de créer un objet pour l'appeler).
 */
public class Lecon04Methodes {

    public static void demo() {
        saluer("Anna");                                   // appel d'une méthode "void"

        int somme = additionner(3, 4);                    // on récupère la valeur renvoyée
        System.out.println("3 + 4 = " + somme);

        System.out.println("aire d'un rectangle 5x3 = " + aireRectangle(5.0, 3.0));

        // Une méthode peut en appeler une autre :
        System.out.println("le plus grand entre 8 et 2 est " + maximum(8, 2));
    }

    /** Affiche un message. Ne renvoie rien -> type de retour "void". */
    private static void saluer(String nom) {
        System.out.println("Bonjour " + nom + " !");
    }

    /** Renvoie la somme de deux entiers. Le « int » avant le nom = type renvoyé. */
    private static int additionner(int a, int b) {
        return a + b;                 // "return" renvoie la valeur et termine la méthode
    }

    /** Calcule l'aire d'un rectangle (largeur x hauteur). */
    private static double aireRectangle(double largeur, double hauteur) {
        return largeur * hauteur;
    }

    /** Renvoie le plus grand des deux entiers. */
    private static int maximum(int a, int b) {
        if (a > b) {
            return a;
        }
        return b;
    }
}
