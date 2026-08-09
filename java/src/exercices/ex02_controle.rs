//! Exercice 02 — Contrôle de flux (révise la leçon 3).

/// EX 2.1 — Renvoie la mention selon la note (sur 20) :
///   note >= 16 -> "Très bien"
///   note >= 14 -> "Bien"
///   note >= 12 -> "Assez bien"
///   note >= 10 -> "Passable"
///   sinon      -> "Insuffisant"
pub fn mention(note: u32) -> &'static str {
    todo!("enchaîne des if / else if")
}

/// EX 2.2 — Le célèbre FizzBuzz pour un nombre :
///   multiple de 3 ET de 5 -> "FizzBuzz"
///   multiple de 3 seul     -> "Fizz"
///   multiple de 5 seul     -> "Buzz"
///   sinon                  -> le nombre converti en texte (ex. "7")
pub fn fizzbuzz(n: u32) -> String {
    todo!("teste d'abord le cas 3 ET 5, puis 3, puis 5, puis le reste")
}

/// EX 2.3 — Renvoie la somme de tous les entiers de 1 à n inclus.
/// Exemple : somme_jusqua(5) = 1+2+3+4+5 = 15.
/// Indice : une boucle `for` et un accumulateur `mut`.
pub fn somme_jusqua(n: u32) -> u32 {
    todo!("accumule dans une variable mutable au fil d'une boucle for")
}

/// EX 2.4 — Renvoie la factorielle de n (n! = 1*2*...*n), avec 0! = 1.
/// Exemple : factorielle(5) = 120.
pub fn factorielle(n: u32) -> u32 {
    todo!("multiplie successivement de 1 à n")
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_mention() {
        assert_eq!(mention(18), "Très bien");
        assert_eq!(mention(14), "Bien");
        assert_eq!(mention(13), "Assez bien");
        assert_eq!(mention(11), "Passable");
        assert_eq!(mention(5), "Insuffisant");
    }

    #[test]
    fn test_fizzbuzz() {
        assert_eq!(fizzbuzz(15), "FizzBuzz");
        assert_eq!(fizzbuzz(9), "Fizz");
        assert_eq!(fizzbuzz(10), "Buzz");
        assert_eq!(fizzbuzz(7), "7");
    }

    #[test]
    fn test_somme_jusqua() {
        assert_eq!(somme_jusqua(5), 15);
        assert_eq!(somme_jusqua(1), 1);
        assert_eq!(somme_jusqua(10), 55);
    }

    #[test]
    fn test_factorielle() {
        assert_eq!(factorielle(0), 1);
        assert_eq!(factorielle(5), 120);
        assert_eq!(factorielle(1), 1);
    }
}
