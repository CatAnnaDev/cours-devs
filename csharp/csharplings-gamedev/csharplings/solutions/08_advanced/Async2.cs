namespace Csharplings;

public static class Async2
{
    public const bool NotDone = false;

    public static async Task<string> LoadAsync(string name, List<string> log)
    {
        log.Add($"debut {name}");
        await Task.Yield();
        log.Add($"fin {name}");

        return name.ToUpperInvariant();
    }

    public static async Task<string[]> LoadAllAsync(List<string> log)
    {
        Task<string> tileset = LoadAsync("tileset", log);
        Task<string> musique = LoadAsync("musique", log);

        return await Task.WhenAll(tileset, musique);
    }

    public static async Task<int> CountdownAsync(int steps, CancellationToken token)
    {
        int done = 0;

        for (int i = 0; i < steps; i++)
        {
            token.ThrowIfCancellationRequested();
            await Task.Yield();
            done++;
        }

        return done;
    }

    public static async Task<string> SafeAsync()
    {
        try
        {
            await FailAsync();
            return "jamais";
        }
        catch (InvalidOperationException error)
        {
            return $"rattrape : {error.Message}";
        }
    }

    private static async Task FailAsync()
    {
        await Task.Yield();

        throw new InvalidOperationException("disque plein");
    }

    public static void Run()
    {
        var log = new List<string>();
        string[] loaded = LoadAllAsync(log).GetAwaiter().GetResult();

        Check.Sequence(loaded, new[] { "TILESET", "MUSIQUE" }, "WhenAll rend les resultats dans l'ordre des taches");
        Check.Equal(log[0], "debut tileset", "la premiere tache demarre");
        Check.Equal(log[1], "debut musique", "et la seconde demarre avant que la premiere finisse : c'est ca, en parallele");

        var source = new CancellationTokenSource();

        Check.Equal(CountdownAsync(3, source.Token).GetAwaiter().GetResult(), 3, "sans annulation, on va au bout");

        source.Cancel();

        Check.Throws<OperationCanceledException>(
            () => CountdownAsync(3, source.Token).GetAwaiter().GetResult(),
            "un token annule interrompt la boucle");

        Check.Equal(SafeAsync().GetAwaiter().GetResult(), "rattrape : disque plein",
            "await relance l'exception telle quelle, un try/catch normal suffit");

        Task failing = FailAsync();

        Check.Throws<AggregateException>(() => failing.Wait(), "alors que Wait, lui, l'emballe");
    }
}
