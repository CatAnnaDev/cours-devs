package exercices;

/**
 * Exercice 02 — Contrôle de flux (révise la leçon 3).
 */
public class Ex02Controle {

    /**
     * EX 2.1 — Renvoie la mention selon la note (sur 20) :
     *   >= 16 -> "Très bien" ; >= 14 -> "Bien" ; >= 12 -> "Assez bien" ;
     *   >= 10 -> "Passable" ; sinon -> "Insuffisant".
     */
    public static String mention(int note) {
        throw new UnsupportedOperationException("à faire : enchaîne des if / else if");
    }

    /**
     * EX 2.2 — FizzBuzz pour un nombre :
     *   multiple de 3 ET de 5 -> "FizzBuzz" ; de 3 seul -> "Fizz" ;
     *   de 5 seul -> "Buzz" ; sinon -> le nombre en texte (ex. "7").
     * Indice : pour convertir un nombre en texte, utilise String.valueOf(n).
     */
    public static String fizzbuzz(int n) {
        throw new UnsupportedOperationException("à faire : teste 3 ET 5 d'abord, puis 3, puis 5");
    }

    /** EX 2.3 — Somme des entiers de 1 à n inclus. Exemple : sommeJusqua(5) -> 15. */
    public static int sommeJusqua(int n) {
        throw new UnsupportedOperationException("à faire : accumule dans une boucle for");
    }

    /** EX 2.4 — Factorielle de n (n! = 1*2*...*n), avec 0! = 1. Exemple : factorielle(5) -> 120. */
    public static int factorielle(int n) {
        throw new UnsupportedOperationException("à faire : multiplie successivement de 1 à n");
    }
}
