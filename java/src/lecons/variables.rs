//! Leçon 1 — Variables et types.
//!
//! Une variable est un nom donné à une valeur stockée en mémoire.
//! En Rust, une variable est IMMUABLE par défaut : une fois affectée, on ne peut
//! pas changer sa valeur... sauf si on ajoute le mot-clé `mut` (mutable).

pub fn demo() {
    // `let` déclare une variable. Rust devine souvent le type tout seul.
    let age = 25; // type deviné : i32 (entier signé sur 32 bits)
    println!("age = {age}");

    // On peut écrire le type explicitement après « : ».
    let taille: f64 = 1.75; // f64 = nombre à virgule (flottant)
    let initiale: char = 'A'; // un seul caractère, entre apostrophes
    let majeur: bool = true; // booléen : true ou false
    println!("taille = {taille}, initiale = {initiale}, majeur = {majeur}");

    // Immuabilité : sans `mut`, ceci provoquerait une erreur de compilation.
    let mut compteur = 0;
    compteur = compteur + 1;
    compteur += 1; // raccourci équivalent à « compteur = compteur + 1 »
    println!("compteur = {compteur}");

    // « Shadowing » : on peut redéclarer une variable avec le même nom.
    // C'est une NOUVELLE variable, qui peut même changer de type.
    let valeur = "42"; // ici c'est une chaîne de caractères
    let valeur: i32 = valeur.parse().expect("ce n'est pas un nombre");
    println!("valeur (maintenant un entier) = {valeur}");

    // Les constantes : toujours en MAJUSCULES, type obligatoire, jamais mutables.
    const VITESSE_LUMIERE: u32 = 299_792_458; // m/s ; le « _ » aide à lire
    println!("vitesse de la lumière = {VITESSE_LUMIERE} m/s");

    // Aperçu des principaux types numériques :
    //   i8, i16, i32, i64    -> entiers signés (peuvent être négatifs)
    //   u8, u16, u32, u64    -> entiers non signés (positifs ou zéro)
    //   f32, f64             -> nombres à virgule
    let petit: u8 = 255; // un u8 va de 0 à 255
    println!("plus grand u8 possible = {petit}");
}
