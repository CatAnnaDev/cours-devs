//! Leçon 8 — Gérer l'absence de valeur et les erreurs.
//!
//! Beaucoup de langages utilisent `null` ou des exceptions. Rust préfère deux
//! types explicites que le compilateur t'oblige à traiter :
//!   - `Option<T>` : soit `Some(valeur)`, soit `None` (rien). Pour « ça peut manquer ».
//!   - `Result<T, E>` : soit `Ok(valeur)`, soit `Err(erreur)`. Pour « ça peut échouer ».

pub fn demo() {
    // --- Option : chercher un élément qui n'existe peut-être pas ---
    let nombres = [10, 20, 30];

    match trouver_a_lindex(&nombres, 1) {
        Some(v) => println!("élément trouvé : {v}"),
        None => println!("aucun élément à cet indice"),
    }
    match trouver_a_lindex(&nombres, 9) {
        Some(v) => println!("élément trouvé : {v}"),
        None => println!("aucun élément à l'indice 9"),
    }

    // `unwrap_or` fournit une valeur de secours si c'est None.
    let valeur = trouver_a_lindex(&nombres, 9).unwrap_or(-1);
    println!("valeur (avec secours) = {valeur}");

    // --- Result : une opération qui peut échouer proprement ---
    match diviser(10.0, 2.0) {
        Ok(r) => println!("10 / 2 = {r}"),
        Err(e) => println!("erreur : {e}"),
    }
    match diviser(10.0, 0.0) {
        Ok(r) => println!("10 / 0 = {r}"),
        Err(e) => println!("erreur : {e}"), // c'est ce cas qui se produit
    }

    // L'opérateur `?` propage l'erreur automatiquement (voir `aire_securisee`).
    match aire_securisee("5", "3") {
        Ok(a) => println!("aire = {a}"),
        Err(e) => println!("impossible de calculer l'aire : {e}"),
    }
    match aire_securisee("5", "abc") {
        Ok(a) => println!("aire = {a}"),
        Err(e) => println!("impossible de calculer l'aire : {e}"),
    }
}

/// Renvoie l'élément à l'indice donné, ou `None` si l'indice est hors limites.
fn trouver_a_lindex(tableau: &[i32], indice: usize) -> Option<i32> {
    // `.get` renvoie déjà une Option ; `.copied()` transforme Option<&i32> en Option<i32>.
    tableau.get(indice).copied()
}

/// Divise deux nombres, en refusant la division par zéro.
fn diviser(a: f64, b: f64) -> Result<f64, String> {
    if b == 0.0 {
        Err(String::from("division par zéro interdite"))
    } else {
        Ok(a / b)
    }
}

/// Convertit deux textes en nombres puis calcule l'aire.
/// Le `?` après `.parse()` : si la conversion échoue, on sort tout de suite
/// en renvoyant l'erreur ; sinon on continue avec la valeur.
fn aire_securisee(largeur: &str, hauteur: &str) -> Result<f64, std::num::ParseFloatError> {
    let l: f64 = largeur.parse()?;
    let h: f64 = hauteur.parse()?;
    Ok(l * h)
}
