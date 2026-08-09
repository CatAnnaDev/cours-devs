//! neni_learn — ta base pour apprendre à coder, côté Rust. (Je t'ai fait ça, Neniri. — anna)
//!
//! Comment utiliser ce projet :
//!   1. `cargo run`        -> exécute toutes les leçons (démonstrations commentées).
//!   2. `cargo run -- 3`   -> exécute uniquement la leçon n°3.
//!   3. `cargo test`       -> lance les exercices : à toi de les compléter pour
//!                            faire passer les tests (fichiers dans `src/exercices/`).
//!
//! Conseil : ouvre les fichiers de `src/lecons/` dans l'ordre, lis les commentaires,
//! puis va t'entraîner sur l'exercice correspondant dans `src/exercices/`.

mod exercices;
mod lecons;

use std::env;

/// Liste des leçons disponibles, dans l'ordre d'apprentissage conseillé.
/// Chaque entrée : (numéro, titre, fonction de démonstration).
const LECONS: &[(u32, &str, fn())] = &[
    (1, "Variables et types", lecons::variables::demo),
    (2, "Opérateurs", lecons::operateurs::demo),
    (3, "Contrôle de flux (if / match / boucles)", lecons::controle::demo),
    (4, "Fonctions", lecons::fonctions::demo),
    (5, "Collections (tableaux, tuples, vecteurs)", lecons::collections::demo),
    (6, "Chaînes de caractères", lecons::chaines::demo),
    (7, "Structures et énumérations", lecons::structures::demo),
    (8, "Gestion des erreurs (Option / Result)", lecons::erreurs::demo),
    (9, "Propriété et emprunt (ownership / borrowing)", lecons::propriete::demo),
    (10, "Collections en profondeur (Vec, HashMap, HashSet, mémoire)", lecons::collections_avancees::demo),
];

fn main() {
    let args: Vec<String> = env::args().collect();

    // Si l'utilisatrice passe un numéro de leçon, on n'exécute que celle-ci.
    if let Some(arg) = args.get(1) {
        match arg.parse::<u32>() {
            Ok(numero) => match LECONS.iter().find(|(n, _, _)| *n == numero) {
                Some((n, titre, demo)) => {
                    afficher_titre(*n, titre);
                    demo();
                }
                None => {
                    eprintln!("Leçon {numero} introuvable. Leçons disponibles : 1 à {}.", LECONS.len());
                }
            },
            Err(_) => eprintln!("Argument invalide : « {arg} ». Donne un numéro de leçon, ex. `cargo run -- 3`."),
        }
        return;
    }

    // Sinon, on déroule toutes les leçons.
    println!("===========================================");
    println!("  neni_learn — apprendre la programmation");
    println!("===========================================");
    for (numero, titre, demo) in LECONS {
        afficher_titre(*numero, titre);
        demo();
    }
    println!("\nFini ! Lance maintenant `cargo test` pour t'entraîner sur les exercices.");
}

/// Affiche un en-tête lisible avant chaque leçon.
fn afficher_titre(numero: u32, titre: &str) {
    println!("\n--- Leçon {numero} : {titre} ---");
}
