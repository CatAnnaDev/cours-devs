//! Exercice 03 — Fonctions (révise la leçon 4).

/// EX 3.1 — Renvoie le plus grand des deux entiers.
pub fn maximum(a: i32, b: i32) -> i32 {
    todo!("compare a et b, renvoie le plus grand")
}

/// EX 3.2 — Renvoie le carré d'un nombre.
pub fn carre(n: i32) -> i32 {
    todo!("multiplie n par lui-même")
}

/// EX 3.3 — Renvoie la valeur absolue d'un entier (toujours positive ou nulle).
/// Exemple : valeur_absolue(-7) = 7 ; valeur_absolue(3) = 3.
/// Contrainte pédagogique : fais-le avec un `if`, sans la méthode `.abs()`.
pub fn valeur_absolue(n: i32) -> i32 {
    todo!("si n est négatif, renvoie -n, sinon n")
}

/// EX 3.4 — Renvoie `true` si `n` est un nombre premier.
/// Rappel : un nombre premier est >= 2 et n'est divisible que par 1 et lui-même.
/// Indice : teste les diviseurs de 2 à n-1 ; si l'un divise n, ce n'est pas premier.
pub fn est_premier(n: u32) -> bool {
    todo!("gère d'abord n < 2, puis teste les diviseurs")
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_maximum() {
        assert_eq!(maximum(3, 8), 8);
        assert_eq!(maximum(10, 2), 10);
        assert_eq!(maximum(-5, -1), -1);
    }

    #[test]
    fn test_carre() {
        assert_eq!(carre(4), 16);
        assert_eq!(carre(-3), 9);
        assert_eq!(carre(0), 0);
    }

    #[test]
    fn test_valeur_absolue() {
        assert_eq!(valeur_absolue(-7), 7);
        assert_eq!(valeur_absolue(3), 3);
        assert_eq!(valeur_absolue(0), 0);
    }

    #[test]
    fn test_est_premier() {
        assert!(est_premier(2));
        assert!(est_premier(13));
        assert!(!est_premier(1));
        assert!(!est_premier(9));
        assert!(!est_premier(0));
    }
}
