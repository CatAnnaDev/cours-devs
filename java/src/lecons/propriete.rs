//! Leçon 9 — Propriété et emprunt (la grande spécificité de Rust).
//!
//! Rust n'a ni ramasse-miettes (garbage collector) ni `free` manuel. À la place,
//! il applique des règles de PROPRIÉTÉ vérifiées à la compilation :
//!   1. Chaque valeur a un unique PROPRIÉTAIRE (une variable).
//!   2. Quand le propriétaire sort de portée, la valeur est libérée automatiquement.
//!   3. On peut PRÊTER une valeur (l'emprunter) avec « & » sans en transférer la propriété.
//! Ces règles éliminent des bugs mémoire courants... une fois qu'on les apprivoise.

pub fn demo() {
    // --- Copie vs déplacement ---
    // Les types simples (entiers, booléens, char...) sont COPIÉS : pas de souci.
    let a = 5;
    let b = a; // a est copié dans b
    println!("a = {a}, b = {b}"); // les deux restent utilisables

    // Les types possédant des données sur le tas (comme String) sont DÉPLACÉS.
    let s1 = String::from("salut");
    let s2 = s1; // la propriété passe de s1 à s2 ; s1 n'est plus valide.
    // println!("{s1}"); // <-- décommente : le compilateur refuse (valeur déplacée).
    println!("s2 = {s2}");

    // Pour garder les deux, on CLONE explicitement (copie en profondeur).
    let original = String::from("important");
    let copie = original.clone();
    println!("original = {original}, copie = {copie}");

    // --- Emprunt en lecture (&) ---
    let message = String::from("Bonjour le monde");
    // On PRÊTE message à la fonction : elle peut le lire mais pas le garder.
    let n = compter_mots(&message);
    println!("« {message} » contient {n} mots"); // message encore utilisable ici

    // --- Emprunt mutable (&mut) ---
    let mut compteur = String::from("a");
    ajouter_point(&mut compteur); // on prête en écriture
    ajouter_point(&mut compteur);
    println!("compteur après modifications = {compteur}");
    // Règle clé : à un instant donné, soit PLUSIEURS prêts en lecture,
    // soit UN SEUL prêt en écriture. Jamais les deux en même temps.
}

/// Emprunte la chaîne en lecture seule (&str) et compte ses mots.
fn compter_mots(texte: &str) -> usize {
    texte.split_whitespace().count()
}

/// Emprunte la chaîne en écriture (&mut String) et la modifie sur place.
fn ajouter_point(texte: &mut String) {
    texte.push('.');
}
