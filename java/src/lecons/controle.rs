//! Leçon 3 — Le contrôle de flux.
//!
//! « Contrôler le flux » = décider quelles instructions exécuter et combien de fois.
//! Outils : les conditions (if/else, match) et les boucles (loop, while, for).

pub fn demo() {
    // --- if / else if / else ---
    let note = 14;
    if note >= 16 {
        println!("Très bien");
    } else if note >= 12 {
        println!("Bien"); // c'est cette branche qui s'exécute ici
    } else if note >= 10 {
        println!("Passable");
    } else {
        println!("Insuffisant");
    }

    // En Rust, `if` est une EXPRESSION : il peut renvoyer une valeur.
    let parite = if note % 2 == 0 { "pair" } else { "impair" };
    println!("la note est {parite}");

    // --- match : comparer une valeur à plusieurs motifs (plus puissant qu'un if) ---
    let jour = 3;
    let nom = match jour {
        1 => "lundi",
        2 => "mardi",
        3 => "mercredi",
        4..=5 => "fin de semaine de travail", // un intervalle : 4 ou 5
        6 | 7 => "week-end",                   // 6 OU 7
        _ => "jour invalide",                  // « _ » = tous les autres cas
    };
    println!("jour {jour} = {nom}");

    // --- for : répéter pour chaque élément d'un intervalle ou d'une collection ---
    print!("compte à rebours : ");
    for i in (1..=5).rev() {
        // 1..=5 = de 1 à 5 inclus ; .rev() inverse l'ordre
        print!("{i} ");
    }
    println!("partez !");

    // --- while : répéter TANT QUE la condition est vraie ---
    let mut energie = 3;
    while energie > 0 {
        println!("énergie restante : {energie}");
        energie -= 1;
    }

    // --- loop : boucle infinie qu'on arrête avec `break` (qui peut renvoyer une valeur) ---
    let mut n = 1;
    let premier_multiple = loop {
        if n % 7 == 0 && n % 3 == 0 {
            break n; // sort de la boucle ET renvoie n
        }
        n += 1;
    };
    println!("premier multiple commun de 3 et 7 : {premier_multiple}");
}
