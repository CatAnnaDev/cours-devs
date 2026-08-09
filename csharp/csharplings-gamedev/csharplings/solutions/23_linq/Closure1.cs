namespace Csharplings;

public static class Closure1
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

    public static List<Func<int>> BrokenCounters()
    {
        var counters = new List<Func<int>>();

        for (int i = 0; i < 3; i++)
            counters.Add(() => i);

        return counters;
    }

    public static List<Func<int>> FixedCounters()
    {
        var counters = new List<Func<int>>();

        for (int i = 0; i < 3; i++)
        {
            int captured = i;

            counters.Add(() => captured);
        }

        return counters;
    }

    public static int CountAbove(List<int> values, int threshold) =>
        values.Count(value => value > threshold);

    public static int CountPositive(List<int> values) =>
        values.Count(static value => value > 0);

    public static Func<int> Holder;

    public static Func<int> MakeHolder(out WeakReference tracked)
    {
        var buffer = new byte[4096];

        tracked = new WeakReference(buffer);

        return () => buffer.Length;
    }

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    public static void Run()
    {
        Check.Sequence(BrokenCounters().Select(counter => counter()), new[] { 3, 3, 3 },
            "une lambda capture la VARIABLE, pas sa valeur. Les trois lambdas partagent le meme i, et i vaut 3 quand la boucle s'arrete");

        Check.Sequence(FixedCounters().Select(counter => counter()), new[] { 0, 1, 2 },
            "une copie DANS le corps de la boucle donne une variable par tour, donc une capture par tour");

        var frames = new List<int>();
        var callbacks = new List<Action>();

        foreach (int frame in new[] { 1, 2, 3 })
            callbacks.Add(() => frames.Add(frame));

        foreach (Action callback in callbacks)
            callback();

        Check.Sequence(frames, new[] { 1, 2, 3 },
            "la variable d'un foreach est deja une nouvelle variable a chaque tour : ce piege-la a ete corrige dans le langage, celui du 'for' non");

        var values = new List<int> { -2, 5, 9 };

        Check.Equal(CountPositive(values), 2, "une lambda qui ne capture rien donne le bon compte");

        Check.Equal(Measure(() => CountPositive(values)), 0L,
            "et elle n'alloue RIEN : sans capture, le compilateur fabrique le delegue une seule fois et le garde en cache");

        Check.Equal(CountAbove(values, 0), 2, "la version qui capture donne le meme compte");

        Check.True(Measure(() => CountAbove(values, 0)) > 0L,
            "mais elle alloue a chaque appel : capturer 'threshold' oblige a fabriquer un objet de fermeture pour le transporter, plus un delegue qui pointe dessus");

        int shared = 1;
        Func<int> read = () => shared;

        shared = 42;

        Check.Equal(read(), 42,
            "la lambda et le code autour partagent UNE seule variable : ce qui change dehors change dedans, et l'inverse aussi");

        Holder = MakeHolder(out WeakReference tracked);

        Collect();

        Check.True(tracked.IsAlive,
            "et une capture PROLONGE la vie de ce qu'elle capture : le tampon de 4 kilooctets est encore la alors que la methode qui l'a cree est finie depuis longtemps");

        Check.Equal(Holder(), 4096,
            "puisque la lambda, elle, le tient toujours, et il ne repartira qu'avec elle. Un abonnement a un evenement, c'est exactement ca : tant que l'evenement tient la lambda, la lambda tient tout ce que la lambda a vu");

        Check.True(Measure(() => values.Count(value => value > 0)) == 0L,
            "derniere regle : une lambda ecrite sur place sans capture est aussi gratuite. C'est la CAPTURE qui coute, pas la lambda");
    }
}
