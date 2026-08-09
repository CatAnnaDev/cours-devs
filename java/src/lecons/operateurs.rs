//! Leçon 2 — Les opérateurs.
//!
//! Un opérateur combine des valeurs pour en produire une nouvelle.
//! On les classe en familles : arithmétiques, de comparaison, logiques.

pub fn demo() {
    // --- Arithmétiques ---
    let a = 17;
    let b = 5;
    println!("{a} + {b} = {}", a + b); // addition
    println!("{a} - {b} = {}", a - b); // soustraction
    println!("{a} * {b} = {}", a * b); // multiplication
    println!("{a} / {b} = {}", a / b); // division ENTIÈRE (= 3, pas 3.4 !)
    println!("{a} % {b} = {}", a % b); // modulo : le reste de la division (= 2)

    // Pour une vraie division décimale, il faut des flottants :
    let x = 17.0;
    let y = 5.0;
    println!("{x} / {y} = {}", x / y); // 3.4

    // --- Comparaison --- (le résultat est toujours un booléen)
    println!("{a} == {b} ? {}", a == b); // égal
    println!("{a} != {b} ? {}", a != b); // différent
    println!("{a} >  {b} ? {}", a > b); // strictement supérieur
    println!("{a} <= {b} ? {}", a <= b); // inférieur ou égal

    // --- Logiques --- (combinent des booléens)
    let pluie = true;
    let parapluie = false;
    println!("pluie ET parapluie ? {}", pluie && parapluie); // ET : vrai si LES DEUX
    println!("pluie OU parapluie ? {}", pluie || parapluie); // OU : vrai si AU MOINS UN
    println!("PAS pluie ? {}", !pluie); // NON : inverse le booléen

    // Astuce : les comparaisons et les logiques se combinent pour décider.
    let temperature = 22;
    let agreable = temperature >= 18 && temperature <= 26;
    println!("température agréable ? {agreable}");
}
