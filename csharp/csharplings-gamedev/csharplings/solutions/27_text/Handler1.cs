using System.Runtime.CompilerServices;

namespace Csharplings;

[InterpolatedStringHandler]
public ref struct LogHandler
{
    private DefaultInterpolatedStringHandler _inner;

    public LogHandler(int literalLength, int formattedCount, bool enabled, out bool shouldAppend)
    {
        shouldAppend = enabled;
        _inner = enabled ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
        Enabled = enabled;
    }

    public bool Enabled { get; }

    public void AppendLiteral(string value)
    {
        if (Enabled)
            _inner.AppendLiteral(value);
    }

    public void AppendFormatted<T>(T value)
    {
        if (Enabled)
            _inner.AppendFormatted(value);
    }

    public string ToStringAndClear() => Enabled ? _inner.ToStringAndClear() : string.Empty;
}

public static class Handler1
{
    public const bool NotDone = false;

    public static bool Verbose;

    public static int Evaluations;

    public static readonly List<string> Lines = new();

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static void LogNaive(string message)
    {
        if (!Verbose)
            return;

        Lines.Add(message);
    }

    public static void Log(bool enabled, [InterpolatedStringHandlerArgument(nameof(enabled))] ref LogHandler handler)
    {
        if (!enabled)
            return;

        Lines.Add(handler.ToStringAndClear());
    }

    public static int ExpensiveCount()
    {
        Evaluations++;

        return 42;
    }

    public static void Run()
    {
        Verbose = false;
        Evaluations = 0;
        Lines.Clear();

        LogNaive($"ennemis restants : {ExpensiveCount()}");

        Check.Equal(Lines.Count, 0, "le journal est desactive, donc rien n'est enregistre");
        Check.Equal(Evaluations, 1,
            "et pourtant le calcul a bien eu lieu, et la chaine a bien ete construite. Un parametre est evalue AVANT l'appel : le 'if' a l'interieur arrive toujours trop tard");

        Check.True(Measure(() => LogNaive($"ennemis restants : {ExpensiveCount()}")) > 0L,
            "d'ou une allocation par appel, meme quand le journal est eteint. Soixante fois par seconde et vingt points de journalisation, c'est un ramassage de generation 0 toutes les quelques secondes, en production, pour rien");

        Verbose = true;
        Evaluations = 0;
        Lines.Clear();

        LogNaive($"ennemis restants : {ExpensiveCount()}");

        Check.Sequence(Lines, new[] { "ennemis restants : 42" }, "active, il enregistre le bon texte");
        Check.Equal(Evaluations, 1, "avec une evaluation");

        Verbose = false;
        Evaluations = 0;
        Lines.Clear();

        Log(Verbose, $"ennemis restants : {ExpensiveCount()}");

        Check.Equal(Lines.Count, 0, "la version a handler n'enregistre rien non plus quand elle est eteinte");

        Check.Equal(Evaluations, 0,
            "mais le calcul n'a PAS eu lieu. Le compilateur ne construit plus une chaine : il fabrique le handler, lui demande s'il faut continuer, et saute tous les AppendFormatted si la reponse est non");

        Check.Equal(Measure(() => Log(Verbose, $"ennemis restants : {ExpensiveCount()}")), 0L,
            "zero octet et zero calcul, sans que le point d'appel change d'un caractere : c'est exactement ce que fait Debug.Assert et ce que devrait faire tout journal de jeu");

        Verbose = true;
        Evaluations = 0;
        Lines.Clear();

        Log(Verbose, $"ennemis restants : {ExpensiveCount()}");

        Check.Sequence(Lines, new[] { "ennemis restants : 42" }, "et allumee, elle rend exactement le meme texte que l'interpolation ordinaire");
        Check.Equal(Evaluations, 1, "avec une seule evaluation");

        Check.True(Measure(() => Log(true, $"valeur {ExpensiveCount()}")) > 0L,
            "elle alloue alors, forcement : il faut bien fabriquer la chaine qu'on enregistre. Le gain porte sur le cas ETEINT, qui est le cas de 99 % des images");
    }
}
