namespace Demos.Pure;

public sealed class Rng
{
    private const ulong Multiplier = 6364136223846793005UL;

    private ulong _state;
    private readonly ulong _increment;

    public Rng(ulong seed, ulong stream = 1UL)
    {
        _increment = (stream << 1) | 1UL;
        _state = 0UL;

        NextUInt();
        _state += seed;
        NextUInt();
    }

    public uint NextUInt()
    {
        ulong previous = _state;

        unchecked
        {
            _state = previous * Multiplier + _increment;
        }

        uint xorshifted = (uint)(((previous >> 18) ^ previous) >> 27);
        int rotation = (int)(previous >> 59);

        return (xorshifted >> rotation) | (xorshifted << ((-rotation) & 31));
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            return minInclusive;

        uint span = (uint)(maxExclusive - minInclusive);
        uint limit = uint.MaxValue - uint.MaxValue % span;
        uint draw;

        do
        {
            draw = NextUInt();
        }
        while (draw >= limit);

        return minInclusive + (int)(draw % span);
    }

    public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

    public float Range(float minInclusive, float maxExclusive) =>
        minInclusive + NextFloat() * (maxExclusive - minInclusive);

    public bool Chance(float probability) => NextFloat() < probability;

    public float NextGaussian(float mean = 0f, float deviation = 1f)
    {
        float u1 = MathF.Max(NextFloat(), 1e-7f);
        float u2 = NextFloat();

        return mean + deviation * MathF.Sqrt(-2f * MathF.Log(u1)) * MathF.Cos(2f * MathF.PI * u2);
    }

    public void Shuffle<T>(IList<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = Range(0, i + 1);

            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    public int WeightedIndex(ReadOnlySpan<int> weights)
    {
        int total = 0;

        for (int i = 0; i < weights.Length; i++)
            total += weights[i];

        if (total <= 0)
            return -1;

        int draw = Range(0, total);

        for (int i = 0; i < weights.Length; i++)
        {
            draw -= weights[i];

            if (draw < 0)
                return i;
        }

        return weights.Length - 1;
    }

    public static void Demo()
    {
        Console.WriteLine("--- Rng : aleatoire a graine, rejouable et decoupe en flux ---");

        var first = new Rng(seed: 1234);
        var second = new Rng(seed: 1234);

        Console.WriteLine($"  meme graine, meme suite    : {first.Range(0, 100)} {first.Range(0, 100)} {first.Range(0, 100)}");
        Console.WriteLine($"  et la copie donne pareil   : {second.Range(0, 100)} {second.Range(0, 100)} {second.Range(0, 100)}");

        var terrain = new Rng(seed: 1234, stream: 1);
        var loot = new Rng(seed: 1234, stream: 2);

        Console.WriteLine($"  meme graine, flux different: {terrain.Range(0, 100)} contre {loot.Range(0, 100)}");
        Console.WriteLine("  deux flux, c'est ce qui evite qu'ouvrir un coffre en plus decale tout le donjon");

        var draws = new Rng(seed: 7);
        int[] weights = { 70, 25, 4, 1 };
        string[] names = { "commun", "rare", "epique", "legendaire" };
        var counts = new int[4];

        for (int i = 0; i < 10_000; i++)
            counts[draws.WeightedIndex(weights)]++;

        for (int i = 0; i < 4; i++)
            Console.WriteLine($"  {names[i],-12} attendu {weights[i] / 100f:P1}  obtenu {counts[i] / 10000f:P1}");

        var cards = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
        new Rng(seed: 42).Shuffle(cards);

        Console.WriteLine($"  melange a graine 42        : {string.Join(" ", cards)}");

        var bell = new Rng(seed: 99);
        float sum = 0f;

        for (int i = 0; i < 10_000; i++)
            sum += bell.NextGaussian(mean: 100f, deviation: 15f);

        Console.WriteLine($"  gaussienne, moyenne visee 100, obtenue {sum / 10000f:0.0} (degats varies sans extremes absurdes)");
        Console.WriteLine();
    }
}
