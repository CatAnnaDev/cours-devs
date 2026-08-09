//! Leçon 6 — Les chaînes de caractères.
//!
//! Une chaîne est une suite de caractères (du texte). En Rust il existe deux formes :
//!   - `&str`   : une « tranche » de texte, souvent figée (ex. un littéral "bonjour").
//!   - `String` : une chaîne possédée et MODIFIABLE (on peut l'agrandir).

pub fn demo() {
    // Un littéral est de type &str.
    let salutation: &str = "Bonjour";

    // String : créée à partir d'un &str, et modifiable.
    let mut phrase = String::from(salutation);
    phrase.push_str(", le monde"); // ajouter du texte
    phrase.push('!'); // ajouter un seul caractère
    println!("{phrase}");

    // Longueur EN OCTETS (attention : les accents prennent 2 octets en UTF-8).
    println!("longueur = {} octets", phrase.len());

    // Quelques opérations courantes :
    let brut = "   Café Crème   ";
    println!("sans espaces : « {} »", brut.trim()); // enlève les espaces aux bords
    println!("en minuscules : {}", brut.trim().to_lowercase());
    println!("en majuscules : {}", brut.trim().to_uppercase());
    println!("contient « Café » ? {}", brut.contains("Café"));
    println!("remplace : {}", brut.trim().replace("Crème", "au lait"));

    // Découper une chaîne sur un séparateur -> on obtient des morceaux.
    let csv = "rouge,vert,bleu";
    let couleurs: Vec<&str> = csv.split(',').collect();
    println!("couleurs = {:?}", couleurs);

    // Parcourir caractère par caractère (.chars() gère correctement l'UTF-8).
    print!("lettres de « Rust » : ");
    for c in "Rust".chars() {
        print!("[{c}] ");
    }
    println!();

    // Construire du texte avec format! (comme println!, mais renvoie une String).
    let nom = "Anna";
    let age = 25;
    let fiche = format!("{nom} ({age} ans)");
    println!("fiche = {fiche}");
}
