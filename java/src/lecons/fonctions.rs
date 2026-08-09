//! Leçon 4 — Les fonctions.
//!
//! Une fonction est un bloc de code nommé et réutilisable. On lui donne des
//! ENTRÉES (les paramètres) et elle peut renvoyer une SORTIE (la valeur de retour).
//! Découper un programme en petites fonctions le rend plus clair et testable.

pub fn demo() {
    saluer("Anna"); // appel d'une fonction sans valeur de retour

    let somme = additionner(3, 4); // appel avec récupération du résultat
    println!("3 + 4 = {somme}");

    // Les fonctions s'appellent entre elles : ici aire utilise rien d'autre,
    // mais on aurait pu composer plusieurs fonctions.
    println!("aire d'un rectangle 5x3 = {}", aire_rectangle(5.0, 3.0));

    // Une fonction peut renvoyer plusieurs valeurs via un tuple.
    let (min, max) = min_max(8, 2);
    println!("min = {min}, max = {max}");
}

/// Affiche un message. Aucune valeur renvoyée (type de retour implicite : `()`).
fn saluer(nom: &str) {
    println!("Bonjour {nom} !");
}

/// Renvoie la somme de deux entiers.
/// La flèche `-> i32` annonce le type de retour.
fn additionner(a: i32, b: i32) -> i32 {
    // La DERNIÈRE expression sans point-virgule est la valeur renvoyée.
    a + b
    // Équivalent explicite : `return a + b;`
}

/// Calcule l'aire d'un rectangle (largeur x hauteur).
fn aire_rectangle(largeur: f64, hauteur: f64) -> f64 {
    largeur * hauteur
}

/// Renvoie un couple (plus petit, plus grand) à partir de deux entiers.
fn min_max(a: i32, b: i32) -> (i32, i32) {
    if a < b { (a, b) } else { (b, a) }
}
