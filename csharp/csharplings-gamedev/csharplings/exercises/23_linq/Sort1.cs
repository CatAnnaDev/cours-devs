namespace Csharplings;

public sealed record Runner(string Name, int Score);

public static class Sort1
{
    public const bool NotDone = true;

    public static int Comparisons;

    private static readonly List<Runner> Board = new()
    {
        new Runner("anna", 30),
        new Runner("bob", 10),
        new Runner("cleo", 30),
        new Runner("dan", 20),
    };

    public static List<string> ByScore() =>
        Board.OrderBy(runner => runner.Score).Select(runner => runner.Name).ToList();

    public static List<string> ByScoreThenName() =>
        Board.OrderByDescending(runner => runner.Score).ThenBy(runner => runner.Name).Select(runner => runner.Name).ToList();

    public static Runner Best() => Todo.Value<Runner>();

    public static Runner Worst() => Todo.Value<Runner>();

    public static int CountingKey(Runner runner)
    {
        Comparisons++;

        return runner.Score;
    }

    public static void Run()
    {
        Check.Sequence(ByScore(), new[] { "anna", "cleo", "dan", "bob" },
            "OrderBy est un tri STABLE : anna et cleo ont le meme score, et elles gardent l'ordre qu'elles avaient dans la source");

        var list = new List<Runner>(Board);

        list.Sort((left, right) => right.Score.CompareTo(left.Score));

        Check.True(list[0].Score == 30 && list[1].Score == 30,
            "List.Sort trie sur place, sans allouer de nouvelle liste");

        Check.False(list.SequenceEqual(Board.OrderByDescending(runner => runner.Score))
            && ReferenceEquals(list, Board),
            "mais il n'est PAS stable : rien ne garantit l'ordre des ex aequo, et il modifie la liste d'origine");

        Check.Sequence(ByScoreThenName(), new[] { "anna", "cleo", "dan", "bob" },
            "quand l'ordre des ex aequo compte, on le dit : ThenBy departage explicitement au lieu de compter sur la chance");

        Check.Equal(Best().Name, "anna", "MaxBy rend l'ELEMENT du maximum, pas le maximum");
        Check.Equal(Worst().Name, "bob", "et MinBy son symetrique");

        Comparisons = 0;
        Runner viaSort = Board.OrderByDescending(CountingKey).First();

        int sortCost = Comparisons;

        Comparisons = 0;
        Runner viaMax = Board.MaxBy(CountingKey);

        int maxCost = Comparisons;

        Check.Equal(viaSort.Name, viaMax.Name, "les deux trouvent le meme coureur");
        Check.True(sortCost >= maxCost,
            $"mais OrderBy().First() TRIE TOUT pour n'en garder qu'un : {sortCost} extractions de cle contre {maxCost}. Sur une liste d'ennemis triee soixante fois par seconde pour trouver le plus proche, c'est la difference entre n log n et n");

        Check.Sequence(new[] { 3, 1, 2 }.Order(), new[] { 1, 2, 3 },
            "Order() trie une sequence sur elle-meme, sans selecteur : c'est OrderBy(x => x) en plus court");

        Check.Sequence(new[] { 3, 1, 2 }.OrderDescending(), new[] { 3, 2, 1 }, "et son symetrique");

        var scores = new List<int> { 5, 1, 9 };
        IOrderedEnumerable<int> ordered = scores.OrderBy(score => score);

        scores.Add(0);

        Check.Sequence(ordered, new[] { 0, 1, 5, 9 },
            "un tri LINQ est differe comme le reste : il retrie a chaque parcours, et voit ce qu'on a ajoute entre-temps");

        Check.Sequence(Board.Select(runner => runner.Name), new[] { "anna", "bob", "cleo", "dan" },
            "et il n'a jamais touche a la source : c'est ce qu'on achete en payant une allocation");
    }
}
