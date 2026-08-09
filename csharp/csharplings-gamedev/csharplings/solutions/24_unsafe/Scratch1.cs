using System.Buffers;

namespace Csharplings;

public static class Scratch1
{
    public const bool NotDone = false;

    public const int StackLimit = 256;

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static int CountAboveOnStack(ReadOnlySpan<int> scores, int threshold)
    {
        Span<int> kept = stackalloc int[32];
        int count = 0;

        foreach (int score in scores)
        {
            if (score <= threshold || count == kept.Length)
                continue;

            kept[count++] = score;
        }

        return count;
    }

    public static int SortedMedian(ReadOnlySpan<int> values)
    {
        int[] rented = null;

        Span<int> buffer = values.Length <= StackLimit
            ? stackalloc int[values.Length]
            : (rented = ArrayPool<int>.Shared.Rent(values.Length)).AsSpan(0, values.Length);

        values.CopyTo(buffer);
        buffer.Sort();

        int median = buffer[buffer.Length / 2];

        if (rented is not null)
            ArrayPool<int>.Shared.Return(rented);

        return median;
    }

    public static void Run()
    {
        var scores = new[] { 5, 40, 12, 90, 3 };

        Check.Equal(CountAboveOnStack(scores, 10), 3, "un tampon de travail sur la PILE : trois scores au-dessus de dix");

        Check.Equal(Measure(() => CountAboveOnStack(scores, 10)), 0L,
            "et zero octet alloue : 'stackalloc' prend la place sur la pile de la methode, qui se libere toute seule au retour");

        Check.Equal(SortedMedian(new[] { 9, 1, 5 }), 5, "la mediane de trois valeurs");
        Check.Equal(SortedMedian(new[] { 9, 1, 5, 7 }), 7, "sur un nombre pair, on prend celle du dessus");

        Check.Equal(Measure(() => SortedMedian(scores)), 0L,
            "une petite entree tient sur la pile : rien n'est alloue, et le tri se fait dans le tampon sans toucher a la source");

        var big = new int[StackLimit * 4];

        for (int i = 0; i < big.Length; i++)
            big[i] = big.Length - i;

        Check.Equal(SortedMedian(big), StackLimit * 2 + 1,
            "une grande entree bascule sur un tableau LOUE : au-dela de quelques centaines d'octets, la pile n'est plus le bon endroit");

        Check.Equal(Measure(() => SortedMedian(big)), 0L,
            "et le pool ne coute rien non plus une fois chauffe : c'est le meme tableau qu'on emprunte et qu'on rend");

        Span<int> small = stackalloc int[4];

        Check.Equal(small[0], 0, "un stackalloc affecte a un Span est remis a zero");
        Check.Equal(small.Length, 4, "et il connait sa taille, contrairement a un pointeur nu");

        small[3] = 7;

        Check.Equal(small[3], 7, "on y ecrit comme dans un tableau");

        Check.Throws<IndexOutOfRangeException>(() =>
            {
                Span<int> guarded = stackalloc int[4];

                guarded[9] = 1;
            },
            "et surtout : un Span VERIFIE ses bornes. Un int* sur le meme stackalloc ecrirait dans la pile de l'appelant, ecraserait une adresse de retour, et le plantage sortirait ailleurs, plus tard, sans rapport");

        Check.True(StackLimit * sizeof(int) <= 1024,
            "la regle de pouce : au-dela d'un kilooctet environ, on ne prend pas sur la pile. Elle fait un megaoctet par thread, un stackalloc dans une boucle ne se libere qu'au RETOUR de la methode, et un debordement de pile ne se rattrape pas");
    }
}
