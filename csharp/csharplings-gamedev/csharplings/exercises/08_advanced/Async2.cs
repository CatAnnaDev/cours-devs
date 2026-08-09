namespace Csharplings;

public static class Async2
{
    public const bool NotDone = true;

    public static async Task<string> LoadAsync(string name, List<string> log)
    {
        log.Add($"debut {name}");
        await Task.Yield();
        log.Add($"fin {name}");

        return name.ToUpperInvariant();
    }

    public static async Task<string[]> LoadAllAsync(List<string> log)
    {
        string tileset = await LoadAsync("tileset", log);
        string musique = await LoadAsync("musique", log);

        return new[] { tileset, musique };
    }

    public static async Task<int> CountdownAsync(int steps, CancellationToken token)
    {
        int done = 0;

        for (int i = 0; i < steps; i++)
        {
            await Task.Yield();
            done++;
        }

        return done;
    }

    public static async Task<string> SafeAsync()
    {
        await FailAsync();

        return "jamais";
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
