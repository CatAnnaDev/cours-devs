//! Exercice 08 — Collections en profondeur (révise la leçon 10).

use std::collections::HashMap;
use std::collections::HashSet;

/// EX 8.1 — Renvoie le nombre de valeurs DISTINCTES du tableau.
/// Exemple : nb_valeurs_uniques(&[1, 2, 2, 3, 3, 3]) -> 3.
/// Indice : un HashSet ne garde pas les doublons ; renvoie ensuite sa taille (.len()).
pub fn nb_valeurs_uniques(nombres: &[i32]) -> usize {
    todo!("mets les nombres dans un HashSet, puis renvoie .len()")
}

/// EX 8.2 — Renvoie true si le tableau contient au moins un doublon.
/// Indice : ajoute chaque nombre dans un HashSet ; .insert(x) renvoie false si x y était déjà.
pub fn contient_doublon(nombres: &[i32]) -> bool {
    todo!("parcours les nombres et détecte la première valeur déjà vue")
}

/// EX 8.3 — Compte combien de fois chaque mot apparaît.
/// Exemple : compter_occurrences(&["a","b","a"]) -> {"a": 2, "b": 1}.
/// Indice : un HashMap<String, usize> + l'API entry(...).or_insert(0).
/// (Pense à `mot.to_string()` pour passer d'un &str à une String.)
pub fn compter_occurrences(mots: &[&str]) -> HashMap<String, usize> {
    todo!("accumule les comptes dans un HashMap")
}

/// EX 8.4 — Renvoie les valeurs présentes dans LES DEUX tableaux, triées et sans doublon.
/// Exemple : intersection(&[1,2,3,4], &[2,4,6]) -> vec![2, 4].
/// Indice : mets `a` dans un HashSet, garde les éléments de `b` qui y sont, puis trie.
pub fn intersection(a: &[i32], b: &[i32]) -> Vec<i32> {
    todo!("croise les deux tableaux via un HashSet, déduplique et trie")
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_nb_valeurs_uniques() {
        assert_eq!(nb_valeurs_uniques(&[1, 2, 2, 3, 3, 3]), 3);
        assert_eq!(nb_valeurs_uniques(&[]), 0);
        assert_eq!(nb_valeurs_uniques(&[7, 7, 7]), 1);
    }

    #[test]
    fn test_contient_doublon() {
        assert!(contient_doublon(&[1, 2, 2]));
        assert!(!contient_doublon(&[1, 2, 3]));
        assert!(!contient_doublon(&[]));
    }

    #[test]
    fn test_compter_occurrences() {
        let c = compter_occurrences(&["a", "b", "a"]);
        assert_eq!(c.get("a"), Some(&2));
        assert_eq!(c.get("b"), Some(&1));
        assert_eq!(c.get("z"), None);
    }

    #[test]
    fn test_intersection() {
        assert_eq!(intersection(&[1, 2, 3, 4], &[2, 4, 6]), vec![2, 4]);
        assert_eq!(intersection(&[1, 2], &[3, 4]), Vec::<i32>::new());
        assert_eq!(intersection(&[1, 1, 2, 2], &[2, 2, 1]), vec![1, 2]);
    }
}
