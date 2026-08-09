package lecons;

/**
 * Leçon 12 — Héritage (extends) et interfaces (implements).
 *
 * Deux façons de PARTAGER du comportement entre classes :
 *   - l'HÉRITAGE (`extends`) : une classe enfant récupère les champs et méthodes d'une
 *     classe parente, et peut les compléter ou les redéfinir. « Un Chien EST UN Animal ».
 *   - les INTERFACES (`implements`) : un CONTRAT de capacités qu'une classe s'engage à
 *     fournir. « Un Canard SAIT nager ».
 * C'est ce qui permet de programmer de façon souple (le POLYMORPHISME, voir la fin).
 */
public class Lecon12HeritageInterfaces {

    // --- Classe ABSTRACTE : un modèle commun qu'on ne peut PAS créer directement ---
    static abstract class Animal {
        protected final String nom; // protected = visible aussi par les sous-classes

        public Animal(String nom) {
            this.nom = nom;
        }

        // Méthode CONCRÈTE : héritée telle quelle par toutes les sous-classes.
        public String presenter() {
            return "Je suis " + nom + " et je fais : " + crier();
        }

        // Méthode ABSTRAITE : pas de corps ici. CHAQUE sous-classe DOIT la fournir.
        public abstract String crier();
    }

    // --- Héritage : « Chien EST UN Animal » ---
    static class Chien extends Animal {
        public Chien(String nom) {
            super(nom); // appelle le constructeur de la classe parente (obligatoire ici)
        }

        @Override // @Override = « je redéfinis une méthode du parent » (le compilateur vérifie)
        public String crier() {
            return "Wouf";
        }
    }

    static class Chat extends Animal {
        public Chat(String nom) {
            super(nom);
        }

        @Override
        public String crier() {
            return "Miaou";
        }
    }

    // --- Interface : un CONTRAT (une liste de capacités à fournir) ---
    interface Nageur {
        String nager(); // pas de corps : c'est juste la promesse d'avoir cette méthode
    }

    // --- Une classe peut HÉRITER d'une classe ET IMPLÉMENTER une ou plusieurs interfaces ---
    static class Canard extends Animal implements Nageur {
        public Canard(String nom) {
            super(nom);
        }

        @Override
        public String crier() {
            return "Coin";
        }

        @Override
        public String nager() {
            return nom + " barbote sur l'étang";
        }
    }

    public static void demo() {
        // POLYMORPHISME : on manipule des objets différents À TRAVERS leur type commun.
        // Un tableau d'Animal peut contenir un Chien, un Chat, un Canard...
        Animal[] animaux = { new Chien("Rex"), new Chat("Misti"), new Canard("Donald") };
        for (Animal a : animaux) {
            // Le MÊME appel a.crier() donne un résultat différent selon le VRAI type
            // de l'objet. C'est ça, le polymorphisme : très puissant pour écrire du code
            // général qui marche pour tous les "Animal".
            System.out.println(a.presenter());
        }

        // Une interface regroupe "ce qui sait nager", quel que soit l'animal derrière.
        Nageur n = new Canard("Daffy");
        System.out.println(n.nager());

        // --- Côté Rust (comparaison) ---
        // Rust n'a PAS d'héritage de classe. À la place, il a les TRAITS, très proches des
        // interfaces Java : un trait déclare des méthodes, qu'on implémente avec
        // « impl MonTrait for MaStruct ». Le polymorphisme passe par ces traits.
        // Les NOMS changent (classe/struct, interface/trait), mais les IDÉES — partager un
        // comportement, programmer contre un contrat — se transposent d'un langage à l'autre.
    }
}
