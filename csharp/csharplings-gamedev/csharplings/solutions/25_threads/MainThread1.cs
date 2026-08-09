using System.Collections.Concurrent;

namespace Csharplings;

public sealed class MainThreadQueue
{
    private readonly ConcurrentQueue<Action> _pending = new();

    public int PendingCount => _pending.Count;

    public void Post(Action work) => _pending.Enqueue(work);

    public int DrainAll()
    {
        int done = 0;

        while (_pending.TryDequeue(out Action work))
        {
            work();
            done++;
        }

        return done;
    }

    public int Drain(int budget)
    {
        int done = 0;

        while (done < budget && _pending.TryDequeue(out Action work))
        {
            work();
            done++;
        }

        return done;
    }
}

public static class MainThread1
{
    public const bool NotDone = false;

    public static int MainThreadId { get; private set; }

    public static List<string> Spawned { get; } = new();

    public static void Run()
    {
        MainThreadId = Environment.CurrentManagedThreadId;

        var queue = new MainThreadQueue();
        var worker = new Thread(() =>
        {
            for (int i = 0; i < 5; i++)
            {
                string name = "gobelin" + i;

                queue.Post(() => Spawned.Add(name));
            }
        });

        worker.Start();
        worker.Join();

        Check.Equal(queue.PendingCount, 5, "le thread de calcul n'a rien fait d'autre que DEPOSER du travail");
        Check.Equal(Spawned.Count, 0, "il n'a touche a aucune donnee de jeu : rien n'a encore ete cree");

        int applied = queue.Drain(budget: 2);

        Check.Equal(applied, 2, "le thread principal vide la file a son rythme");
        Check.Equal(Spawned.Count, 2, "et c'est LUI qui cree les objets, donc c'est lui qui touche a la scene");
        Check.Equal(queue.PendingCount, 3, "le reste attend la frame suivante");

        Check.Equal(queue.DrainAll(), 3, "un budget par image evite qu'un gros lot de resultats ne fasse un pic de 40 millisecondes");
        Check.Equal(Spawned.Count, 5, "les cinq ennemis finissent par arriver");
        Check.Sequence(Spawned, new[] { "gobelin0", "gobelin1", "gobelin2", "gobelin3", "gobelin4" },
            "et dans l'ordre : une ConcurrentQueue garde l'ordre d'insertion");

        int observedOnWorker = 0;

        var checker = new Thread(() => observedOnWorker = Environment.CurrentManagedThreadId);

        checker.Start();
        checker.Join();

        Check.True(observedOnWorker != MainThreadId,
            "voila pourquoi tout ceci existe : le thread de calcul n'EST pas le thread principal");

        Check.Equal(Environment.CurrentManagedThreadId, MainThreadId,
            "et l'API d'un moteur - creer un noeud, changer une position, jouer un son - n'est utilisable QUE depuis le thread principal. Godot et Unity ne le verifient pas toujours : parfois ca marche, parfois ca corrompt la scene, parfois ca fait tomber le processus");

        var counters = new ConcurrentDictionary<string, int>();
        var writers = new Thread[4];

        for (int i = 0; i < writers.Length; i++)
        {
            writers[i] = new Thread(() =>
            {
                for (int step = 0; step < 1000; step++)
                    counters.AddOrUpdate("kills", 1, (_, current) => current + 1);
            });
        }

        foreach (Thread writer in writers)
            writer.Start();

        foreach (Thread writer in writers)
            writer.Join();

        Check.Equal(counters["kills"], 4000,
            "les collections concurrentes existent aussi : AddOrUpdate est atomique, la ou un Dictionary partage se corrompt");

        Check.True(counters.TryGetValue("kills", out int kills) && kills == 4000,
            "elles coutent plus cher qu'une collection ordinaire, et c'est le bon compromis quand plusieurs threads ecrivent VRAIMENT au meme endroit. Quand ce n'est pas le cas, une tranche par thread reste plus rapide");
    }
}
