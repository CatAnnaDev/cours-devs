package lecons;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

/**
 * Leçon 5 — Tableaux et listes.
 *
 * Pour regrouper plusieurs valeurs, Java propose deux grands outils :
 *   - le TABLEAU (array)  : taille FIXE, choisie à la création. Syntaxe : int[]
 *   - la LISTE (ArrayList): taille VARIABLE, on peut ajouter/retirer des éléments.
 */
public class Lecon05TableauxListes {

    public static void demo() {
        // --- Tableau : taille fixe, tous les éléments du même type ---
        int[] notes = {12, 15, 8, 17, 10};
        System.out.println("première note = " + notes[0]); // l'indexation commence à 0 !
        System.out.println("nombre de notes = " + notes.length); // .length (sans parenthèses)

        // Parcourir avec une boucle "for each" : « pour chaque note dans notes ».
        int total = 0;
        for (int note : notes) {
            total += note;
        }
        double moyenne = (double) total / notes.length; // (double) pour une division décimale
        System.out.println("moyenne = " + moyenne);

        // Afficher un tableau lisiblement : Arrays.toString(...)
        System.out.println("notes = " + Arrays.toString(notes));

        // --- Liste : redimensionnable, l'outil le plus courant au quotidien ---
        // List<String> = « une liste de String ». ArrayList est une mise en œuvre concrète.
        List<String> panier = new ArrayList<>();
        panier.add("pommes");        // ajouter à la fin
        panier.add("pain");
        panier.add("lait");
        System.out.println("le panier contient " + panier.size() + " articles"); // .size() pour une liste

        panier.remove("pain");       // retirer un élément
        System.out.println("après retrait : " + panier);

        // Parcourir avec l'indice :
        for (int i = 0; i < panier.size(); i++) {
            System.out.println("  " + (i + 1) + ". " + panier.get(i)); // .get(i) lit l'élément i
        }

        // Construire une liste de carrés (1 à 5) :
        List<Integer> carres = new ArrayList<>();
        for (int n = 1; n <= 5; n++) {
            carres.add(n * n);
        }
        System.out.println("carrés de 1 à 5 = " + carres);
    }
}
