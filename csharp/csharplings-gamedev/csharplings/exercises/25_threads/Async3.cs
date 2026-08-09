namespace Csharplings;

public static class Async3
{
    public const bool NotDone = true;

    public static int Swallowed;

    public static int Running;

    public static int MaxRunning;

    public static async Task<int> ComputeAsync(int input)
    {
        MaxRunning = Math.Max(MaxRunning, Interlocked.Increment(ref Running));

        await Task.Yield();

        Interlocked.Decrement(ref Running);

        return input * 2;
    }

    public static async Task<int> FailAsync()
    {
        await Task.Yield();

        throw new InvalidOperationException("le chargement a echoue");
    }

    public static async void FireAndForget()
    {
        try
        {
            await FailAsync();
        }
        catch (InvalidOperationException)
        {
            Swallowed++;
        }
    }

    public static ValueTask<int> CachedAsync(int input) => new(input * 2);

    public static async Task<int> SumAsync(int[] inputs)
    {
        int total = 0;

        foreach (int input in inputs)
            total += await ComputeAsync(input);

        return total;
    }

    public static async Task<int> SumOneByOneAsync(int[] inputs)
    {
        int total = 0;

        foreach (int input in inputs)
            total += await ComputeAsync(input);

        return total;
    }

    public static void Run()
    {
        Check.Equal(ComputeAsync(21).GetAwaiter().GetResult(), 42, "une Task porte un resultat");

        Task<int> failing = FailAsync();

        Check.Throws<InvalidOperationException>(() => failing.GetAwaiter().GetResult(),
            "et elle porte aussi l'exception : elle ressort a l'endroit ou on l'attend, pas a l'endroit ou elle a ete levee");

        Check.True(failing.IsFaulted, "la tache est marquee en echec");
        Check.True(failing.Exception is AggregateException,
            "et sa propriete Exception les regroupe, parce qu'une tache combinee peut en porter plusieurs");

        Swallowed = 0;
        FireAndForget();

        Thread.Sleep(50);

        Check.Equal(Swallowed, 1,
            "un 'async void' n'a PAS de tache : personne ne peut l'attendre, et personne ne peut attraper ce qui en sort. Ici le try/catch est a l'interieur, sinon l'exception remonterait au thread et tuerait le processus");

        MaxRunning = 0;

        Check.Equal(SumAsync(new[] { 1, 2, 3 }).GetAwaiter().GetResult(), 12, "trois calculs, un total");

        Check.Equal(MaxRunning, 3,
            "et les TROIS etaient en vol en meme temps : Task.WhenAll les lance tous avant d'attendre. Trois travaux de cent millisecondes en prennent cent");

        MaxRunning = 0;

        Check.Equal(SumOneByOneAsync(new[] { 1, 2, 3 }).GetAwaiter().GetResult(), 12, "le meme total");

        Check.Equal(MaxRunning, 1,
            "obtenu avec un seul calcul en vol a la fois, donc en trois cents millisecondes. C'est l'erreur la plus courante de tout le C# asynchrone : un 'await' dans un foreach serialise tout, et rien ne le signale");

        Check.Equal(CachedAsync(21).GetAwaiter().GetResult(), 42, "une ValueTask porte le meme resultat");

        Check.True(CachedAsync(1).IsCompleted,
            "mais quand la reponse est deja la, elle n'alloue AUCUN objet : c'est ce qu'on veut pour un cache, ou pour un 'charge si pas deja charge' appele mille fois");

        Check.True(Task.FromResult(1).IsCompleted, "Task.FromResult joue le meme role quand la signature impose une Task");

        Task<int> cpu = Task.Run(() =>
        {
            int total = 0;

            for (int i = 0; i < 100_000; i++)
                total += i;

            return total;
        });

        Check.Equal(cpu.GetAwaiter().GetResult(), 704982704,
            "Task.Run envoie du calcul PUR sur le pool de threads : c'est le bon outil pour un pathfinding ou une generation de terrain, pas pour attendre un fichier");

        Check.True(Task.CompletedTask.IsCompletedSuccessfully,
            "et la regle qui vaut pour les deux moteurs : une tache ne revient PAS forcement sur le thread principal. Sans contexte de synchronisation, la suite d'un await tourne sur le thread du pool, et toucher a la scene depuis la fait exactement les degats de l'exercice precedent");
    }
}
