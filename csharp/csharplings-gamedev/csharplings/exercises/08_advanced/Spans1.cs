namespace Csharplings;

public static class Spans1
{
    public const bool NotDone = true;

    public static int Sum(int[] values)
    {
        int total = 0;

        foreach (int value in values)
            total += value;

        return total;
    }

    public static void Normalize(Span<float> weights)
    {
        float total = 0f;

        foreach (float weight in weights)
            total += weight;
    }

    public static int MajorVersion(ReadOnlySpan<char> text)
    {
        return Todo.Value<int>();
    }

    public static int Highest()
    {
        return Todo.Value<int>();
    }

    public static void Run()
    {
        int[] damage = { 10, 20, 30, 40 };

        Check.Equal(Sum(damage), 100, "un tableau se convertit tout seul en span");
        Check.Equal(Sum(damage.AsSpan(1, 2)), 50, "une tranche ne copie rien, elle pointe dans le tableau");

        float[] weights = { 1f, 3f };
        Normalize(weights);

        Check.Near(weights[0], 0.25, "un Span ecrit dans le tableau d'origine");
        Check.Near(weights[1], 0.75, "il n'y a jamais eu de copie");

        Check.Equal(MajorVersion("4.2.1"), 4, "decouper une chaine sans allouer de sous-chaine");
        Check.Equal(MajorVersion("12"), 12, "et supporter le cas sans separateur");

        Check.Equal(Highest(), 87, "stackalloc pose le tableau sur la pile : zero travail pour le ramasse-miettes");
    }
}
