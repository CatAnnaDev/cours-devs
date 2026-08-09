namespace Csharplings;

public readonly struct CellKey
{
    public CellKey(int column, int row)
    {
        Column = column;
        Row = row;
    }

    public int Column { get; }
    public int Row { get; }
}

public static class Boxing1
{
    public const bool NotDone = true;

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static int SumList(IEnumerable<int> values)
    {
        int total = 0;

        foreach (int value in values)
            total += value;

        return total;
    }

    public static int SumEnumerable(IEnumerable<int> values)
    {
        int total = 0;

        foreach (int value in values)
            total += value;

        return total;
    }

    public static void Run()
    {
        var tiles = new Dictionary<CellKey, string>
        {
            [new CellKey(3, 4)] = "herbe",
            [new CellKey(0, 0)] = "eau",
        };

        Check.Equal(tiles[new CellKey(3, 4)], "herbe", "la table retrouve bien la case");
        Check.False(tiles.ContainsKey(new CellKey(9, 9)), "et ne trouve rien pour une case absente");

        Check.Equal(
            Measure(() =>
            {
                for (int i = 0; i < 100; i++)
                    tiles.TryGetValue(new CellKey(3, 4), out _);
            }),
            0L,
            "cent recherches, zero octet : sans IEquatable, chaque comparaison emballe la structure dans un objet");

        var values = new List<int> { 1, 2, 3, 4, 5 };

        Check.Equal(SumList(values), 15, "la somme est la meme des deux cotes");
        Check.Equal(SumEnumerable(values), 15, "quel que soit le type du parametre");

        Check.Equal(Measure(() => SumList(values)), 0L,
            "parcourir une List<int> directement : l'enumerateur est un struct, il ne va pas sur le tas");

        Check.True(Measure(() => SumEnumerable(values)) > 0L,
            "mais la meme boucle derriere IEnumerable<int> emballe cet enumerateur a chaque appel");

        Check.True(
            Measure(() => SumEnumerable(values)) > Measure(() => SumList(values)),
            "declarer le parametre en List plutot qu'en IEnumerable, c'est gratuit et ca supprime l'allocation");
    }
}
