package exercices;

import java.util.List;
import java.util.Map;

/**
 * Exercice 08 — Collections en profondeur (révise la leçon 10).
 */
public class Ex08Collections {

    /**
     * EX 8.1 — Nombre de valeurs DISTINCTES du tableau.
     * Exemple : nbValeursUniques({1,2,2,3,3,3}) -> 3.
     * Indice : un HashSet ne garde pas les doublons ; renvoie ensuite sa taille (.size()).
     */
    public static int nbValeursUniques(int[] nombres) {
        throw new UnsupportedOperationException("à faire : mets les nombres dans un HashSet, puis renvoie sa size");
    }

    /**
     * EX 8.2 — Renvoie true si le tableau contient au moins un doublon.
     * Indice : .add(x) sur un HashSet renvoie false si x y était déjà.
     */
    public static boolean contientDoublon(int[] nombres) {
        throw new UnsupportedOperationException("à faire : détecte la première valeur déjà vue");
    }

    /**
     * EX 8.3 — Compte combien de fois chaque mot apparaît.
     * Exemple : compterOccurrences({"a","b","a"}) -> {a=2, b=1}.
     * Indice : un HashMap + getOrDefault(mot, 0) + 1.
     */
    public static Map<String, Integer> compterOccurrences(String[] mots) {
        throw new UnsupportedOperationException("à faire : accumule les comptes dans un HashMap");
    }

    /**
     * EX 8.4 — Valeurs présentes dans LES DEUX tableaux, triées et sans doublon.
     * Exemple : intersection({1,2,3,4}, {2,4,6}) -> [2, 4].
     * Indice : mets `a` dans un HashSet, garde les éléments de `b` qui y sont, puis trie
     * (Collections.sort(...) ou List.sort(null)).
     */
    public static List<Integer> intersection(int[] a, int[] b) {
        throw new UnsupportedOperationException("à faire : croise les deux tableaux, déduplique et trie");
    }
}
