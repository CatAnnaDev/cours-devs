namespace Csharplings;

public static class Collections1
{
    public const bool NotDone = true;

    public static int[] BuildLevels(int count)
    {
        int[] levels = new int[count];

        for (int i = 0; i <= count; i++)
            levels[i] = i * 10;

        return levels;
    }

    public static int LastOf(int[] values)
    {
        return values[values.Length];
    }

    public static void Run()
    {
        Check.Sequence(BuildLevels(3), new[] { 0, 10, 20 }, "3 niveaux : indices 0, 1 et 2");
        Check.Sequence(BuildLevels(0), Array.Empty<int>(), "un tableau vide reste vide");

        Check.Equal(LastOf(new[] { 4, 8, 15 }), 15, "le dernier element");
        Check.Equal(LastOf(new[] { 7 }), 7, "un tableau d'un seul element");
    }
}
