using System.Threading.Channels;

namespace Csharplings;

public sealed record Chunk(int Index, int Cost);

public static class Pipeline1
{
    public const bool NotDone = false;

    public static Channel<Chunk> CreateBounded(int capacity) =>
        Channel.CreateBounded<Chunk>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
        });

    public static Channel<Chunk> CreateDropping(int capacity) =>
        Channel.CreateBounded<Chunk>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
        });

    public static async Task<int> ProduceAsync(ChannelWriter<Chunk> writer, int count)
    {
        for (int i = 0; i < count; i++)
            await writer.WriteAsync(new Chunk(i, i * 2));

        writer.Complete();

        return count;
    }

    public static async Task<List<int>> ConsumeAsync(ChannelReader<Chunk> reader)
    {
        var seen = new List<int>();

        await foreach (Chunk chunk in reader.ReadAllAsync())
            seen.Add(chunk.Index);

        return seen;
    }

    public static void Run()
    {
        Channel<Chunk> channel = CreateBounded(2);

        Task<int> produced = ProduceAsync(channel.Writer, 6);
        Task<List<int>> consumed = ConsumeAsync(channel.Reader);

        bool finished = Task.WaitAll(new Task[] { produced, consumed }, TimeSpan.FromSeconds(3));

        Check.True(finished,
            "le consommateur doit SORTIR de sa boucle. Il n'en sort que si le producteur appelle Complete() : sans ca il attend pour toujours, et le jeu se fige a la fin du chargement. C'est le bug le plus difficile a diagnostiquer de toute la section");

        Check.Equal(produced.Result, 6, "six chunks produits");
        Check.Sequence(consumed.Result, new[] { 0, 1, 2, 3, 4, 5 },
            "six chunks consommes, dans l'ordre, alors que la file n'en tient que DEUX a la fois");

        Check.True(channel.Reader.Completion.IsCompleted, "et le canal se sait termine");

        Channel<Chunk> full = CreateBounded(2);

        Check.True(full.Writer.TryWrite(new Chunk(0, 0)), "une file bornee accepte tant qu'il reste de la place");
        Check.True(full.Writer.TryWrite(new Chunk(1, 0)), "deux places, deux ecritures");
        Check.False(full.Writer.TryWrite(new Chunk(2, 0)),
            "et la troisieme est REFUSEE. C'est le point de toute la mecanique : le producteur ne peut pas prendre trente secondes d'avance et remplir la memoire pendant que le thread principal peine a suivre");

        Check.True(full.Reader.TryRead(out Chunk first) && first.Index == 0, "on lit dans l'ordre d'arrivee");
        Check.True(full.Writer.TryWrite(new Chunk(2, 0)), "et une place liberee rouvre l'ecriture");

        Channel<Chunk> dropping = CreateDropping(2);

        dropping.Writer.TryWrite(new Chunk(0, 0));
        dropping.Writer.TryWrite(new Chunk(1, 0));
        dropping.Writer.TryWrite(new Chunk(2, 0));

        Check.True(dropping.Reader.TryRead(out Chunk kept) && kept.Index == 1,
            "l'autre politique JETTE le plus ancien au lieu d'attendre : c'est ce qu'on veut pour des positions reseau ou un flux de telemetrie, ou la donnee perimee ne vaut rien");

        Check.Equal(dropping.Reader.Count, 1, "il reste le plus recent des deux");

        Channel<Chunk> unbounded = Channel.CreateUnbounded<Chunk>();

        for (int i = 0; i < 1000; i++)
            unbounded.Writer.TryWrite(new Chunk(i, 0));

        Check.Equal(unbounded.Reader.Count, 1000,
            "une file NON bornee accepte tout, et c'est justement ce qui la rend dangereuse : un producteur plus rapide que le consommateur la fait grossir jusqu'a la fin de la memoire");

        unbounded.Writer.Complete();

        Check.False(unbounded.Writer.TryWrite(new Chunk(0, 0)),
            "apres Complete, plus une seule ecriture ne passe : la fermeture est definitive, comme une annulation");

        Check.Equal(unbounded.Reader.Count, 1000, "mais ce qui est deja dedans reste lisible jusqu'au bout");
    }
}
