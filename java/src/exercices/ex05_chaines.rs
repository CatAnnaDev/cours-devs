//! Exercice 05 — Chaînes de caractères (révise la leçon 6).

/// EX 5.1 — Renvoie le nombre de caractères de la chaîne.
/// Indice : `.chars().count()` compte correctement même avec des accents.
pub fn longueur(texte: &str) -> usize {
    todo!("compte les caractères")
}

/// EX 5.2 — Renvoie la chaîne tout en majuscules.
pub fn crier(texte: &str) -> String {
    todo!("utilise to_uppercase()")
}

/// EX 5.3 — Renvoie la chaîne inversée.
/// Exemple : inverser("Rust") = "tsuR".
/// Indice : .chars().rev().collect()
pub fn inverser(texte: &str) -> String {
    todo!("inverse l'ordre des caractères")
}

/// EX 5.4 — Renvoie `true` si le texte est un palindrome (se lit pareil à l'envers).
/// On compare en ignorant la casse. Exemple : "Kayak" est un palindrome.
pub fn est_palindrome(texte: &str) -> bool {
    todo!("compare le texte en minuscules avec sa version inversée")
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_longueur() {
        assert_eq!(longueur("Rust"), 4);
        assert_eq!(longueur(""), 0);
    }

    #[test]
    fn test_crier() {
        assert_eq!(crier("salut"), "SALUT");
    }

    #[test]
    fn test_inverser() {
        assert_eq!(inverser("Rust"), "tsuR");
        assert_eq!(inverser("abc"), "cba");
    }

    #[test]
    fn test_est_palindrome() {
        assert!(est_palindrome("kayak"));
        assert!(est_palindrome("Kayak"));
        assert!(est_palindrome("radar"));
        assert!(!est_palindrome("rust"));
    }
}
