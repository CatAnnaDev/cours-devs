package lecons;

/**
 * Leçon 2 — Les opérateurs.
 *
 * Un opérateur combine des valeurs pour en produire une nouvelle.
 * Familles : arithmétiques (+ - * / %), de comparaison (== != < >), logiques (&& || !).
 */
public class Lecon02Operateurs {

    public static void demo() {
        // --- Arithmétiques ---
        int a = 17;
        int b = 5;
        System.out.println(a + " + " + b + " = " + (a + b));
        System.out.println(a + " - " + b + " = " + (a - b));
        System.out.println(a + " * " + b + " = " + (a * b));
        System.out.println(a + " / " + b + " = " + (a / b)); // division ENTIÈRE = 3 (pas 3.4 !)
        System.out.println(a + " % " + b + " = " + (a % b)); // modulo : le RESTE de la division = 2

        // Attention au piège : entre deux int, « / » donne un int.
        // Pour un résultat décimal, au moins un nombre doit être un double :
        double x = 17.0;
        double y = 5.0;
        System.out.println(x + " / " + y + " = " + (x / y)); // 3.4

        // --- Comparaison --- (le résultat est toujours un boolean)
        System.out.println(a + " == " + b + " ? " + (a == b)); // égal
        System.out.println(a + " != " + b + " ? " + (a != b)); // différent
        System.out.println(a + " >  " + b + " ? " + (a > b));  // strictement supérieur
        System.out.println(a + " <= " + b + " ? " + (a <= b)); // inférieur ou égal

        // --- Logiques --- (combinent des booléens)
        boolean pluie = true;
        boolean parapluie = false;
        System.out.println("pluie ET parapluie ? " + (pluie && parapluie)); // ET : vrai si LES DEUX
        System.out.println("pluie OU parapluie ? " + (pluie || parapluie)); // OU : vrai si AU MOINS UN
        System.out.println("PAS pluie ? " + (!pluie));                       // NON : inverse le booléen

        // Combiner comparaisons et logiques pour décider :
        int temperature = 22;
        boolean agreable = temperature >= 18 && temperature <= 26;
        System.out.println("température agréable ? " + agreable);
    }
}
