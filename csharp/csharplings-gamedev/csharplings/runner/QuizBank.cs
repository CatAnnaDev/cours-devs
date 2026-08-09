namespace Csharplings.Runner;

public static class Bank
{
    public static IReadOnlyList<Question> Build() => new List<Question>
    {
        new("01_variables", "const et readonly",
            "Quelle est la difference entre 'const' et 'readonly' ?",
            new[] { "aucune, c'est un alias", "const est fige a la compilation, readonly est fixe une fois au demarrage", "readonly est fige a la compilation, const au demarrage", "const marche sur les objets, readonly non" }, 1,
            "Un const doit etre initialise sur sa ligne de declaration et sa valeur est recopiee partout ou on l'utilise. Un readonly peut etre calcule dans le constructeur, ce qui permet une valeur qui depend du demarrage."),

        new("01_variables", "var n'est pas dynamique",
            "var x = 5; puis x = \"texte\"; Que dit le compilateur ?",
            new[] { "rien, var accepte tout", "erreur : x est un int, fige a la compilation", "un avertissement seulement", "rien, mais ca plante a l'execution" }, 1,
            "var demande au compilateur de DEVINER le type a partir de la valeur, pas de l'oublier. Le type est aussi fige que si on l'avait ecrit. Ce n'est pas le 'var' de JavaScript."),

        new("03_flow", "switch expression",
            "Pourquoi un switch expression exige-t-il souvent une branche '_' ?",
            new[] { "pour la lisibilite", "parce qu'il doit rendre une valeur dans TOUS les cas", "c'est une convention de style", "pour attraper les exceptions" }, 1,
            "Contrairement au switch classique qui peut ne rien faire, l'expression doit produire une valeur. Sans cas par defaut, le compilateur signale les entrees non couvertes."),

        new("03_flow", "break et continue",
            "Dans une boucle, quelle est la difference entre 'continue' et 'break' ?",
            new[] { "continue saute au tour suivant, break sort de la boucle", "break saute au tour suivant, continue sort", "les deux sortent de la boucle", "continue relance la boucle depuis le debut" }, 0,
            "continue quand l'element ne t'interesse pas, break quand tu as trouve ce que tu cherchais. Dans une boucle imbriquee, break ne sort que de la boucle la plus interne."),

        new("04_methods", "out contre ref",
            "Quelle est la difference entre 'out' et 'ref' ?",
            new[] { "aucune", "out DOIT etre rempli par la methode, ref recoit une variable deja initialisee", "ref doit etre rempli, out non", "out marche sur les structs, ref sur les classes" }, 1,
            "Avec out, l'appelant n'a pas besoin d'initialiser et la methode est OBLIGEE d'assigner : c'est un second retour. Avec ref, la variable doit deja avoir une valeur, et la methode peut la lire comme la modifier."),

        new("04_methods", "parametre optionnel",
            "Qu'est-ce qui rend un parametre optionnel ?",
            new[] { "l'attribut [Optional]", "une valeur par defaut dans la signature", "le mot-cle params", "le declarer en dernier" }, 1,
            "float Damage(float baseDamage, float multiplier = 1f) : le second devient optionnel. Ils doivent venir apres les obligatoires, et a l'appel on peut nommer les arguments pour la lisibilite."),

        new("05_strings", "immuabilite",
            "texte.Trim(); sur sa propre ligne. Que vaut 'texte' ensuite ?",
            new[] { "la version nettoyee", "exactement ce qu'il valait avant", "une chaine vide", "null" }, 1,
            "Une string est immuable : toutes ses methodes rendent une NOUVELLE chaine et ne touchent pas a l'originale. Il faut recuperer le resultat : texte = texte.Trim();"),

        new("05_strings", "interpolation",
            "Comment formater un flottant a deux decimales dans une chaine interpolee ?",
            new[] { "$\"{valeur:0.00}\"", "$\"{valeur.Round(2)}\"", "$\"{valeur:F}\" uniquement", "$\"{valeur, 2}\"" }, 0,
            "Les deux-points introduisent un format. La virgule, elle, sert a l'alignement : {valeur,8} reserve huit colonnes. Les deux se combinent : {valeur,8:0.00}."),

        new("02_types", "division entiere",
            "current vaut 3, max vaut 4, les deux sont des int. Que vaut 'float ratio = current / max;' ?",
            new[] { "0.75", "0", "0.8 arrondi", "ca ne compile pas" }, 1,
            "int / int donne un int : la division se fait AVANT la conversion en float, et 3/4 donne 0. Il faut convertir un operande d'abord : (float)current / max."),

        new("02_types", "suffixe float",
            "Pourquoi 'float vitesse = 1.5;' ne compile-t-il pas ?",
            new[] { "1.5 est un double, et on ne convertit pas un double en float implicitement", "il manque le mot-cle new", "float n'accepte pas les decimales", "il faut ecrire 1,5 avec une virgule" }, 0,
            "Un litteral decimal est un double par defaut. Le passage double vers float perd de la precision, donc le compilateur l'exige explicitement : 1.5f."),

        new("02_types", "Parse contre TryParse",
            "Le joueur tape 'douze' dans un champ de saisie. Que fait int.Parse(texte) ?",
            new[] { "il rend 0", "il rend null", "il leve une FormatException", "il rend 12" }, 2,
            "Parse plante sur une entree invalide. int.TryParse rend un bool et remplit la variable en 'out' : il ne plante jamais, et c'est celui qu'on veut face a une saisie utilisateur."),

        new("06_collections", "retirer en iterant",
            "Que se passe-t-il si on appelle list.Remove(x) au milieu d'un foreach sur cette meme liste ?",
            new[] { "l'element est retire, sans probleme", "une InvalidOperationException a l'iteration suivante", "la boucle saute un element", "la liste est copiee automatiquement" }, 1,
            "L'enumerateur d'une List detecte la modification et leve. Les parades : RemoveAll(condition), ou une boucle for qui descend de la fin vers zero."),

        new("06_collections", "cle absente",
            "Quelle ecriture ne plante PAS sur une cle absente d'un Dictionary ?",
            new[] { "inventaire[\"potion\"]", "inventaire.TryGetValue(\"potion\", out int n)", "inventaire.Get(\"potion\")", "inventaire.First(\"potion\")" }, 1,
            "L'indexeur leve une KeyNotFoundException en lecture. TryGetValue rend false et laisse la variable a sa valeur par defaut."),

        new("07_oop", "struct passe a une methode",
            "Une methode recoit un struct en parametre normal et modifie un de ses champs. Qu'arrive-t-il a l'original ?",
            new[] { "il est modifie", "rien, la methode a travaille sur une copie", "ca ne compile pas", "ca depend si le struct est readonly" }, 1,
            "Un struct est copie a l'assignation et au passage. Pour modifier l'original, il faut 'ref'. C'est la source de la moitie des bugs silencieux avec les structs."),

        new("07_oop", "interface contre heritage",
            "Combien de classes de base et combien d'interfaces une classe C# peut-elle avoir ?",
            new[] { "une classe, plusieurs interfaces", "plusieurs des deux", "une de chaque", "plusieurs classes, une interface" }, 0,
            "Heritage simple pour les classes, multiple pour les interfaces. C'est pour ca que la composition et les interfaces sont le modele par defaut en gamedev."),

        new("08_advanced", "requete LINQ rejouee",
            "var grands = scores.Where(s => s > 10); puis on ajoute un score et on parcourt 'grands'. Que voit-on ?",
            new[] { "l'etat au moment du Where", "le nouveau score aussi, la requete se rejoue", "une exception de modification", "une liste vide" }, 1,
            "Where rend une requete paresseuse, pas un resultat. Chaque parcours la rejoue sur la source actuelle. ToList() fige le resultat au moment de l'appel."),

        new("08_advanced", "desabonnement d'event",
            "emetteur.Truc += () => Faire(); puis emetteur.Truc -= () => Faire(); Combien d'abonnes reste-t-il ?",
            new[] { "zero", "un : les deux lambdas sont des objets DIFFERENTS", "ca ne compile pas", "deux" }, 1,
            "Deux lambdas ecrites separement ne sont jamais egales, donc le -= ne retire rien. Il faut s'abonner avec un groupe de methodes, ou garder la reference du delegate."),

        new("08_advanced", "yield return",
            "Une methode avec 'yield return' est appelee mais personne ne parcourt le resultat. Que s'execute-t-il ?",
            new[] { "tout le corps", "rien du tout", "la premiere iteration seulement", "le corps jusqu'au premier yield" }, 1,
            "Un iterateur est paresseux : appeler la methode ne fabrique que la machine a etats. Rien ne tourne avant le premier MoveNext, donc avant le premier tour de foreach."),

        new("08_advanced", "record et egalite",
            "Deux 'record' positionnels construits avec les memes valeurs. Que rend == ?",
            new[] { "true, un record compare ses valeurs", "false, deux objets differents", "ca ne compile pas sans surcharge", "true seulement si c'est un record struct" }, 0,
            "Un record genere l'egalite par valeur, le ToString, la deconstruction et 'with'. C'est ce qui le rend parfait pour des donnees immuables comme un jet de degats."),

        new("08_advanced", "using et Dispose",
            "Deux 'using var' dans le meme bloc. Dans quel ordre les Dispose sont-ils appeles ?",
            new[] { "l'ordre de declaration", "l'ordre INVERSE de la declaration", "un ordre non defini", "seulement le dernier" }, 1,
            "Comme une pile : le dernier ouvert est le premier ferme. Ca compte quand le second depend du premier, par exemple un writer sur un stream."),

        new("09_godot", "ou faire GetNode",
            "Dans quelle fonction faut-il appeler GetNode, et pourquoi ?",
            new[] { "le constructeur, pour l'avoir tot", "_Ready, parce que les enfants sont prets", "_Process, pour rester a jour", "_EnterTree, c'est le plus tot possible" }, 1,
            "Le constructeur tourne avant l'entree dans l'arbre. _EnterTree passe avant que les enfants soient prets. _Ready garantit que les enfants existent, et on stocke le resultat une fois pour toutes."),

        new("09_godot", "noeud libere",
            "Apres node.QueueFree(), quel test dit la verite sur l'objet natif ?",
            new[] { "node == null", "node is null", "IsInstanceValid(node)", "node.IsQueuedForDeletion()" }, 2,
            "Le wrapper C# survit a la destruction du natif, donc la variable n'est pas nulle. Seul IsInstanceValid regarde l'objet natif. C'est le piege inverse de celui de Unity."),

        new("09_godot", "delta time",
            "Position += direction * 200f; dans _Process. Que se passe-t-il sur un ecran 144 Hz ?",
            new[] { "rien, c'est correct", "l'objet va 2,4 fois plus vite que sur un ecran 60 Hz", "l'objet va plus lentement", "Godot corrige automatiquement" }, 1,
            "Sans delta, la vitesse est par IMAGE et non par seconde. A 144 Hz il y a 2,4 fois plus d'images, donc 2,4 fois plus de deplacement. Tout ce qui bouge se multiplie par delta."),

        new("10_gamedev", "aller vers une cible",
            "Pour aller vers une cible a vitesse constante, on ecrit (cible - position) puis quoi ?",
            new[] { "rien, on multiplie directement par la vitesse", ".Normalized() avant de multiplier", ".Length() avant de multiplier", ".Abs()" }, 1,
            "Sans Normalized, la longueur du vecteur depend de la distance : plus la cible est loin, plus on va vite. Normalized ramene la direction a une longueur de 1."),

        new("10_gamedev", "monde vers case",
            "Comment convertir une position en pixels vers une case de grille ?",
            new[] { "une division ENTIERE, ou FloorToInt", "un arrondi au plus proche", "une multiplication par la taille de case", "un modulo par la largeur" }, 0,
            "Case vers monde : case fois taille. Monde vers case : division entiere. L'arrondi au plus proche decale la grille d'une demi-case et cree des bugs de collision aux bords."),

        new("11_patterns", "annuler / refaire",
            "Dans un historique a deux piles, que fait une NOUVELLE action apres plusieurs annulations ?",
            new[] { "elle s'empile sur les actions annulees", "elle vide la pile des annulees", "elle est refusee", "elle inverse les deux piles" }, 1,
            "La branche du futur n'existe plus : garder les actions annulees permettrait de 'refaire' quelque chose qui n'a plus de sens apres la nouvelle action."),

        new("11_patterns", "donnees partagees",
            "500 gobelins a l'ecran. Qu'est-ce qui est par instance et qu'est-ce qui est partage ?",
            new[] { "tout par instance, c'est plus simple", "points de vie et position par instance, degats et vitesse partages", "tout partage, une seule fiche", "degats par instance, points de vie partages" }, 1,
            "Ce qui change appartient a l'instance, ce qui est commun est une REFERENCE vers une fiche unique. C'est ce que sont les Resource de Godot et les ScriptableObject de Unity."),

        new("12_math", "ecart d'angles",
            "De 350 degres a 10 degres, quel est l'ecart le plus court ?",
            new[] { "340 degres", "20 degres", "-340 degres", "180 degres" }, 1,
            "Il faut ramener l'ecart dans -180..180, c'est ce que fait AngleDifference. Sans ca, une tourelle fait un tour complet au lieu de pivoter de 20 degres."),

        new("12_math", "comparer des distances",
            "Pour trouver l'ennemi le plus proche parmi cent, qu'est-ce qui est correct ET rapide ?",
            new[] { "comparer les distances au CARRE", "comparer les distances, avec la racine", "comparer les distances de Manhattan", "trier avec OrderBy sur la distance" }, 0,
            "Si a < b alors a au carre < b au carre pour des longueurs positives. On evite donc cent racines carrees sans changer le resultat. OrderBy, en plus, alloue."),

        new("12_math", "aleatoire a graine",
            "A quoi sert un generateur aleatoire a graine dans un jeu ?",
            new[] { "a etre plus rapide", "a rejouer exactement le meme donjon, et a debuguer un bug aleatoire", "a eviter les repetitions", "a rien, c'est moins aleatoire" }, 1,
            "Meme graine, meme suite : le donjon est reproductible, un replay rejoue a l'identique, et un bug 'aleatoire' devient reproductible. En prime, des flux separes evitent qu'un tirage en plus decale tout le reste."),

        new("13_systems", "pas de temps fixe",
            "Une frame a dure 2 secondes avec un pas de 1/60. Combien de pas de physique faut-il jouer ?",
            new[] { "120, sinon la physique prend du retard", "un seul, avec un gros delta", "un nombre plafonne, et on jette le reste", "zero, on saute la frame" }, 2,
            "120 pas prendraient plus d'une frame a calculer, ce qui en generera encore plus : c'est la spirale de la mort. On plafonne, et on abandonne le retard au lieu de l'accumuler."),

        new("13_systems", "sauvegarde robuste",
            "Un fichier de sauvegarde a un champ manquant. Que doit faire le chargement ?",
            new[] { "planter, le fichier est corrompu", "prendre la valeur par defaut pour ce champ et continuer", "recreer une sauvegarde vide", "demander au joueur" }, 1,
            "TryGetValue et TryParse partout : une sauvegarde qui plante, c'est une partie perdue. Et les flottants s'ecrivent en InvariantCulture, sinon la virgule francaise casse tout."),

        new("14_engine", "ou mettre le mouvement",
            "Le deplacement d'un personnage va dans quelle fonction ?",
            new[] { "_Process / Update", "_PhysicsProcess / FixedUpdate", "les deux", "un timer" }, 1,
            "La physique avance a pas fixe, le rendu suit le framerate. Le mouvement va dans la boucle fixe, l'affichage et l'input dans la boucle de rendu."),

        new("14_engine", "camera en retard",
            "Une camera qui suit le joueur a une frame de retard. Pourquoi ?",
            new[] { "son _Process passe AVANT celui du joueur", "elle lit une position perimee du serveur", "il manque un delta", "elle est dans le mauvais arbre" }, 0,
            "Il faut la faire passer apres sa cible : ProcessPriority plus grand chez Godot, LateUpdate chez Unity. Sinon elle suit toujours la position d'avant."),

        new("14_engine", "diagonale trop rapide",
            "On additionne les entrees horizontale et verticale sans rien d'autre. Que vaut la vitesse en diagonale ?",
            new[] { "la meme", "41 pour cent plus rapide", "29 pour cent plus lente", "le double" }, 1,
            "(1,1) a une longueur de racine de 2, soit environ 1,41. Il faut normaliser le vecteur d'entree, sinon marcher en diagonale est la meilleure strategie du jeu."),

        new("14_engine", "masques de collision",
            "Les valeurs d'un masque de couches doivent etre quoi ?",
            new[] { "des entiers consecutifs 1, 2, 3, 4", "des puissances de deux 1, 2, 4, 8", "des chaines de caracteres", "n'importe quels entiers uniques" }, 1,
            "Une couche par BIT, donc des puissances de deux. Ajouter c'est un OU, retirer c'est un ET avec le complement, tester au moins une c'est (masque et couches) different de zero."),

        new("15_perf", "cout d'un foreach",
            "Un foreach sur un parametre declare 'IEnumerable<int>' alloue combien, contre le meme declare 'List<int>' ?",
            new[] { "rien dans les deux cas", "l'IEnumerable emballe son enumerateur, la List non", "la List alloue, l'IEnumerable non", "les deux allouent autant" }, 1,
            "L'enumerateur de List est une structure. Derriere l'interface il est emballe dans un objet, a chaque appel. Declarer le type concret suffit a supprimer l'allocation."),

        new("15_perf", "boxing invisible",
            "Un struct sans IEquatable sert de cle de Dictionary. Que se passe-t-il a chaque comparaison ?",
            new[] { "rien de special", "il est emballe dans un objet sur le tas", "le dictionnaire le recopie", "une exception" }, 1,
            "Sans IEquatable, le comparateur par defaut passe par ValueType.Equals, qui emballe. Mesurable, et invisible autrement. Implementer IEquatable<T> et GetHashCode l'elimine."),

        new("15_perf", "supprimer en boucle",
            "On parcourt une liste de l'index 0 vers la fin en retirant des elements. Quel est le bug ?",
            new[] { "une exception", "des elements sont SAUTES", "la liste est inversee", "aucun bug" }, 1,
            "Retirer decale tout ce qui suit : l'element qui prend la place du supprime n'est jamais visite. On parcourt de la fin vers zero, ou on utilise RemoveAll."),

        new("15_perf", "HUD par frame",
            "Un label mis a jour avec $\"PV {x}\" a chaque frame. Quel est le probleme et la parade ?",
            new[] { "aucun probleme", "60 chaines par seconde pour rien : ne reconstruire que si la valeur a change", "il faut un StringBuilder statique", "il faut passer le label en static" }, 1,
            "Une chaine est immuable : chaque interpolation en fabrique une neuve. On garde la derniere valeur affichee et la derniere chaine produite, et on ne refait le travail que si ca a bouge."),

        new("16_memory", "taille d'un objet vide",
            "Combien coute un objet de classe totalement vide sur le tas, en 64 bits ?",
            new[] { "0 octet", "8 octets", "24 octets", "autant que ses champs" }, 2,
            "L'en-tete que tout objet porte. Un premier int est meme gratuit, il tient dans le remplissage ; c'est le quatrieme qui coute 8 octets de plus."),

        new("16_memory", "parametre objet",
            "Une methode recoit un objet et fait 'parametre = new Autre();'. Que voit l'appelant ?",
            new[] { "son objet a change", "rien : l'adresse a ete passee PAR COPIE", "une exception", "ca depend si la classe est sealed" }, 1,
            "Une variable objet contient une adresse, et cette adresse est copiee au passage. Modifier les champs de l'objet se voit ; reassigner le parametre, non. Il faudrait 'ref'."),

        new("16_memory", "lambda et capture",
            "Une lambda qui ne capture rien alloue combien a chaque passage ?",
            new[] { "0 octet, elle est mise en cache", "96 octets", "24 octets", "autant que son corps" }, 0,
            "Le compilateur la fabrique une fois et la reutilise. Des qu'elle capture une variable locale, il faut une fermeture : 96 octets, a chaque passage."),

        new("16_memory", "copie defensive",
            "On appelle une methode qui modifie un champ 'readonly' de type struct. Que se passe-t-il ?",
            new[] { "ca ne compile pas", "le champ est modifie", "la modification part sur une COPIE et disparait", "une exception a l'execution" }, 2,
            "Le compilateur fabrique une copie avant l'appel pour garantir le readonly. Le code compile, tourne, et ne fait rien. La parade : un 'readonly struct' qui rend une nouvelle instance."),

        new("16_memory", "list contre tableau",
            "list[0].MethodeQuiModifie() sur une List<struct>. Et array[0].MethodeQuiModifie() ?",
            new[] { "les deux modifient", "aucun ne modifie", "la liste modifie un temporaire, le tableau modifie l'original", "le tableau modifie un temporaire, la liste l'original" }, 2,
            "L'indexeur d'une List est une methode : elle rend une copie. Un element de tableau est une vraie variable. C'est la meme famille de piege que la copie defensive."),

        new("17_ecs", "pointeur fantome",
            "Un identifiant d'entite ne contient que l'index du slot. Le slot est libere puis reutilise. Que fait l'ancien identifiant ?",
            new[] { "il ne resout plus", "il resout sur la NOUVELLE entite", "il leve une exception", "il resout sur null" }, 1,
            "Il faut une GENERATION en plus de l'index : detruire l'incremente, et resoudre compare les deux. Sans ca, un ancien identifiant designe silencieusement l'occupant suivant."),

        new("17_ecs", "suppression par echange",
            "Dans une colonne de composants dense, on retire une ligne du milieu. Comment garder la memoire contigue ?",
            new[] { "decaler tout ce qui suit", "echanger avec la DERNIERE ligne puis raccourcir", "laisser un trou", "reallouer la colonne" }, 1,
            "Temps constant au lieu de lineaire. Le seul piege : il faut mettre a jour la table creuse de l'entite qui a DEMENAGE, sinon on ne la retrouve plus."),

        new("17_ecs", "HasAll d'un masque",
            "Comment tester qu'un masque d'entite contient TOUS les bits demandes ?",
            new[] { "(bits & requis) != 0", "(bits & requis) == requis", "bits == requis", "(bits | requis) == bits" }, 1,
            "Compare a zero, c'est 'au moins un bit en commun', donc HasAny. Comparer au masque DEMANDE est ce qui distingue les deux, et confondre les deux est le bug classique."),

        new("17_ecs", "enumerateur en structure",
            "Pourquoi 'foreach' sur une requete avec un enumerateur en STRUCTURE n'alloue rien ?",
            new[] { "foreach n'exige pas IEnumerable : une methode GetEnumerator publique suffit", "les structures sont toujours gratuites", "le compilateur met en cache l'enumerateur", "il alloue quand meme, mais sur la pile du GC" }, 0,
            "foreach fonctionne par convention, pas par interface. Si GetEnumerator rend une structure avec MoveNext et Current, rien n'est emballe. La version 'yield return' alloue son etat sur le tas."),

        new("18_bridge", "propriete du moteur",
            "Chez Godot, une ligne qui lit node.Position trois fois coute combien d'appels natifs ?",
            new[] { "aucun, c'est un champ", "un seul, le compilateur met en cache", "trois", "ca depend du type du noeud" }, 2,
            "Une propriete du moteur n'est pas un champ : chaque acces traverse la frontiere C# vers natif. On lit une fois dans une variable locale, on calcule, on reecrit une fois."),

        new("18_bridge", "cout invisible",
            "Pourquoi le cout des appels natifs n'apparait-il dans aucun profil de ramasse-miettes ?",
            new[] { "parce qu'il n'alloue rien", "parce que le profileur les ignore volontairement", "parce qu'ils sont trop rapides", "parce qu'ils sont comptes en generation 2" }, 0,
            "Ils ne creent aucun objet : rien a collecter, donc rien a voir dans un profil memoire. C'est exactement pour ca que ce cout reste introuvable si on ne le cherche pas au bon endroit."),

        new("18_bridge", "chaine vers nom moteur",
            "bus.Emit(\"died\") appele cent fois. Combien d'objets StringName sont fabriques ?",
            new[] { "zero, les litteraux sont mis en cache", "cent, par conversion implicite", "un seul", "cela depend du moteur" }, 1,
            "La conversion chaine vers StringName est implicite, donc invisible a la lecture, et elle alloue a chaque appel. Un champ 'static readonly' regle le probleme : c'est a ca que servent les SignalName generes."),

        new("18_bridge", "cout d'un signal",
            "Un signal moteur a deux arguments contre un event C# 'Action<int>' : lequel alloue, et pourquoi ?",
            new[] { "aucun des deux", "le signal, parce que 'params' fabrique un tableau a chaque emission", "l'event, parce qu'il capture", "les deux autant" }, 1,
            "Le tableau d'arguments est neuf a chaque appel. Un signal SANS argument est gratuit, lui : le compilateur passe un tableau vide partage. Signal pour ce que l'editeur doit brancher, event C# pour le reste."),

        new("19_unity", "ou s'abonner",
            "Chez Unity, dans quelle paire de fonctions s'abonne-t-on et se desabonne-t-on a un evenement ?",
            new[] { "Start et OnDestroy", "OnEnable et OnDisable", "Awake et OnDestroy", "le constructeur et le destructeur" }, 1,
            "OnEnable rejoue a CHAQUE reactivation, donc un script reactive se reabonne. Et la destruction passe par OnDisable avant OnDestroy : un seul endroit a ecrire couvre les deux cas. Avec Start et OnDestroy, un script desactive reste abonne, et reactive il ne se reabonne jamais."),

        new("19_unity", "Awake ou Start",
            "Pourquoi chercher un autre objet de la scene est-il un pari dans Awake et une certitude dans Start ?",
            new[] { "Awake est plus rapide, donc moins fiable", "tous les Awake de la scene passent avant tous les Start", "Awake ne tourne pas au chargement de scene", "il n'y a aucune difference" }, 1,
            "L'ordre entre deux Awake n'est pas defini, donc dans le tien l'autre objet n'existe peut-etre pas encore. Quand ton Start tourne, tous les Awake sont passes. L'equivalent Godot est _EnterTree contre _Ready."),

        new("19_unity", "deltaTime dans FixedUpdate",
            "Que rend Time.deltaTime lu depuis FixedUpdate ?",
            new[] { "le temps de la frame affichee", "fixedDeltaTime, le pas de physique", "zero", "une exception" }, 1,
            "Unity substitue le pas fixe quand on est dans la boucle de physique. Le code a donc l'air correct des deux cotes alors qu'il ne mesure pas la meme chose. Deux autres pieges de la meme API : maximumDeltaTime plafonne un gel a un tiers de seconde, et unscaledDeltaTime ignore timeScale."),

        new("19_unity", "singleton Unity",
            "Dans le OnDestroy d'un singleton Unity, pourquoi ecrire 'if (Instance == this) Instance = null;' plutot que 'Instance = null;' ?",
            new[] { "pour eviter une exception", "parce qu'un doublon qui se detruit effacerait l'instance du vrai", "parce que == est plus rapide", "aucune raison, c'est du style" }, 1,
            "Une scene chargee deux fois cree un doublon, qui se detruit lui-meme dans son Awake. Sans le garde, son OnDestroy met la propriete a null et le vrai gestionnaire devient introuvable pour toute la partie."),

        new("19_unity", "objet detruit",
            "Chez Unity, apres Destroy(obj) et la fin de la frame : que rendent 'obj == null' et 'obj is null' ?",
            new[] { "true et true", "false et false", "TRUE et FALSE", "FALSE et TRUE" }, 2,
            "Unity surcharge l'operateur ==, qui rend true quand l'objet natif est parti. Le motif 'is null' ne passe PAS par l'operateur et voit une reference bien vivante. Et le '?.' non plus : il appelle quand meme."),

        new("19_unity", "serialisation Unity",
            "Lequel de ces membres Unity sauvegarde-t-il ?",
            new[] { "public int Score { get; set; }", "[SerializeField] private int _score;", "public readonly int Score;", "public static int Score;" }, 1,
            "Unity serialise les CHAMPS, pas les proprietes, et ni readonly ni static. Le champ prive marque SerializeField apparait dans l'inspecteur sans etre public dans le code."),

        new("19_unity", "dictionnaire dans l'inspecteur",
            "Comment faire survivre un Dictionary a la serialisation Unity ?",
            new[] { "le marquer [SerializeField]", "l'aplatir en deux listes dans les callbacks de serialisation", "utiliser SerializeReference", "c'est impossible" }, 1,
            "Unity ne sait pas serialiser un Dictionary et ne previent pas. On remplit deux listes dans OnBeforeSerialize et on reconstruit le dictionnaire dans OnAfterDeserialize."),

        new("19_unity", "camera en retard",
            "Chez Unity, une camera qui suit sa cible dans Update a parfois une frame de retard. Pourquoi, et ou faut-il la mettre ?",
            new[] { "il manque un delta ; le corriger suffit", "son Update passe avant celui de la cible, et l'ordre des scripts n'est pas garanti : il faut LateUpdate", "elle doit passer en FixedUpdate", "il faut la mettre enfant de la cible" }, 1,
            "LateUpdate passe apres TOUS les Update de la scene, donc elle voit forcement la position finale. C'est l'equivalent du ProcessPriority plus grand chez Godot."),

        new("19_unity", "input dans FixedUpdate",
            "A 60 images par seconde avec une physique a 50 Hz, que devient un appui lu dans FixedUpdate ?",
            new[] { "il est vu une fois, comme dans Update", "il peut etre PERDU, ou compte DEUX fois", "il est vu deux fois systematiquement", "FixedUpdate ne peut pas lire l'input" }, 1,
            "Certaines frames ne jouent aucun pas de physique, d'autres en jouent deux. On lit l'input dans Update, on memorise, et on consomme le drapeau dans FixedUpdate."),

        new("19_unity", "GetComponent dans Update",
            "GetComponent appele dans Update pendant soixante frames : combien de recherches, et quelle est la parade ?",
            new[] { "une seule, Unity met en cache", "soixante ; chercher dans Awake et garder dans un champ", "soixante, et il n'y a rien a faire", "zero, c'est resolu a la compilation" }, 1,
            "Chaque appel parcourt les composants de l'objet, et c'est un appel natif. C'est le meme reflexe que cacher GetNode chez Godot. TryGetComponent, en plus, remplace le GetComponent suivi d'un test null."),

        new("19_unity", "materiau clone",
            "renderer.material.color = rouge; sur cent ennemis. Combien de materiaux en memoire ?",
            new[] { "un", "cent, et aucun ne sera ramasse par le GC", "deux", "cela depend du shader" }, 1,
            "Lire '.material' clone. Ce sont des objets natifs, donc il faut un Destroy pour chacun. La vraie reponse pour une couleur par instance est un MaterialPropertyBlock : zero clone, et le rendu reste groupe."),

        new("19_unity", "compilation en avance",
            "Une fabrique par reflexion marche parfaitement dans l'editeur Unity et echoue sur console. Pourquoi ?",
            new[] { "la console est trop lente", "IL2CPP compile en avance : un type que personne n'instancie dans le code n'a pas de constructeur genere", "la reflexion est interdite sur console", "il manque un fichier de configuration" }, 1,
            "L'editeur interprete, la console est compilee a l'avance. Le compilateur ne genere que ce qu'il VOIT. La parade est une table de '() => new Machin()' : chaque construction est visible, rien n'est supprime, et un type oublie se remarque a la lecture de la table plutot que trois heures plus tard sur le devkit."),

        new("19_unity", "statiques entre parties",
            "Le rechargement de domaine est desactive. Que devient un 'static int _score' en relancant la partie ?",
            new[] { "il repart a zero", "il garde sa valeur de la partie precedente", "une exception", "il devient null" }, 1,
            "Rien ne les reinitialise, evenements statiques compris : des objets morts restent abonnes et repondent encore. Il faut un reset explicite appele au demarrage de chaque partie."),

        new("20_time", "affichage saccade",
            "La physique tourne a 60 Hz, l'ecran a 240 Hz, et on affiche directement la position physique. Que voit le joueur ?",
            new[] { "un mouvement fluide", "quatre frames identiques puis un saut", "un mouvement deux fois trop rapide", "rien, c'est identique" }, 1,
            "C'est l'escalier. Il faut garder la position d'avant le pas et celle d'apres, et afficher entre les deux avec l'alpha de l'accumulateur. Un teleport doit remettre LES DEUX etats."),

        new("20_time", "timer qui derive",
            "Un timer d'intervalle 0.1 s remet son compteur a ZERO apres chaque declenchement. Sur dix secondes a 0.03 s par frame, combien de declenchements manque-t-il ?",
            new[] { "aucun", "environ seize", "un seul", "la moitie" }, 1,
            "Mesure faite : 83 au lieu de 99. Remettre a zero jette le reste a chaque fois, et l'ecart ne se rattrape jamais. On SOUSTRAIT l'intervalle, et on boucle pour rattraper les gros deltas."),

        new("20_time", "compteur d'images en pause",
            "Le jeu est en pause avec une echelle de temps a zero. Que vaut '1f / Delta' pour afficher les FPS ?",
            new[] { "60", "zero", "l'infini", "NaN" }, 2,
            "Diviser par un delta de jeu nul donne l'infini. Le compteur d'images se calcule sur le temps REEL, qui continue de s'ecouler : c'est aussi lui qui fait vivre le menu de pause et les fondus de son."),

        new("20_time", "float et temps absolu",
            "Un 'float' accumule 1/60 pendant 10 000 secondes. De combien derive-t-il ?",
            new[] { "de rien", "d'environ 28 SECONDES", "d'un millieme de seconde", "il plante" }, 1,
            "600 000 additions, chacune arrondie. Et passe 524 288 secondes, soit six jours, un float ne peut plus representer un ecart d'une frame : l'horloge s'arrete net, en silence. Temps absolu en double, ou comptage en pas entiers."),

        new("21_physics", "projectile qui traverse",
            "Une balle avance de 100 pixels par frame. Le mur en fait 10 d'epaisseur. Que voit un test de chevauchement fait une fois par frame ?",
            new[] { "l'impact", "rien du tout, la balle a traverse entre deux images", "une exception", "un impact sur deux" }, 1,
            "C'est le tunneling. Il faut tester le TRAJET et non les deux extremites : un balayage par tranches, qui donne en plus l'instant exact du contact. Sinon on decoupe le deplacement en morceaux plus petits que le plus fin des murs."),

        new("21_physics", "glisser le long d'un mur",
            "Comment garder la part du mouvement parallele a une surface ?",
            new[] { "velocity - normale * (velocity . normale)", "velocity * normale", "velocity + normale", "velocity.Normalized() * normale.Length()" }, 0,
            "On retire la composante qui rentrait dans la surface. Le test que tout le monde oublie : ne corriger QUE si le produit scalaire est negatif, sinon on colle le joueur aux murs qu'il vient de quitter, et le sol mange son saut."),

        new("21_physics", "integration stable",
            "Le meme ressort, deux lignes echangees. Quel ordre est stable ?",
            new[] { "position d'abord, puis vitesse", "vitesse d'abord, puis position avec la NOUVELLE vitesse", "les deux sont equivalents", "il faut les faire en meme temps" }, 1,
            "Mesure faite : l'ordre naif fait monter l'amplitude a 4 fois 10 puissance 13 en dix secondes, l'ordre semi-implicite reste a 1,01. Ce n'est pas une question de style."),

        new("21_physics", "sortir d'un mur",
            "On repousse un corps hors d'un solide mais on ne touche pas a sa vitesse. Que se passe-t-il ?",
            new[] { "rien, c'est suffisant", "il repart dedans, accelere, et finit par traverser", "il tremble sur place a vitesse constante", "il reste bloque" }, 1,
            "La vitesse continue de croitre a chaque frame, et un jour le deplacement d'une frame depasse l'epaisseur du sol. Il faut annuler la composante de vitesse dirigee vers la surface, et seulement celle-la."),

        new("22_json", "la casse compte",
            "Un fichier ecrit en camelCase est relu avec un JsonSerializerOptions par defaut. Que se passe-t-il ?",
            new[] { "une JsonException", "rien : aucune erreur, et tous les champs a leur valeur par defaut", "les champs sont remplis correctement", "un avertissement dans la console" }, 1,
            "System.Text.Json est SENSIBLE a la casse par defaut, contrairement a Newtonsoft. Une sauvegarde entiere revient vide en silence. La parade : PropertyNameCaseInsensitive = true, ou la meme PropertyNamingPolicy des deux cotes."),

        new("22_json", "champs et proprietes",
            "Une classe a 'public int Gold;' et 'public int Level { get; set; }'. Que contient le JSON par defaut ?",
            new[] { "les deux", "seulement Level", "seulement Gold", "aucun des deux" }, 1,
            "Le serialiseur ne regarde que les PROPRIETES publiques. Un champ a besoin de [JsonInclude], ou de IncludeFields = true dans les options. C'est l'inverse exact de la serialisation de Unity, qui ne regarde que les champs."),

        new("22_json", "type statique contre type reel",
            "Item item = new Weapon { Damage = 12 }; puis JsonSerializer.Serialize(item). Que contient le fichier ?",
            new[] { "l'arme entiere, degats compris", "seulement les proprietes de Item : les degats sont perdus", "une exception", "un discriminant de type" }, 1,
            "Le serialiseur suit le type STATIQUE de T, pas l'objet qu'il a en main. Serialize(v, v.GetType()) ou Serialize<object>(v) corrigent la RACINE seulement : dans une List<Item>, chaque element reste tronque. La vraie reponse est [JsonPolymorphic]."),

        new("22_json", "propriete declaree object",
            "Une propriete 'public object Payload' contient une Weapon. Que rend-elle apres un aller-retour ?",
            new[] { "une Weapon", "un JsonElement", "null", "un Dictionary<string, object>" }, 1,
            "object ecrit tout (c'est le seul type pour lequel le serialiseur cherche le type reel) mais ne relit rien : il rend un JsonElement, du texte analyse. 'object' est un aller sans retour, et c'est pour ca qu'on ecrit du generique."),

        new("22_json", "JsonSerializerOptions",
            "Ou faut-il declarer un JsonSerializerOptions ?",
            new[] { "dans la methode, c'est plus lisible", "en static readonly, une fois pour toute l'application", "peu importe, c'est un objet leger", "dans le constructeur de chaque objet serialise" }, 1,
            "Mesure faite : environ 9 500 octets par configuration neuve, parce que le premier usage construit tout le cache de reflexion du type. Et le premier usage GELE les options : les modifier ensuite leve InvalidOperationException. Une variante se fabrique avec new JsonSerializerOptions(autre)."),

        new("22_json", "un Vector2 dans une sauvegarde",
            "Un Vector2 (struct readonly, proprietes en lecture seule) est ecrit puis relu sans convertisseur. Que vaut-il ?",
            new[] { "sa valeur d'origine", "Vector2.Zero, sans aucune erreur", "null", "une JsonException" }, 1,
            "L'ecriture marche, la relecture rend zero : le serialiseur construit le struct vide et n'a aucun setter pour le remplir. Le joueur reapparait a l'origine. Il faut un JsonConverter<Vector2>, ou [JsonConstructor] quand le type est a toi."),

        new("22_json", "genericite totale",
            "Comment ecrire UN convertisseur valable pour Stat<int>, Stat<float> et Stat<string> ?",
            new[] { "un JsonConverter<Stat<object>>", "un JsonConverterFactory qui reconnait typeof(Stat<>)", "trois convertisseurs, il n'y a pas d'autre solution", "un attribut [JsonConverter] sur chaque usage" }, 1,
            "CanConvert compare type.GetGenericTypeDefinition() a typeof(Stat<>), la definition OUVERTE. CreateConverter fait le chemin inverse avec MakeGenericType puis Activator.CreateInstance. Un convertisseur ecrit une fois, valable pour tous les T d'aujourd'hui et ceux de demain."),

        new("22_json", "le discriminant",
            "Dans un fichier polymorphe, ou doit se trouver le discriminant de type ?",
            new[] { "n'importe ou dans l'objet", "en PREMIER champ de l'objet", "en dernier", "dans un fichier a part" }, 1,
            "Sinon la lecture leve NotSupportedException : il faut savoir quoi construire avant de lire les champs. Un outil qui reordonne tes fichiers par ordre alphabetique casse donc tous les chargements. Et serialiser depuis la classe DERIVEE n'ecrit aucun discriminant du tout."),

        new("22_json", "JsonElement et duree de vie",
            "Une methode fait 'using JsonDocument d = ...' et rend d.RootElement. Que vaut cet element a l'appelant ?",
            new[] { "une copie valide", "une ObjectDisposedException des qu'on le lit", "null", "un element vide" }, 1,
            "Un JsonElement ne CONTIENT pas ses donnees : il pointe dans le tampon du document, deja rendu au pool. Clone() en fait une copie autonome, seule facon de faire sortir un morceau de JSON de son using."),

        new("22_json", "le cout par image",
            "200 entites serialisees a chaque image. Quelle version alloue le moins ?",
            new[] { "JsonSerializer.Serialize, qui rend une string", "SerializeToUtf8Bytes", "Serialize(writer, ...) sur un Utf8JsonWriter remis a zero avec Reset", "les trois sont equivalentes" }, 2,
            "Mesure faite : 19 360 octets pour la string, 9 856 pour le tableau d'octets, 312 pour le writer reutilise. La string paye deux fois, parce que le JSON est fabrique en UTF-8 puis retranscrit en UTF-16. En lecture, Utf8JsonReader lit un champ a zero octet."),

        new("22_json", "les champs qu'on ne connait pas",
            "Une v2 du jeu charge une sauvegarde ecrite par la v3, puis la reecrit. Sans [JsonExtensionData], que devient le champ ajoute en v3 ?",
            new[] { "il est conserve", "il est perdu definitivement", "le chargement echoue", "il declenche un avertissement" }, 1,
            "Le champ inconnu est ignore a la lecture, donc absent a la reecriture. Lancer une vieille version UNE fois detruit la sauvegarde. [JsonExtensionData] sur un Dictionary<string, JsonElement> le met de cote et le reecrit."),

        new("23_linq", "execution differee",
            "var q = liste.Where(Test) sur 10 elements, puis q.Count(), puis q.First(). Combien d'appels a Test ?",
            new[] { "10 : le resultat est mis en cache", "11 : la requete se rejoue, Count parcourt tout et First s'arrete au premier", "20 : deux parcours complets", "aucun, tant qu'on n'ecrit pas ToList" }, 1,
            "Une requete est une recette, pas un resultat : chaque parcours la rejoue depuis le debut. Count va jusqu'au bout, First s'arrete des qu'il trouve. Des qu'on compte poser plus d'une question, on materialise une fois avec ToList."),

        new("23_linq", "capture dans une boucle",
            "for (int i = 0; i < 3; i++) lambdas.Add(() => i); Que rendent les trois lambdas ?",
            new[] { "0, 1, 2", "3, 3, 3", "0, 0, 0", "une exception" }, 1,
            "Une lambda capture la VARIABLE, pas sa valeur, et le 'for' n'en a qu'une pour toute la boucle. Une copie dans le corps de la boucle donne une variable par tour. Le meme piege dans un 'foreach' a ete corrige dans le langage."),

        new("23_linq", "le prix d'une capture",
            "Qu'est-ce qui coute une allocation : la lambda, ou la capture ?",
            new[] { "la lambda, toujours", "la capture : sans elle, le delegue est fabrique une fois et mis en cache", "les deux, a chaque appel", "aucune des deux" }, 1,
            "Une lambda sans capture est mise en cache par le compilateur : zero octet. Des qu'elle capture, il faut un objet pour transporter la capture. Le mot-cle 'static' devant une lambda interdit la capture, et transforme l'oubli en erreur de compilation."),

        new("23_linq", "yield et arguments",
            "Une methode avec 'yield return' commence par 'if (source is null) throw'. Quand l'exception sort-elle ?",
            new[] { "a l'appel", "au premier parcours, pas avant", "jamais", "a la compilation" }, 1,
            "Une methode qui contient un yield ne s'execute PAS avant le premier MoveNext, verification des arguments comprise. Il faut donc une methode d'entree sans yield qui verifie et delegue a un iterateur prive."),

        new("23_linq", "GroupBy contre ToLookup",
            "Quelle est la difference entre GroupBy et ToLookup ?",
            new[] { "aucune", "GroupBy est differe, ToLookup est immediat", "ToLookup est differe, GroupBy immediat", "ToLookup ne marche que sur les nombres" }, 1,
            "Deux operateurs qui font la meme chose a deux moments opposes. Et une cle absente rend une sequence VIDE dans un lookup, la ou un Dictionary leverait : c'est ce qui le rend plus pratique pour un index."),

        new("23_linq", "trouver le minimum",
            "Pour trouver l'ennemi le plus faible d'une liste de 200, que choisir ?",
            new[] { "OrderBy(...).First()", "MinBy(...)", "Sort() puis [0]", "les trois sont equivalents" }, 1,
            "MinBy fait un seul passage et ne copie pas la source. OrderBy().First() demande un tri complet pour n'en garder qu'un. Et OrderBy est stable la ou List.Sort ne l'est pas, mais List.Sort modifie la liste d'origine."),

        new("23_linq", "FirstOrDefault sur un struct",
            "Une recherche de Vector2 ne trouve rien. Que rend FirstOrDefault ?",
            new[] { "null", "Vector2.Zero, indistinguable d'une vraie position", "une exception", "le premier element quand meme" }, 1,
            "default(T) pour un struct, c'est zero. Rien ne distingue 'pas trouve' de 'trouve a l'origine', et l'ennemi fonce vers le coin de la carte. La surcharge FirstOrDefault(predicat, repli) prend une valeur de repli explicite."),

        new("23_linq", "sequence infinie",
            "Que fait Count() sur une sequence produite par 'while (true) yield return ...' ?",
            new[] { "il rend l'infini", "il ne revient jamais et fige la boucle de jeu", "il leve une exception", "il s'arrete a 1000" }, 1,
            "Sur une source infinie, il faut TOUJOURS un operateur qui limite - Take, TakeWhile, First, Any - avant un operateur qui compte, trie ou materialise. Sans exception et sans message : le jeu se fige, c'est tout."),

        new("23_linq", "le parametre parcouru deux fois",
            "Une methode prend un IEnumerable<int> et fait Count() puis Max(). Que se passe-t-il si on lui passe un iterateur de fichier ?",
            new[] { "rien de special", "le second parcours rend le vide, sans erreur", "une exception explicite", "le fichier est relu automatiquement" }, 1,
            "Un flux consomme ne se rembobine pas. Materialiser une fois en entree - source as List<T> ?? source.ToList() - ou prendre IReadOnlyList<T> en parametre, ce qui fait disparaitre le probleme a la compilation."),

        new("23_linq", "l'enumerateur emballe",
            "Le meme foreach sur une List<T>, puis sur la meme liste rangee dans un IEnumerable<T>. Qu'est-ce qui change ?",
            new[] { "rien", "la seconde version alloue : l'interface emballe l'enumerateur, qui est un struct", "la seconde est plus rapide", "la premiere alloue, pas la seconde" }, 1,
            "Mesure faite : 0 octet contre 40. Une signature qui prend IEnumerable<T> au lieu de List<T> suffit a payer ca, a chaque appel. C'est la raison numero un pour typer ses parametres chauds en List<T> ou en Span<T>."),

        new("24_unsafe", "pourquoi fixed",
            "A quoi sert le mot-cle 'fixed' ?",
            new[] { "a rendre la variable constante", "a empecher le ramasse-miettes de DEPLACER l'objet le temps du bloc", "a allouer sur la pile", "a liberer la memoire" }, 1,
            "Le ramasse-miettes compacte le tas en deplacant les objets. Une adresse notee avant un deplacement ne designerait plus rien. Le bloc fixed doit donc rester COURT : tant qu'il dure, le tas ne peut plus etre compacte."),

        new("24_unsafe", "arithmetique de pointeur",
            "int* p pointe le debut d'un tableau. De combien d'octets p++ avance-t-il ?",
            new[] { "un", "quatre, soit UN element", "huit", "cela depend de la plateforme" }, 1,
            "L'arithmetique de pointeur compte en ELEMENTS, pas en octets : le compilateur multiplie par sizeof(T). Et soustraire deux pointeurs rend un nombre d'elements, pas un nombre d'octets."),

        new("24_unsafe", "stackalloc dans une boucle",
            "Un stackalloc de 1 Ko a l'interieur d'une boucle de 10 000 tours. Que se passe-t-il ?",
            new[] { "rien, il est libere a chaque tour", "la pile deborde : elle n'est liberee qu'au RETOUR de la methode", "il bascule automatiquement sur le tas", "une exception rattrapable" }, 1,
            "stackalloc reserve sur la pile de la METHODE, pas du bloc. Il faut le sortir de la boucle. Et un debordement de pile ne se rattrape pas : le processus meurt, sans message utile."),

        new("24_unsafe", "l'ordre des champs",
            "Un struct { byte, int, byte } contre { int, byte, byte }. Quelles tailles ?",
            new[] { "6 et 6", "12 et 8", "8 et 8", "6 et 12" }, 1,
            "Le compilateur ALIGNE chaque champ sur sa propre taille et bouche les trous. Ranger ses champs du plus grand au plus petit est l'optimisation la moins chere qui existe : sur dix mille entites, c'est quarante kilooctets et un tiers de lignes de cache en moins."),

        new("24_unsafe", "un type blittable",
            "Qu'est-ce qui rend un struct passable tel quel a du code natif ?",
            new[] { "l'attribut [Serializable]", "ne contenir aucune reference : RuntimeHelpers.IsReferenceOrContainsReferences<T>() rend false", "etre readonly", "etre declare unsafe" }, 1,
            "Blittable veut dire que les octets ont la meme forme des deux cotes de la frontiere : rien a convertir, rien a copier. Un struct qui contient une string ne l'est pas, et on ne peut meme pas l'epingler."),

        new("24_unsafe", "MemoryMarshal.Cast",
            "MemoryMarshal.Cast<Vector2, float>(points) sur deux points. Que rend-il ?",
            new[] { "une copie de quatre floats", "une VUE de quatre floats sur les memes octets", "un tableau vide", "une exception" }, 1,
            "La vue change, la memoire non : ecrire dedans ecrit dans la source. Zero copie, ce qui est exactement ce qu'attend une API graphique ou un envoi reseau. Un reste qui ne fait pas un element entier est tronque."),

        new("24_unsafe", "modifier un struct dans une List",
            "Pourquoi 'liste[0].Vie = 42;' ne compile-t-il pas sur une List<Particule> ?",
            new[] { "il manque un cast", "l'indexeur rend une COPIE : la modifier ne modifierait rien", "les structs sont immuables", "il faut le mot-cle unsafe" }, 1,
            "Le compilateur refuse plutot que de te laisser y croire. CollectionsMarshal.AsSpan(liste) ouvre le tableau interne, et 'ref Particule p = ref span[i]' est un alias sur l'element. Attention : ajouter un element peut REALLOUER ce tableau, et le Span pris avant pointe alors dans le vide."),

        new("24_unsafe", "memoire native",
            "Qui libere la memoire obtenue par NativeMemory.Alloc ?",
            new[] { "le ramasse-miettes", "toi, avec NativeMemory.Free, exactement une fois", "personne, elle est liberee a la fin du programme", "le finaliseur du Span" }, 1,
            "Cette memoire est invisible du ramasse-miettes : elle ne compte dans aucune generation et ne bouge jamais, ce qui est precisement l'interet. En echange, la classe qui la detient implemente IDisposable, met son pointeur a null, et tolere un Dispose appele deux fois."),

        new("24_unsafe", "pointeur de fonction",
            "Qu'est-ce qu'un delegate*<int, int> ne peut pas faire, contrairement a un Func<int, int> ?",
            new[] { "rendre une valeur", "viser une methode d'instance ou capturer quoi que ce soit", "etre range dans un tableau", "etre appele" }, 1,
            "Un pointeur de fonction est une adresse de huit octets, pas un objet : pas de cible, pas de liste d'invocation, pas de fermeture, donc aucune allocation. Quand ca suffit - table d'opcodes, dispatch d'ECS, rappel natif - c'est ce qu'il y a de plus rapide en C#."),

        new("24_unsafe", "les verifications de bornes",
            "Comment supprimer la verification de bornes d'une boucle sur un tableau, sans code non sur ?",
            new[] { "c'est impossible", "ecrire i < tableau.Length dans la condition : le compilateur la supprime alors tout seul", "utiliser un Span, qui ne verifie rien", "ajouter l'attribut [SkipLocalsInit]" }, 1,
            "C'est la vraie conclusion de la section : le compilateur reconnait le motif 0 a Length et enleve la comparaison lui-meme. Un Span, lui, verifie ses bornes comme un tableau - ce qu'il evite, c'est la COPIE, pas le controle."),

        new("25_threads", "compteur partage",
            "Quatre threads incrementent le meme int un million de fois. Que vaut-il a la fin ?",
            new[] { "un million exactement", "moins, parfois beaucoup moins", "plus", "une exception est levee" }, 1,
            "'compteur++' n'est pas UNE operation, c'en est trois : lire, ajouter, reecrire. Deux threads qui lisent la meme valeur ecrivent la meme valeur. Interlocked.Increment fait les trois d'un bloc - et compter dans une case par thread est encore plus rapide."),

        new("25_threads", "verifier puis agir",
            "Un TryTake verifie le stock sous un verrou, puis retire sous un AUTRE verrou. Que peut-il arriver ?",
            new[] { "rien, les deux sont verrouilles", "deux threads passent le test avant que l'un des deux ne retire, et le stock devient negatif", "un interblocage", "une exception" }, 1,
            "La verification et l'action doivent etre dans le MEME bloc. C'est le bug 'check then act' : il est invisible en relecture, ne se reproduit jamais en pas-a-pas, et sort une fois sur mille chez les joueurs."),

        new("25_threads", "Parallel.For et l'accumulation",
            "Quelle forme de Parallel.For somme un tableau sans perdre d'additions ET sans payer une operation atomique par element ?",
            new[] { "total += valeurs[i]", "Interlocked.Add a chaque element", "la surcharge a etat LOCAL : un total par tache, publie une fois a la fin", "un lock autour de l'addition" }, 2,
            "localInit, le corps qui fait avancer l'etat local, et localFinally appele UNE fois par tache. Mieux encore quand c'est possible : donner une TRANCHE a chaque tache, parce que deux taches qui n'ecrivent jamais au meme endroit n'ont rien a synchroniser."),

        new("25_threads", "l'API du moteur",
            "Un thread de calcul veut creer un noeud dans la scene. Que doit-il faire ?",
            new[] { "le creer, c'est thread-safe", "deposer le travail dans une file que le thread PRINCIPAL vide", "prendre un lock sur la scene", "utiliser Interlocked" }, 1,
            "L'API d'un moteur n'est utilisable que depuis le thread principal, et ni Godot ni Unity ne le verifient toujours : parfois ca marche, parfois ca corrompt la scene. Une ConcurrentQueue<Action>, videe avec un BUDGET par image pour eviter le pic quand tout arrive en meme temps."),

        new("25_threads", "faux partage",
            "Quatre threads, quatre compteurs differents dans un meme tableau, aucune donnee partagee. Pourquoi est-ce lent ?",
            new[] { "ce n'est pas lent", "les quatre tiennent dans la meme ligne de cache : chaque ecriture invalide la ligne chez les autres coeurs", "le tableau est verrouille", "le ramasse-miettes intervient" }, 1,
            "Et le resultat reste JUSTE : le faux partage n'est pas un bug de justesse, rien ne le signale. Rembourrer a 64 octets regle le symptome ; accumuler dans une variable locale et n'ecrire qu'une fois a la fin regle la cause."),

        new("25_threads", "annulation",
            "Comment interrompre proprement un thread qui charge un niveau ?",
            new[] { "Thread.Abort", "lui passer un CancellationToken qu'il REGARDE lui-meme", "le mettre en pause", "lever une exception depuis l'exterieur" }, 1,
            "Il n'existe aucune facon sure d'interrompre un thread de force : on l'arreterait au milieu d'une ecriture. L'annulation est cooperative, toujours. ThrowIfCancellationRequested quand l'appelant doit savoir, un simple test quand un resultat partiel a un sens."),

        new("25_threads", "async void",
            "Qu'est-ce qu'un 'async void' a de particulier ?",
            new[] { "rien, c'est un raccourci", "il n'a pas de tache : personne ne peut l'attendre ni attraper ce qui en sort", "il tourne sur le thread principal", "il est plus rapide" }, 1,
            "Une exception qui s'en echappe remonte au thread et tue le processus. Le seul usage legitime est un gestionnaire d'evenement, et il doit alors attraper tout ce qui sort. Partout ailleurs : async Task."),

        new("25_threads", "await dans une boucle",
            "Trois chargements de 100 ms attendus dans un foreach. Combien de temps ?",
            new[] { "100 ms", "300 ms", "cela depend du nombre de coeurs", "33 ms" }, 1,
            "Un 'await' dans une boucle SERIALISE tout, et rien ne le signale. Task.WhenAll les lance tous avant d'attendre : 100 ms. C'est l'erreur la plus courante de tout le C# asynchrone."),

        new("25_threads", "file bornee",
            "Un thread genere des chunks plus vite que le thread principal ne les integre. Que fait une file NON bornee ?",
            new[] { "elle bloque le producteur", "elle grossit jusqu'a la fin de la memoire", "elle jette les plus anciens", "elle leve une exception" }, 1,
            "Une file bornee bloque le producteur quand elle est pleine, ce qui le cale sur la vitesse du consommateur. FullMode.DropOldest jette a la place, ce qu'on veut pour des positions reseau ou la donnee perimee ne vaut rien."),

        new("25_threads", "Complete()",
            "Un producteur remplit un Channel et oublie writer.Complete(). Que fait le consommateur en 'await foreach' ?",
            new[] { "il sort normalement", "il attend pour toujours : le jeu se fige a la fin du chargement", "il leve une exception", "il redemarre" }, 1,
            "C'est le bug le plus difficile a diagnostiquer de la section : rien ne plante, rien ne s'affiche, le jeu est simplement fige. Complete() est ce qui fait sortir ReadAllAsync de sa boucle."),

        new("26_binary", "depassement de champ",
            "Un champ de 10 bits recoit la valeur 2000. Que se passe-t-il ?",
            new[] { "une exception", "le masque garde les bits du bas : 976, sans aucun avertissement", "la valeur est plaquee a 1023", "le champ voisin est ecrase" }, 1,
            "Un depassement en binaire ne leve jamais, il ment. La valeur maximale d'un champ de n bits est 2 puissance n moins un, et c'est a TOI de la verifier avant d'ecrire."),

        new("26_binary", "quantifier une position",
            "Une position hors de la plage prevue est quantifiee sans etre plaquee. Que voit-on en jeu ?",
            new[] { "rien de special", "le joueur se teleporte a l'autre bout de la carte", "une exception", "une position arrondie" }, 1,
            "La conversion deborde et repart de l'autre cote. C'est le bug de reseau le plus spectaculaire qui soit. Une position se PLAQUE aux bornes ; un angle, lui, est cyclique et doit s'ENROULER, jamais se plaquer."),

        new("26_binary", "ordre des octets",
            "Un entier ecrit en petit-boutiste est relu en gros-boutiste. Que rend la lecture ?",
            new[] { "une exception", "un nombre parfaitement valide et completement faux", "zero", "la bonne valeur" }, 1,
            "Aucune exception, aucun avertissement, et la machine de developpement est petit-boutiste donc rien ne se voit. La regle : l'ordre fait partie du FORMAT et s'ecrit explicitement avec BinaryPrimitives, une fois pour toutes."),

        new("26_binary", "varint et nombres negatifs",
            "Pourquoi -1 encode en varint prend-il cinq octets ?",
            new[] { "les negatifs sont interdits", "en complement a deux, -1 vaut 0xFFFFFFFF : tous les bits sont a un", "il faut un octet de signe", "c'est un bug du format" }, 1,
            "Le zigzag entrelace les signes : 0, -1, 1, -2 deviennent 0, 1, 2, 3. C'est (v << 1) ^ (v >> 31). Un delta de -1 retombe alors sur un seul octet, ce qui est exactement le cas frequent d'un protocole a delta."),

        new("26_binary", "chaine dans un format binaire",
            "Comment ecrire une chaine dans un fichier binaire ?",
            new[] { "terminee par un octet nul", "PREFIXEE de sa longueur en octets", "entre guillemets", "en UTF-16 sans marqueur" }, 1,
            "Le prefixe de longueur est plus court a lire, supporte les octets nuls, et permet de SAUTER le champ sans le decoder. Le zero final est une convention du C, imposee par les API natives, pas un bon format de fichier."),

        new("26_binary", "encodage delta",
            "Un delta est applique sur une baseline differente de celle de l'emetteur. Que se passe-t-il ?",
            new[] { "une exception", "le resultat est faux et personne ne s'en apercoit", "le paquet est rejete", "la baseline se corrige seule" }, 1,
            "D'ou les deux regles de tout protocole a delta : le receveur ACQUITTE la baseline qu'il possede, et l'emetteur renvoie un instantane COMPLET de temps en temps, pour que toute desynchronisation finisse par se corriger."),

        new("26_binary", "sauvegarde atomique",
            "Le courant est coupe pendant l'ecriture d'un fichier de sauvegarde. Que faut-il avoir fait pour ne pas perdre la partie precedente ?",
            new[] { "une copie de secours apres coup", "ecrire dans un temporaire, le RELIRE, puis le renommer", "ecrire plus vite", "verifier l'espace disque avant" }, 1,
            "Le renommage est l'operation que le systeme de fichiers garantit indivisible : le fichier reel n'est jamais a moitie ecrit. Avec une empreinte a la fin du contenu pour detecter la corruption, et un refus TOTAL en cas d'echec - une sauvegarde corrompue n'est pas une sauvegarde partielle."),

        new("26_binary", "binaire contre JSON",
            "Quand choisir le JSON plutot qu'un format binaire ?",
            new[] { "toujours, c'est plus simple", "pour ce qu'un HUMAIN edite : reglages, tables d'objets, dialogues", "jamais, c'est trop lent", "seulement pour le reseau" }, 1,
            "Le JSON se lit dans un editeur, se compare dans un diff git, se corrige a la main et survit a un champ ajoute. Le binaire est pour ce qu'une MACHINE ecrit en masse : sauvegardes, replays, paquets reseau, terrain - et il permet de sauter directement au n-ieme element."),

        new("27_text", "Split contre Span",
            "Que coute string.Split(',') sur une ligne de quatre champs ?",
            new[] { "rien", "un tableau plus une chaine par champ", "une seule chaine", "une exception si un champ est vide" }, 1,
            "Sur un fichier de mille lignes, ce sont des milliers d'objets pour un resultat de quelques octets. IndexOf plus Slice sur un ReadOnlySpan<char> fait le meme travail a zero allocation - et int.TryParse accepte directement un span."),

        new("27_text", "la culture",
            "Un float est ecrit dans un fichier de sauvegarde avec ToString() sur une machine francaise. Que contient le fichier ?",
            new[] { "1.5", "1,5 - illisible pour tout autre joueur", "une exception", "la valeur brute en binaire" }, 1,
            "Culture INVARIANTE pour ce qu'une machine relit, culture du joueur pour ce qu'un humain lit. Et ToUpperInvariant, jamais ToUpper : en turc, le i devient un I a point suspendu et casse toute cle contenant cette lettre."),

        new("27_text", "un journal desactive",
            "Log($\"ennemis : {Compte()}\") avec un test 'if (verbose)' A L'INTERIEUR de Log. Compte() est-il appele ?",
            new[] { "non, le test l'evite", "oui : un parametre est evalue AVANT l'appel", "seulement en mode debug", "cela depend du compilateur" }, 1,
            "Le calcul a lieu ET la chaine est construite, a chaque appel, meme journal eteint. Un handler de chaine interpolee resout ca sans changer un caractere au point d'appel : quand shouldAppend sort a false, le compilateur saute tous les AppendFormatted."),

        new("27_text", "un char n'est pas un caractere",
            "Que vaut \"\\U0001F3AE\".Length, pour un emoji de manette ?",
            new[] { "1", "2", "4", "cela depend de la police" }, 1,
            "Un char est une unite UTF-16 de seize bits ; tout ce qui depasse s'ecrit sur deux demi-caracteres. Tronquer a l'aveugle coupe l'emoji en deux : c'est le carre blanc dans les pseudos. EnumerateRunes parcourt les vrais points de code."),

        new("27_text", "TryFormat",
            "Que rend un TryFormat dont le tampon est trop petit ?",
            new[] { "il leve une exception", "il rend false, et l'appelant doit le tester", "il agrandit le tampon", "il tronque silencieusement" }, 1,
            "C'est la convention de tout le framework. TryFormat ecrit dans un tampon FOURNI et ne fabrique aucune chaine : combine a un stackalloc, c'est le plancher absolu du texte en jeu, a zero octet."),

        new("27_text", "comparer des noms",
            "Un jeu compare des noms d'objets soixante fois par seconde. Que faire ?",
            new[] { "utiliser string.Intern", "traduire chaque nom en identifiant entier, une fois, au chargement", "comparer avec ReferenceEquals", "mettre les chaines en majuscules d'abord" }, 1,
            "L'operateur == des chaines compare le contenu, caractere par caractere. Un identifiant entier se compare et se hache en une instruction. Et le hachage des chaines est randomise a chaque demarrage : ne jamais l'ecrire dans un fichier."),

        new("28_reflect", "scanner les types",
            "Quel filtre manque le plus souvent quand on scanne un assemblage pour instancier des types ?",
            new[] { "ecarter les interfaces", "garder seulement ceux qui ont un constructeur SANS argument", "ecarter les classes scellees", "filtrer par espace de noms" }, 1,
            "Sans lui, ca marche jusqu'au jour ou un collegue ajoute un parametre a son constructeur, et le jeu ne demarre plus avec un MissingMethodException illisible. Et GetTypes ne garantit aucun ordre : il faut trier, sinon le demarrage cesse d'etre reproductible."),

        new("28_reflect", "le vrai usage d'un attribut",
            "A quoi sert le mieux un attribut personnalise lu au demarrage ?",
            new[] { "a remplacer un fichier de configuration", "a VERIFIER un contrat : pas d'identifiant en double, rien d'obligatoire qui manque", "a accelerer le chargement", "a documenter le code" }, 1,
            "Un attribut range de l'information a cote du type, et le demarrage peut alors valider l'ensemble avant que la partie commence. AttributeUsage limite ou il se pose, et Inherited = false evite qu'une classe fille herite silencieusement de l'identifiant de sa mere."),

        new("28_reflect", "fabriquer un objet",
            "Quel est le principal avantage d'une table de lambdas sur Activator.CreateInstance ?",
            new[] { "elle est plus courte a ecrire", "le compilateur VOIT chaque 'new' : rien n'est supprime au trim ni oublie par IL2CPP", "elle accepte des constructeurs a arguments", "elle est thread-safe" }, 1,
            "La vitesse n'est que le bonus. Un type oublie se voit a la LECTURE de la table, pas trois heures plus tard sur une console. Entre les deux, une fabrique compilee par arbre d'expression fait le travail de reflexion une seule fois."),

        new("28_reflect", "GetValue et l'emballage",
            "Que coute PropertyInfo.GetValue sur une propriete de type float ?",
            new[] { "rien", "un emballage : le float devient un objet sur le tas, a chaque lecture", "une exception si la propriete est privee", "une copie de l'objet entier" }, 1,
            "C'est le prix de 'object' comme type de retour. Et GetProperties reconstruit son tableau a chaque appel : la seule forme acceptable est de chercher les membres UNE fois dans un static readonly, et de ne garder que les PropertyInfo."),

        new("28_reflect", "MakeGenericType",
            "MakeGenericType(typeof(Vector2)) sur ComponentStore<>. Qu'obtient-on ?",
            new[] { "une version affaiblie qui emballe tout", "un VRAI ComponentStore<Vector2> : la List a l'interieur est une List<Vector2>", "une interface", "une copie du type ouvert" }, 1,
            "Le type ferme est un type comme un autre, sans emballage. A mettre en cache dans un Dictionary<Type, ...> parce que MakeGenericType et Activator sont chers. Attention sur IL2CPP : une combinaison generique que le compilateur n'a jamais VUE n'existe pas."),

        new("28_reflect", "le trim",
            "Type.GetType(\"MonJeu.EffetBrulure\") marche dans l'editeur. Et dans un build console ?",
            new[] { "pareil", "le type peut avoir ete supprime par le trim ou jamais genere par IL2CPP", "plus rapide", "il faut juste recompiler" }, 1,
            "Rien ne relie une chaine a une classe : ce que le compilateur ne VOIT pas n'existe pas. [RequiresUnreferencedCode] marque le code concerne pour que l'analyseur previenne a chaque appel, au lieu de laisser la surprise pour la version console."),

        new("10_gamedev", "hitstop",
            "Un gel de 80 ms met l'echelle de temps a zero. Sur quel temps doit-il se decompter ?",
            new[] { "le temps de jeu", "le temps REEL, qui continue de couler", "le nombre d'images", "la duree de l'animation" }, 1,
            "Sur le temps de jeu, il ne finirait jamais : le temps de jeu vaut zero pendant le gel. Et un second coup pendant le gel prend le MAXIMUM, il ne s'ajoute pas - sinon un combo de six coups fige le jeu une demi-seconde et le joueur croit a un plantage."),

        new("10_gamedev", "zone morte de stick",
            "Une zone morte appliquee separement sur X et sur Y. Que devient une diagonale legere ?",
            new[] { "elle est preservee", "elle devient purement horizontale ou verticale", "elle est annulee", "elle est amplifiee" }, 1,
            "La composante la plus faible passe sous le seuil et disparait. Il faut une zone morte RADIALE - sur la longueur du vecteur - puis REMAPPER ce qui reste sur zero-un, sinon franchir le seuil fait sauter la vitesse d'un coup."),

        new("10_gamedev", "coup qui touche plusieurs fois",
            "Une epee traverse un ennemi pendant cinq images de contact. Qu'est-ce qui empeche cinq coups ?",
            new[] { "les images d'invulnerabilite suffisent", "un registre des cibles deja touchees PAR CETTE attaque", "un cooldown sur l'arme", "rien, c'est le comportement voulu" }, 1,
            "Les i-frames aident, mais toutes les attaques n'en donnent pas : une aura de degats ou une zone de feu n'en donnent aucune. Un HashSet d'identifiants dans l'attaque, dont Add rend false si la cible y est deja, est le seul garde qui marche dans tous les cas."),

        new("10_gamedev", "ciblage qui clignote",
            "Deux ennemis a distance presque egale. Pourquoi le reticule saute-t-il de l'un a l'autre a chaque image ?",
            new[] { "le cone est trop large", "il manque une hysteresis : un demi-pixel suffit a inverser le classement", "la portee est mal calculee", "il faut trier la liste" }, 1,
            "La cible COURANTE doit voir son score multiplie par un facteur inferieur a 1, pour qu'il faille une avance nette pour la detroner. Coller n'est pas s'accrocher : une cible qui sort du cone ou de la portee reste lachee."),

        new("10_gamedev", "viser une cible mobile",
            "La cible fuit plus vite que le projectile. Que rend le calcul d'interception ?",
            new[] { "un point tres loin devant", "rien : le discriminant est negatif, il n'y a pas de solution", "la position actuelle", "NaN" }, 1,
            "Il faut le DIRE au lieu de rendre NaN. Deux autres cas particuliers : une racine negative est une solution dans le passe, on prend l'autre ; et une cible qui va exactement a la vitesse du projectile annule le terme carre, donc l'equation devient lineaire et diviser par zero part vers l'infini."),

        new("10_gamedev", "compteur de pitie",
            "Une table annoncee a 5 pour cent avec une garantie apres 10 echecs. Quel est le taux reel ?",
            new[] { "5 pour cent", "nettement plus", "nettement moins", "exactement 10 pour cent" }, 1,
            "La garantie ajoute des reussites que le tirage n'aurait pas donnees. C'est bon a savoir avant d'ecrire un chiffre dans un menu. Et le compteur se remet a zero sur une reussite COMME sur une garantie, sinon la pitie devient permanente au bout d'une heure."),

        new("19_unity", "ScriptableObject",
            "Un ennemi ecrit ses points de vie courants dans le ScriptableObject qui le configure. Que se passe-t-il ?",
            new[] { "rien, chaque ennemi a sa copie", "tous les ennemis partagent la valeur, et dans l'editeur la modification survit a l'arret du jeu", "une exception", "la valeur est perdue au chargement" }, 1,
            "Un ScriptableObject est une instance UNIQUE partagee. L'asset porte la configuration, l'instance porte l'etat. Un champ qui change pendant la partie n'a rien a faire dedans - sinon la modification finit versionnee dans le depot."),

        new("19_unity", "transform.position",
            "Combien de franchissements de la frontiere natif-manage fait 'transform.position += v' ?",
            new[] { "zero, c'est un champ", "deux : une lecture puis une ecriture", "un", "cela depend de la plateforme" }, 1,
            "Ce n'est pas un champ, c'est un appel dans le moteur. SetPositionAndRotation en fait un seul la ou position puis rotation en font deux. Et la propriete rend une COPIE : c'est pour ca que 'transform.position.x = 5' ne compile pas."),

        new("19_unity", "Destroy",
            "Juste apres Destroy(x), que vaut 'x == null' ?",
            new[] { "true", "false : l'objet est supprime a la FIN de l'image", "une exception", "cela depend du composant" }, 1,
            "Unity inscrit l'objet sur une liste et le supprime a la fin de l'image. Un nettoyage lance dans la meme image ne trouve rien, et tout ce qu'on fait a l'objet entre-temps est perdu sans avertissement. Apres un Destroy, on cesse d'utiliser la reference immediatement."),

        new("19_unity", "deplacer un Rigidbody",
            "Pourquoi ecrire directement la position d'un Rigidbody est-il dangereux ?",
            new[] { "c'est plus lent", "ca TELEPORTE : le moteur ne voit aucun trajet, donc aucune collision, et le corps traverse les murs", "ca desactive la gravite", "ca ne compile pas" }, 1,
            "MovePosition DEMANDE un deplacement applique au prochain pas de physique, en gardant l'etat d'avant : c'est ce qui permet de tester le trajet. Et l'entree se lit dans Update mais s'applique dans FixedUpdate, qui n'a pas la meme cadence."),

        new("19_unity", "charger un asset",
            "La meme texture est chargee deux fois. Combien faut-il de Release pour la liberer ?",
            new[] { "un seul", "deux : autant que de Load", "aucun, le GC s'en charge", "trois" }, 1,
            "Le moteur compte les demandeurs, et seul le passage a zero libere. Un Release en TROP ne doit rien faire, sinon le compteur passe sous zero et l'asset est libere alors qu'un autre niveau l'utilise. Celui qui charge est celui qui libere, dans un Dispose."),

        new("19_unity", "rebuild de Canvas",
            "Un chrono change d'un caractere sur un canevas de vingt elements. Combien d'elements sont recalcules ?",
            new[] { "un", "les vingt : un canevas est reconstruit en ENTIER", "aucun", "deux" }, 1,
            "C'est la premiere cause de saccades d'interface. La parade ne coute rien : separer ce qui change a chaque image de ce qui ne change jamais, un canevas par groupe. Et comparer avant d'ecrire, pour que reaffecter la meme valeur ne salisse rien."),
    };
}
