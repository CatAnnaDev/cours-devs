package lecons;

import java.util.Arrays;

/**
 * Leçon 6 — Les chaînes de caractères (String).
 *
 * Une String est une suite de caractères (du texte). En Java, on l'écrit entre
 * guillemets doubles : "bonjour". Les String offrent de nombreuses méthodes utiles.
 *
 * Important : une String est IMMUABLE. Les méthodes comme toUpperCase() ne modifient
 * pas la chaîne d'origine, elles en RENVOIENT une nouvelle.
 */
public class Lecon06Chaines {

    public static void demo() {
        String salutation = "Bonjour";

        // Concaténation : assembler des chaînes avec « + ».
        String phrase = salutation + ", le monde !";
        System.out.println(phrase);

        // Longueur (nombre de caractères) :
        System.out.println("longueur = " + phrase.length());

        // Quelques opérations courantes (chacune RENVOIE une nouvelle String) :
        String brut = "   Café Crème   ";
        System.out.println("sans espaces : « " + brut.trim() + " »");        // enlève les espaces aux bords
        System.out.println("en minuscules : " + brut.trim().toLowerCase());
        System.out.println("en majuscules : " + brut.trim().toUpperCase());
        System.out.println("contient « Café » ? " + brut.contains("Café"));
        System.out.println("remplace : " + brut.trim().replace("Crème", "au lait"));

        // Lire un caractère précis (indice à partir de 0) :
        System.out.println("1er caractère de 'Rust' : " + "Rust".charAt(0));

        // Découper une chaîne sur un séparateur -> un tableau de morceaux.
        String csv = "rouge,vert,bleu";
        String[] couleurs = csv.split(",");
        System.out.println("couleurs = " + Arrays.toString(couleurs));

        // Parcourir caractère par caractère :
        System.out.print("lettres de « Java » : ");
        for (char c : "Java".toCharArray()) {
            System.out.print("[" + c + "] ");
        }
        System.out.println();

        // Construire du texte formaté avec des trous « %s » (texte) et « %d » (entier) :
        String fiche = String.format("%s (%d ans)", "Anna", 25);
        System.out.println("fiche = " + fiche);

        // Comparer deux chaînes : TOUJOURS avec .equals(...), jamais avec == !
        // (== compare les emplacements mémoire, pas le contenu.)
        String mot = "rust";
        System.out.println("mot vaut-il « rust » ? " + mot.equals("rust"));
    }
}
