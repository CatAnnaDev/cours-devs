namespace Csharplings;

public sealed class Inventory
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, int> _items = new();

    public int Count
    {
        get
        {
            lock (_gate)
                return _items.Count;
        }
    }

    public void Add(string item, int amount)
    {
        lock (_gate)
            _items[item] = _items.GetValueOrDefault(item) + amount;
    }

    public bool TryTake(string item, int amount)
    {
        lock (_gate)
        {
            if (_items.GetValueOrDefault(item) < amount)
                return false;

            _items[item] -= amount;

            return true;
        }
    }
}

public static class Lock1
{
    public const bool NotDone = false;

    public const int Threads = 4;
    public const int PerThread = 200_000;

    private static readonly object FirstGate = new();
    private static readonly object SecondGate = new();

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static Exception Failure;

    public static void RunTogether(Action body)
    {
        Failure = null;

        using var gate = new ManualResetEventSlim(false);
        var threads = new Thread[Threads];

        for (int i = 0; i < Threads; i++)
        {
            threads[i] = new Thread(() =>
            {
                gate.Wait();

                try
                {
                    body();
                }
                catch (Exception error)
                {
                    Interlocked.CompareExchange(ref Failure, error, null);
                }
            });
        }

        foreach (Thread thread in threads)
            thread.Start();

        Thread.Sleep(20);
        gate.Set();

        foreach (Thread thread in threads)
            thread.Join();
    }

    public static bool WouldDeadlock()
    {
        bool blocked = false;

        lock (FirstGate)
        {
            var other = new Thread(() =>
            {
                lock (SecondGate)
                    blocked = !Monitor.TryEnter(FirstGate, TimeSpan.FromMilliseconds(50));
            });

            other.Start();
            other.Join();
        }

        return blocked;
    }

    public static void Run()
    {
        var inventory = new Inventory();

        RunTogether(() =>
        {
            for (int step = 0; step < PerThread; step++)
                inventory.Add("potion", 1);
        });

        Check.True(Failure is null,
            "premier verdict : aucun thread n'a leve. Sans verrou, un Dictionary partage finit par lever 'a concurrent update corrupted its state' - il ne perd pas seulement des valeurs, il CASSE, et le plantage sort sur un thread de fond ou personne ne l'attrape");

        Check.Equal(inventory.Count, 1, "un seul type d'objet, comme prevu");
        Check.True(inventory.TryTake("potion", Threads * PerThread),
            "et le compte exact : un Dictionary n'est PAS sur en ecriture concurrente, et sans verrou il ne perd pas seulement des valeurs, il se corrompt et peut boucler a l'infini");

        Check.False(inventory.TryTake("potion", 1), "l'inventaire est vide, et la verification et le retrait se font sous le MEME verrou");

        Check.False(inventory.TryTake("epee", 1),
            "sinon deux threads passeraient tous les deux le test avant que l'un des deux ne retire : c'est le bug 'verifier puis agir', et il est invisible en relecture");

        Check.Equal(Measure(() => inventory.Add("or", 1)), 0L,
            "un verrou non dispute ne coute rien du tout : le cas frequent est une operation atomique, pas un appel systeme");

        Check.True(WouldDeadlock(),
            "et voila l'autre danger. Ce thread tient le second verrou et demande le premier, pendant que le premier est deja tenu : sans le delai, les deux s'attendent pour toujours. Le jeu ne plante pas, il se FIGE");

        Check.True(Monitor.TryEnter(FirstGate, TimeSpan.FromMilliseconds(1)),
            "TryEnter avec un delai sert a diagnostiquer, jamais a corriger : la vraie parade est de toujours prendre les verrous dans le MEME ordre");

        Monitor.Exit(FirstGate);

        Check.True(true,
            "et la meilleure parade reste de n'avoir rien a verrouiller : donner une tranche a chacun, ou faire passer les resultats par une file, comme dans les deux exercices suivants");
    }
}
