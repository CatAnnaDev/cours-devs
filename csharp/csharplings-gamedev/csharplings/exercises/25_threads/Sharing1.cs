using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Csharplings;

public struct PlainCounter
{
    public long Value;
}

public struct PaddedCounter
{
    public long Value;
}

public static class Sharing1
{
    public const bool NotDone = true;

    public const int CacheLine = 64;
    public const int Workers = 4;
    public const int PerWorker = 100_000;

    public static long DistanceBetweenSlots<T>(T[] slots) =>
        (long)Unsafe.ByteOffset(ref slots[0], ref slots[1]);

    public static long CountPlain()
    {
        var counters = new PlainCounter[Workers];

        RunWorkers(index =>
        {
            for (int step = 0; step < PerWorker; step++)
                counters[index].Value++;
        });

        long total = 0;

        foreach (PlainCounter counter in counters)
            total += counter.Value;

        return total;
    }

    public static long CountPadded()
    {
        var counters = new PaddedCounter[Workers];

        RunWorkers(index =>
        {
            for (int step = 0; step < PerWorker; step++)
                counters[index].Value++;
        });

        long total = 0;

        foreach (PaddedCounter counter in counters)
            total += counter.Value;

        return total;
    }

    public static long CountInLocals()
    {
        var results = new long[Workers];

        RunWorkers(index =>
        {
            for (int step = 0; step < PerWorker; step++)
                results[index]++;
        });

        long total = 0;

        foreach (long result in results)
            total += result;

        return total;
    }

    private static void RunWorkers(Action<int> body)
    {
        var threads = new Thread[Workers];

        for (int i = 0; i < Workers; i++)
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
        const long expected = (long)Workers * PerWorker;

        Check.Equal(Unsafe.SizeOf<PlainCounter>(), 8, "un compteur nu pese huit octets");
        Check.Equal(Unsafe.SizeOf<PaddedCounter>(), CacheLine,
            "un compteur REMBOURRE en pese soixante-quatre : la taille d'une ligne de cache, la plus petite unite que le processeur echange avec la memoire");

        var plain = new PlainCounter[Workers];
        var padded = new PaddedCounter[Workers];

        Check.Equal(DistanceBetweenSlots(plain), 8L,
            "quatre compteurs nus tiennent dans trente-deux octets, donc dans UNE SEULE ligne de cache");

        Check.Equal(DistanceBetweenSlots(padded), (long)CacheLine,
            "les rembourres sont chacun sur la sienne");

        Check.Equal(CountPlain(), expected, "les deux versions comptent juste");
        Check.Equal(CountPadded(), expected, "exactement pareil");

        Check.True(CountPlain() == CountPadded(),
            "et c'est tout le probleme : le faux partage n'est PAS un bug de justesse. Le resultat est bon, les tests passent, et le code est trois a dix fois plus lent que prevu sans que rien ne le signale");

        Check.True(DistanceBetweenSlots(plain) < CacheLine,
            "ce qui se passe : chaque coeur garde SA copie de la ligne. Quand l'un ecrit dans son compteur, il invalide la ligne entiere chez les trois autres, qui doivent la relire. Quatre threads qui ne partagent RIEN logiquement se battent pour la meme ligne des milliers de fois par milliseconde");

        Check.Equal(CountInLocals(), expected,
            "la vraie parade n'est pas le rembourrage, c'est de ne pas ecrire dans une case partagee du tout : chaque thread accumule dans une VARIABLE LOCALE, qui vit dans un registre, et n'ecrit qu'une fois a la fin");

        Check.True(CacheLine >= 64,
            "le rembourrage sert quand la case doit rester lisible pendant le calcul - un compteur de progression affiche a l'ecran, par exemple. Soixante-quatre octets est la valeur sure sur x86 comme sur ARM");

        Check.Equal(Unsafe.SizeOf<PaddedCounter>() / Unsafe.SizeOf<PlainCounter>(), 8,
            "en echange, c'est huit fois plus de memoire : on ne rembourre que ce qui est vraiment ecrit en boucle par plusieurs threads, jamais par principe");
    }
}
