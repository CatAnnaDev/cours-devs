using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Csharplings;

public static class Bounds1
{
    public const bool NotDone = false;

    public static int SumChecked(int[] values)
    {
        int total = 0;

        for (int i = 0; i < values.Length; i++)
            total += values[i];

        return total;
    }

    public static int SumUnchecked(int[] values)
    {
        ref int start = ref MemoryMarshal.GetArrayDataReference(values);
        int total = 0;

        for (int i = 0; i < values.Length; i++)
            total += Unsafe.Add(ref start, i);

        return total;
    }

    public static int SumSpan(ReadOnlySpan<int> values)
    {
        int total = 0;

        foreach (int value in values)
            total += value;

        return total;
    }

    public static int ReadWayPast(int[] values) =>
        Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(values), values.Length);

    public static void Run()
    {
        var values = new[] { 1, 2, 3, 4, 5 };

        Check.Equal(SumChecked(values), 15, "la version verifiee");
        Check.Equal(SumUnchecked(values), 15, "la version sans verification donne le meme resultat");
        Check.Equal(SumSpan(values), 15, "et le Span aussi");

        Check.Throws<IndexOutOfRangeException>(() => { _ = values[9]; },
            "un acces hors bornes sur un tableau LEVE : le compilateur a insere une comparaison avant chaque lecture");

        Check.Throws<IndexOutOfRangeException>(() => { _ = values.AsSpan()[9]; },
            "un Span verifie ses bornes exactement pareil : il n'echange pas la securite contre la vitesse");

        Check.Equal(ReadWayPast(values) is int, true,
            "Unsafe.Add, lui, ne verifie RIEN. Lire un element apres la fin ne leve pas : ca rend l'entier qui traine la, et le programme continue avec une valeur inventee");

        Check.Equal(SumUnchecked(Array.Empty<int>()), 0,
            "sur un tableau vide, la boucle ne tourne pas : c'est la CONDITION de boucle qui protege, plus le langage");

        Check.Equal(SumChecked(new[] { -1, 1 }), 0, "les deux versions restent equivalentes tant que les indices sont bons");
        Check.Equal(SumUnchecked(new[] { -1, 1 }), 0, "et elles ne le sont plus du tout des qu'ils ne le sont pas");

        Span<int> window = values.AsSpan(1, 3);

        Check.Equal(window.Length, 3, "un Span sur une TRANCHE : trois elements a partir du deuxieme");
        Check.Equal(SumSpan(window), 9, "et il ne voit que sa tranche");

        window[0] = 20;

        Check.Equal(values[1], 20, "en ecrivant dans le vrai tableau, sans copie");

        Check.Throws<ArgumentOutOfRangeException>(() => values.AsSpan(3, 5),
            "et une tranche qui deborde est refusee A LA CONSTRUCTION : une erreur au moment ou on la commet, pas trois cents lignes plus loin");

        Check.True(values.Length > 0,
            "la conclusion n'est pas 'utilise Unsafe' : c'est que le compilateur SUPPRIME deja la verification quand la boucle va de 0 a Length. Ecrire i < values.Length plutot que i <= n suffit a l'obtenir, sans une ligne de code non sur");
    }
}
