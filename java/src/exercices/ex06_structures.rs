//! Exercice 06 — Structures et énumérations (révise la leçon 7).

/// Un rectangle défini par sa largeur et sa hauteur.
pub struct Rectangle {
    pub largeur: f64,
    pub hauteur: f64,
}

impl Rectangle {
    /// EX 6.1 — Renvoie l'aire du rectangle (largeur x hauteur).
    pub fn aire(&self) -> f64 {
        todo!("multiplie self.largeur par self.hauteur")
    }

    /// EX 6.2 — Renvoie le périmètre du rectangle (2 x (largeur + hauteur)).
    pub fn perimetre(&self) -> f64 {
        todo!("applique la formule du périmètre")
    }

    /// EX 6.3 — Renvoie `true` si le rectangle est un carré.
    pub fn est_carre(&self) -> bool {
        todo!("compare largeur et hauteur")
    }
}

/// Les quatre opérations d'une mini-calculatrice.
pub enum Operation {
    Addition,
    Soustraction,
    Multiplication,
    Division,
}

/// EX 6.4 — Applique l'opération aux deux nombres.
/// Pour la division par zéro, renvoie 0.0 (choix simplifié pour l'exercice).
/// Indice : un `match operation { ... }`.
pub fn calculer(operation: &Operation, a: f64, b: f64) -> f64 {
    todo!("fais un match sur l'opération")
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_aire() {
        let r = Rectangle { largeur: 5.0, hauteur: 3.0 };
        assert_eq!(r.aire(), 15.0);
    }

    #[test]
    fn test_perimetre() {
        let r = Rectangle { largeur: 5.0, hauteur: 3.0 };
        assert_eq!(r.perimetre(), 16.0);
    }

    #[test]
    fn test_est_carre() {
        assert!(Rectangle { largeur: 4.0, hauteur: 4.0 }.est_carre());
        assert!(!Rectangle { largeur: 4.0, hauteur: 5.0 }.est_carre());
    }

    #[test]
    fn test_calculer() {
        assert_eq!(calculer(&Operation::Addition, 6.0, 4.0), 10.0);
        assert_eq!(calculer(&Operation::Soustraction, 6.0, 4.0), 2.0);
        assert_eq!(calculer(&Operation::Multiplication, 6.0, 4.0), 24.0);
        assert_eq!(calculer(&Operation::Division, 6.0, 4.0), 1.5);
        assert_eq!(calculer(&Operation::Division, 6.0, 0.0), 0.0);
    }
}
