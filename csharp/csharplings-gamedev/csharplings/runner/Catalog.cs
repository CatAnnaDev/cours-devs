namespace Csharplings.Runner;

public sealed record Exercise(
    string Id,
    string Section,
    string ClassName,
    string Title,
    string Instructions,
    string Hint
);

public static class Catalog
{
    public static readonly IReadOnlyList<Exercise> All =
    [
        new("intro1", "00_intro", "Intro1", "Comment ca marche",
            "Rien a corriger dans le code : il tourne deja.\nOuvre le fichier, mets 'NotDone' a false, sauvegarde.\nC'est la boucle que tu repeteras 195 fois.",
            "La ligne est 'public const bool NotDone = true;'. Remplace true par false."),

        new("intro2", "00_intro", "Intro2", "Lire une erreur de compilation",
            "Le fichier ne compile pas. Lis le message d'erreur au-dessus : il te donne\nle fichier, la ligne, la colonne et la cause. Corrige, puis passe NotDone a false.",
            "En C# chaque instruction se termine par un point-virgule."),

        new("variables1", "01_variables", "Variables1", "Declarer une variable",
            "Une variable, c'est un type + un nom + une valeur.\nDeclare les variables manquantes pour que les verifications passent.",
            "La syntaxe est : int age = 30;"),

        new("variables2", "01_variables", "Variables2", "var et types explicites",
            "'var' laisse le compilateur deviner le type a partir de la valeur.\nCe n'est PAS un type dynamique : le type est fige a la compilation.",
            "var x = 5; donne un int. Tu ne peux plus ecrire x = \"texte\"; ensuite."),

        new("variables3", "01_variables", "Variables3", "const et readonly",
            "'const' : valeur figee a la compilation, jamais modifiable.\n'readonly' : fixee une fois au demarrage, plus modifiable ensuite.",
            "Un const doit etre initialise sur la meme ligne que sa declaration."),

        new("types1", "02_types", "Types1", "Les types de base",
            "int, float, double, bool, char, string.\nAttention aux suffixes : 1.5 est un double, 1.5f est un float.",
            "float vitesse = 1.5f; le 'f' est obligatoire."),

        new("types2", "02_types", "Types2", "Conversions et cast",
            "int / int donne un int : la partie decimale est jetee.\nPour garder les decimales il faut convertir AVANT la division.",
            "(float)a / b convertit a en float, puis divise. (float)(a / b) est trop tard."),

        new("types3", "02_types", "Types3", "Texte vers nombre",
            "int.Parse plante si le texte n'est pas un nombre.\nint.TryParse renvoie true/false et ne plante jamais : prefere-le.",
            "if (int.TryParse(texte, out int valeur)) { ... }"),

        new("flow1", "03_flow", "Flow1", "if / else",
            "Complete la logique de conditions pour que la fonction renvoie le bon rang.",
            "L'ordre des if compte : teste d'abord le cas le plus restrictif."),

        new("flow2", "03_flow", "Flow2", "switch expression",
            "Le switch moderne renvoie une valeur au lieu de faire des breaks.\nLe '_' est le cas par defaut, il est obligatoire s'il manque des cas.",
            "return mood switch { Mood.Happy => \"content\", _ => \"?\" };"),

        new("flow3", "03_flow", "Flow3", "Boucles for et while",
            "'for' quand tu connais le nombre de tours. 'while' quand tu attends une condition.",
            "for (int i = 0; i < 10; i++) fait 10 tours, de 0 a 9."),

        new("flow4", "03_flow", "Flow4", "foreach, break, continue",
            "'foreach' parcourt une collection sans index.\n'continue' saute au tour suivant, 'break' sort de la boucle.",
            "continue quand tu veux ignorer un element, break quand tu as trouve ce que tu cherchais."),

        new("methods1", "04_methods", "Methods1", "Ecrire une methode",
            "Une methode : type de retour, nom, parametres, corps.\n'void' veut dire qu'elle ne renvoie rien.",
            "public static int Double(int x) { return x * 2; }"),

        new("methods2", "04_methods", "Methods2", "Parametres optionnels et nommes",
            "Un parametre avec une valeur par defaut devient optionnel.\nA l'appel, on peut nommer les arguments pour la lisibilite.",
            "public static float Damage(float baseDamage, float multiplier = 1f)"),

        new("methods3", "04_methods", "Methods3", "out et ref",
            "'out' : la methode DOIT remplir la variable, elle sert de second retour.\n'ref' : la methode recoit la vraie variable et peut la modifier.",
            "public static bool TryDivide(int a, int b, out int result)"),

        new("strings1", "05_strings", "Strings1", "Interpolation",
            "Le $ devant une chaine permet d'y injecter des expressions entre accolades.\nOn peut aussi formater : {valeur:0.00}",
            "$\"Score : {score}\" est plus lisible que \"Score : \" + score"),

        new("strings2", "05_strings", "Strings2", "Manipuler du texte",
            "Split, Trim, ToUpper, Contains, StartsWith, string.Join.\nUne string est immuable : chaque methode renvoie une NOUVELLE string.",
            "texte.Trim() ne modifie pas texte, il faut recuperer le resultat."),

        new("collections1", "06_collections", "Collections1", "Les tableaux",
            "Un tableau a une taille figee. Les index vont de 0 a Length - 1.",
            "Un tableau de 3 cases a les index 0, 1, 2. Il n'y a PAS d'index 3.\nRegarde les conditions de boucle et le dernier element."),

        new("collections2", "06_collections", "Collections2", "List<T>",
            "Une liste grandit et retrecit. Add, Remove, Contains, Count, indexation.",
            "On ne peut PAS retirer d'une liste pendant qu'on la parcourt avec foreach.\nSoit RemoveAll(condition), soit une boucle for qui descend de la fin vers 0."),

        new("collections3", "06_collections", "Collections3", "Dictionary<K,V>",
            "Un dictionnaire associe une cle a une valeur, avec une recherche instantanee.\nTryGetValue evite de planter sur une cle absente.",
            "inventaire[\"potion\"] = 3; puis inventaire.TryGetValue(\"potion\", out int n)"),

        new("classes1", "07_oop", "Classes1", "Classe et constructeur",
            "Une classe regroupe des donnees et le comportement qui va avec.\nLe constructeur porte le nom de la classe et n'a pas de type de retour.",
            "public Player(string name) { Name = name; }"),

        new("classes2", "07_oop", "Classes2", "Proprietes",
            "Une propriete ressemble a un champ mais peut controler lecture et ecriture.\n{ get; private set; } : lisible par tous, modifiable seulement de l'interieur.",
            "public int Health { get; private set; } puis une methode publique pour la changer."),

        new("classes3", "07_oop", "Classes3", "Heritage et override",
            "'virtual' autorise une classe fille a redefinir la methode.\n'override' effectue cette redefinition. 'base.X()' appelle la version du parent.",
            "public override string Describe() => ... ; sans 'virtual' cote parent ca ne compile pas."),

        new("interfaces1", "07_oop", "Interfaces1", "Interfaces",
            "Une interface est un contrat : elle dit CE QU'ON PEUT FAIRE, pas comment.\nUne classe peut implementer plusieurs interfaces, mais n'heriter que d'une classe.",
            "Il faut implementer TOUS les membres de l'interface, en public."),

        new("structs1", "07_oop", "Structs1", "struct contre class",
            "Un struct est copie quand on l'assigne ou qu'on le passe a une methode.\nUne class est partagee : deux variables pointent le meme objet.",
            "Modifier une copie de struct ne change pas l'original. C'est tout le piege."),

        new("enums1", "07_oop", "Enums1", "Enums",
            "Un enum remplace les nombres et les chaines magiques par des noms.\nIl se marie tres bien avec le switch.",
            "public enum State { Idle, Run, Jump } puis State s = State.Idle;"),

        new("generics1", "08_advanced", "Generics1", "Generiques",
            "Le <T> permet d'ecrire du code une fois pour tous les types.\nLa contrainte 'where T : ...' limite les types acceptes.",
            "public static T First<T>(List<T> items) => items[0];"),

        new("null1", "08_advanced", "Null1", "Gerer le null",
            "?. n'appelle que si l'objet n'est pas null.\n?? donne une valeur de repli. ??= assigne seulement si c'est null.",
            "player?.Weapon?.Name ?? \"aucune\" ne plante jamais."),

        new("exceptions1", "08_advanced", "Exceptions1", "Exceptions",
            "try/catch attrape une erreur au lieu de laisser le programme mourir.\nOn attrape le type le plus precis possible, jamais 'Exception' a l'aveugle.",
            "catch (DivideByZeroException) { ... } et finally s'execute toujours."),

        new("linq1", "08_advanced", "Linq1", "LINQ",
            "LINQ decrit ce qu'on veut au lieu d'ecrire la boucle.\nWhere filtre, Select transforme, OrderBy trie, Sum/Count/Any agregent.",
            "scores.Where(s => s > 10).Select(s => s * 2).ToList()"),

        new("delegates1", "08_advanced", "Delegates1", "Lambdas, Action et Func",
            "Une lambda est une fonction anonyme : x => x * 2\nAction ne renvoie rien, Func renvoie quelque chose (dernier type = le retour).",
            "Func<int, int, int> add = (a, b) => a + b;"),

        new("events1", "08_advanced", "Events1", "Evenements et desabonnement",
            "Un event previent plusieurs auditeurs. C'est l'emetteur qui garde\nl'auditeur en vie : sans -= tu as une fuite memoire.",
            "emetteur.Truc += Handler; puis emetteur.Truc -= Handler; quand on s'en va."),

        new("async1", "08_advanced", "Async1", "async et await",
            "'await' attend sans bloquer le thread.\nUne methode qui contient await doit etre 'async' et renvoyer Task ou Task<T>.",
            "public static async Task<int> LoadAsync() { await Task.Delay(10); return 42; }"),

        new("records1", "08_advanced", "Records1", "record et immuabilite",
            "Un record compare ses VALEURS au lieu de son adresse memoire.\n'with' fabrique une copie modifiee sans jamais toucher l'original.",
            "Transforme la classe en 'public sealed record WeaponStats(string Name, int Damage, float Weight)'.\nUn record positionnel donne l'egalite, le ToString, la deconstruction et 'with' d'un coup."),

        new("patterns1", "08_advanced", "Patterns1", "Pattern matching",
            "Les motifs remplacent des cascades de if : type, propriete, intervalle, liste.\nIls sont testes DANS L'ORDRE : le plus precis passe en premier.",
            "Motif de propriete : Hero { Health: 0 }. Intervalle : < 100. Liste : [1, .., 3].\n'when' ajoute une condition libre, 'is' declare une variable au passage.\n\nAttention : les motifs de LISTE sont du C# 11. Godot 4 les accepte, Unity non\n(il est fige en C# 9). Le reste de l'exercice passe partout."),

        new("tuples1", "08_advanced", "Tuples1", "Tuples et deconstruction",
            "Un tuple renvoie plusieurs valeurs sans inventer une classe pour ca.\nUne methode Deconstruct rend n'importe quelle classe destructurable.",
            "(int Min, int Max) MinMax(...) puis return (min, max);\npublic void Deconstruct(out string name, out int health)"),

        new("operators1", "08_advanced", "Operators1", "Surcharge d'operateurs",
            "Un type de degats qui s'additionne et se compare comme un nombre.\nSi tu ecris ==, tu DOIS ecrire !=, Equals et GetHashCode avec.",
            "public static Damage operator +(Damage a, Damage b) => ...\n> et < vont toujours par paire, sinon ca ne compile pas."),

        new("extensions1", "08_advanced", "Extensions1", "Methodes d'extension",
            "Ajouter des methodes a un type que tu ne possedes pas : Vector2, int, List.\nLe mot-cle est 'this' devant le premier parametre, dans une classe statique.",
            "public static Vector2 WithY(this Vector2 value, float y)\nUne extension s'appelle meme sur null : c'est son corps qui doit gerer le cas."),

        new("iterators1", "08_advanced", "Iterators1", "yield et evaluation paresseuse",
            "'yield return' fabrique une sequence morceau par morceau.\nRien ne s'execute tant que personne ne parcourt le resultat.",
            "yield return i; dans une boucle. Une suite infinie est permise : c'est Take qui l'arrete."),

        new("disposable1", "08_advanced", "Disposable1", "IDisposable et using",
            "Textures, fichiers, sockets : ce qui s'ouvre doit se fermer, meme si ca plante.\n'using' garantit la liberation a la sortie du bloc, dans l'ordre inverse.",
            "class Texture : IDisposable avec public void Dispose().\nPuis 'using var t = new Texture(...);'. Dispose doit etre sans effet la deuxieme fois."),

        new("linq2", "08_advanced", "Linq2", "LINQ : regrouper, trier, replier",
            "GroupBy range, ToDictionary indexe, Aggregate replie, Zip apparie.\nEt le piege du siecle : une requete se REJOUE a chaque parcours.",
            "OrderByDescending(...).ThenBy(...) pour departager les ex aequo.\nAggregate(0, (total, item) => total + item.Weight)"),

        new("async2", "08_advanced", "Async2", "Paralleliser et annuler",
            "Lancer deux chargements puis attendre les deux, c'est WhenAll.\nUn CancellationToken permet d'arreter proprement ce qui traine.",
            "Garde les Task dans des variables AVANT de les attendre, sinon tu enchaines.\ntoken.ThrowIfCancellationRequested() au debut de chaque tour de boucle."),

        new("spans1", "08_advanced", "Spans1", "Span et zero allocation",
            "Un Span est une fenetre sur de la memoire deja la : pas de copie, pas de dechet.\nC'est ce qui evite les micro-saccades du ramasse-miettes en plein jeu.",
            "ReadOnlySpan<int> accepte un tableau tel quel. text.Slice(0, n) ne copie rien.\nstackalloc int[4] pose le tableau sur la pile."),

        new("godot1", "09_godot", "Godot1", "Le cycle de vie d'un Node",
            "Ordre reel : constructeur, _EnterTree, _Ready, puis _Process a chaque frame,\net _ExitTree a la destruction. GetNode ne marche qu'a partir de _Ready.",
            "Le constructeur s'execute AVANT que le noeud soit dans l'arbre."),

        new("godot2", "09_godot", "Godot2", "Le delta time",
            "Sans delta, la vitesse depend du nombre d'images par seconde.\nAvec delta, elle est identique sur toutes les machines.",
            "Position += direction * vitesse * (float)delta;"),

        new("godot3", "09_godot", "Godot3", "GetNode et validite",
            "On recupere les noeuds dans _Ready et on les stocke.\nApres un QueueFree, seul IsInstanceValid dit la verite.",
            "GetNodeOrNull renvoie null au lieu de planter. IsInstanceValid(n) verifie l'objet natif."),

        new("godot4", "09_godot", "Godot4", "Signaux et evenements",
            "Un composant previent le reste du jeu sans savoir qui ecoute.\nC'est ce qui evite d'avoir des references croisees partout.",
            "Declare l'event, invoque-le quand la valeur change, abonne-toi de l'autre cote."),

        new("godot5", "09_godot", "Godot5", "Singleton et static",
            "Le comportement va en static, l'etat va en instance.\nUn singleton expose une instance unique derriere une propriete statique.",
            "Instance se pose a l'entree dans l'arbre et se remet a null a la sortie."),

        new("vectors1", "10_gamedev", "Vectors1", "Vecteurs : direction et distance",
            "Le calcul que tu ecriras le plus souvent de toute ta vie de gamedev :\naller d'un point vers un autre. Cible moins position, normalise.",
            "(cible - position).Normalized() donne une direction de longueur 1.\nSans Normalized, plus la cible est loin plus tu vas vite. DistanceTo evite la racine a la main."),

        new("timers1", "10_gamedev", "Timers1", "Cooldowns et temps de recharge",
            "Tir automatique, dash, potion, invulnerabilite : c'est toujours le meme compteur.\nOn descend a chaque frame, on agit quand il touche zero.",
            "Descendre : _remaining = Mathf.Max(_remaining - delta, 0);\nUtiliser : si _remaining > 0 on refuse, sinon on agit et on recharge a Cooldown."),

        new("smoothing1", "10_gamedev", "Smoothing1", "Suivi de camera et lissage",
            "Suivre le joueur avec la camera, tourner vers une cible, remonter une barre de vie.\nLe piege : le lissage naif depend du nombre d'images par seconde.",
            "MoveToward avance d'un pas fixe et ne depasse jamais.\nPour un lissage stable : Lerp(actuel, cible, 1 - Mathf.Exp(-force * delta))."),

        new("pool1", "10_gamedev", "Pool1", "Recycler au lieu de creer",
            "Creer une balle 600 fois par seconde fait ramer le jeu.\nOn en fabrique un stock une fois, on les reutilise ensuite.",
            "Take : depiler s'il en reste, sinon en creer une nouvelle.\nGive : remettre dans la pile apres avoir remis l'objet a zero."),

        new("grid1", "10_gamedev", "Grid1", "Grilles et coordonnees monde",
            "Tilemap, inventaire, pathfinding, jeu de plateau : convertir entre\nla case (colonne, ligne) et la position en pixels.",
            "Case vers monde : case * tailleCase. Monde vers case : division ENTIERE (FloorToInt).\nCase vers index d'un tableau plat : ligne * largeur + colonne."),

        new("hitstop1", "10_gamedev", "Hitstop1", "Figer le temps a l'impact",
            "Quatre-vingts millisecondes de gel au moment ou le coup touche : c'est ce qui donne du POIDS\na une frappe, et ca ne coute rien a produire.\n\nAvec un piege qui saute au visage des qu'on l'ecrit : si le gel decompte le temps de JEU, et\nque le temps de jeu vaut zero pendant le gel, il ne se termine jamais. Le jeu est fige pour de\nbon.",
            "Le gel consomme le temps REEL, celui qui continue de couler quand l'echelle de temps est a\nzero.\nUn second coup pendant le gel prend le MAXIMUM, il ne s'ajoute pas : un combo de six coups\nfigerait le jeu une demi-seconde et le joueur croirait a un plantage.\nLe son, l'interface et le menu de pause continuent, eux, sur le temps reel.\nEt l'accumulateur de physique se remplit du temps de JEU : le remplir du temps reel ferait tout\nrattraper d'un coup a la fin du gel, et le personnage traverserait le decor."),

        new("stick1", "10_gamedev", "Stick1", "Zone morte et courbe de stick",
            "Un stick au repos ne rend jamais exactement zero, et un stick use fait deriver le personnage\ntout seul. D'ou la zone morte. Sauf qu'il y en a deux facons de la faire, et la plus evidente\nest la mauvaise.\n\nLa zone morte par COMPOSANTE transforme une diagonale legere en mouvement purement horizontal.\nLe joueur pousse en biais, le personnage part tout droit, et personne ne comprend pourquoi.",
            "Zone morte RADIALE : on regarde la longueur du vecteur, pas ses composantes, ce qui preserve\nexactement la direction.\nEt on REMAPPE ce qui reste sur zero-un : sans ca, franchir le seuil fait sauter la vitesse de 0\na 0.25 d'un coup.\nUn stick lit parfois plus de 1 dans les coins : sans plafond, on court plus vite en diagonale.\nUne courbe de reponse - la longueur elevee a une puissance - ecrase le bas de la course pour\ngagner en precision, sans toucher au maximum.\nEt normaliser un vecteur NUL divise par zero : tester la longueur avant, toujours."),

        new("hurt1", "10_gamedev", "Hurt1", "Encaisser un coup",
            "Recevoir un coup, c'est trois choses a la fois : des degats, un recul, et une periode\nd'invulnerabilite. Plus une quatrieme dont personne ne se souvient la premiere fois.\n\nUne epee qui traverse un ennemi le touche a CHAQUE image de contact. Sans registre de cibles\ndeja frappees par cette attaque-la, l'ennemi meurt en trois images et le jeu n'a aucun sens.",
            "Un HashSet d'identifiants dans l'attaque : Add rend false si la cible y est deja, ce qui est\nexactement le test qu'on veut.\nLes images d'invulnerabilite sont un simple compteur decremente par le delta.\nLe recul part de l'ATTAQUANT vers la cible, jamais dans le sens du stick, et il s'amortit.\nDeux corps exactement superposes, ca arrive : normaliser leur ecart rend NaN, il faut une\ndirection de repli.\nEt les points de vie s'arretent a zero : un negatif traverse ensuite tous les calculs de\npourcentage."),

        new("target1", "10_gamedev", "Target1", "Choisir une cible sans la faire clignoter",
            "La plus proche dans un cone devant soi : un produit scalaire et une comparaison de distances\nau carre, c'est vite ecrit.\n\nEt c'est injouable. Deux ennemis a distance presque egale se volent le reticule a chaque image,\nparce qu'un demi-pixel suffit a inverser le classement. Ce qui manque tient en un mot :\nl'hysteresis.",
            "Le cone se teste en comparant le produit scalaire du regard et de la direction au COSINUS du\ndemi-angle : pas d'arc cosinus, pas de racine.\nLa portee se compare au carre, pour la meme raison.\nL'hysteresis : la cible COURANTE voit son score multiplie par un facteur inferieur a 1, donc il\nfaut une avance nette pour la detroner. Coller n'est pas s'accrocher.\nUne cible qui sort du cone ou de la portee est lachee, et l'etat interne suit.\nEt une cible exactement sur soi n'est pas ciblable : normaliser un vecteur nul rend NaN, et NaN\npasse toutes les comparaisons en silence."),

        new("intercept1", "10_gamedev", "Intercept1", "Tirer sur une cible qui bouge",
            "Viser la ou l'ennemi EST ne le touche jamais. Il faut viser ou il SERA quand le projectile\narrivera, ce qui demande de connaitre le temps de vol... qui depend de l'endroit ou il sera.\n\nC'est une equation du second degre, et ses trois cas particuliers sont exactement les trois\nbugs qu'on trouve dans le code des autres.",
            "On cherche le temps t tel que la distance a parcourir vaille la vitesse du projectile fois t.\nEn developpant : a = v.v - vitesse au carre, b = 2 (ecart . v), c = ecart.ecart.\nDiscriminant negatif : la cible fuit plus vite que le projectile, il n'y a PAS de solution, et\nil faut le dire au lieu de rendre NaN.\nRacine negative : c'est une solution dans le PASSE, on la rejette et on prend l'autre.\nEt a proche de zero - la cible va exactement a la vitesse du projectile - fait disparaitre le\nterme carre : sans ce test on divise par zero."),

        new("loot1", "10_gamedev", "Loot1", "Table de butin et compteur de pitie",
            "Des poids, un tirage, un objet. Les poids CUMULES permettent de repondre par une recherche\ndichotomique au lieu d'un parcours, et c'est aux BORNES que tout le monde se trompe d'un cran.\n\nEt un taux annonce a 5 pour cent produit des joueurs qui font cinquante tentatives et vont\necrire que le jeu est casse. D'ou la pitie : une garantie apres N echecs.",
            "On stocke le cumul, pas le poids : 70, 95, 100. Le tirage se ramene modulo le total, puis une\ndichotomie trouve la premiere tranche strictement superieure.\nUn poids nul doit etre REFUSE : il fabrique une tranche vide que la recherche peut atteindre.\nLe compteur de pitie se remet a zero sur une reussite comme sur une garantie. L'oublier rend\nla pitie permanente au bout d'une heure de jeu.\nEt il faut le savoir : une table a 5 pour cent AVEC pitie n'est pas a 5 pour cent. Le taux reel\nest plus haut, et c'est ce chiffre-la qu'on affiche si on affiche quelque chose."),

        new("states1", "11_patterns", "States1", "Machine a etats",
            "Idle, Run, Jump, Fall, Dead. Le bug classique n'est pas l'etat courant,\nc'est d'autoriser une transition qui ne devrait pas exister.",
            "Un switch sur le couple (Current, next) dit ce qui est permis.\nMort est un puits : on y entre depuis partout, on n'en sort jamais."),

        new("command1", "11_patterns", "Command1", "Commandes, annuler et refaire",
            "Une action qui sait se defaire elle-meme devient annulable gratuitement.\nDeux piles suffisent : ce qui est fait, ce qui est annule.",
            "Undo depile 'fait', appelle Undo, empile dans 'annule'. Redo fait l'inverse.\nUne nouvelle action efface la pile 'annule' : la branche du futur n'existe plus."),

        new("bus1", "11_patterns", "Bus1", "Bus de messages type",
            "Le score, les quetes et le son reagissent au meme evenement sans se connaitre.\nLe TYPE du message sert d'adresse : un dictionnaire Type vers abonnes.",
            "Dictionary<Type, List<Delegate>> puis _handlers[typeof(T)].\nA la publication, il faut recaster : ((Action<T>)handler)(message).\n\nAttention : 'record struct' est du C# 10, donc Godot oui, Unity non (fige en C# 9).\nCote Unity, ecris un 'readonly struct' classique : le bus, lui, ne change pas."),

        new("components1", "11_patterns", "Components1", "Composition plutot qu'heritage",
            "Au lieu d'une classe EnnemiVolantQuiTireEtExplose, on assemble des composants.\nC'est le modele de Unity, de Godot, et de tous les moteurs modernes.",
            "Get<T>() cherche dans la liste : _components.OfType<T>().FirstOrDefault().\nUn composant doit survivre a l'absence de ses voisins : verifie le null."),

        new("locator1", "11_patterns", "Locator1", "Services et interfaces",
            "Le code de jeu appelle IAudio, pas RealAudio. On peut donc le remplacer\npar une version muette dans les tests sans toucher a une ligne du jeu.",
            "Registry[typeof(T)] = service; puis (T)Registry[typeof(T)].\nUn service absent doit dire lequel manque, pas planter avec une cle introuvable."),

        new("data1", "11_patterns", "Data1", "Donnees partagees et etat propre",
            "500 gobelins a l'ecran, UNE seule fiche de stats en memoire.\nCe qui est commun est partage, ce qui change est propre a l'instance.",
            "L'instance garde une REFERENCE vers sa definition, elle ne la recopie pas.\nPoints de vie et position sont par instance ; degats et vitesse sont dans la fiche."),

        new("angles1", "12_math", "Angles1", "Angles et rotations",
            "Viser, tourner vers une cible, savoir si l'ennemi est dans le champ de vision.\nLe piege : de 350 a 10 degres, il n'y a que 20 degres, pas 340.",
            "L'angle d'un vecteur : (cible - position).Angle(). Ecart le plus court : Mathf.AngleDifference.\nPour ramener un angle dans 0..7 : Mathf.PosMod."),

        new("easing1", "12_math", "Easing1", "Interpolation et courbes",
            "InverseLerp est le Lerp a l'envers : il rend le pourcentage.\nEt un lissage exponentiel donne le meme resultat a 30 ou 144 images par seconde.",
            "InverseLerp(from, to, value) = (value - from) / (to - from), avec garde-fou.\nLissage stable : Lerp(actuel, cible, 1 - Exp(-force * delta))."),

        new("collision1", "12_math", "Collision1", "Collisions",
            "Cercle contre cercle, rectangle contre rectangle, cercle contre rectangle.\nEt surtout : de combien repousser pour sortir d'un mur.",
            "Cercles : compare les distances au CARRE, ca evite une racine.\nCercle-rectangle : trouve le point du rectangle le plus proche (un Clamp par axe)."),

        new("rays1", "12_math", "Rays1", "Rayons et ligne de vue",
            "Projeter un point sur un segment, croiser deux segments, savoir si un garde\nvoit le joueur. Un segment n'est pas une droite : il s'arrete.",
            "Projection : Clamp du produit scalaire divise par la longueur au carre, entre 0 et 1.\nCroisement : le produit vectoriel donne t et u, et les DEUX doivent tenir dans 0..1."),

        new("random1", "12_math", "Random1", "Aleatoire maitrise",
            "Un aleatoire a graine rejoue exactement le meme donjon.\nEt un tirage pondere fait tomber l'epique une fois sur cent, pas une fois sur trois.",
            "Range doit ramener dans [min, max[ : min + valeur % (max - min).\nTirage pondere : additionne les poids, tire dans le total, soustrais jusqu'a passer sous zero."),

        new("curves1", "12_math", "Curves1", "Beziers et trajectoires",
            "La courbe d'une fleche, d'un saut, d'une camera qui suit un rail.\nUne Bezier, ce ne sont que des Lerp de Lerp.",
            "Quadratique : lerp(lerp(a,b,t), lerp(b,c,t), t).\nUn saut : la ligne droite, plus une hauteur en 4 * h * t * (1 - t)."),

        new("inventory1", "13_systems", "Inventory1", "Inventaire et piles",
            "On remplit d'abord les piles existantes, ensuite les cases vides.\nEt ce qui ne rentre pas doit etre RENDU, pas avale silencieusement.",
            "Add renvoie ce qui n'a pas pu etre range. Deux passes : d'abord empiler, puis poser.\nRemove verifie le total AVANT de commencer, sinon il retire a moitie."),

        new("damage1", "13_systems", "Damage1", "Calcul de degats",
            "L'ordre des operations change tout : le critique double avant l'armure.\nEt un coup fait toujours au moins un point, sinon on soigne l'ennemi.",
            "Critique d'abord, puis armure (soustraction) ou resistance (multiplication).\nApply ne compte que ce qu'il restait de points de vie, et ignore les morts."),

        new("pathfinding1", "13_systems", "Pathfinding1", "Trouver un chemin",
            "Un parcours en largeur trouve toujours le chemin le plus court sur une grille.\nUne file d'attente, une table 'je viens d'ou', et on remonte a la fin.",
            "Queue pour les cases a visiter, Dictionary pour retenir d'ou on vient.\nLa table sert AUSSI de liste des cases deja vues : pas besoin d'un second ensemble."),

        new("fixed1", "13_systems", "Fixed1", "Pas de temps fixe",
            "La physique doit avancer par pas constants, sinon elle change avec les FPS.\nOn accumule le temps ecoule et on consomme les pas entiers.",
            "while (accumulateur >= pas) { accumulateur -= pas; pas++; }\nEt on plafonne : sans limite, une frame lente en genere 200 et le jeu se noie."),

        new("spatial1", "13_systems", "Spatial1", "Grille spatiale",
            "1000 entites, ca fait 500 000 paires a tester. Avec une grille, une poignee.\nRegle absolue : le filtrage grossier peut proposer trop, jamais oublier.",
            "Un Dictionary de (colonne, ligne) vers la liste des ids.\nUne requete balaie toutes les cases touchees par le cercle, pas seulement la case du centre."),

        new("input1", "13_systems", "Input1", "Buffer d'entree et coyote time",
            "Les deux astuces qui font qu'un jeu de plateforme est agreable :\ngarder l'appui juste trop tot, et pardonner le saut juste trop tard.",
            "Deux compteurs : depuis quand on a appuye, depuis quand on a quitte le sol.\nSi les deux sont dans leur fenetre, on saute, et on remet les deux a l'infini."),

        new("save1", "13_systems", "Save1", "Sauvegarder et recharger",
            "Ecrire l'etat en texte, le relire, et surtout survivre a un fichier incomplet\nou abime. Une sauvegarde qui plante, c'est une partie perdue.",
            "TryGetValue et TryParse partout : un champ absent ou illisible prend sa valeur par defaut.\nEcris les flottants avec CultureInfo.InvariantCulture, sinon la virgule casse tout."),

        new("order1", "14_engine", "Order1", "_Process contre _PhysicsProcess",
            "Deux boucles, pas une : le rendu suit les FPS, la physique avance a pas fixe.\nEt la camera doit passer APRES ce qu'elle suit, sinon elle a une frame de retard.",
            "Le deplacement va dans _PhysicsProcess, l'affichage dans _Process.\nProcessPriority ordonne les _Process : plus grand veut dire plus tard (le LateUpdate de Unity)."),

        new("cache1", "14_engine", "Cache1", "Chercher une fois, pas soixante",
            "GetNode et GetComponent coutent cher. On les appelle dans _Ready et on garde\nle resultat. Meme idee pour un calcul de stats : on ne recalcule que si ca a change.",
            "Un champ prive rempli dans _Ready, utilise ensuite dans _Process.\nPour les stats : un booleen '_dirty' remis a true par tout ce qui modifie l'entree."),

        new("actions1", "14_engine", "Actions1", "Appui, maintien, relachement",
            "'enfoncee' et 'vient d'etre enfoncee' ne sont pas la meme chose : sans la difference,\ntenir la touche tire en rafale. Il faut garder l'etat de la frame precedente.",
            "just_pressed = enfoncee maintenant ET pas a la frame d'avant.\nEt le vecteur de deplacement doit etre normalise, sinon la diagonale va 41 pour cent plus vite."),

        new("movement1", "14_engine", "Movement1", "Gravite, friction, hauteur de saut",
            "On ne regle pas un saut en tatonnant sur une vitesse : on choisit une hauteur\net on en DEDUIT la vitesse. Et une chute doit avoir une vitesse terminale.",
            "Hauteur h avec gravite g : vitesse initiale = -sqrt(2 * g * h).\nAcceleration et friction passent par MoveToward : il ne depasse jamais la cible."),

        new("layers1", "14_engine", "Layers1", "Couches et masques de collision",
            "Une couche par bit, un masque qui dit ce qu'on ecoute. C'est le systeme de Godot,\nde Unity, et de a peu pres tous les moteurs.",
            "Les valeurs doivent etre des puissances de deux : 1, 2, 4, 8, 16.\nAjouter : |. Retirer : & ~. Tester au moins un : (mask & layers) != 0."),

        new("camera1", "14_engine", "Camera1", "Camera : zone morte, bornes, secousse",
            "Une camera collee au joueur tremble. Une camera sans bornes montre le vide.\nEt une secousse qui s'arrete net se voit tout de suite.",
            "Zone morte : on ne bouge que de (distance - zone morte).\nBornes : Clamp entre position + demi-ecran et fin - demi-ecran, par axe.\nSecousse : amplitude en trauma AU CARRE, trauma qui redescend lineairement."),

        new("coroutines1", "14_engine", "Coroutines1", "Coroutines maison",
            "Une coroutine, c'est un IEnumerator qu'on fait avancer d'un cran par frame.\nC'est exactement le modele de Unity, et il tient en trente lignes.",
            "MoveNext fait avancer jusqu'au prochain yield. Current dit ce qu'on attend.\nOn parcourt la liste a l'envers pour pouvoir en retirer pendant l'iteration."),

        new("tween1", "14_engine", "Tween1", "Tween et callback de fin",
            "Interpoler une valeur dans le temps, finir EXACTEMENT sur la cible,\net ne prevenir qu'une seule fois. Les trois pieges d'un tween maison.",
            "Clamp le temps normalise a 1 avant d'interpoler, puis pose Value = _to a la fin.\nUn booleen 'Finished' empeche le callback de partir a chaque frame suivante."),

        new("alloc1", "15_perf", "Alloc1", "Zero allocation dans la boucle chaude",
            "Ici les verifications COMPTENT les octets alloues. Une boucle for sur une List\nne doit rien allouer. LINQ, si : et 60 fois par seconde, ca se voit.",
            "Remplace Sum, Count et OrderBy par des boucles a la main.\nPour trouver le plus proche, compare les distances au CARRE : pas de racine."),

        new("boxing1", "15_perf", "Boxing1", "Boxing : l'allocation invisible",
            "Une structure sans IEquatable utilisee comme cle de dictionnaire s'emballe dans un\nobjet a CHAQUE comparaison. Idem pour un foreach derriere IEnumerable<T>.",
            "Implemente IEquatable<CellKey> : Equals(CellKey), Equals(object) et GetHashCode.\nEt declare le parametre en List<int> plutot qu'en IEnumerable<int>."),

        new("text1", "15_perf", "Text1", "Texte et HUD sans dechets",
            "Un $\"PV {x}\" par frame, c'est 60 chaines par seconde pour rien.\nOn ne reconstruit que si la valeur a change, et on colle avec un StringBuilder.",
            "Garde la derniere valeur affichee et la derniere chaine produite : si rien n'a change, renvoie-la.\nPour assembler : un StringBuilder, Append, puis un seul ToString."),

        new("loops1", "15_perf", "Loops1", "Supprimer sans tout casser",
            "Retirer d'une liste en la parcourant vers l'avant saute des elements.\nEt quand l'ordre n'a pas d'importance, on peut retirer en temps constant.",
            "Parcours de la fin vers zero, ou RemoveAll.\nSuppression par echange : on ecrase la case avec le dernier element, puis on retire le dernier."),

        new("memory1", "15_perf", "Memory1", "Structures, tableaux et reutilisation",
            "1000 objets recrees chaque frame, ce sont 1000 allocations. Un tableau de structures\nreutilise, c'est zero. Et list[i] rend une COPIE, pas l'element.",
            "Un tableau de structures se modifie EN PLACE : particles[i].Life -= delta.\nUn foreach sur des structures ne donne que des copies, il faut une boucle for."),

        new("budget1", "15_perf", "Budget1", "Etaler le travail et doser la distance",
            "On ne met pas a jour 1000 ennemis a chaque frame. On en fait un quart par frame,\net ceux qui sont loin se contentent d'une mise a jour par seconde.",
            "Tranche : index % tranches == frame % tranches, avec PosMod pour les negatifs.\nLoin du joueur, on augmente l'intervalle entre deux mises a jour."),

        new("stack1", "16_memory", "Stack1", "Ce que contient vraiment une variable",
            "Une structure EST la valeur. Un objet n'est qu'une adresse vers le tas.\nEt un parametre objet passe cette adresse... par copie. C'est la source de la moitie des bugs.",
            "Choisis 'struct' ou 'class' pour que chaque verification dise la verite.\nUn tableau d'objets ne contient que des adresses nulles : aucun objet n'existe tant qu'on n'a pas fait 'new'."),

        new("refs1", "16_memory", "Refs1", "ref, out et in",
            "'ref' passe la variable elle-meme. 'out' impose de la remplir. 'in' passe l'adresse\nsans droit d'ecriture, pour eviter de recopier une grosse structure.",
            "Un 'ref' local est un ALIAS : 'ref int slot = ref Slot(tab, 1);' puis 'slot = 99;' ecrit dans le tableau.\nUne methode peut renvoyer 'ref' : c'est ce qui permet de modifier un element de tableau sans le recopier."),

        new("delegates2", "16_memory", "Delegates2", "Delegates, multicast et fuites",
            "Un delegate garde son objet en vie. Se desabonner avec une NOUVELLE lambda\nne desabonne rien : c'est la fuite memoire la plus repandue en gamedev.",
            "Pour pouvoir faire '-=', abonne-toi avec un groupe de methodes, pas une lambda ecrite sur place.\nGetInvocationList() donne les abonnes un par un : c'est comme ca qu'on isole celui qui plante."),

        new("gc1", "16_memory", "Gc1", "Le tas, l'en-tete et les generations",
            "Ici on MESURE : un objet vide coute 24 octets d'en-tete, 1000 structures tiennent\nen une seule allocation, et 200 000 objets jetables declenchent de vraies pauses.",
            "Reutilise un seul tampon alloue avant la boucle au lieu d'en creer un a chaque tour.\nUne collection gen0 est rapide, mais c'est une PAUSE : dans 16 millisecondes de budget, ca se voit."),

        new("copies1", "16_memory", "Copies1", "Copies defensives",
            "Appeler une methode sur un champ 'readonly' de type structure travaille sur une COPIE.\nLe code compile, tourne, et ne fait rien. Personne ne trouve ce bug du premier coup.",
            "La parade : rends la structure 'readonly struct' et renvoie une nouvelle instance au lieu de modifier.\nlist[0].Methode() modifie un temporaire ; array[0].Methode() modifie l'original."),

        new("entities1", "17_ecs", "Entities1", "Identifiants a generation",
            "Un identifiant d'entite porte DEUX choses : ou (le slot) et quand (la generation).\nSans la generation, un slot recycle fait resoudre l'ancien identifiant sur la nouvelle\nentite. C'est le pointeur fantome, et c'est le bug le plus penible du metier.",
            "Detruire incremente la generation du slot avant de le rendre au stock.\nIsAlive compare l'index ET la generation, apres avoir verifie les bornes.\nEt l'egalite d'un identifiant porte sur les deux champs, sinon le dictionnaire confond les deux."),

        new("storage1", "17_ecs", "Storage1", "Stockage en colonnes",
            "Le coeur d'un ECS : une colonne par composant, rangee DENSE, plus une table creuse\nqui dit ou se trouve la ligne d'une entite. Retirer se fait par echange avec la\nderniere ligne, donc la memoire reste contigue et le parcours reste gratuit.",
            "Get doit renvoyer un 'ref' pour ecrire dans la colonne sans recopier la structure.\nApres un echange, il faut mettre a jour la table creuse de l'entite qui a DEMENAGE.\nEt exposer un Span plutot qu'un tableau recopie, sinon chaque lecture alloue."),

        new("masks1", "17_ecs", "Masks1", "Masques de composants",
            "Un bit par type de composant, un entier par entite : savoir si une entite repond a\nune requete devient une seule operation binaire. C'est ce qui rend les requetes\ninstantanees, et c'est exactement le systeme des couches de collision.",
            "Le bit d'un composant est 1 << son rang, jamais son rang.\nHasAll compare au masque DEMANDE : (bits & requis) == requis. Compare a zero, c'est HasAny.\nRetirer un bit, c'est un ET avec le complement. Et BitOperations.PopCount compte les bits.\n\nAttention : BitOperations vient de .NET Core 3.0, donc Godot oui, Unity non.\nCote Unity, compte les bits a la main : decale et masque tant qu'il reste des bits."),

        new("query1", "17_ecs", "Query1", "Iterer sans allouer",
            "Une requete se parcourt 60 fois par seconde. Si son enumerateur est un objet, c'est\n60 objets par seconde et par requete. Un enumerateur en STRUCTURE, reconnu par foreach\nsans passer par IEnumerable, ne coute rien du tout.",
            "foreach n'a pas besoin d'IEnumerable : une methode GetEnumerator publique suffit,\net si elle renvoie une structure avec MoveNext et Current, rien n'est alloue.\nLa version 'yield return' est plus courte a ecrire et alloue son etat sur le tas."),

        new("systems1", "17_ecs", "Systems1", "Systemes et commandes differees",
            "Les systemes tournent dans un ordre fixe et ne touchent jamais la structure du monde\nen pleine iteration : ils empilent des commandes, appliquees a la fin de la frame.\nResultat : tous les systemes d'une meme frame voient le meme monde.",
            "Naitre et mourir passent par le tampon, jamais par un appel direct depuis un systeme.\nLe tampon s'applique APRES tous les systemes, et se vide en s'appliquant,\nsinon les memes commandes repartent a chaque frame."),

        new("bench1", "17_ecs", "Bench1", "Colonnes contre objets, mesure",
            "La conclusion de la section, chiffres en main : ce que coute vraiment un tableau de\nstructures contre 10 000 objets, et ce qui alloue dans une boucle de jeu. Le resultat\nva peut-etre te surprendre : ce n'est pas 'objet contre structure' le probleme.",
            "Un tableau de N structures, c'est UNE allocation : 24 octets d'en-tete plus N fois la taille.\nUn parcours a l'index n'alloue rien, quel que soit le type range dedans.\nCe qui alloue, c'est IEnumerable<T>, LINQ, et une List<T> qui grandit sans capacite annoncee."),

        new("crossing1", "18_bridge", "Crossing1", "Chaque propriete est un appel natif",
            "Le sujet Godot le plus couteux et le plus invisible : une propriete comme Position n'est\nPAS un champ. Chaque lecture et chaque ecriture franchit la frontiere C# vers moteur. Une\nligne qui lit Position trois fois paye trois appels.\n\nEt ca n'alloue rien, donc ca n'apparait dans aucun profil de ramasse-miettes. C'est\nexactement pour ca que personne ne le trouve.",
            "Lis la propriete UNE fois dans une variable locale, fais tout ton calcul dessus, reecris\nune fois a la fin.\nEt souviens-toi de copies1 : 'node.Position.X = 5f' ne compile pas, ni ici ni dans Godot.\nUne propriete rend une COPIE de la structure, on n'ecrit pas dans un de ses champs.\nIl faut lire, fabriquer un nouveau vecteur, reecrire."),

        new("names1", "18_bridge", "Names1", "Les noms qui allouent en silence",
            "Passer une chaine litterale la ou le moteur attend un nom (signal, propriete, action,\ngroupe) declenche une conversion IMPLICITE : un objet par appel. Cent emissions, cent\nobjets, et rien dans le code ne le laisse deviner.",
            "Garde le nom dans un champ 'static readonly' et passe-le tel quel : plus aucune conversion.\nC'est exactement ce que sont les SignalName.X et PropertyName.X que Godot te genere.\nUne emission sans argument avec un nom garde ne coute alors plus rien du tout."),

        new("signals1", "18_bridge", "Signals1", "Ce que coute un signal",
            "Un signal moteur passe ses arguments dans un tableau, et 'params' en fabrique un neuf a\nchaque emission. Un event C# n'en fabrique aucun, et il donne des arguments TYPES au lieu\nd'un tableau a indexer. Reste a savoir quand chacun est le bon choix.",
            "Signal moteur quand l'editeur doit voir la connexion, ou quand du code non-C# ecoute.\nEvent C# pour tout ce qui reste entre tes scripts.\nA noter : un signal SANS argument ne coute rien, le compilateur passe un tableau vide partage."),

        new("marshal1", "18_bridge", "Marshal1", "Les collections du moteur sont copiees",
            "Un tableau moteur et une List<T> ne sont pas deux vues sur la meme memoire : passer de\nl'un a l'autre RECOPIE tout. Faire cette conversion dans une boucle de jeu, c'est allouer\nquatre kilo-octets par frame pour additionner mille entiers.",
            "Convertis UNE fois, au chargement, et garde ensuite la forme dont tu as besoin.\nSi tu dois lire du cote moteur a chaque frame, lis element par element : plus lent par\nelement, mais zero allocation. Et remplir le tableau moteur directement ne convertit rien."),

        new("deferred1", "18_bridge", "Deferred1", "Plus tard, quand ce sera sur",
            "On ne modifie pas l'arbre de scene au milieu d'un callback physique : le moteur est en\ntrain de parcourir ses listes. La parade est une file d'appels differes, videe a la fin de\nla frame. C'est le meme principe que le tampon de commandes de systems1, mais impose par\nle moteur cette fois.",
            "La file se vide jusqu'a etre EPUISEE : un appel differe qui en demande un autre part dans la\nmeme vidange, pas a la frame suivante. Verifie dans le moteur.\nRevers de la medaille : un appel qui se redemande lui-meme gele la frame, et rien ne t'en protege.\nPendant le callback physique, en revanche, le noeud demande n'existe pas encore : ne compte pas dessus."),

        new("lifecycle1", "19_unity", "Lifecycle1", "Le cycle de vie d'un MonoBehaviour",
            "Ordre reel : constructeur, Awake, OnEnable, puis Start avant le premier Update, puis les\nboucles, et OnDisable suivi de OnDestroy a la fin. Deux details font toute la difference :\nTOUS les Awake de la scene passent avant TOUS les Start, et OnEnable rejoue a chaque\nreactivation, pas seulement au demarrage.\n\nC'est le pendant Unity de godot1, et les pieges ne sont pas les memes.",
            "Chercher un autre objet dans Awake est un pari, dans Start c'est une certitude.\nOn s'abonne dans OnEnable et on se desabonne dans OnDisable : reactiver un script rejoue\nOnEnable sans rejouer Awake ni Start, et la destruction passe par OnDisable avant OnDestroy,\ndonc le desabonnement est fait dans les deux cas avec un seul endroit a ecrire."),

        new("destroyed1", "19_unity", "Destroyed1", "L'objet detruit qui n'est pas null",
            "Unity surcharge l'operateur == sur ses objets : un objet detruit rend TRUE quand on le\ncompare a null, alors que la variable contient toujours une reference. Du coup 'x == null'\net 'x is null' ne veulent pas dire la meme chose, et le '?.' ne protege de rien.\n\nC'est le piege inverse de Godot, ou le test null ment dans l'autre sens et ou il faut\nIsInstanceValid. Meme cause : deux objets, un manage et un natif, qui ne meurent pas ensemble.",
            "Sur un objet Unity, teste '== null' et jamais 'is null' : le motif ne passe pas par l'operateur.\nDestroy met dans une file appliquee en FIN DE FRAME, donc l'objet vit encore et recoit son Update.\nDestroyImmediate, lui, detruit sur place."),

        new("delta1", "19_unity", "Delta1", "Time.deltaTime et ses trois pieges",
            "Sans delta, ton jeu va deux fois plus vite sur un ecran a 120 Hz. Ca, c'est la base.\nMais l'API Time de Unity a trois surprises : le MEME Time.deltaTime lu dans FixedUpdate\nrend le pas fixe et non le temps de la frame, un gel de deux secondes arrive plafonne a un\ntiers de seconde, et trois horloges differentes ne racontent pas la meme histoire apres\nune pause.",
            "Time.deltaTime dans FixedUpdate vaut fixedDeltaTime : le code a l'air correct des deux cotes.\nmaximumDeltaTime plafonne le delta pour eviter la spirale de la mort apres un chargement.\nunscaledDeltaTime ignore timeScale : c'est celui des menus, des fondus de son et de l'interface.\nEt compter en frames n'est jamais compter en secondes."),

        new("serialize1", "19_unity", "Serialize1", "Ce que Unity sait serialiser",
            "Unity serialise les CHAMPS, pas les proprietes. Et il ne sait pas serialiser un\nDictionary. Le pire, c'est qu'il ne previent pas : le champ disparait de l'inspecteur et\nde la sauvegarde, en silence, et tu cherches pendant une heure.\n\nLa parade officielle est d'aplatir le dictionnaire en deux listes, dans les deux callbacks.",
            "Une propriete auto n'est pas serialisee : son champ de stockage est prive et genere.\nOnBeforeSerialize remplit les deux listes depuis le dictionnaire, OnAfterDeserialize refait\nle dictionnaire depuis les listes.\nLes listes sont un cache, pas la verite : elles ne se mettent a jour qu'a la sauvegarde."),

        new("updatetax1", "19_unity", "UpdateTax1", "Le prix d'un Update par objet",
            "Chaque Update() est un appel du moteur vers ton code, et chacun franchit la frontiere\nnatif vers manage. Mille scripts, c'est mille franchissements par frame. Un seul script\nqui boucle sur mille objets, c'est UN franchissement. On mesure les deux ici.",
            "Le 'manager pattern' : un MonoBehaviour, une liste d'objets C# ordinaires, une boucle a\nl'index. Les objets de jeu n'ont pas besoin d'etre des MonoBehaviour pour exister.\nVersion pauvre de la meme idee : desactiver un script suffit a supprimer son appel."),

        new("lateupdate1", "19_unity", "LateUpdate1", "Trois boucles, pas une",
            "Unity a TROIS boucles par frame : FixedUpdate a pas fixe, Update au framerate, puis\nLateUpdate apres tous les Update. Une camera qui suit sa cible dans Update a une frame de\nretard si son script passe avant : c'est exactement ce qu'on voit trembler a l'ecran.\n\nEt un appui lu dans FixedUpdate est perdu une frame sur six, ou compte double.",
            "LateUpdate passe apres TOUS les Update de la scene : c'est la que va une camera qui suit.\nA 60 images par seconde et 50 pas de physique, certaines frames jouent ZERO FixedUpdate et\nd'autres en jouent deux : on lit l'input dans Update, on memorise, on consomme dans FixedUpdate.\nDernier detail : un Update que tu n'ecris pas ne coute rien, Unity ne branche que le declare."),

        new("getcomponent1", "19_unity", "GetComponent1", "Chercher une fois, pas soixante",
            "GetComponent parcourt les composants de l'objet, et c'est un appel natif. Dans Update,\nc'est soixante recherches par seconde et par script pour un resultat qui ne change jamais.\n\nEt le piege de plus : l'operateur == de Unity n'est pas un test de reference. Il demande au\nmoteur si l'objet natif vit encore, donc il coute, donc on ne le met pas dans une boucle.",
            "Cherche dans Awake, garde dans un champ prive, utilise ensuite. C'est le cache1 de la\nsection Godot, avec GetComponent au lieu de GetNode.\nTryGetComponent remplace le GetComponent suivi d'un test null quand le composant peut manquer.\nEt une reference cachee se VERIFIE avant usage : detruire un objet detruit ses composants."),

        new("waits1", "19_unity", "Waits1", "Les attentes de coroutine qui allouent",
            "'yield return new WaitForSeconds(0.1f)' fabrique un objet a chaque battement. Dans une\ncoroutine qui tourne en boucle, c'est un objet par battement pour toute la partie. Or une\nattente est une DUREE : une seule instance suffit.",
            "Mets l'attente dans un champ 'static readonly' et rends la meme a chaque fois : le timing\nest identique, la facture memoire non.\n'yield return null' attend une frame et n'alloue rien du tout."),

        new("material1", "19_unity", "Material1", "Le materiau qui se clone tout seul",
            "Lire 'renderer.material' ne lit rien : ca CLONE le materiau et rend la copie. Une\npropriete, pas une methode, et elle alloue un objet natif que le ramasse-miettes ne\nprendra jamais. Cent ennemis teintes, cent materiaux qui restent en memoire pour toujours.",
            "'sharedMaterial' ne clone pas et modifie tout le monde a la fois.\n'material' clone au PREMIER acces, et ne clone qu'une fois par afficheur.\nUn clone est un objet natif : il faut un Destroy pour chacun, dans OnDestroy."),

        new("singleton1", "19_unity", "Singleton1", "Un singleton qui survit au changement de scene",
            "Le comportement pur va en static, l'etat va en instance, et un gestionnaire global est une\ninstance unique derriere une propriete statique. Cote Unity trois details sont obligatoires :\nposer l'instance dans Awake, se detruire soi-meme si une autre existe deja, et ne remettre la\npropriete a null que 'si c'est encore moi'.\n\nEt le garde s'ecrit '== null'. En 'is null', une reference perimee casserait le singleton\npour toute la partie.",
            "Awake pose l'instance, DontDestroyOnLoad la fait survivre au chargement de scene suivant.\nUn doublon appelle Destroy sur LUI-MEME, pas sur l'existant : c'est le cas d'une scene chargee deux fois.\nOnDestroy verifie 'Instance == this' avant de remettre a null, sinon le doublon efface l'original.\nEt un objet natif mort laisse une reference que seul '== null' detecte."),

        new("aot1", "19_unity", "Aot1", "Ce que le compilateur ne voit pas n'existe pas",
            "Unity compile en avance pour les consoles et le mobile : IL2CPP genere du code natif AVANT\nde connaitre l'execution. Un type que personne n'instancie dans le code n'a donc pas de\nconstructeur genere, et le construire par reflexion echoue. Pas a la compilation : a\nl'execution, sur la console, et jamais dans l'editeur.\n\nC'est le pendant Unity de 18_bridge : une contrainte de plateforme, pas de langage.",
            "Remplace la fabrique par reflexion par une TABLE de fabriques : un dictionnaire de\n'() => new Machin()'. Le compilateur voit chaque construction, donc il genere tout, donc rien\nn'est supprime. Et un type oublie se voit a la lecture de la table, pas trois heures plus tard.\nMeme idee pour un registre : un champ statique sur un type generique donne un emplacement par\ntype ferme, sans recherche et sans emballage, la ou un Dictionary<Type, object> emballe."),

        new("reload1", "19_unity", "Reload1", "Les statiques qui survivent a la partie",
            "Presque toutes les equipes desactivent le rechargement de domaine pour gagner du temps de\ncompilation. Consequence : entrer en jeu ne remet plus les statiques a zero. Le score\nreprend ou il en etait, et l'evenement statique garde les abonnes de la partie precedente.",
            "Un reset explicite appele au demarrage de chaque partie : l'etat, les collections, ET les\nevenements statiques remis a null.\nSans ca, des objets morts continuent de repondre aux evenements, et ils restent en vie a\ncause de l'abonnement lui-meme."),

        new("asset1", "19_unity", "Asset1", "ScriptableObject : configuration, pas etat",
            "Un ScriptableObject n'est pas un modele qu'on copie : c'est une instance UNIQUE, partagee\npar tout ce qui le reference. Charger deux fois le meme asset rend le meme objet.\n\nD'ou le piege qui coute le plus cher de tout Unity : y ranger de l'etat de partie. Un ennemi\nqui ecrit ses points de vie dans l'asset les retire a tous les autres - et dans l'editeur, la\nmodification SURVIT a l'arret du mode jeu et finit versionnee dans le depot.",
            "L'asset porte la CONFIGURATION - points de vie maximum, vitesse, degats - et l'instance porte\nl'ETAT courant.\nUn champ qui change pendant la partie n'a rien a faire dans un ScriptableObject.\nEn echange, regler un ennemi devient une modification de DONNEES : le designer change une\nvaleur dans l'inspecteur, personne ne recompile, et un patch d'equilibrage ne touche pas une\nligne de C#."),

        new("transform1", "19_unity", "Transform1", "Chaque acces au transform traverse la frontiere",
            "'transform.position' ressemble a un champ. C'est un appel dans le moteur natif, et\n'transform.position += v' en fait DEUX : une lecture puis une ecriture.\n\nEt l'autre moitie du piege : la propriete rend une COPIE. C'est pour ca que\n'transform.position.x = 5' ne compile pas - le compilateur refuse plutot que de te laisser\ncroire que ca marche.",
            "SetPositionAndRotation fait UN franchissement la ou position puis rotation en font deux.\nChanger une seule composante demande de reconstruire le vecteur entier et de le REECRIRE.\nLire coute aussi cher qu'ecrire : une traversee par objet et par acces.\nEt la parade est celle de 18_bridge cote Godot : garder ses positions dans un tableau a soi,\ncalculer dessus, et n'ecrire dans le moteur qu'une fois par objet et par image."),

        new("destroy1", "19_unity", "Destroy1", "Destroy ne detruit pas tout de suite",
            "Juste apres Destroy(x), l'objet est TOUJOURS la : Unity l'inscrit sur une liste et le\nsupprime a la fin de l'image. Donc '== null' vaut encore false, un nettoyage lance dans la\nmeme image ne trouve rien, et tout ce qu'on fait a l'objet dans cet intervalle est perdu sans\nle moindre avertissement.\n\nC'est le complement direct de destroyed1 : la, l'objet mort ressemblait a null ; ici, l'objet\ncondamne ressemble a vivant.",
            "PendingDestruction, puis FlushDestruction a la fin de l'image : c'est la mecanique reelle.\nDetruire deux fois le meme objet dans une image ne doit l'inscrire qu'une fois.\nDestroyImmediate detruit sur place - il existe pour les outils d'editeur, et l'appeler pendant\nune partie casse tout ce qui tient encore une reference dans la meme image.\nEt la regle : apres un Destroy, on cesse d'utiliser la reference IMMEDIATEMENT. On ne compte\npas sur '== null' pour nous prevenir dans la meme image."),

        new("rigidbody1", "19_unity", "Rigidbody1", "Rigidbody, FixedUpdate et l'escalier",
            "L'entree se lit dans Update, une fois par image. Le corps avance dans FixedUpdate, cinquante\nfois par seconde. Les deux boucles n'ont pas la meme cadence, et les confondre est le bug de\nphysique numero un.\n\nEcrire la position d'un Rigidbody le TELEPORTE : le moteur ne voit aucun deplacement entre les\ndeux etats, donc aucune collision sur le trajet, donc le corps traverse les murs.",
            "MovePosition ne bouge rien tout de suite : il DEMANDE un deplacement, applique au prochain\npas de physique, en gardant l'etat d'avant. C'est ce qui permet de tester le trajet et de\ns'arreter contre un mur - le balayage de 21_physics, fait par le moteur.\nOn lit l'entree dans Update et on l'APPLIQUE dans FixedUpdate.\nEt l'interpolation affiche entre l'etat precedent et l'etat courant selon l'alpha de\nl'accumulateur : c'est interp1 de 20_time. Sans elle, a 50 pas et 144 images, le joueur voit\nun escalier et croit que le jeu rame alors qu'il tourne parfaitement."),

        new("load1", "19_unity", "Load1", "Ce qu'on charge, on le libere",
            "Charger deux fois la meme texture ne double pas la memoire : le moteur la partage et compte\nles demandeurs. Ce qui veut dire qu'il faut autant de Release que de Load, et que le dernier\nseul libere vraiment.\n\nLa fuite classique tient en une phrase : une scene rechargee dix fois qui garde dix prises sur\nles memes assets, et quarante megaoctets qui ne repartiront jamais.",
            "Un compteur de references par chemin : Load l'incremente, Release le decremente, et seul le\npassage a zero libere.\nUn Release en trop ne doit RIEN faire : sans ce garde, le compteur passe sous zero et l'asset\nest libere alors qu'un autre niveau l'utilise. Le plantage sort ailleurs, plus tard.\nEt la seule facon de charger qui tienne sur la duree : celui qui charge est celui qui libere,\ndans un IDisposable, appele par un 'using' qui s'execute meme si une exception traverse le\nchargement."),

        new("canvas1", "19_unity", "Canvas1", "Un caractere qui change reconstruit tout le canevas",
            "Un canevas est reconstruit en ENTIER des qu'un seul de ses elements change. Un chrono qui\navance d'une seconde fait recalculer les vingt emplacements de l'inventaire, soixante fois par\nseconde.\n\nC'est la premiere cause de saccades d'interface dans Unity, et la parade ne coute rien.",
            "Separer ce qui change a chaque image de ce qui ne change jamais : un canevas pour le HUD\nvivant, un pour les menus, un pour l'inventaire. Surtout pas un seul pour toute l'interface.\nPlusieurs changements dans la MEME image ne font qu'une reconstruction : le canevas est marque\nsale, et il n'est relu qu'une fois, a la fin.\nEt le test le moins cher du monde, que presque personne n'ecrit : comparer avant d'ecrire.\nReaffecter la meme valeur ne doit pas salir le canevas."),

        new("interp1", "20_time", "Interp1", "Interpoler l'affichage",
            "La physique avance a pas fixe, l'ecran affiche plus souvent. Afficher directement la\nposition physique fait un ESCALIER : plusieurs frames identiques, puis un saut. La parade\nest de garder deux etats et d'afficher entre les deux, avec l'alpha de l'accumulateur.\n\nC'est le 'physics interpolation' de Godot et l'interpolation des Rigidbody de Unity, et\ntu l'ecriras a la main pour tout ce qui n'est pas un corps rigide.",
            "Garde la position d'AVANT le pas et celle d'apres, affiche Lerp(avant, apres, alpha).\nL'alpha, c'est ce qui reste dans l'accumulateur divise par le pas.\nEt un teleport doit remettre LES DEUX etats, sinon l'objet traverse l'ecran en glissant."),

        new("repeat1", "20_time", "Repeat1", "Un timer qui ne derive pas",
            "Trois facons d'ecrire un timer qui se repete, dont deux perdent des declenchements.\nRemettre le compteur a zero jette le reste a chaque fois : sur dix secondes tu as deja\nperdu seize declenchements, et l'ecart ne se rattrape jamais.",
            "Soustrais l'intervalle au lieu de remettre a zero, et boucle : un delta de 0.25 avec un\nintervalle de 0.1 doit declencher deux fois.\nEncore mieux : retiens l'INSTANT du prochain declenchement et ajoute l'intervalle dessus.\nMais pense a plafonner le rattrapage, sinon un gel declenche six cents fois d'un coup."),

        new("scale1", "20_time", "Scale1", "timeScale, pause et temps reel",
            "Mettre le jeu en pause, c'est mettre l'echelle de temps a zero. Mais le menu de pause,\nles fondus de son et le matchmaking doivent continuer : ils tournent sur le temps REEL.\nDeux deltas, donc, et savoir lequel chaque chose consomme.",
            "Delta = temps reel fois echelle ; UnscaledDelta = temps reel tout court.\nUn chrono declare 'ignore la pause' consomme le second, les autres le premier.\nEt le compteur d'images se calcule sur le temps REEL : 1 / Delta pendant une pause donne l'infini."),

        new("stable1", "20_time", "Stable1", "Prouver l'independance au framerate",
            "Ici on ne fait pas confiance, on VERIFIE : la meme simulation tournee a 60 et a 240\nimages par seconde doit finir au meme endroit. Le lissage naif echoue de 20 unites, et\nune integration naive change ta hauteur de saut selon l'ecran du joueur.",
            "Lerp(valeur, cible, 0.1f) par frame depend du framerate ; 1 - Exp(-force * delta) non.\nMoveToward est stable par construction, sa vitesse est deja multipliee par delta.\nEt pour la chute : utilise la MOYENNE de l'ancienne et de la nouvelle vitesse,\nca tombe pile sur la valeur exacte a n'importe quel pas de temps."),

        new("clock1", "20_time", "Clock1", "Le temps absolu et le float qui s'ecroule",
            "Un delta tient tres bien dans un float. Un temps ABSOLU, non : chaque addition arrondit,\net au bout de 524 288 secondes un float ne peut plus representer un ecart d'une frame.\nL'horloge s'arrete net, en silence. Six jours de fonctionnement suffisent.",
            "Mesure la derive : accumule le meme pas 600 000 fois en float, puis en double.\nLa vraie parade, c'est de compter les PAS en entier et de multiplier une seule fois :\nune addition ne peut pas deriver s'il n'y en a pas. C'est aussi ce qui rend un replay rejouable."),

        new("sweep1", "21_physics", "Sweep1", "Le projectile qui traverse le mur",
            "Une balle a 6000 pixels par seconde avance de 100 pixels par frame. Un mur de 10\npixels d'epaisseur ? Elle est devant a la frame N, derriere a la frame N+1, et un test de\nchevauchement ne voit RIEN. C'est le tunneling, et il n'existe aucun jeu qui n'y ait\npas eu droit.\n\nDeux parades : tester le TRAJET plutot que les extremites, ou decouper le deplacement.",
            "Le balayage segment contre rectangle se fait par tranches : pour chaque axe, l'intervalle\nde temps ou on est entre les deux bords. Si les intervalles se croisent, on touche, et le\ndebut du croisement est l'instant du contact.\nUn axe de deplacement nul est le cas special : soit on est deja dans la tranche, soit jamais."),

        new("slide1", "21_physics", "Slide1", "Glisser au lieu de s'arreter",
            "Contre un mur, un personnage qui s'arrete net est un personnage casse. Il doit garder la\npart de son mouvement parallele a la surface. C'est ce que font MoveAndSlide de Godot et\nle CharacterController de Unity, et ca tient en une ligne de produit scalaire.",
            "Glisser : velocity - normale * (velocity . normale). Rebondir : la meme chose fois (1 + elasticite).\nUn sol est une surface dont la normale s'ecarte de la verticale de moins que la pente maximale,\ndonc un produit scalaire compare a un cosinus.\nEt le test qui manque partout : ne corrige QUE si on rentre dans la surface, sinon on colle\nle joueur aux murs qu'il vient de quitter."),

        new("probe1", "21_physics", "Probe1", "Interroger la physique sans allouer",
            "Soixante requetes par frame qui rendent chacune une liste, c'est soixante listes par frame.\nLes deux moteurs exposent donc des versions ou l'APPELANT fournit le tampon et la fonction\nrend un compte. Avec un piege : un tampon trop petit perd des resultats en silence.",
            "Prends un Span<T> en parametre, remplis-le, rends le nombre trouve, et arrete-toi quand\nil est plein. Trie EN PLACE (un tri par insertion suffit) au lieu d'appeler OrderBy.\nEt la regle absolue du filtrage grossier : il peut proposer trop, il ne doit jamais oublier."),

        new("forces1", "21_physics", "Forces1", "Integrer sans faire exploser le jeu",
            "Le meme ressort, les memes chiffres, deux lignes echangees : l'un oscille tranquillement,\nl'autre part a dix millions. Mettre a jour la vitesse AVANT la position n'est pas un detail\nde style, c'est ce qui rend une simulation stable.",
            "Semi-implicite : la vitesse d'abord, puis la position avec la NOUVELLE vitesse.\nUne impulsion s'ajoute directement a la vitesse et ne regarde pas delta ; une force est\ndivisee par la masse et multipliee par delta.\nEt un amortissement se regle PAR SECONDE : Pow(garde, delta), jamais une constante par frame."),

        new("resolve1", "21_physics", "Resolve1", "Sortir d'un mur sans trembler",
            "Repousser un corps hors d'un solide ne suffit pas : si tu ne touches pas a sa vitesse, il\nrepart dedans a la frame suivante, accelere, et finit par traverser. Il faut annuler la\ncomposante de vitesse qui rentrait dans la surface, et seulement celle-la.",
            "Reprends le plus petit deplacement de sortie de collision1, ajoute une marge minuscule,\npuis annule la part de vitesse dirigee vers la surface : velocity -= normale * (velocity . normale).\nEt uniquement si le produit scalaire est negatif, sinon tu manges le saut du joueur."),

        new("write1", "22_json", "Write1", "Ecrire un objet en JSON",
            "JSON, c'est du texte : des accolades, des crochets, des paires nom-valeur. Le seul format\nque lisent a la fois ton jeu, ton editeur de niveaux, ton serveur et un humain dans un ticket\nde bug.\n\nDeux surprises attendent tout le monde au premier essai : les CHAMPS publics ne partent pas,\nseules les PROPRIETES, et une propriete calculee part quand meme.",
            "JsonSerializer.Serialize(valeur, options) suffit pour ecrire.\nUn champ a besoin de [JsonInclude], ou de IncludeFields = true dans les options.\nWriteIndented = true pour lire pendant le developpement, PropertyNamingPolicy pour renommer\ntoutes les proprietes d'un coup.\nEt les options se declarent UNE fois en static readonly, jamais dans la methode."),

        new("read1", "22_json", "Read1", "Relire, et le piege qui coute une soiree",
            "Le piege numero un de System.Text.Json : par defaut la CASSE COMPTE. Un fichier ecrit en\ncamelCase et relu sans option ne leve aucune erreur, ne dit rien, et rend un objet neuf ou\nchaque champ vaut sa valeur par defaut. C'est la difference de comportement numero un avec\nNewtonsoft, et elle se decouvre en general en production.\n\nLe reste de l'exercice, c'est tout ce qu'un fichier venu du disque peut avoir de casse.",
            "PropertyNameCaseInsensitive = true, ou la meme PropertyNamingPolicy des deux cotes.\nNumberHandling.AllowReadingFromString pour un nombre entre guillemets, AllowTrailingCommas et\nReadCommentHandling.Skip pour ce qui a ete edite a la main.\nEt tout ce qui vient du disque passe par une version qui attrape JsonException et rend un\nbool : une sauvegarde corrompue ne doit pas remonter jusqu'a la boucle de jeu."),

        new("attributes1", "22_json", "Attributes1", "Nommer, ignorer, absorber",
            "Le nom C# et le nom dans le fichier sont deux choses differentes, et confondre les deux, c'est\ns'interdire de renommer quoi que ce soit ensuite. Les attributs servent a couper ce lien.\n\nEt un enum part en NOMBRE par defaut : le jour ou tu inseres une valeur au milieu de l'enum,\ntoutes les sauvegardes existantes decalent d'un cran, en silence.",
            "[JsonPropertyName] fige le nom dans le fichier, [JsonPropertyOrder] la position,\n[JsonIgnore] fait disparaitre un cache, et avec Condition = WhenWritingDefault il ne s'ecrit\nque s'il vaut autre chose que zero ou null.\n[JsonConverter(typeof(JsonStringEnumConverter<T>))] ecrit l'enum en toutes lettres.\n[JsonExtensionData] sur un Dictionary<string, JsonElement> ramasse tout ce que cette version\nne connait pas et le REECRIT ensuite : sans lui, lancer une vieille version efface les champs\nde la nouvelle."),

        new("convert1", "22_json", "Convert1", "Une facade, et le prix des options",
            "Si tu viens de Newtonsoft : SerializeObject devient JsonSerializer.Serialize,\nDeserializeObject<T> devient Deserialize<T>, JsonSerializerSettings devient\nJsonSerializerOptions, [JsonProperty] devient [JsonPropertyName]. Ecrire cette facade une\nfois evite d'eparpiller les options dans quarante fichiers.\n\nEt ce n'est pas qu'une question de style : ici on MESURE ce que coute un JsonSerializerOptions\nfabrique dans la methode au lieu d'etre partage.",
            "Un static readonly JsonSerializerOptions par configuration, et une variante se fabrique avec\nle constructeur de copie : new JsonSerializerOptions(autre) { WriteIndented = true }.\nLe premier usage GELE les options : les modifier apres leve InvalidOperationException.\nSerializeObject prend un object et passe value.GetType() : c'est ce que fait Newtonsoft, et\nc'est ce qui evite le piege de generic1."),

        new("generic1", "22_json", "Generic1", "Genericite partielle : le type statique decide",
            "Une arme rangee dans une variable declaree Item se serialise en Item : ses degats\nn'atteignent jamais le fichier. Aucune erreur, aucun avertissement. Le serialiseur suit le\ntype STATIQUE de T, pas l'objet qu'il a en main.\n\nC'est le bug de sauvegarde le plus courant du C#, et il ne se voit qu'au rechargement.",
            "Trois parades, et il faut savoir laquelle porte jusqu'ou : Serialize(v, v.GetType()) et\nSerialize<object>(v) ne corrigent que la RACINE ; les elements d'une List<Item> restent\ntronques.\nUne propriete declaree object ecrit tout mais ne relit rien : elle rend un JsonElement.\nD'ou le generique : SaveSlot<Weapon> fait l'aller-retour complet la ou SaveSlot<Item> tronque.\nEt 'where T : new()' permet de rendre un objet neuf plutot que null sur un fichier vide."),

        new("poly1", "22_json", "Poly1", "Une liste d'objets de types differents",
            "Un sac contient une epee, une potion et une cle. Trois types, une seule liste, et il faut\nque chacun revienne dans son vrai type avec ses methodes virtuelles qui repartent.\n\nLa reponse est un DISCRIMINANT : un petit nom de type ecrit dans le fichier. Ce nom est un\ncontrat de sauvegarde, pas un detail : renommer la classe C# ne doit rien casser, changer le\ndiscriminant casse tout.",
            "[JsonPolymorphic(TypeDiscriminatorPropertyName = \"kind\")] sur la classe de base, puis un\n[JsonDerivedType(typeof(X), \"x\")] par type concret. Sans le premier, le serialiseur choisit\n'$type' tout seul.\nTrois regles a connaitre : un type derive non declare leve NotSupportedException a l'ecriture,\nle discriminant doit etre le PREMIER champ de l'objet a la lecture, et serialiser depuis une\nvariable declaree Sword n'ecrit aucun discriminant du tout."),

        new("generic2", "22_json", "Generic2", "Genericite totale : un convertisseur pour tous les T",
            "Un convertisseur ecrit a la main ne marche que pour UN type ferme. Ecrire StatConverter<int>,\npuis StatConverter<float>, puis StatConverter<string> n'est pas de la genericite, c'est du\ncopier-coller avec des etapes.\n\nJsonConverterFactory est la reponse : elle reconnait la definition OUVERTE Stat<>, et fabrique\nle convertisseur ferme qui convient, a l'execution.",
            "CanConvert compare type.GetGenericTypeDefinition() a typeof(Stat<>) : c'est le moule qu'on\nreconnait, pas le nom.\nCreateConverter fait le chemin inverse : typeof(StatConverter<>).MakeGenericType(argument),\npuis Activator.CreateInstance.\nEt dans Write, la valeur interne passe par le SERIALISEUR, jamais par un ToString : c'est lui\nqui sait qu'une chaine se met entre guillemets et qu'un flottant s'ecrit avec un point."),

        new("custom1", "22_json", "Custom1", "Un convertisseur pour Vector2",
            "Un Vector2 s'ecrit tres bien tout seul : {\"X\":1.5,\"Y\":-2}. Et il se relit a ZERO, sans une\nseule erreur. Un struct readonly n'a que des proprietes en lecture seule : le serialiseur\nfabrique le struct vide et n'a aucun moyen de le remplir.\n\nLa sauvegarde a l'air parfaite et le joueur reapparait a l'origine. Meme histoire avec les\nVector3 de Godot et de Unity, que tu ne peux pas modifier.",
            "JsonConverter<Vector2> : Write ecrit [x, y] avec WriteStartArray et WriteNumberValue,\nRead relit deux nombres et VERIFIE ce qu'il lit. Un convertisseur qui ne verifie pas laisse le\nlecteur desynchronise au milieu du fichier, et l'erreur sort trois cents lignes plus loin.\nDeux fois moins de caracteres au passage.\nPour TES propres structs il y a plus simple : [JsonConstructor] designe le constructeur, et ses\nparametres se branchent sur les proprietes de meme nom."),

        new("partial1", "22_json", "Partial1", "Lire un morceau sans tout construire",
            "Un menu de sauvegardes affiche douze noms et douze dates. Douze fichiers de deux megaoctets\ndeserialises en entier pour ca, c'est deux secondes d'attente et un pic de memoire.\n\nTrois outils, du plus economique au plus confortable : Utf8JsonReader qui avance dans les\noctets sans rien construire, JsonDocument qui indexe et laisse fouiller, JsonNode qui laisse\nMODIFIER puis reecrire.",
            "TryGetProperty pour un champ qui peut manquer : GetProperty leve KeyNotFoundException, et un\nfichier d'avant l'ajout d'un champ n'est pas un fichier corrompu.\nJsonDocument est IDisposable, et un JsonElement ne CONTIENT pas ses donnees : il pointe dedans.\nEn faire sortir un du using le tue, sauf a appeler Clone().\nUtf8JsonReader se parcourt avec Read() en boucle, ValueTextEquals compare un nom de propriete\nsans decoder une seule chaine : zero octet alloue."),

        new("version1", "22_json", "Version1", "Une sauvegarde qui survit a la mise a jour",
            "Le format change a chaque patch : un entier devient un objet, un champ est renomme, un\nnouveau reglage apparait. Les joueurs, eux, ont des fichiers de toutes les versions, et une\nsauvegarde qui ne se charge plus est un joueur perdu.\n\nLa chaine complete : lire le numero de version, appliquer les migrations une par une, puis\ndeserialiser. Et ne jamais jeter ce qu'on ne comprend pas.",
            "Un fichier sans champ version EST une version 1, par definition : c'est la premiere decision\net elle se prend une seule fois.\nUne migration par PAS, chainee avec des 'si la version est inferieure a N' : ajouter une v4\nplus tard doit tenir en une ligne.\nUne migration qui ajoute sans RETIRER traine ses vieux champs pour toujours.\nEt [JsonExtensionData] garde les champs d'une version plus recente pour les reecrire : sans\nlui, lancer une vieille version une seule fois detruit la sauvegarde."),

        new("perf1", "22_json", "Perf1", "Ce que le JSON coute, mesure",
            "Deux cents entites serialisees a chaque image. En string : 13 kilooctets par appel, 800 par\nseconde, un ramassage de generation 0 toutes les deux ou trois secondes. Le meme JSON, au\nmeme octet pres, avec un tampon et un writer reutilises : quelques centaines d'octets.\n\nEt cote AOT : la serialisation par reflexion disparait au trim et sur IL2CPP. La generation de\nsource, ou un ecrivain a la main comme ici, sont les deux seules issues.",
            "Trois etages : Serialize rend une string donc fabrique le JSON en UTF-8 puis le retranscrit en\nUTF-16, SerializeToUtf8Bytes saute l'etape texte mais alloue le tableau, et\nSerialize(writer, valeur, options) sur un Utf8JsonWriter remis a zero avec Reset n'alloue\nplus que des miettes.\nEn lecture, meme plancher : Utf8JsonReader compte sans construire un seul objet, a zero octet.\nUn writer se REUTILISE avec Reset(tampon) : en fabriquer un neuf a chaque appel annule tout."),

        new("replay1", "23_linq", "Replay1", "Une requete n'est pas un resultat",
            "Une requete LINQ ne contient rien : c'est une recette. Elle ne s'execute pas quand on\nl'ecrit, elle s'execute a chaque fois qu'on la PARCOURT. Compter puis prendre le premier,\nc'est deux fois tout le travail.\n\nEt comme elle pointe la source au lieu de la copier, elle voit ce qu'on ajoute apres coup.",
            "Un compteur d'appels dans le predicat rend l'execution differee visible.\nToList et ToArray figent le resultat une fois pour toutes : c'est ce qu'on fait des qu'on\ncompte poser plus d'une question.\nModifier la source pendant un foreach leve InvalidOperationException, parce que la requete\nlit la liste au fur et a mesure."),

        new("closure1", "23_linq", "Closure1", "Ce que la lambda capture",
            "Une lambda capture la VARIABLE, pas sa valeur. Trois lambdas fabriquees dans un 'for'\npartagent le meme compteur, et rendent toutes la valeur finale. Le meme piege dans un\n'foreach' a ete corrige dans le langage ; dans un 'for', il est toujours la.\n\nEt une capture a deux prix : un objet alloue a chaque appel, et un objet maintenu en vie tant\nque la lambda existe.",
            "Une copie de la variable DANS le corps de la boucle donne une variable par tour.\nUne lambda qui ne capture rien est fabriquee une seule fois et mise en cache : zero octet.\nDes qu'elle capture, il faut un objet pour transporter la capture, a chaque appel.\nLe mot-cle 'static' devant une lambda interdit la capture : le compilateur refuse au lieu de\nlaisser passer une allocation par image."),

        new("yield1", "23_linq", "Yield1", "Ecrire ses propres operateurs",
            "LINQ n'est pas magique : ce sont des methodes d'extension sur IEnumerable<T>, et 'yield\nreturn' donne l'execution differee gratuitement. Ecrire les siennes est la facon la plus\nrapide de comprendre les autres.\n\nAvec un piege que presque personne ne connait : une methode qui contient un 'yield' ne\ns'execute pas du tout avant le premier parcours. Sa verification d'arguments non plus.",
            "Une methode d'extension : public static IEnumerable<T> Truc<T>(this IEnumerable<T> source...).\nPour que les arguments soient verifies TOUT DE SUITE, il faut deux methodes : une methode\nd'entree sans yield qui verifie et delegue, et l'iterateur prive qui yield.\nEt un operateur qui rend des paquets doit fabriquer une nouvelle liste par paquet : recycler\nle meme tampon changerait les paquets deja rendus dans le dos de l'appelant."),

        new("group1", "23_linq", "Group1", "Regrouper, joindre, aplatir",
            "Trois operateurs qui remplacent chacun une boucle imbriquee : GroupBy range par cle,\nSelectMany aplatit une sequence de sequences, Join croise deux collections par une table de\nhachage au lieu de comparer tout avec tout.\n\nEt une paire qui ressemble a un doublon sans en etre un : GroupBy est differe, ToLookup est\nimmediat.",
            "ToLookup construit sa table tout de suite, et une cle absente y rend une sequence VIDE\nplutot qu'une exception : c'est ce qui le rend plus pratique qu'un Dictionary pour des index.\nGroupJoin est le 'left join' : il garde les elements de gauche qui n'ont aucun correspondant.\nEt les groupes sortent dans l'ordre de PREMIERE apparition de la cle, jamais dans l'ordre\nalphabetique."),

        new("sort1", "23_linq", "Sort1", "Trier, et surtout ne pas trier",
            "OrderBy est un tri STABLE : deux elements de meme cle gardent leur ordre d'origine. List.Sort\nne l'est pas, et modifie la liste au passage. Savoir lequel des deux on veut evite un bug\nd'affichage qui ne se reproduit qu'une fois sur trois.\n\nEt le reflexe le plus cher du C# : OrderBy().First(), qui trie deux cents ennemis pour n'en\ngarder qu'un.",
            "MinBy et MaxBy rendent l'ELEMENT, en un seul passage et sans copier la source.\nThenBy departage explicitement les ex aequo, au lieu de compter sur la stabilite du tri.\nOrder() et OrderDescending() trient une sequence sur elle-meme, sans selecteur.\nEt comme le reste, un tri LINQ est differe : il retrie a chaque parcours, et ne touche jamais\na la source."),

        new("sets1", "23_linq", "Sets1", "Ensembles, doublons et egalite",
            "Distinct, Except, Intersect et Union sont des operations d'ENSEMBLES : elles passent par\nEquals et GetHashCode, et elles dedoublonnent au passage. D'ou la surprise la plus courante :\nelles marchent sur un record et pas sur une classe ordinaire.\n\nParce qu'une classe ordinaire compare des REFERENCES, et que deux objets identiques restent\ndeux objets.",
            "Un record fabrique Equals et GetHashCode a partir de ses champs. Une class, non.\nDistinctBy, MinBy, MaxBy prennent la cle en parametre : c'est la solution pour les types\nqu'on ne peut pas modifier.\nSequenceEqual compare dans l'ORDRE, SetEquals compare des ensembles.\nEt un HashSet dont Add rend false est la facon la plus courte d'ecrire 'ne fais ceci qu'une\nfois par cible'."),

        new("lazy1", "23_linq", "Lazy1", "Des suites infinies",
            "Un 'while (true)' avec un yield est une suite infinie parfaitement utilisable : tailles de\nvagues, positions de spawn sur un cercle, identifiants uniques. Rien n'est calcule tant que\npersonne ne demande, et rien n'est stocke.\n\nA une condition : il faut TOUJOURS un operateur qui limite avant un operateur qui compte,\ntrie ou materialise. Sinon la boucle de jeu se fige, sans exception et sans message.",
            "Take, TakeWhile, First et Any s'arretent des qu'ils ont ce qu'il leur faut.\nCount, Max, OrderBy et ToList vont jusqu'au bout : sur une source infinie, ils ne reviennent\njamais.\nSkip consomme sans rendre, ce qui n'est pas gratuit.\nEt Enumerable.Range, Repeat et Empty fabriquent des sequences sans allouer de tableau."),

        new("twice1", "23_linq", "Twice1", "Le parametre parcouru deux fois",
            "Une methode qui prend un IEnumerable<T> et l'utilise deux fois - un Count puis un Max -\nparcourt la source deux fois. Si c'est une requete, elle se rejoue. Si c'est un fichier ou\nun curseur, le second parcours rend le VIDE, sans erreur.\n\nC'est le bug qui ne se voit jamais en test, parce qu'en test on passe toujours une List.",
            "Materialiser une fois en entree, sans recopier ce qui est deja une liste :\nsource as List<T> ?? source.ToList().\nTryGetNonEnumeratedCount dit si la taille est connue sans parcourir : vrai pour une List, un\ntableau ou un Dictionary, faux pour un iterateur.\nEt prendre IReadOnlyList<T> plutot que IEnumerable<T> en parametre, c'est demander a\nl'appelant une source rejouable : le probleme disparait a la compilation."),

        new("empty1", "23_linq", "Empty1", "Rien trouve : null, zero ou default ?",
            "FirstOrDefault sur une sequence d'objets rend null, et tout va bien. Sur une sequence de\nSTRUCTS il rend default(T), c'est-a-dire Vector2.Zero : rien ne distingue plus 'pas trouve'\nde 'trouve a l'origine', et l'ennemi fonce vers le coin de la carte.\n\nToute la famille First / Single / Max se decline en 'leve' et 'rend un repli'. Choisir, c'est\ndire si l'absence est un cas de jeu ou un bug.",
            "La surcharge FirstOrDefault(predicat, repli) prend une valeur de repli EXPLICITE, qu'on ne\npeut pas confondre avec un vrai resultat.\nDefaultIfEmpty(x) fait la meme chose en amont d'un Max ou d'un Average.\nSingle affirme qu'il ne peut y en avoir qu'un : le 'OrDefault' porte sur le vide, jamais sur\nle trop-plein.\nEt Sum de rien vaut zero, alors que Max de rien leve : demander le maximum de rien n'a pas de\nreponse."),

        new("cost1", "23_linq", "Cost1", "Ce que LINQ coute par image",
            "On mesure, comme dans 15_perf. Un foreach sur une List n'alloue rien, parce que son\nenumerateur est un struct. Range la MEME liste dans un IEnumerable et le meme foreach alloue :\nl'interface emballe l'enumerateur.\n\nUne signature qui prend IEnumerable<T> au lieu de List<T> suffit donc a payer une allocation\npar appel. Et OrderBy().First() trie deux cents ennemis pour en garder un.",
            "Chaque operateur d'un pipeline LINQ, c'est un objet d'etat, un delegue, et un enumerateur\nemballe.\nLa propriete Count d'une List est un champ : gratuite. La methode Count(predicat) parcourt.\nAny s'arrete au premier trouve ; Where().ToList().Count materialise tout avant de regarder.\nEt la regle n'est pas 'LINQ est mauvais' : au chargement personne ne le verra, dans l'Update\nc'est une saccade toutes les quelques secondes."),

        new("pointers1", "24_unsafe", "Pointers1", "Un pointeur, et pourquoi il faut epingler",
            "Une adresse, et rien d'autre. L'incrementer avance d'un ELEMENT, pas d'un octet, parce que le\ncompilateur connait la taille du type.\n\nAvec une contrainte que le C n'a pas : le ramasse-miettes DEPLACE les objets pour compacter le\ntas. Une adresse notee avant un deplacement ne designerait plus rien. 'fixed' epingle l'objet\nle temps du bloc, et pas une ligne de plus.",
            "unsafe sur la classe ou la methode, puis fixed (int* p = tableau) { ... }.\n*p lit l'element pointe, p[i] lit le i-eme, p++ avance d'un element.\nsizeof(T) donne la taille d'un type non gere : sizeof(Vector2) vaut 8.\nUne string s'epingle exactement comme un tableau, et c'est comme ca qu'on la passe au natif.\nLe bloc fixed doit rester COURT : tant qu'il dure, le ramasse-miettes ne peut plus compacter."),

        new("scratch1", "24_unsafe", "Scratch1", "De la memoire de travail sur la pile",
            "Un tampon de travail dont on n'a besoin que le temps d'une methode n'a rien a faire sur le\ntas. 'stackalloc' le prend sur la pile : rien a allouer, rien a liberer, rien a ramasser.\n\nA deux conditions : que ce soit petit, et qu'on l'affecte a un Span. Un Span verifie ses\nbornes ; un pointeur nu sur le meme stackalloc ecrirait dans la pile de l'appelant.",
            "Span<int> tampon = stackalloc int[32]; affecte a un Span, il est remis a zero et connait sa\ntaille.\nAu-dela d'un kilooctet environ, on passe a ArrayPool<T>.Shared.Rent, et on rend ce qu'on a\nloue. Le motif classique est un ternaire : stackalloc si petit, tableau loue sinon.\nEt un stackalloc dans une boucle ne se libere qu'au RETOUR de la methode : il faut le sortir\nde la boucle, sinon la pile deborde et un debordement de pile ne se rattrape pas."),

        new("reinterpret1", "24_unsafe", "Reinterpret1", "Voir les memes octets autrement",
            "Un tableau de Vector2 EST deja un tableau de floats : deux par point, les uns derriere les\nautres. Le recopier pour le passer a une API qui veut des floats est une perte seche.\n\nMemoryMarshal change la VUE sans toucher a la memoire. C'est la brique de toute serialisation\nbinaire, de tout envoi au GPU et de tout paquet reseau.",
            "MemoryMarshal.Cast<Vector2, float>(span) rend une vue, pas une copie : ecrire dedans ecrit\ndans la source.\nAsBytes fait la meme chose en octets, Read<T> et Write<T> lisent et ecrivent un struct a une\nposition donnee d'un tampon d'octets.\nUn reste qui ne fait pas un element entier est TRONQUE, jamais arrondi vers le haut.\nEt tout ceci exige un type sans reference : le compilateur le verifie avec la contrainte\n'unmanaged'."),

        new("layout1", "24_unsafe", "Layout1", "Taille, alignement, et champs ranges n'importe comment",
            "Un octet, un entier, un octet : douze octets au lieu de six. Le compilateur aligne chaque\nchamp sur sa propre taille et bouche les trous. Ranger ses champs du plus grand au plus petit\nest la seule optimisation gratuite de tout ce cours.\n\nSur dix mille entites, ce sont quarante kilooctets et un tiers de lignes de cache en plus a\nlire a chaque parcours.",
            "Unsafe.SizeOf<T>() donne la taille reelle du type en memoire.\nStructLayout(LayoutKind.Sequential, Pack = 1) supprime tout alignement : reserve aux formats\nde fichier et aux protocoles, jamais a de la donnee chaude.\nLayoutKind.Explicit avec des FieldOffset fabrique une UNION : plusieurs champs sur les memes\noctets, de quoi lire une couleur comme un entier ou comme quatre composantes.\nEt RuntimeHelpers.IsReferenceOrContainsReferences<T>() est le test exact qui dit si un type\npeut etre recopie octet par octet."),

        new("bounds1", "24_unsafe", "Bounds1", "Les verifications de bornes, et ce qu'on perd a les enlever",
            "Chaque acces a un tableau est precede d'une comparaison. On peut la supprimer avec\nUnsafe.Add, et le prix est immediat : une lecture hors bornes ne leve plus, elle rend ce qui\ntraine a cette adresse, et le programme continue avec une valeur inventee.\n\nLa vraie conclusion de l'exercice n'est pas celle qu'on croit.",
            "MemoryMarshal.GetArrayDataReference donne une reference sur le premier element,\nUnsafe.Add(ref r, i) avance de i elements sans rien verifier.\nUn Span verifie ses bornes comme un tableau : il n'echange pas la securite contre la vitesse,\nil evite la COPIE.\nEt le compilateur supprime deja la verification quand la boucle va de 0 a Length : ecrire\ni < valeurs.Length plutot que i <= n suffit a l'obtenir, sans une ligne de code non sur."),

        new("native1", "24_unsafe", "Native1", "De la memoire que le ramasse-miettes ne voit pas",
            "NativeMemory.Alloc reserve une zone hors du tas gere. Elle ne compte dans aucune generation,\nne declenche aucun ramassage, et surtout ne BOUGE jamais : une API native peut en garder\nl'adresse d'une image sur l'autre.\n\nEn echange, personne ne la liberera pour toi. Un Alloc, un Free, exactement une fois.",
            "NativeMemory.AllocZeroed(nombre, taille) rend une zone deja a zero, NativeMemory.Free la rend.\nUn Span<T> peut se poser SUR de la memoire native : new Span<int>(pointeur, longueur), et\ntoute l'API de Span devient disponible.\nLa classe qui la detient implemente IDisposable, met son pointeur a null apres liberation, et\ntolere un Dispose appele deux fois.\nSans garde, lire apres liberation lit une zone rendue au systeme : parfois les anciennes\nvaleurs, parfois celles d'une autre allocation, parfois un plantage du processus entier."),

        new("inplace1", "24_unsafe", "Inplace1", "Modifier un struct dans une liste sans le copier",
            "'liste[0].Vie = 42' ne compile pas, et ce n'est pas une limitation arbitraire : l'indexeur\nrend une COPIE du struct, donc la modifier ne modifierait rien. Le compilateur refuse plutot\nque de te laisser y croire.\n\nLa parade classique - lire, modifier, reecrire - copie chaque particule deux fois par image.\nIl y a mieux.",
            "CollectionsMarshal.AsSpan(liste) ouvre le tableau INTERNE de la List : plus d'indexeur, plus\nde copie a l'aller ni au retour.\n'ref Particule p = ref span[i]' est un ALIAS sur l'element : ecrire dedans ecrit dans la liste.\nUne methode peut rendre un 'ref', ce qui permet de chercher un element puis de le modifier sur\nplace.\nEt le danger : ajouter un element peut REALLOUER le tableau interne. Le Span pris avant pointe\nalors l'ancien tableau. On le reprend apres toute modification de taille, et on ne le garde\njamais dans un champ."),

        new("funcptr1", "24_unsafe", "FuncPtr1", "Des pointeurs de fonction plutot que des delegues",
            "Un delegue est un objet : une cible, une liste d'invocation, une allocation des qu'on en\nfabrique un. Un pointeur de fonction est une adresse, huit octets, et rien d'autre.\n\nUne table d'opcodes de machine virtuelle, un dispatch de systemes d'ECS, un rappel passe a du\ncode natif : quand la fonction est statique et ne capture rien, c'est ce qu'il y a de plus\nrapide en C#.",
            "Le type s'ecrit delegate*<int, int> : les parametres, puis le retour en dernier.\nOn en obtient un avec '&' devant une methode STATIQUE, et on l'appelle comme une methode.\nUn tableau de delegate*<...> se parcourt sans rien allouer.\nLa limite est voulue : pas de methode d'instance, pas de capture. Quand il en faut une, c'est\nun delegue qu'il faut, et il faut le fabriquer UNE fois plutot qu'a chaque image."),

        new("interop1", "24_unsafe", "Interop1", "La frontiere avec le code natif",
            "Un type BLITTABLE - que des nombres, aucune reference - a la meme forme des deux cotes de la\nfrontiere : il traverse tel quel, sans conversion et sans copie. Tout le reste est converti a\nchaque appel, champ par champ.\n\nEt le cas qui coute le plus cher est celui qu'on ne voit pas : le C# stocke ses chaines en\nUTF-16, le natif attend presque toujours de l'UTF-8.",
            "RuntimeHelpers.IsReferenceOrContainsReferences<T>() dit si un type est recopiable octet par\noctet. Un struct qui contient une string ne l'est pas, et on ne peut meme pas l'epingler.\nStructLayout(LayoutKind.Sequential) garantit l'ordre des champs, et Marshal.SizeOf doit alors\ndonner la meme taille que Unsafe.SizeOf.\nGCHandle epingle quand l'adresse doit survivre a l'appel, la ou 'fixed' ne dure qu'un bloc.\nEt une chaine se convertit soi-meme dans un tampon en stackalloc, sans oublier le ZERO final :\nune chaine native s'arrete au premier octet nul."),

        new("race1", "25_threads", "Race1", "Le compteur qui perd des incrementations",
            "Quatre threads, deux cent mille incrementations chacun, et le total est inferieur au\ncompte. Rien n'a plante, rien n'a prevenu : des incrementations ont simplement disparu.\n\nParce que 'compteur++' n'est pas UNE operation, c'en est trois : lire, ajouter un, reecrire.\nEt rien de tout ceci ne se voit sur ta machine ni en pas-a-pas dans le debogueur.",
            "Interlocked.Increment fait les trois d'un bloc, et rend la NOUVELLE valeur.\nAdd ajoute, Exchange pose et rend l'ancienne, CompareExchange n'ecrit que si la valeur est\nbien celle qu'on croyait : c'est la brique de tout ce qui se fait sans verrou.\nEt la version la plus rapide n'utilise rien de tout ca : chaque thread compte dans SA case, et\non additionne a la fin. Pas de synchronisation, parce qu'il n'y a rien a partager."),

        new("lock1", "25_threads", "Lock1", "Verrouiller, et les deux facons de se tromper",
            "Un Dictionary partage n'est pas seulement imprecis en ecriture concurrente : il se CORROMPT,\net peut boucler a l'infini a la lecture suivante. Un verrou regle ca.\n\nDeux pieges alors. Verifier sous un verrou puis agir sous un autre ne protege rien. Et prendre\ndeux verrous dans des ordres opposes fige le jeu pour toujours, sans plantage et sans message.",
            "Le type Lock de .NET 9 remplace le vieux 'lock (new object())' : meme syntaxe, et plus\nrapide.\nLa verification et l'action doivent etre dans le MEME bloc, sinon deux threads passent tous\nles deux le test avant que l'un des deux n'agisse.\nUn verrou non dispute ne coute rien : le cas frequent est une operation atomique.\nMonitor.TryEnter avec un delai sert a DIAGNOSTIQUER un interblocage, jamais a le corriger : la\nparade est de toujours prendre les verrous dans le meme ordre, ou de n'avoir rien a partager."),

        new("parallel1", "25_threads", "Parallel1", "Paralleliser une boucle",
            "Parallel.For decoupe une boucle sur tous les coeurs. Avec la meme piege qu'ailleurs :\n'total += x' depuis plusieurs taches perd des additions.\n\nEt une operation atomique par element coute plus cher que le calcul lui-meme. La bonne forme\naccumule dans un total LOCAL par tache, et n'en publie qu'un par tache.",
            "La surcharge a cinq arguments prend un etat local : une fonction qui l'initialise, le corps\nqui le fait avancer, et une fonction appelee UNE fois par tache a la fin.\nMieux encore : donner une TRANCHE a chaque tache. Deux taches qui n'ecrivent jamais au meme\nendroit n'ont rien a synchroniser.\nMaxDegreeOfParallelism limite le nombre de taches simultanees, indispensable dans un jeu ou\nil faut laisser des coeurs au rendu et a l'audio.\nEt en dessous de quelques milliers d'elements, la boucle simple gagne : repartir le travail et\nreveiller des threads n'est pas gratuit."),

        new("mainthread1", "25_threads", "MainThread1", "Revenir sur le thread principal",
            "L'API d'un moteur - creer un noeud, changer une position, jouer un son - n'est utilisable QUE\ndepuis le thread principal. Godot et Unity ne le verifient pas toujours : parfois ca marche,\nparfois ca corrompt la scene, parfois ca fait tomber le processus.\n\nD'ou la seule forme qui tienne : le thread de calcul DEPOSE du travail, le thread principal le\nvide a son rythme.",
            "Une ConcurrentQueue<Action> : le worker fait Enqueue, la boucle de jeu fait TryDequeue.\nAvec un BUDGET par image, sinon un gros lot de resultats fait un pic de quarante millisecondes\nau moment ou tout arrive en meme temps.\nUne ConcurrentQueue garde l'ordre d'insertion.\nEt ConcurrentDictionary.AddOrUpdate est atomique quand plusieurs threads ecrivent VRAIMENT au\nmeme endroit. Quand ce n'est pas le cas, une tranche par thread reste plus rapide."),

        new("sharing1", "25_threads", "Sharing1", "Le faux partage",
            "Quatre threads, quatre compteurs differents, aucune donnee partagee. Et pourtant trois a dix\nfois plus lent que prevu.\n\nParce que les quatre compteurs tiennent dans la MEME ligne de cache, la plus petite unite que\nle processeur echange avec la memoire. Chaque ecriture invalide la ligne chez les trois autres\ncoeurs. Le resultat reste juste : ce n'est pas un bug de justesse, et rien ne le signale.",
            "StructLayout(LayoutKind.Explicit, Size = 64) donne a chaque compteur sa propre ligne.\nUnsafe.ByteOffset entre deux elements d'un tableau montre l'ecart reel.\nMais la vraie parade n'est pas le rembourrage : c'est d'accumuler dans une VARIABLE LOCALE,\nqui vit dans un registre, et de n'ecrire qu'une fois a la fin.\nLe rembourrage sert quand la case doit rester lisible pendant le calcul, par exemple une barre\nde progression. Il coute huit fois la memoire : on ne le fait jamais par principe."),

        new("cancel1", "25_threads", "Cancel1", "Annuler proprement",
            "Le joueur quitte la scene pendant un chargement. Il n'existe AUCUNE facon sure d'interrompre\nun thread de force : on l'arreterait au milieu d'une ecriture. L'annulation est donc\ncooperative, toujours : le thread doit REGARDER son jeton.\n\nUne seule mecanique couvre 'le joueur a annule', 'la scene est detruite' et 'le serveur ne\nrepond plus'.",
            "ThrowIfCancellationRequested quand l'appelant doit SAVOIR que le travail est incomplet ;\nun simple test de IsCancellationRequested quand un resultat partiel a un sens.\nUn CancellationTokenSource construit avec un delai devient un delai d'expiration.\nCreateLinkedTokenSource s'annule des que l'une de ses sources s'annule.\nRegister pose un rappel de nettoyage declenche AU MOMENT de l'annulation.\nEt un CancellationTokenSource s'appelle Dispose : il tient un minuteur et des rappels, en\nfabriquer un par requete sans le liberer est une fuite lente."),

        new("async3", "25_threads", "Async3", "Task, ValueTask, et async void",
            "Une tache porte un resultat OU une exception, et l'exception ressort a l'endroit ou on\nl'attend. Sauf pour un 'async void', qui n'a pas de tache du tout : personne ne peut\nl'attendre, personne ne peut attraper ce qui en sort, et une exception qui s'en echappe tue\nle processus.\n\nEt l'erreur la plus courante n'est pas celle-la : c'est un 'await' dans un foreach, qui\nserialise trois travaux de cent millisecondes en trois cents.",
            "Task.WhenAll lance tout et attend l'ensemble.\nValueTask n'alloue rien quand la reponse est deja la : c'est ce qu'on veut pour un cache ou\nun 'charge si pas deja charge' appele mille fois.\nTask.Run envoie du calcul PUR sur le pool de threads : bon pour un pathfinding, pas pour\nattendre un fichier.\nEt la regle qui vaut pour les deux moteurs : sans contexte de synchronisation, la suite d'un\nawait tourne sur un thread du pool. Toucher a la scene depuis la fait les degats de\nmainthread1."),

        new("pipeline1", "25_threads", "Pipeline1", "Un producteur, un consommateur, une file bornee",
            "Un thread genere des chunks, le thread principal les integre. Si la file n'est pas BORNEE,\nle producteur prend trente secondes d'avance et remplit la memoire pendant que le thread\nprincipal peine a suivre.\n\nUne file bornee bloque le producteur quand elle est pleine, ce qui le cale naturellement sur\nla vitesse du consommateur. C'est tout le sujet.",
            "Channel.CreateBounded avec une capacite, et FullMode : Wait fait attendre le producteur,\nDropOldest jette le plus ancien - ce qu'on veut pour des positions reseau, ou la donnee\nperimee ne vaut rien.\nreader.ReadAllAsync() se parcourt avec 'await foreach' et s'arrete tout seul.\nMais SEULEMENT si le producteur appelle Complete(). L'oublier fige le jeu a la fin du\nchargement, et c'est le bug le plus difficile a diagnostiquer de toute la section.\nSingleReader et SingleWriter, quand c'est vrai, laissent le canal choisir une version plus\nrapide."),

        new("pack1", "26_binary", "Pack1", "Plusieurs valeurs dans un seul entier",
            "Points de vie, equipe, etat, visibilite : quatre champs qui prennent au moins sept octets\nseparement, et dix-huit BITS ensemble. Un decalage et un masque, rien de plus.\n\nAvec un piege qui ne pardonne pas : une valeur trop grande pour son champ ne leve pas\nd'exception. Le masque garde les bits du bas et le reste disparait en silence.",
            "Ecrire : (valeur & masque) << position, le tout assemble avec des ou binaires.\nLire : (paquet >> position) & masque.\nRemplacer un champ : effacer ses bits avec & ~masque, puis poser les nouveaux.\nLa valeur maximale d'un champ de n bits est 2 puissance n moins un, et c'est a TOI de la\nverifier.\nEt BitOperations donne PopCount, TrailingZeroCount, RoundUpToPowerOf2 : de quoi parcourir un\nmasque de composants d'ECS en une instruction par entite."),

        new("quantize1", "26_binary", "Quantize1", "Un float en deux octets",
            "Une position n'a pas besoin de trente-deux bits. Sur une carte de deux mille unites, seize\nbits donnent une precision d'un centieme d'unite - plus fine qu'un pixel - pour la moitie de\nla place. Un angle tient sur UN octet.\n\nEt les deux se traitent differemment : une position se PLAQUE aux bornes, un angle s'ENROULE.",
            "Quantifier : normaliser dans [0, 1] sur la plage connue, multiplier par la valeur maximale\ndu type, arrondir.\nUne position hors plage doit etre plaquee AVANT la conversion : sans ca elle deborde, et le\njoueur se retrouve a l'autre bout de la carte.\nUn angle est cyclique : on retire sa partie entiere de tours au lieu de le plaquer.\nUne composante de vecteur normalise va sur un sbyte avec 127, jamais 128, pour que la plage\nsoit symetrique et que zero reste zero.\nL'erreur maximale se calcule : plage divisee par le nombre de paliers, divisee par deux."),

        new("endian1", "26_binary", "Endian1", "L'ordre des octets",
            "x86 et ARM ecrivent l'octet de poids faible en premier. Tous les protocoles reseau font\nl'inverse. Melanger les deux rend un nombre parfaitement valide et completement faux : aucune\nexception, aucun avertissement.\n\nEt comme ta machine de developpement est petit-boutiste, un code qui suppose l'ordre machine\nmarche partout jusqu'au premier appareil qui ne l'est pas.",
            "BinaryPrimitives.WriteInt32LittleEndian et sa version BigEndian ecrivent l'ordre\nEXPLICITEMENT : c'est la seule facon de ne jamais dependre de la machine.\nIl en existe une par taille et par type, flottants compris.\nReverseEndianness retourne une valeur deja lue.\nEt la regle : l'ordre fait partie du FORMAT, on le decide une fois. Un fichier de sauvegarde\npeut rester en petit-boutiste, un paquet reseau se fait en gros-boutiste, et un lecteur ne\ndevine jamais."),

        new("varint1", "26_binary", "Varint1", "Les petits nombres doivent etre petits",
            "Un identifiant d'objet vaut presque toujours moins de cent. L'ecrire sur quatre octets, c'est\ntrois octets de zeros. Un varint utilise sept bits par octet et garde le huitieme pour dire\n's'il y a une suite'.\n\nAvec deux pieges : les nombres negatifs, qui deviennent enormes en complement a deux, et un\nlecteur qui ne se borne pas et qu'un fichier corrompu fait boucler.",
            "Ecrire : tant que la valeur depasse 127, sortir les sept bits du bas avec le bit de\ncontinuation, puis decaler de sept.\nLire : accumuler les sept bits utiles, decaler de sept a chaque octet, s'arreter quand le bit\nde continuation est absent - et REFUSER au-dela de cinq octets.\nLe zigzag entrelace les signes : 0, -1, 1, -2 deviennent 0, 1, 2, 3. C'est (v << 1) ^ (v >> 31),\net le retour est (v >> 1) ^ -(v & 1).\nUn varint peut etre plus GROS qu'un entier fixe : il gagne parce que dans un jeu, presque tous\nles nombres sont petits."),

        new("writer1", "26_binary", "Writer1", "Un ecrivain et un lecteur binaires",
            "Un format binaire n'a aucun nom de champ : c'est le CODE qui est le format, et on relit dans\nl'ordre exact ou l'on a ecrit. Vingt octets la ou le JSON en prend plus du double.\n\nDeux controles sont obligatoires, et ce sont eux l'exercice : un tampon trop petit a\nl'ecriture, et un fichier tronque a la lecture.",
            "Un 'ref struct' pour l'ecrivain et le lecteur : il ne peut vivre que sur la pile, donc\nl'ecriture entiere n'alloue rien, et le compilateur l'impose.\nUne methode privee qui reserve n octets, verifie qu'il en reste, et avance le curseur : tout\nle reste passe par elle.\nUne chaine s'ecrit PREFIXEE de sa longueur en octets, jamais terminee par un zero : plus court\na lire, ca supporte les octets nuls, et ca permet de sauter le champ sans le decoder.\nSans le controle a l'ecriture, on ecrit dans la pile de l'appelant et le plantage sort\nailleurs, plus tard, sans rapport."),

        new("snapshot1", "26_binary", "Snapshot1", "N'envoyer que ce qui a change",
            "Deux cents entites, soixante fois par seconde. Envoyer l'etat complet de chacune, c'est\nsaturer la connexion du joueur. Presque rien ne change d'une image a l'autre.\n\nUn masque de bits dit quels champs suivent ; le receveur applique la difference sur sa\nBASELINE. Et c'est la que le protocole devient piegeux : applique sur la mauvaise baseline,\nle resultat est faux et personne ne s'en apercoit.",
            "Un bit par champ, compare a la baseline, puis seuls les champs marques partent.\nOn envoie l'octet de masque MEME quand il vaut zero : 'toujours la, n'a pas bouge' n'est pas\nla meme chose que le silence.\nDeux regles obligatoires : le receveur ACQUITTE la baseline qu'il possede, et l'emetteur\nrenvoie un instantane COMPLET de temps en temps, pour que toute desynchronisation finisse par\nse corriger toute seule."),

        new("checksum1", "26_binary", "Checksum1", "Detecter la corruption, et ne jamais perdre l'ancienne sauvegarde",
            "Un octet retourne sur le disque, une coupure de courant pendant l'ecriture : le fichier est\nillisible, et le joueur a perdu quarante heures.\n\nDeux mecaniques, et elles vont ensemble. Une empreinte a la fin du fichier detecte la\ncorruption. Et l'ecriture ATOMIQUE - temporaire, relecture, renommage - garantit que le\nfichier reel n'est jamais a moitie ecrit.",
            "FNV-1a tient en trois lignes : partir de 2166136261, puis pour chaque octet un ou exclusif\nsuivi d'une multiplication par 16777619.\nUne verification qui echoue ne rend RIEN : une sauvegarde corrompue n'est pas une sauvegarde\npartielle, c'est une sauvegarde absente.\nEt la sequence atomique : ecrire dans path + '.tmp', RELIRE le temporaire pour verifier son\nempreinte, puis renommer. Le renommage est l'operation que le systeme de fichiers garantit\nindivisible.\nEcrire directement par-dessus, c'est perdre l'ancienne partie en meme temps que la nouvelle."),

        new("compare1", "26_binary", "Compare1", "Binaire contre JSON, mesure a l'appui",
            "Le meme monde de deux cents entites, ecrit dans les deux formats. Le binaire ne paye ni les\nnoms de champs repetes deux cents fois, ni la reconversion de chaque nombre en texte, et il\nrend les flottants au BIT pres.\n\nLa conclusion n'est pas 'le binaire gagne'. C'est de savoir lequel des deux repond a quelle\nquestion.",
            "Un format a taille FIXE permet de sauter directement a la centieme entite : offset = 100 fois\nla taille d'une entite. Aucun format texte ne sait faire ca.\nUn reste incomplet doit etre ignore : c'est au format de dire combien d'entites il contient,\njamais a la taille du fichier.\nEt la vraie reponse : JSON pour ce qu'un HUMAIN edite - reglages, tables d'objets, dialogues,\nparce que ca se lit dans un editeur, se compare dans un diff git et se corrige a la main.\nBinaire pour ce qu'une MACHINE ecrit en masse : sauvegardes, replays, paquets reseau, terrain."),

        new("slice1", "27_text", "Slice1", "Decouper du texte sans rien allouer",
            "Split fabrique un tableau, plus une chaine par champ, plus une chaine par Trim. Sur un\nfichier de donnees de mille lignes, ce sont des milliers d'objets pour un resultat de quelques\noctets.\n\nUn ReadOnlySpan<char> est une FENETRE sur la chaine d'origine : le decouper ne copie rien du\ntout.",
            "IndexOf pour trouver le separateur, Slice pour avancer la fenetre, et on recommence.\nint.TryParse et float.TryParse acceptent directement un span : plus besoin de Substring.\nTrim sur un span deplace les bornes de la fenetre, il ne fabrique pas de chaine.\nDeux comparaisons se font avec SequenceEqual : '==' sur deux spans comparerait les fenetres,\npas leur contenu.\nEt un StartsWith tout nu attrape tous les mots qui commencent pareil : 'spawner' passe le test\nde 'spawn'."),

        new("culture1", "27_text", "Culture1", "La culture qui casse une sauvegarde",
            "Sur une machine francaise, un float s'ecrit avec une VIRGULE. Le fichier devient illisible\npour tous les autres joueurs, et pour le meme joueur qui change la langue de son systeme.\nRelu, il rend zero : sensibilite a zero, position a l'origine, volume muet.\n\nLa regle tient en une phrase, et elle vaut aussi pour les comparaisons de chaines.",
            "Culture INVARIANTE pour tout ce qu'une MACHINE relit - sauvegardes, reglages, reseau - et\nculture du joueur pour ce qu'un HUMAIN lit.\nPour comparer des identifiants, on compare des octets : StringComparison.Ordinal et\nOrdinalIgnoreCase, qui sont aussi les plus rapides.\nToUpperInvariant, jamais ToUpper : en turc, le i devient un I a point suspendu, ce qui casse\ntoute cle de dictionnaire contenant la lettre i.\nEt un Dictionary de cles techniques se construit avec StringComparer.Ordinal, explicitement."),

        new("create1", "27_text", "Create1", "Fabriquer une chaine sans tampon intermediaire",
            "Composer un texte de HUD, c'est en general un StringBuilder, un ToString, et deux objets\njetes par image. string.Create ecrit directement dans le tampon de la chaine finale, et\nTryFormat n'en fabrique aucune du tout.\n\nSoixante fois par seconde, la difference est un ramassage de generation 0 toutes les quelques\nsecondes, ou aucun.",
            "string.Create(longueur, etat, (span, etat) => ...) : le rappel remplit le tampon de la\nchaine, et il n'y a pas d'etape intermediaire.\nLa surcharge qui prend une chaine interpolee et un Span<char> de travail evite meme le\nStringBuilder.\nTryFormat existe sur tous les types numeriques et sur les dates : il ecrit dans un tampon\nFOURNI, rend un bool et le nombre de caracteres poses.\nUn tampon trop petit rend false au lieu de lever : c'est la convention de tout le framework,\net l'appelant DOIT la tester."),

        new("handler1", "27_text", "Handler1", "Le journal qui ne coute rien quand il est eteint",
            "'if (verbose) Log($\"...\")' n'est pas la question : le probleme est\n'Log($\"ennemis : {Compte()}\")', ou le parametre est evalue AVANT l'appel. Le test a\nl'interieur arrive toujours trop tard : le calcul a eu lieu et la chaine a ete construite.\n\nUn handler de chaine interpolee resout ca sans changer un seul caractere au point d'appel.",
            "Un ref struct marque [InterpolatedStringHandler], avec un constructeur\n(int literalLength, int formattedCount, bool actif, out bool shouldAppend).\nQuand shouldAppend sort a false, le compilateur SAUTE tous les AppendFormatted, donc les\narguments ne sont jamais evalues.\nCote methode : Log(bool actif, [InterpolatedStringHandlerArgument(nameof(actif))] ref Handler h).\nEt DefaultInterpolatedStringHandler fait tout le travail a l'interieur quand c'est allume."),

        new("identity1", "27_text", "Identity1", "Comparer des noms coute cher",
            "L'operateur == de deux chaines compare le CONTENU, caractere par caractere. Faire ca soixante\nfois par seconde sur des noms d'objets, c'est parcourir des milliers de caracteres pour\nrepondre a une question qui tient dans un entier.\n\nLa parade d'un jeu : traduire chaque nom en identifiant, une seule fois, au chargement.",
            "Un registre : un Dictionary<string, int> ordinal pour le sens nom vers identifiant, une List\npour le retour. Le nom reste consultable pour l'affichage, on cesse juste de s'en servir dans\nles boucles.\nUn readonly record struct autour de l'entier donne l'egalite et le hachage gratuitement, et un\ntype distinct qu'on ne peut pas confondre avec un autre entier.\nDeux valeurs egales DOIVENT rendre le meme code de hachage : violer ce contrat casse\nsilencieusement tout Dictionary et tout HashSet.\nEt le hachage des chaines est randomise a chaque demarrage : ne jamais l'ecrire dans un\nfichier ni l'envoyer sur le reseau."),

        new("runes1", "27_text", "Runes1", "Un char n'est pas un caractere",
            "Un emoji de manette a une longueur de deux. Un char est une unite UTF-16 de seize bits, et\ntout ce qui depasse s'ecrit sur DEUX unites, appelees demi-caracteres.\n\nConsequence directe : tronquer un pseudo a l'aveugle coupe l'emoji en deux et rend une chaine\nqui contient un demi-caractere. C'est le carre blanc dans les pseudos, et parfois un plantage\ndu moteur de rendu.",
            "text.EnumerateRunes() parcourt les vrais points de code.\nRune.Utf16SequenceLength dit combien d'unites chaque rune occupe : c'est ce qu'il faut\naccumuler pour tronquer sans casser.\nRune.TryCreate recombine deux demi-caracteres, et REFUSE un demi-caractere seul.\nTrois comptes coexistent pour un meme texte : la longueur en char, le nombre de runes, et la\ntaille en octets UTF-8 - et il faut savoir lequel une API attend.\nDerniere nuance : meme un compte de runes juste ne suffit pas, parce qu'une lettre accentuee\npeut s'ecrire en deux runes. Pour ce que l'oeil voit, il faut compter les graphemes."),

        new("parse1", "27_text", "Parse1", "Un type de donnees qui sait se lire lui-meme",
            "Les tables d'un jeu - regles d'apparition, tables de butin, dialogues - se chargent au\ndemarrage, et c'est la que se decide la moitie du temps de chargement.\n\nISpanParsable met la lecture DANS le type, la fait travailler sur des spans, et donne la meme\npaire Parse / TryParse que tout le framework.",
            "L'interface demande quatre methodes : Parse et TryParse, chacune en version span et en\nversion string, qui delegue a la premiere.\nParse leve une FormatException, TryParse rend un bool : l'un pour ce qui est un bug, l'autre\npour ce qui vient d'un fichier.\nTryParse refuse ce qui n'est PAS entierement un nombre : il n'y a pas de lecture partielle.\nEt NumberStyles decide ce qui est tolere : Integer accepte les espaces autour, None non."),

        new("format1", "27_text", "Format1", "Formats, alignement, et gabarits analyses une fois",
            "Les deux-points introduisent un format, la virgule un alignement, et les deux se combinent :\n{valeur,8:0.00}. C'est ce qui fait un tableau de scores lisible.\n\nEt pour un gabarit traduit, charge une fois et utilise mille fois, CompositeFormat l'analyse\nUNE seule fois au lieu de le relire a chaque appel.",
            "Formats personnalises : 0 est un chiffre obligatoire, # un chiffre facultatif.\nFormats standard en une lettre : D complete de zeros, P fait un pourcentage, X de\nl'hexadecimal, N des groupes de milliers.\nL'alignement est un MINIMUM : une valeur plus longue n'est jamais tronquee.\nCompositeFormat.Parse echoue a la construction sur un gabarit invalide, pas au milieu d'un\ncombat trois heures plus tard.\nEt une chaine interpolee ordinaire bat deja string.Format : le compilateur la traduit en appels\ndirects, sans tableau d'arguments ni emballage."),

        new("scan1", "28_reflect", "Scan1", "Trouver les types tout seul",
            "Un jeu moddable, un registre de systemes, une liste d'ecrans : scanner l'assemblage permet\nqu'un type ajoute dans un fichier existe sans qu'on touche a une liste centrale.\n\nAvec trois filtres que tout le monde oublie, et un piege d'ordre : GetTypes ne garantit AUCUN\nordre, et un jeu qui en depend cesse d'etre reproductible d'une compilation a l'autre.",
            "Trois filtres obligatoires : ecarter les interfaces, ecarter les classes abstraites, et\ngarder seulement celles qui ont un constructeur sans argument.\nSans le troisieme, ca marche jusqu'au jour ou un collegue ajoute un parametre a son\nconstructeur, et le jeu ne demarre plus avec un MissingMethodException illisible.\nIsAssignableFrom se lit a l'envers de IsAssignableTo : les confondre fait passer le test et\nsortir une liste vide, sans que rien ne le signale.\nEt on TRIE le resultat, puis on le garde : un scan est un prix de demarrage, jamais un prix\nd'execution."),

        new("attrs1", "28_reflect", "Attrs1", "Des donnees a cote du type",
            "Un attribut range de l'information a cote d'une classe : un identifiant de module, un ordre\nde chargement, un drapeau experimental. Le demarrage les lit et construit son plan.\n\nSon meilleur usage n'est pas de configurer, c'est de VERIFIER : qu'aucun identifiant n'est en\ndouble, qu'aucun module obligatoire ne manque, au demarrage plutot qu'en jeu.",
            "Un attribut est une classe qui herite d'Attribute : arguments du constructeur pour ce qui ne\npeut pas manquer, proprietes 'init' pour le facultatif.\nAttributeUsage limite ou il peut se poser, et le compilateur refuse alors le reste : une\nverification gratuite.\nInherited = false evite qu'une classe fille herite silencieusement de l'identifiant de sa mere.\nGetCustomAttribute rend null pour une absence, il ne leve jamais.\nEt IsDefined repond juste oui ou non sans construire l'attribut : la version economique quand\non filtre des milliers de types."),

        new("activator1", "28_reflect", "Activator1", "Fabriquer un objet : trois facons, trois prix",
            "Activator.CreateInstance cherche le constructeur, verifie les droits et emballe le resultat,\na CHAQUE appel. Une fabrique compilee fait le travail une fois. Une table de lambdas ne le\nfait jamais.\n\nEt l'avantage principal de la table n'est meme pas la vitesse : c'est que le compilateur VOIT\nchaque 'new' qu'elle contient.",
            "Un Dictionary<string, Func<T>> rempli de lambdas 'static () => new Machin()'.\nLe trim ne les supprime pas, IL2CPP genere leur code, et un type oublie se voit a la LECTURE\nde la table, pas trois heures plus tard sur une console.\nEntre les deux : Expression.Lambda<Func<T>>(Expression.New(constructeur)).Compile() traduit\nl'arbre en code machine une fois, et l'appel suivant est un appel de delegue ordinaire. Utile\nquand la liste des types n'est vraiment connue qu'a l'execution.\nEt ce que la reflexion ne verifie qu'a l'execution, une table le rend impossible a ecrire."),

        new("members1", "28_reflect", "Members1", "Lire et ecrire des proprietes par leur nom",
            "Un panneau de reglages, un editeur de niveaux, une console de triche : tous ont besoin de\nlister les champs modifiables d'un objet et d'y ecrire par nom.\n\nAvec deux couts a connaitre : GetProperties reconstruit son tableau a chaque appel, et\nGetValue EMBALLE la valeur - un float devient un objet sur le tas, a chaque lecture.",
            "Chercher les membres UNE fois, dans un static readonly, et ne garder que les PropertyInfo :\ntout ce qui suit n'est plus qu'un appel.\nFiltrer sur CanWrite, sinon une propriete calculee arrive jusqu'a SetValue et leve.\nVerifier le TYPE avant d'ecrire avec PropertyType.IsInstanceOfType : sans ca, l'erreur sort\ndans une pile d'appels de reflexion illisible.\nBindingFlags decide de ce qu'on voit. Le prive est atteignable quand on le demande\nexplicitement : pratique pour un editeur, dangereux ailleurs, parce que plus rien ne garantit\nqu'un champ prive existera encore au prochain patch."),

        new("generic3", "28_reflect", "Generic3", "Fabriquer un type generique a l'execution",
            "Un ECS doit ranger les composants par type, et il ne connait les types qu'au demarrage.\nMakeGenericType ferme ComponentStore<> sur un type decouvert a l'execution, et ce qui en sort\nest un VRAI ComponentStore<Vector2> : la liste a l'interieur est une List<Vector2>, sans\nemballage.\n\nC'est l'inverse exact de generic2 : la, on reconnaissait un type ouvert ; ici, on en fabrique\nun.",
            "typeof(Store<>).MakeGenericType(type), puis Activator.CreateInstance, et on range le\nresultat derriere une interface NON generique pour pouvoir le stocker.\nLe tout mis en cache dans un Dictionary<Type, ...> : MakeGenericType et Activator sont chers,\non ne les appelle qu'une fois par type.\nMakeGenericMethod fait la meme chose pour une methode.\nEt l'avertissement qui compte : sur IL2CPP, une combinaison generique que le compilateur n'a\njamais VUE n'existe pas. Fabriquee par reflexion, elle echoue sur console si rien dans le code\nne l'instancie explicitement."),

        new("trim1", "28_reflect", "Trim1", "Ce que le compilateur ne voit pas n'existe pas",
            "Type.GetType(\"Csharplings.BurnEffect\") marche parfaitement dans l'editeur. Rien ne relie\ncette chaine a la classe : le trim supprime le type, IL2CPP ne genere pas son code, et l'echec\nsort a l'execution, sur la machine du joueur.\n\nC'est la meme lecon que 19_unity/aot1, vue depuis la reflexion. Et la conclusion de la\nsection : la reflexion au DEMARRAGE se defend, la reflexion en JEU presque jamais.",
            "Une table de fabriques contient des 'new' que le compilateur voit : rien n'est supprime, tout\nest genere, et un type oublie se voit a la lecture.\n[RequiresUnreferencedCode] marque le code incompatible avec le trim : l'analyseur previent\nalors a chaque appel, au lieu de laisser la surprise pour la version console. Le message doit\nexpliquer quoi faire, parce que celui qui le lira ne sera pas celui qui l'a ecrit.\nUn typeof est une reference que le trim comprend, une chaine ne l'est pas.\nEt un scan par reflexion au demarrage reste possible : il faut juste que les types qu'il\ntrouve soient references quelque part, ou preserves explicitement."),
    ];

    public static Exercise Find(string id) =>
        All.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
}
