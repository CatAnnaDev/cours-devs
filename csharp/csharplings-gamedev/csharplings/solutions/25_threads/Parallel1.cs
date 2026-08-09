namespace Csharplings;

public static class Parallel1
{
    public const bool NotDone = false;

    public static long SumSequential(int[] values)
    {
        long total = 0;

        foreach (int value in values)
            total += value;

        return total;
    }

    public static long SumParallelBroken(int[] values)
    {
        long total = 0;

        Parallel.For(0, values.Length, i => total += values[i]);

        return total;
    }

    public static long SumParallelAtomic(int[] values)
    {
        long total = 0;

        Parallel.For(0, values.Length, i => Interlocked.Add(ref total, values[i]));

        return total;
    }

    public static long SumParallelLocal(int[] values)
    {
        long total = 0;

        Parallel.For(
            0,
            values.Length,
            () => 0L,
            (i, state, local) => local + values[i],
            local => Interlocked.Add(ref total, local));

        return total;
    }

    public static void ScaleInSlices(float[] values, float factor)
    {
        int slices = Math.Min(Environment.ProcessorCount, values.Length);
        int size = (values.Length + slices - 1) / slices;

        Parallel.For(0, slices, slice =>
        {
            int start = slice * size;
            int end = Math.Min(start + size, values.Length);

            for (int i = start; i < end; i++)
                values[i] *= factor;
        });
    }

    public static void Run()
    {
        var values = new int[100_000];

        for (int i = 0; i < values.Length; i++)
            values[i] = 1;

        Check.Equal(SumSequential(values), 100_000L, "la version sequentielle donne le bon total");

        Check.True(SumParallelBroken(values) <= 100_000L,
            "la version parallele naive rend AU MIEUX le bon total, et presque toujours moins : 'total += x' depuis plusieurs threads perd des additions, exactement comme un compteur");

        Check.Equal(SumParallelAtomic(values), 100_000L,
            "Interlocked.Add corrige le resultat");

        Check.Equal(SumParallelLocal(values), 100_000L,
            "et cette version-la corrige le resultat SANS payer une operation atomique par element : chaque tache accumule dans un total LOCAL, et n'en publie qu'un par tache");

        var speeds = new float[1001];

        for (int i = 0; i < speeds.Length; i++)
            speeds[i] = 2f;

        ScaleInSlices(speeds, 3f);

        Check.Equal(speeds[0], 6f, "decouper en tranches : chaque tache ecrit dans SA part du tableau");
        Check.Equal(speeds[1000], 6f, "y compris la derniere, qui est presque toujours plus courte que les autres");
        Check.True(speeds.All(speed => Mathf.IsEqualApprox(speed, 6f)),
            "et aucune synchronisation n'est necessaire, parce que deux taches n'ecrivent jamais au meme endroit");

        var small = new int[8];

        Array.Fill(small, 1);

        Check.Equal(SumParallelLocal(small), 8L, "sur huit elements le resultat est juste");

        Check.True(small.Length < Environment.ProcessorCount * 1000,
            "mais paralleliser huit additions coute PLUS cher que de les faire : il faut repartir le travail, reveiller des threads, et les attendre. En dessous de quelques milliers d'elements ou de quelques microsecondes par element, la boucle simple gagne");

        int[] captured = values;
        long checkedTotal = 0;

        Parallel.For(0, 4, new ParallelOptions { MaxDegreeOfParallelism = 2 }, _ => Interlocked.Increment(ref checkedTotal));

        Check.Equal(checkedTotal, 4L,
            "MaxDegreeOfParallelism limite le nombre de taches simultanees : indispensable dans un jeu, ou il faut laisser des coeurs au rendu et a l'audio");

        Check.Equal(captured.Length, 100_000, "et la source n'a pas bouge");
    }
}
