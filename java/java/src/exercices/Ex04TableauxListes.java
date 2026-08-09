package exercices;

import java.util.List;

/**
 * Exercice 04 — Tableaux et listes (révise la leçon 5).
 */
public class Ex04TableauxListes {

    /** EX 4.1 — Somme de tous les éléments du tableau. somme({1,2,3}) -> 6 ; somme({}) -> 0. */
    public static int somme(int[] nombres) {
        throw new UnsupportedOperationException("à faire : parcours le tableau et accumule");
    }

    /**
     * EX 4.2 — Renvoie le plus grand élément du tableau.
     * Le tableau est supposé NON VIDE (on verra le cas vide à la leçon 8 avec Optional).
     */
    public static int plusGrand(int[] nombres) {
        throw new UnsupportedOperationException("à faire : pars du 1er élément puis compare les autres");
    }

    /**
     * EX 4.3 — Renvoie une liste contenant uniquement les nombres pairs.
     * Exemple : pairs({1,2,3,4}) -> [2, 4].
     * Indice : crée « new ArrayList<Integer>() » puis ajoute les pairs avec .add(...).
     */
    public static List<Integer> pairs(int[] nombres) {
        throw new UnsupportedOperationException("à faire : crée une liste et ajoute les nombres pairs");
    }

    /** EX 4.4 — Compte combien de fois « cible » apparaît dans le tableau. */
    public static int compter(int[] nombres, int cible) {
        throw new UnsupportedOperationException("à faire : incrémente un compteur à chaque égalité");
    }
}
