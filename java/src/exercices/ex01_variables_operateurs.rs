//! Exercice 01 — Variables et opérateurs (révise la leçon 1 et 2).

/// EX 1.1 — Renvoie le double du nombre donné.
/// Exemple : double(21) doit renvoyer 42.
pub fn double(n: i32) -> i32 {
    todo!("multiplie n par 2")
}

/// EX 1.2 — Renvoie la moyenne de deux notes.
/// Attention à la division : pour obtenir un résultat décimal, travaille en f64.
/// Exemple : moyenne(10.0, 15.0) doit renvoyer 12.5.
pub fn moyenne(note1: f64, note2: f64) -> f64 {
    todo!("additionne les deux notes puis divise par 2")
}

/// EX 1.3 — Renvoie `true` si le nombre est pair, `false` sinon.
/// Indice : un nombre est pair si son reste dans la division par 2 vaut 0 (opérateur %).
pub fn est_pair(n: i32) -> bool {
    todo!("utilise le modulo % et une comparaison")
}

/// EX 1.4 — Renvoie `true` si l'âge correspond à une personne majeure (18 ans ou plus).
pub fn est_majeur(age: u32) -> bool {
    todo!("compare age à 18")
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_double() {
        assert_eq!(double(21), 42);
        assert_eq!(double(0), 0);
        assert_eq!(double(-3), -6);
    }

    #[test]
    fn test_moyenne() {
        assert_eq!(moyenne(10.0, 15.0), 12.5);
        assert_eq!(moyenne(20.0, 20.0), 20.0);
    }

    #[test]
    fn test_est_pair() {
        assert!(est_pair(4));
        assert!(!est_pair(7));
        assert!(est_pair(0));
    }

    #[test]
    fn test_est_majeur() {
        assert!(est_majeur(18));
        assert!(est_majeur(40));
        assert!(!est_majeur(17));
    }
}
