namespace Csharplings;

public static class Cancel1
{
    public const bool NotDone = true;

    public static int Steps;

    public static int LoadChunks(int count, CancellationToken token)
    {
        Steps = 0;

        for (int i = 0; i < count; i++)
        {
            Steps++;
        }

        return Steps;
    }

    public static int LoadPolitely(int count, CancellationToken token)
    {
        Steps = 0;

        for (int i = 0; i < count; i++)
        {
            Steps++;
        }

        return Steps;
    }

    public static void Run()
    {
        using var source = new CancellationTokenSource();

        Check.Equal(LoadChunks(10, source.Token), 10, "sans annulation, le chargement va au bout");

        source.Cancel();

        Check.True(source.IsCancellationRequested, "un jeton annule le reste pour toujours : on n'annule pas un CancellationTokenSource a moitie");

        Check.Throws<OperationCanceledException>(() => LoadChunks(10, source.Token),
            "ThrowIfCancellationRequested transforme l'annulation en exception : c'est le style quand l'appelant doit SAVOIR que le travail est incomplet");

        Check.Equal(LoadPolitely(10, source.Token), 0,
            "le style 'poli' rend simplement ce qui a ete fait : c'est le bon choix quand un resultat partiel a un sens, par exemple des chunks deja charges");

        using var second = new CancellationTokenSource();
        int cleanups = 0;

        using (second.Token.Register(() => cleanups++))
        {
            Check.Equal(cleanups, 0, "Register pose un rappel de nettoyage");

            second.Cancel();

            Check.Equal(cleanups, 1, "declenche AU MOMENT de l'annulation : de quoi fermer un fichier ou rendre un tampon sans attendre que la boucle s'en apercoive");
        }

        using var timed = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

        Thread.Sleep(60);

        Check.True(timed.IsCancellationRequested,
            "un delai transforme le meme jeton en delai d'expiration : une seule mecanique pour 'le joueur a quitte la scene' et pour 'le serveur ne repond pas'");

        using var manual = new CancellationTokenSource();
        using var lifetime = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(manual.Token, lifetime.Token);

        Check.False(linked.IsCancellationRequested, "un jeton LIE ecoute plusieurs sources");

        lifetime.Cancel();

        Check.True(linked.IsCancellationRequested,
            "et s'annule des que l'une d'elles s'annule : c'est comme ca qu'un chargement s'arrete quand le joueur annule OU quand la scene est detruite");

        Check.False(manual.IsCancellationRequested, "sans toucher aux sources d'origine");

        var cooperative = new CancellationTokenSource();
        int producedBeforeStop = 0;

        var worker = new Thread(() =>
        {
            while (!cooperative.Token.IsCancellationRequested && producedBeforeStop < 1_000_000)
                producedBeforeStop++;
        });

        worker.Start();
        cooperative.Cancel();
        worker.Join();

        Check.True(producedBeforeStop >= 0,
            "et le mot le plus important de tout ceci est COOPERATIF : personne ne tue le thread. Il n'existe aucune facon sure d'interrompre un thread de force, parce qu'on l'arreterait au milieu d'une ecriture. Le thread doit REGARDER son jeton");

        cooperative.Dispose();

        Check.True(true,
            "un CancellationTokenSource s'appelle Dispose : il tient un minuteur et des rappels. En fabriquer un par requete sans le liberer est une fuite lente, du genre qui se voit au bout de deux heures de jeu");
    }
}
