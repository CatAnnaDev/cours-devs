namespace Csharplings;

public static class Iterators1
{
    public const bool NotDone = false;

    public static IEnumerable<int> Countdown(int from)
    {
        for (int i = from; i >= 0; i--)
            yield return i;
    }

    public static IEnumerable<int> Fibonacci()
    {
        int previous = 0;
        int current = 1;

        while (true)
        {
            yield return previous;
            (previous, current) = (current, previous + current);
        }
    }

    public static IEnumerable<Vector2> Ring(int count, float radius)
    {
        for (int i = 0; i < count; i++)
            yield return Vector2.FromAngle(Mathf.Tau * i / count) * radius;
    }

    public static IEnumerable<string> Wave(List<string> log)
    {
        log.Add("debut");
        yield return "gobelin";
        log.Add("milieu");
        yield return "golem";
        log.Add("fin");
    }

    public static void Run()
    {
        Check.Sequence(Countdown(3), new[] { 3, 2, 1, 0 }, "un compte a rebours qui finit a zero");
        Check.Sequence(Countdown(0), new[] { 0 }, "et qui marche deja depuis zero");

        Check.Sequence(Fibonacci().Take(7), new[] { 0, 1, 1, 2, 3, 5, 8 },
            "une suite infinie dont on ne consomme que le debut");

        List<Vector2> ring = Ring(4, 10f).ToList();

        Check.Equal(ring.Count, 4, "quatre points de spawn sur un cercle");
        Check.Near(ring[0], new Vector2(10f, 0f), "le premier part vers la droite");
        Check.Near(ring[1], new Vector2(0f, 10f), "le deuxieme un quart de tour plus loin");

        var log = new List<string>();
        IEnumerable<string> wave = Wave(log);

        Check.Equal(log.Count, 0, "appeler la methode n'execute pas une seule ligne du corps");

        List<string> spawned = wave.ToList();

        Check.Sequence(spawned, new[] { "gobelin", "golem" }, "c'est le parcours qui produit les valeurs");
        Check.Sequence(log, new[] { "debut", "milieu", "fin" }, "et qui deroule le corps, morceau par morceau");
    }
}
