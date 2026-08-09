namespace Csharplings;

public sealed class GameRandom
{
    private uint _state;

    public GameRandom(uint seed)
    {
        _state = seed == 0u ? 1u : seed;
    }

    public uint NextUInt()
    {
        _state ^= _state << 13;
        _state ^= _state >> 17;
        _state ^= _state << 5;

        return _state;
    }

    public float Value() => NextUInt() / 4294967296f;

    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            return minInclusive;

        return minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));
    }

    public T Pick<T>(IReadOnlyList<T> items) => items[Range(0, items.Count)];

    public void Shuffle<T>(IList<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = Range(0, i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}

public static class Random1
{
    public const bool NotDone = false;

    public static string WeightedPick(GameRandom random, List<(string Name, int Weight)> table)
    {
        int total = 0;

        foreach ((string _, int weight) in table)
            total += weight;

        int roll = random.Range(0, total);

        foreach ((string name, int weight) in table)
        {
            if (roll < weight)
                return name;

            roll -= weight;
        }

        return table[table.Count - 1].Name;
    }

    public static void Run()
    {
        var first = new GameRandom(1234u);
        var second = new GameRandom(1234u);

        var a = new List<int>();
        var b = new List<int>();

        for (int i = 0; i < 20; i++)
        {
            a.Add(first.Range(0, 1000));
            b.Add(second.Range(0, 1000));
        }

        Check.Sequence(a, b, "meme graine, meme suite : c'est ce qui rend un donjon rejouable");
        Check.True(a.Distinct().Count() > 10, "et ce n'est pas juste la meme valeur repetee");

        var other = new GameRandom(9999u);
        Check.False(other.Range(0, 1000) == a[0] && other.Range(0, 1000) == a[1],
            "une graine differente donne une autre suite");

        var bounded = new GameRandom(7u);

        for (int i = 0; i < 2000; i++)
        {
            int roll = bounded.Range(5, 10);

            if (roll < 5 || roll >= 10)
                throw new CheckFailedException($"Range est sorti de ses bornes : {roll}");
        }

        Check.True(true, "2000 tirages restent dans [5, 10[");

        var unit = new GameRandom(42u);

        for (int i = 0; i < 2000; i++)
        {
            float value = unit.Value();

            if (value < 0f || value >= 1f)
                throw new CheckFailedException($"Value est sorti de [0, 1[ : {value}");
        }

        Check.True(true, "et 2000 flottants restent dans [0, 1[");

        var deck = Enumerable.Range(0, 10).ToList();
        var copy = Enumerable.Range(0, 10).ToList();

        new GameRandom(3u).Shuffle(deck);
        new GameRandom(3u).Shuffle(copy);

        Check.Sequence(deck, copy, "deux melanges de meme graine sont identiques");
        Check.Sequence(deck.OrderBy(card => card), Enumerable.Range(0, 10), "un melange ne perd ni ne duplique de carte");
        Check.False(deck.SequenceEqual(Enumerable.Range(0, 10)), "et il a vraiment melange");

        var loot = new List<(string Name, int Weight)>
        {
            ("commun", 89),
            ("rare", 10),
            ("epique", 1),
        };

        var counts = new Dictionary<string, int> { ["commun"] = 0, ["rare"] = 0, ["epique"] = 0 };
        var drops = new GameRandom(2024u);

        for (int i = 0; i < 20000; i++)
            counts[WeightedPick(drops, loot)]++;

        Check.Equal(counts.Values.Sum(), 20000, "chaque tirage rend forcement quelque chose");
        Check.True(counts["commun"] > counts["rare"], "le commun tombe plus souvent que le rare");
        Check.True(counts["rare"] > counts["epique"], "et le rare plus souvent que l'epique");
        Check.True(counts["epique"] > 0, "mais l'epique tombe quand meme parfois");
        Check.True(counts["commun"] > 16000 && counts["commun"] < 20000, "les proportions suivent les poids");
    }
}
