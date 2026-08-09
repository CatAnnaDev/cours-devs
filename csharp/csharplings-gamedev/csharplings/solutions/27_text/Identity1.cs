namespace Csharplings;

public readonly record struct ItemId(int Value)
{
    public static ItemId From(string name) => new(Registry.IdOf(name));

    public override string ToString() => Registry.NameOf(Value);
}

public static class Registry
{
    private static readonly Dictionary<string, int> Ids = new(StringComparer.Ordinal);
    private static readonly List<string> Names = new();

    public static int Count => Names.Count;

    public static int IdOf(string name)
    {
        if (Ids.TryGetValue(name, out int existing))
            return existing;

        Ids[name] = Names.Count;
        Names.Add(name);

        return Names.Count - 1;
    }

    public static string NameOf(int id) => Names[id];
}

public static class Identity1
{
    public const bool NotDone = false;

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static void Run()
    {
        string built = new string(new[] { 'e', 'p', 'e', 'e' });

        Check.Equal(built, "epee", "l'operateur == des chaines compare le CONTENU, caractere par caractere");

        Check.False(ReferenceEquals(built, "epee"),
            "alors que ce sont deux objets differents : celui-la a ete construit a l'execution, l'autre est un litteral");

        Check.True(ReferenceEquals("epee", "epee"),
            "deux litteraux identiques, eux, sont le MEME objet : le compilateur les met en commun dans la table d'internement");

        Check.True(ReferenceEquals(string.Intern(built), "epee"),
            "et string.Intern y range une chaine construite. A manier avec precaution : rien n'en sort jamais, cette table vit aussi longtemps que le processus");

        Check.True("epee".Length == 4, "comparer deux chaines coute au pire un passage sur leurs caracteres");

        Check.Equal(ItemId.From("epee"), ItemId.From("epee"),
            "la parade d'un jeu : traduire chaque nom en ENTIER, une fois, au chargement");

        Check.Equal(ItemId.From("epee").Value, 0, "le premier nom vu prend l'identifiant zero");
        Check.Equal(ItemId.From("potion").Value, 1, "le suivant, un");
        Check.Equal(ItemId.From("epee").Value, 0, "et un nom deja connu retrouve le sien");

        Check.Equal(Registry.Count, 2, "deux noms distincts dans le registre");

        Check.Equal(ItemId.From("epee").ToString(), "epee",
            "le nom reste consultable pour l'affichage et le debogage : on ne le PERD pas, on cesse juste de s'en servir dans les boucles");

        var byId = new Dictionary<ItemId, int> { [ItemId.From("epee")] = 3 };

        Check.True(byId.ContainsKey(new ItemId(0)),
            "un identifiant entier se compare en une instruction et se hache en une instruction, la ou une chaine parcourt tous ses caracteres pour les deux");

        ItemId sword = ItemId.From("epee");
        ItemId potion = ItemId.From("potion");

        Check.Equal(Measure(() => { _ = sword == potion; }), 0L, "comparer deux identifiants n'alloue rien");
        Check.Equal(Measure(() => { _ = sword.GetHashCode(); }), 0L, "les hacher non plus");

        Check.False(sword.Equals(potion), "et deux identifiants differents restent differents");

        Check.Equal(sword.GetHashCode(), new ItemId(0).GetHashCode(),
            "deux valeurs egales donnent le meme code de hachage : c'est le contrat, et le violer casse silencieusement tout Dictionary et tout HashSet");

        Check.Equal("epee".GetHashCode(), "epee".GetHashCode(),
            "les chaines respectent le meme contrat, dans un meme processus : leur hachage est RANDOMISE au demarrage, donc il ne faut jamais l'ecrire dans un fichier ni l'envoyer sur le reseau");
    }
}
