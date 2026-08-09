using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Csharplings;

public sealed class EnemyState
{
    public string Name { get; set; }

    public int Health { get; set; }

    public float X { get; set; }

    public float Y { get; set; }
}

public static class Perf1
{
    public const bool NotDone = true;

    private static readonly JsonSerializerOptions Options = new();

    private static readonly ArrayBufferWriter<byte> Buffer = new(64 * 1024);

    private static readonly Utf8JsonWriter Writer = new(Buffer);

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static string WriteString(List<EnemyState> world) =>
        JsonSerializer.Serialize(world, Options);

    public static byte[] WriteBytes(List<EnemyState> world) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(world, Options));

    public static int WriteReused(List<EnemyState> world)
    {
        Buffer.Clear();

        using var writer = new Utf8JsonWriter(Buffer);

        JsonSerializer.Serialize(writer, world, Options);

        return Buffer.WrittenCount;
    }

    public static int WriteByHand(EnemyState enemy)
    {
        Buffer.Clear();
        Writer.Reset(Buffer);

        Writer.WriteStartObject();
        Writer.WriteString("Name", enemy.Name);
        Writer.WriteNumber("Health", enemy.Health);
        Writer.WriteNumber("X", enemy.X);
        Writer.WriteNumber("Y", enemy.Y);
        Writer.WriteEndObject();
        Writer.Flush();

        return Buffer.WrittenCount;
    }

    public static int CountAlive(byte[] utf8)
    {
        using JsonDocument document = JsonDocument.Parse(utf8);

        return document.RootElement.EnumerateArray().Count(enemy => enemy.GetProperty("Health").GetInt32() > 0);
    }

    public static void Run()
    {
        var world = new List<EnemyState>();

        for (int i = 0; i < 200; i++)
            world.Add(new EnemyState { Name = "gobelin" + i, Health = i % 3 == 0 ? 0 : 10, X = i * 10f, Y = 0f });

        string text = WriteString(world);
        byte[] bytes = WriteBytes(world);

        Check.Equal(bytes.Length, Encoding.UTF8.GetByteCount(text), "les trois ecritures produisent exactement le meme JSON");
        Check.Equal(WriteReused(world), bytes.Length, "au dernier octet pres");

        long viaString = Measure(() => WriteString(world));
        long viaBytes = Measure(() => WriteBytes(world));
        long viaWriter = Measure(() => WriteReused(world));

        Check.True(viaString > viaBytes,
            $"une string coute {viaString} octets pour un fichier de {bytes.Length} : le JSON est fabrique en UTF-8 puis retranscrit en UTF-16, donc paye deux fois. SerializeToUtf8Bytes doit SAUTER cette etape, pas la refaire ensuite");

        Check.True(viaBytes > viaWriter * 5,
            $"SerializeToUtf8Bytes tombe a {viaBytes} en sautant l'etape texte, mais alloue encore le tableau de sortie a chaque appel");

        Check.True(viaWriter < 1000,
            $"un tampon et un writer REUTILISES tombent a {viaWriter} octets. Soixante fois par seconde, c'est la difference entre 800 kilooctets et 19 : la premiere version declenche un ramassage de generation 0 toutes les deux ou trois secondes");

        Check.Equal(Measure(() => WriteByHand(world[0])), 0L,
            "et une ecriture a la main sur un writer reutilise n'alloue RIEN du tout : c'est le plancher, celui qu'on vise pour de la telemetrie ou un replay envoye a chaque image");

        Check.Equal(CountAlive(bytes), 133, "en lecture aussi il y a un plancher : compter sans construire un seul objet");

        Check.Equal(Measure(() => CountAlive(bytes)), 0L,
            "zero octet. Utf8JsonReader est un struct qui avance dans les octets, ValueTextEquals compare sans decoder");

        Check.True(Measure(() => JsonSerializer.Deserialize<List<EnemyState>>(bytes, Options)) > 10_000L,
            "la desserialisation complete, elle, construit 200 objets et 200 chaines : c'est le prix du confort, et il se paie une fois au chargement, pas a chaque image");

        var indented = new JsonSerializerOptions { WriteIndented = true };

        Check.True(JsonSerializer.Serialize(world, indented).Length > text.Length * 3 / 2,
            "et l'indentation ajoute la moitie de la taille du fichier en espaces et en retours a la ligne : lisible pendant le developpement, jamais dans ce que tu livres");
    }
}
