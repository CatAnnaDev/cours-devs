//! Leçon 5 — Les collections.
//!
//! Une collection regroupe plusieurs valeurs. Les trois bases :
//!   - le tableau (`array`)  : taille FIXE, connue à la compilation.
//!   - le tuple              : taille fixe, mais types DIFFÉRENTS autorisés.
//!   - le vecteur (`Vec`)    : taille VARIABLE, on peut ajouter/retirer.

pub fn demo() {
    // --- Tableau : [type; taille], tous les éléments du même type ---
    let notes = [12, 15, 8, 17, 10];
    println!("première note = {}", notes[0]); // l'indexation commence à 0 !
    println!("nombre de notes = {}", notes.len());

    // Parcourir un tableau et calculer une somme.
    let mut total = 0;
    for note in notes {
        total += note;
    }
    let moyenne = total as f64 / notes.len() as f64; // `as f64` convertit le type
    println!("moyenne = {moyenne}");

    // --- Tuple : regroupe des valeurs de types variés ---
    let personne = ("Anna", 25, 1.75); // (nom, âge, taille)
    println!("nom = {}, âge = {}", personne.0, personne.1); // accès par .indice

    // Déstructuration : éclater le tuple dans des variables nommées.
    let (nom, age, taille) = personne;
    println!("{nom} a {age} ans et mesure {taille} m");

    // --- Vecteur : la collection la plus utilisée car redimensionnable ---
    let mut panier: Vec<String> = Vec::new();
    panier.push(String::from("pommes")); // ajouter à la fin
    panier.push(String::from("pain"));
    panier.push(String::from("lait"));
    println!("le panier contient {} articles", panier.len());

    panier.pop(); // retirer le dernier élément
    println!("après retrait : {:?}", panier); // {:?} affiche la structure brute

    // Parcourir avec l'indice ET la valeur grâce à .enumerate().
    for (i, article) in panier.iter().enumerate() {
        println!("  {}. {article}", i + 1);
    }

    // Macro pratique pour créer un vecteur déjà rempli :
    let carres: Vec<i32> = (1..=5).map(|n| n * n).collect();
    println!("carrés de 1 à 5 = {:?}", carres);
}
