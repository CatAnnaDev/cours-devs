//! Leçon 7 — Structures (`struct`) et énumérations (`enum`).
//!
//! Une `struct` regroupe plusieurs données liées sous un même nom : c'est ta
//! propre « boîte » avec des champs. Une `enum` représente un choix parmi
//! plusieurs variantes possibles. Ensemble, ils servent à modéliser le monde.

/// Une structure : un compte bancaire avec deux champs.
struct Compte {
    titulaire: String,
    solde: f64,
}

// On attache des méthodes à une structure dans un bloc `impl`.
impl Compte {
    /// Fonction « associée » qui construit un nouveau compte (convention : `new`).
    fn nouveau(titulaire: &str) -> Compte {
        Compte {
            titulaire: titulaire.to_string(),
            solde: 0.0,
        }
    }

    /// Méthode : `&mut self` = elle peut modifier le compte appelant.
    fn deposer(&mut self, montant: f64) {
        self.solde += montant;
    }

    /// Méthode en lecture seule : `&self` = elle lit sans modifier.
    fn afficher(&self) {
        println!("Compte de {} : {:.2} €", self.titulaire, self.solde);
    }
}

/// Une énumération : un feu tricolore ne peut être que dans UN de ces états.
enum Feu {
    Rouge,
    Orange,
    Vert,
}

/// Fonction qui réagit à l'état du feu grâce à `match`.
fn consigne(feu: &Feu) -> &str {
    match feu {
        Feu::Rouge => "Stop",
        Feu::Orange => "Ralentis",
        Feu::Vert => "Passe",
    }
}

pub fn demo() {
    // Utilisation de la structure.
    let mut compte = Compte::nouveau("Anna");
    compte.deposer(150.0);
    compte.deposer(49.99);
    compte.afficher();

    // Utilisation de l'énumération.
    let feux = [Feu::Vert, Feu::Orange, Feu::Rouge];
    for feu in &feux {
        println!("feu -> {}", consigne(feu));
    }
}
