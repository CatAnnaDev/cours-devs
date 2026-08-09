package lecons;

/**
 * Leçon 1 — Variables et types.
 *
 * Une variable est un nom donné à une valeur stockée en mémoire.
 * En Java, on doit indiquer le TYPE de chaque variable : cela dit quelle sorte
 * de valeur elle peut contenir (un entier, un texte, un booléen...).
 */
public class Lecon01Variables {

    public static void demo() {
        // Déclaration : <type> <nom> = <valeur> ;  (ne pas oublier le point-virgule !)
        int age = 25;                 // int  = entier (nombre sans virgule)
        System.out.println("age = " + age);

        double taille = 1.75;         // double = nombre à virgule
        char initiale = 'A';          // char  = un seul caractère, entre apostrophes
        boolean majeur = true;        // boolean = vrai (true) ou faux (false)
        String prenom = "Anna";       // String = une chaîne de texte, entre guillemets
        System.out.println("taille = " + taille + ", initiale = " + initiale
                + ", majeur = " + majeur + ", prenom = " + prenom);

        // Une variable peut changer de valeur (sauf si elle est "final", voir plus bas).
        int compteur = 0;
        compteur = compteur + 1;
        compteur += 1;                // raccourci équivalent à « compteur = compteur + 1 »
        System.out.println("compteur = " + compteur);

        // "final" = la valeur ne pourra JAMAIS changer (une constante).
        final double PI = 3.14159;
        System.out.println("PI = " + PI);

        // "var" : depuis Java 10, le type peut être deviné à partir de la valeur.
        var ville = "Paris";          // Java comprend que c'est une String
        System.out.println("ville = " + ville);

        // Aperçu des principaux types de nombres :
        //   byte, short, int, long  -> entiers (de plus en plus grands)
        //   float, double           -> nombres à virgule
        long grandNombre = 9_000_000_000L;   // le « L » indique un long ; « _ » aide à lire
        System.out.println("grand nombre = " + grandNombre);

        // Conversion entre types numériques (« cast ») :
        int entier = (int) taille;    // force 1.75 en entier -> 1 (la partie après la virgule est perdue)
        System.out.println("taille convertie en entier = " + entier);
    }
}
