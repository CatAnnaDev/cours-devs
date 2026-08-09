package lecons;

import java.util.Optional;

/**
 * Leçon 8 — Gérer les erreurs et les valeurs absentes.
 *
 * Deux situations distinctes :
 *   - Une valeur peut MANQUER  -> on utilise Optional<T> (Optional.of(x) ou Optional.empty()).
 *   - Une opération peut ÉCHOUER -> on lève une EXCEPTION, qu'on attrape avec try/catch.
 *
 * Java a aussi la valeur spéciale « null » (= rien), mais elle est source de bugs :
 * Optional est une façon plus sûre d'exprimer « peut-être une valeur, peut-être rien ».
 */
public class Lecon08Erreurs {

    public static void demo() {
        // --- Optional : une valeur peut être présente ou absente ---
        int[] nombres = {10, 20, 30};

        Optional<Integer> trouve = elementAlIndex(nombres, 1);
        if (trouve.isPresent()) {
            System.out.println("élément trouvé : " + trouve.get());
        }

        Optional<Integer> absent = elementAlIndex(nombres, 9);
        // orElse(...) fournit une valeur de secours si c'est vide.
        System.out.println("valeur à l'indice 9 (avec secours) = " + absent.orElse(-1));

        // --- Exceptions : try / catch ---
        // On "essaie" (try) du code risqué ; si une erreur survient, on l'"attrape" (catch).
        try {
            int resultat = diviser(10, 2);
            System.out.println("10 / 2 = " + resultat);

            int erreur = diviser(10, 0); // ceci va lever une exception
            System.out.println("on n'arrive jamais ici : " + erreur);
        } catch (ArithmeticException e) {
            System.out.println("erreur attrapée : " + e.getMessage());
        }

        // Convertir un texte en nombre peut échouer :
        try {
            int n = Integer.parseInt("123");
            System.out.println("converti : " + n);
            int mauvais = Integer.parseInt("abc"); // lève NumberFormatException
            System.out.println("jamais affiché : " + mauvais);
        } catch (NumberFormatException e) {
            System.out.println("« abc » n'est pas un nombre valide");
        }
    }

    /** Renvoie l'élément à l'indice donné, ou Optional.empty() si l'indice est hors limites. */
    static Optional<Integer> elementAlIndex(int[] tableau, int indice) {
        if (indice >= 0 && indice < tableau.length) {
            return Optional.of(tableau[indice]);
        }
        return Optional.empty();
    }

    /** Divise deux entiers ; lève une exception si on divise par zéro. */
    static int diviser(int a, int b) {
        if (b == 0) {
            // "throw" déclenche une exception qui interrompt la méthode.
            throw new ArithmeticException("division par zéro interdite");
        }
        return a / b;
    }
}
