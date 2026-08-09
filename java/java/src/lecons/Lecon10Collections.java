package lecons;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

/**
 * Leçon 10 — Les collections en profondeur (ArrayList, HashMap, HashSet) et la mémoire.
 *
 * Tu as vu les bases en leçon 5. Ici on regarde QUI FAIT QUOI parmi les grandes
 * collections, et surtout COMMENT elles sont rangées en mémoire — c'est ce qui explique
 * pourquoi telle opération est rapide et telle autre lente.
 * (Le détail des coûts en « Big O » est dans le dossier notions/.)
 */
public class Lecon10Collections {

    public static void demo() {
        // ================================================================
        // 1) ArrayList : une liste rangée dans un TABLEAU interne sur le tas
        // ================================================================
        // Une variable "liste" ne contient PAS les éléments : elle contient une
        // RÉFÉRENCE (une adresse) vers un objet ArrayList posé sur le TAS (heap).
        // Cet objet garde un TABLEAU interne (le "backing array") et une taille.
        // Le tableau a une CAPACITÉ >= taille ; quand il est plein, l'ArrayList
        // crée un tableau plus grand et RECOPIE tout dedans.
        //
        //   pile (référence)        tas (objet ArrayList)
        //   liste ────────────────▶ [ size=3 | backing -> [10, 20, 30, _, _] ]
        List<Integer> liste = new ArrayList<>();
        for (int i = 1; i <= 5; i++) {
            liste.add(i * 10);
        }
        System.out.println("liste = " + liste + "  (size = " + liste.size() + ")");

        // Comme c'est un tableau interne, l'accès par index est instantané (O(1)).
        System.out.println("liste.get(2) = " + liste.get(2));
        // Mais CHERCHER une valeur oblige à tout parcourir (O(n)) :
        System.out.println("liste contient 30 ? " + liste.contains(30));
        // Si tu connais la taille d'avance, réserve la capacité pour éviter les recopies :
        List<Integer> pret = new ArrayList<>(100);
        System.out.println("ArrayList prêt pour 100 éléments, size = " + pret.size());

        // ================================================================
        // 2) HashMap : associer une CLÉ à une VALEUR (table de hachage)
        // ================================================================
        // Idée : la clé passe dans hashCode() -> un numéro de CASIER (bucket).
        // On range la paire (clé, valeur) dans ce casier. Pour retrouver une clé,
        // on recalcule son casier -> accès direct, O(1) en moyenne (pas de parcours).
        String[] texte = {"pomme", "pain", "pomme", "lait", "pain", "pomme"};
        Map<String, Integer> comptes = new HashMap<>();
        for (String mot : texte) {
            // getOrDefault(mot, 0) : la valeur actuelle, ou 0 si le mot est nouveau.
            comptes.put(mot, comptes.getOrDefault(mot, 0) + 1);
        }
        System.out.println("comptes = " + comptes); // l'ordre n'est PAS garanti
        System.out.println("nombre de 'pomme' = " + comptes.get("pomme"));

        // ================================================================
        // 3) HashSet : un ENSEMBLE de valeurs uniques
        // ================================================================
        // En interne, c'est un HashMap où seules les CLÉS comptent (pas de valeur).
        // Donc : pas de doublons, et tester la présence est O(1).
        int[] nombres = {1, 2, 2, 3, 3, 3, 4};
        Set<Integer> uniques = new HashSet<>();
        for (int n : nombres) {
            uniques.add(n);
        }
        System.out.println("valeurs uniques = " + uniques.size() + " " + uniques);
        System.out.println("uniques contient 3 ? " + uniques.contains(3)); // O(1)

        // ================================================================
        // À retenir
        // ================================================================
        // - ArrayList : tableau interne sur le tas. Index O(1), recherche O(n).
        // - HashMap   : table de hachage. Accès par clé O(1) en moyenne.
        // - HashSet   : un HashMap "sans valeur". Présence O(1), valeurs uniques.
        // Choisir la bonne structure = ta première optimisation (voir notions/).
    }
}
