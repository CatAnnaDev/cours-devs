//! Leçon 10 — Les collections en profondeur (Vec, HashMap, HashSet) et la mémoire.
//!
//! Tu as vu les bases des Vec en leçon 5. Ici on regarde QUI FAIT QUOI parmi les
//! grandes collections, et surtout COMMENT elles sont rangées en mémoire — c'est ce
//! qui explique pourquoi telle opération est rapide et telle autre lente.
//! (Le détail des coûts en « Big O » est dans le dossier `notions/`.)

use std::collections::HashMap;
use std::collections::HashSet;

pub fn demo() {
    // ===================================================================
    // 1) Vec : une liste rangée dans un bloc CONTIGU sur le tas
    // ===================================================================
    // Un Vec, c'est en fait 3 petites infos posées sur la PILE (stack) :
    //   - un POINTEUR vers les données,
    //   - len      : combien d'éléments il contient VRAIMENT,
    //   - capacity : combien il peut en contenir AVANT de devoir s'agrandir.
    // Les éléments, eux, sont sur le TAS (heap), les uns à la suite des autres :
    //
    //     PILE (stack)              TAS (heap)
    //     ┌────────────┐            ┌────┬────┬────┬────┬────┐
    //     │ ptr   ─────┼──────────▶ │ 10 │ 20 │ 30 │ ?? │ ?? │
    //     │ len = 3    │            └────┴────┴────┴────┴────┘
    //     │ cap = 5    │              ↑ utilisé ↑   ↑ réservé ↑
    //     └────────────┘
    let mut v: Vec<i32> = Vec::new();
    println!("Vec vide        : len={}, capacity={}", v.len(), v.capacity());
    for i in 1..=5 {
        v.push(i * 10);
        println!("après push({:<2}) : len={}, capacity={}", i * 10, v.len(), v.capacity());
    }
    // Tu vois la capacity grimper par paliers (souvent en doublant). Chaque palier,
    // c'est une RÉALLOCATION : un bloc plus grand est créé et tout est RECOPIÉ.
    // Si tu connais la taille d'avance, réserve la place pour éviter ces recopies :
    let pret: Vec<i32> = Vec::with_capacity(100);
    println!("with_capacity(100) : len={}, capacity={}", pret.len(), pret.capacity());

    // Comme les éléments sont contigus, l'accès par index est instantané (O(1)) :
    // l'adresse se calcule directement (ptr + index × taille_d_un_i32).
    println!("v[2] = {}", v[2]);
    // En revanche, CHERCHER une valeur oblige à tout parcourir (O(n)) :
    println!("v contient 30 ? {}", v.contains(&30));

    // ===================================================================
    // 2) HashMap : associer une CLÉ à une VALEUR (une table de hachage)
    // ===================================================================
    // Idée : la clé passe dans une "fonction de hachage" qui donne un numéro de
    // CASIER (bucket). On range la paire (clé, valeur) dans ce casier. Pour
    // retrouver une clé plus tard, on recalcule son casier → accès direct, O(1)
    // en moyenne (pas besoin de parcourir).
    //
    //   hash("pomme") -> casier 3   ┌─ casier 0 ─┐
    //   hash("pain")  -> casier 7   │     ...     │
    //                               │  3: pomme→3 │
    //                               │  7: pain →2 │
    //                               └─────────────┘
    let texte = ["pomme", "pain", "pomme", "lait", "pain", "pomme"];
    let mut comptes: HashMap<&str, u32> = HashMap::new();
    for mot in texte {
        // entry(...).or_insert(0) : « donne-moi la case de ce mot, ou crée-la à 0 ».
        // Le `*` devant déréférence pour ajouter 1 à la valeur stockée.
        *comptes.entry(mot).or_insert(0) += 1;
    }
    println!("comptes = {:?}", comptes); // l'ordre n'est PAS garanti (table de hachage)
    println!("nombre de 'pomme' = {}", comptes.get("pomme").copied().unwrap_or(0));

    // ===================================================================
    // 3) HashSet : un ENSEMBLE de valeurs uniques
    // ===================================================================
    // En interne, c'est un HashMap où seules les CLÉS comptent (pas de valeur).
    // Donc : pas de doublons, et tester la présence est O(1).
    let nombres = [1, 2, 2, 3, 3, 3, 4];
    let uniques: HashSet<i32> = nombres.iter().copied().collect();
    println!("valeurs uniques = {} {:?}", uniques.len(), uniques);
    println!("uniques contient 3 ? {}", uniques.contains(&3)); // O(1), pas de parcours

    // ===================================================================
    // À retenir
    // ===================================================================
    // - Vec      : contigu sur le tas (ptr/len/cap). Index O(1), recherche O(n).
    // - HashMap  : table de hachage. Accès par clé O(1) en moyenne.
    // - HashSet  : un HashMap "sans valeur". Présence O(1), valeurs uniques.
    // Choisir la bonne structure = ta première optimisation (voir notions/).
}
