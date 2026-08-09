namespace Csharplings;

public static class Delegates1
{
    public const bool NotDone = false;

    public static List<int> Transform(List<int> values, Func<int, int> operation)
    {
        var result = new List<int>();

        foreach (int value in values)
            result.Add(operation(value));

        return result;
    }

    public static int Repeat(int times, Action work)
    {
        int done = 0;

        for (int i = 0; i < times; i++)
        {
            work();
            done++;
        }

        return done;
    }

    public static void Run()
    {
        Func<int, int> doubler = value => value * 2;
        Check.Sequence(Transform(new List<int> { 1, 2, 3 }, doubler), new[] { 2, 4, 6 }, "doubler chaque valeur");

        Func<int, int, int> add = (a, b) => a + b;
        Check.Equal(add(3, 4), 7, "Func<int, int, int> : deux entrees, une sortie");

        int counter = 0;
        Action tick = () => counter++;
        Check.Equal(Repeat(5, tick), 5, "l'action a bien tourne 5 fois");
        Check.Equal(counter, 5, "une lambda peut modifier une variable de l'exterieur");

        Check.Sequence(
            Transform(new List<int> { 1, 2, 3 }, value => value * value),
            new[] { 1, 4, 9 },
            "une lambda ecrite directement a l'appel");
    }
}
