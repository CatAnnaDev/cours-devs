namespace Csharplings;

public static class Race1
{
    public const bool NotDone = false;

    public const int Threads = 4;
    public const int PerThread = 200_000;
    public const int Expected = Threads * PerThread;

    private static int _plain;
    private static int _atomic;
    private static long _wide;

    public static int RaceTotal()
    {
        _plain = 0;

        RunOnThreads(() =>
        {
            for (int i = 0; i < PerThread; i++)
                _plain++;
        });

        return _plain;
    }

    public static int AtomicTotal()
    {
        _atomic = 0;

        RunOnThreads(() =>
        {
            for (int i = 0; i < PerThread; i++)
                Interlocked.Increment(ref _atomic);
        });

        return _atomic;
    }

    public static long ShardedTotal()
    {
        var shards = new long[Threads];

        RunOnThreads(index =>
        {
            long local = 0;

            for (int i = 0; i < PerThread; i++)
                local++;

            shards[index] = local;
        });

        long total = 0;

        foreach (long shard in shards)
            total += shard;

        _wide = total;

        return total;
    }

    public static bool LosesUpdates()
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (RaceTotal() < Expected)
                return true;
        }

        return false;
    }

    private static void RunOnThreads(Action body) => RunOnThreads(_ => body());

    private static void RunOnThreads(Action<int> body)
    {
        var threads = new Thread[Threads];

        for (int i = 0; i < Threads; i++)
        {
            int index = i;

            threads[i] = new Thread(() => body(index));
        }

        foreach (Thread thread in threads)
            thread.Start();

        foreach (Thread thread in threads)
            thread.Join();
    }

    public static void Run()
    {
        Check.True(LosesUpdates(),
            $"quatre threads, {PerThread} incrementations chacun, et le total est INFERIEUR a {Expected}. Rien n'a plante, rien n'a prevenu : des incrementations ont simplement disparu");

        Check.True(_plain > 0, "il en reste, mais pas le compte");

        Check.Equal(AtomicTotal(), Expected,
            "parce que 'compteur++' n'est pas UNE operation : c'est lire, ajouter un, reecrire. Deux threads qui lisent la meme valeur ecrivent la meme valeur, et une incrementation est perdue. Interlocked.Increment fait les trois d'un bloc");

        Check.Equal(ShardedTotal(), (long)Expected,
            "et voici la version qui va le plus vite : chaque thread compte dans SA case, et on additionne a la fin. Pas de synchronisation du tout, parce qu'il n'y a rien a partager");

        Check.Equal(_wide, (long)Expected, "le total final est le meme");

        int shared = 0;

        Check.Equal(Interlocked.Increment(ref shared), 1, "Interlocked.Increment rend la NOUVELLE valeur");
        Check.Equal(Interlocked.Add(ref shared, 10), 11, "Add ajoute et rend le resultat");
        Check.Equal(Interlocked.Exchange(ref shared, 99), 11, "Exchange pose la nouvelle valeur et rend l'ANCIENNE");
        Check.Equal(Interlocked.CompareExchange(ref shared, 5, 99), 99,
            "et CompareExchange n'ecrit que si la valeur est bien celle qu'on croyait : c'est la brique de tout ce qui se fait sans verrou");
        Check.Equal(shared, 5, "ici la comparaison a reussi, donc l'ecriture a eu lieu");

        Check.Equal(Interlocked.CompareExchange(ref shared, 42, 99), 5,
            "un second essai avec la meme valeur attendue echoue : quelqu'un est passe avant");
        Check.Equal(shared, 5, "et rien n'a ete ecrit, ce qui est exactement le but");

        Check.True(Environment.ProcessorCount >= 1,
            "derniere chose : rien de tout ceci ne se voit sur une machine a un seul coeur, ni en pas-a-pas dans le debogueur. Une course de donnees se reproduit sur la machine du joueur, une fois sur mille, et jamais sur la tienne");
    }
}
