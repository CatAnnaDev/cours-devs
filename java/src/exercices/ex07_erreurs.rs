//! Exercice 07 — Option et Result (révise la leçon 8).

/// EX 7.1 — Renvoie le premier élément du tableau, ou `None` s'il est vide.
pub fn premier(nombres: &[i32]) -> Option<i32> {
    todo!("utilise .first() puis adapte le type, ou un if sur la longueur")
}

/// EX 7.2 — Divise `a` par `b`.
///   - si b vaut 0      -> Err avec le message "division par zéro"
///   - sinon            -> Ok(a / b)
pub fn diviser(a: f64, b: f64) -> Result<f64, String> {
    todo!("teste b == 0.0 puis renvoie Err ou Ok")
}

/// EX 7.3 — Convertit un texte en entier.
///   - "42"   -> Ok(42)
///   - "abc"  -> Err("nombre invalide")
/// Indice : `texte.parse::<i32>()` renvoie un Result ; transforme l'erreur avec `.map_err`.
pub fn parser_entier(texte: &str) -> Result<i32, String> {
    todo!("appelle parse() et convertis l'erreur en message")
}

/// EX 7.4 — Renvoie l'inverse (1/n) d'un nombre, ou `None` si n vaut 0.
pub fn inverse(n: f64) -> Option<f64> {
    todo!("renvoie None si n == 0.0, sinon Some(1.0 / n)")
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_premier() {
        assert_eq!(premier(&[7, 8, 9]), Some(7));
        assert_eq!(premier(&[]), None);
    }

    #[test]
    fn test_diviser() {
        assert_eq!(diviser(10.0, 2.0), Ok(5.0));
        assert_eq!(diviser(1.0, 0.0), Err("division par zéro".to_string()));
    }

    #[test]
    fn test_parser_entier() {
        assert_eq!(parser_entier("42"), Ok(42));
        assert_eq!(parser_entier("abc"), Err("nombre invalide".to_string()));
    }

    #[test]
    fn test_inverse() {
        assert_eq!(inverse(4.0), Some(0.25));
        assert_eq!(inverse(0.0), None);
    }
}
