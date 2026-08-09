package lecons;

import java.util.ArrayList;
import java.util.List;

/**
 * Leçon 9 — Valeurs, références et null.
 *
 * En Java il y a deux grandes familles de types, qui se comportent différemment :
 *   - Les types PRIMITIFS (int, double, boolean, char...) : la variable contient
 *     directement la valeur. Quand on les copie, on copie la valeur.
 *   - Les types OBJETS (String, tableaux, listes, tes propres classes...) : la
 *     variable contient une RÉFÉRENCE (une "adresse") vers l'objet, pas l'objet lui-même.
 *
 * Comprendre cette différence évite beaucoup de bugs de débutante.
 */
public class Lecon09References {

    public static void demo() {
        // --- Primitifs : copie de la valeur ---
        int a = 5;
        int b = a;     // b reçoit une COPIE de la valeur de a
        b = 99;        // modifier b ne change pas a
        System.out.println("a = " + a + ", b = " + b); // a = 5, b = 99

        // --- Objets : copie de la référence (les deux pointent le MÊME objet) ---
        List<String> liste1 = new ArrayList<>();
        liste1.add("pomme");
        List<String> liste2 = liste1;  // liste2 pointe le MÊME objet que liste1
        liste2.add("banane");          // on modifie l'objet partagé...
        System.out.println("liste1 = " + liste1); // ...donc liste1 voit aussi "banane" !

        // Le passage de paramètres suit la même logique :
        int x = 10;
        essayerDeChanger(x);
        System.out.println("x après l'appel = " + x); // toujours 10 (un int est copié)

        List<String> fruits = new ArrayList<>();
        fruits.add("kiwi");
        ajouterUnFruit(fruits);
        System.out.println("fruits après l'appel = " + fruits); // modifié ! (référence partagée)

        // --- null : l'absence d'objet ---
        String texte = null; // la variable ne pointe vers aucun objet
        System.out.println("texte est-il null ? " + (texte == null));
        // Appeler une méthode sur null provoque une NullPointerException :
        // System.out.println(texte.length()); // <-- planterait. On teste null AVANT d'utiliser.
        int longueur = (texte == null) ? 0 : texte.length();
        System.out.println("longueur sûre = " + longueur);
    }

    /** Reçoit une COPIE de l'entier : modifier le paramètre n'affecte pas l'appelant. */
    static void essayerDeChanger(int valeur) {
        valeur = 999; // ne change que la copie locale
    }

    /** Reçoit la référence vers la liste : la modifier affecte la liste de l'appelant. */
    static void ajouterUnFruit(List<String> liste) {
        liste.add("mangue");
    }
}
