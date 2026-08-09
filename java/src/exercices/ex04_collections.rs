//! Exercice 04 — Collections (révise la leçon 5).

/// EX 4.1 — Renvoie la somme de tous les éléments du tableau.
/// Exemple : somme(&[1, 2, 3]) = 6 ; somme(&[]) = 0.
pub fn somme(nombres: &[i32]) -> i32 {
    todo!("parcours le tableau et accumule")
}

/// EX 4.2 — Renvoie le plus grand élément, ou `None` si le tableau est vide.
pub fn plus_grand(nombres: &[i32]) -> Option<i32> {
    todo!("gère le cas vide, sinon trouve le maximum")
}

/// EX 4.3 — Renvoie un nouveau vecteur contenant uniquement les nombres pairs.
/// Exemple : pairs(&[1, 2, 3, 4]) = vec![2, 4].
pub fn pairs(nombres: &[i32]) -> Vec<i32> {
    todo!("crée un Vec et y pousse les nombres pairs")
}

/// EX 4.4 — Compte combien de fois `cible` apparaît dans le tableau.
pub fn compter(nombres: &[i32], cible: i32) -> usize {
    todo!("incrémente un compteur à chaque égalité avec cible")
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_somme() {
        assert_eq!(somme(&[1, 2, 3]), 6);
        assert_eq!(somme(&[]), 0);
        assert_eq!(somme(&[-1, 1]), 0);
    }

    #[test]
    fn test_plus_grand() {
        assert_eq!(plus_grand(&[3, 9, 5]), Some(9));
        assert_eq!(plus_grand(&[-4, -2, -8]), Some(-2));
        assert_eq!(plus_grand(&[]), None);
    }

    #[test]
    fn test_pairs() {
        assert_eq!(pairs(&[1, 2, 3, 4]), vec![2, 4]);
        assert_eq!(pairs(&[1, 3, 5]), Vec::<i32>::new());
    }

    #[test]
    fn test_compter() {
        assert_eq!(compter(&[1, 2, 2, 3, 2], 2), 3);
        assert_eq!(compter(&[1, 2, 3], 9), 0);
    }
}
