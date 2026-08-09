using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

namespace Csharplings;

public sealed class WorldEntity
{
    public int Id { get; set; }

    public int Health { get; set; }

    public float X { get; set; }

    public float Y { get; set; }
}

public static class Compare1
{
    public const bool NotDone = false;

    public const int EntitySize = 16;

    private static readonly JsonSerializerOptions Json = new();

    public static byte[] WriteBinary(List<WorldEntity> world)
    {
        var buffer = new byte[world.Count * EntitySize];
        Span<byte> cursor = buffer;

        foreach (WorldEntity entity in world)
        {
            BinaryPrimitives.WriteInt32LittleEndian(cursor, entity.Id);
            BinaryPrimitives.WriteInt32LittleEndian(cursor.Slice(4), entity.Health);
            BinaryPrimitives.WriteSingleLittleEndian(cursor.Slice(8), entity.X);
            BinaryPrimitives.WriteSingleLittleEndian(cursor.Slice(12), entity.Y);

            cursor = cursor.Slice(EntitySize);
        }

        return buffer;
    }

    public static List<WorldEntity> ReadBinary(ReadOnlySpan<byte> raw)
    {
        var world = new List<WorldEntity>(raw.Length / EntitySize);

        while (raw.Length >= EntitySize)
        {
            world.Add(new WorldEntity
            {
                Id = BinaryPrimitives.ReadInt32LittleEndian(raw),
                Health = BinaryPrimitives.ReadInt32LittleEndian(raw.Slice(4)),
                X = BinaryPrimitives.ReadSingleLittleEndian(raw.Slice(8)),
                Y = BinaryPrimitives.ReadSingleLittleEndian(raw.Slice(12)),
            });

            raw = raw.Slice(EntitySize);
        }

        return world;
    }

    public static byte[] WriteJson(List<WorldEntity> world) => JsonSerializer.SerializeToUtf8Bytes(world, Json);

    public static void Run()
    {
        var world = new List<WorldEntity>(200);

        for (int i = 0; i < 200; i++)
            world.Add(new WorldEntity { Id = i, Health = i % 100, X = i * 1.5f, Y = -i });

        byte[] binary = WriteBinary(world);
        byte[] json = WriteJson(world);

        Check.Equal(binary.Length, 200 * EntitySize, "seize octets par entite, sans un octet de decoration");

        Check.True(json.Length > binary.Length * 2,
            $"le meme monde en JSON pese {json.Length} octets contre {binary.Length}, soit deux fois et demie plus. Les noms de champs sont repetes DEUX CENTS fois, et chaque nombre redevient du texte");

        List<WorldEntity> back = ReadBinary(binary);

        Check.Equal(back.Count, 200, "la relecture rend les deux cents entites");
        Check.Equal(back[137].Health, 37, "avec leurs valeurs");
        Check.Equal(back[137].X, 205.5f, "et les flottants au BIT pres, parce qu'on a ecrit leurs octets au lieu de les convertir en texte");

        Check.Equal(back[0].Id, 0, "la premiere");
        Check.Equal(back[199].Id, 199, "et la derniere");

        Check.Equal(ReadBinary(binary.AsSpan(0, EntitySize * 3)).Count, 3,
            "un format a taille FIXE permet de lire les trois premieres entites sans toucher au reste, et de sauter directement a la centieme : offset = 100 fois seize");

        Check.Equal(ReadBinary(binary.AsSpan(0, EntitySize + 5)).Count, 1,
            "et un reste incomplet est ignore : c'est au format de dire combien d'entites il contient, jamais a la taille du fichier");

        Check.Equal(Encoding.UTF8.GetString(json).Count(character => character == '"'), 200 * 8,
            "huit guillemets par entite dans le JSON : quatre noms de champ, deux guillemets chacun. C'est exactement ce que le binaire ne paye pas");

        Check.True(binary.Length * 2 < json.Length, "et ce monde-la est le cas FAVORABLE au JSON : des noms de champs courts et des entiers ronds. Avec des noms lisibles et des flottants a sept decimales, le rapport monte a cinq ou six");

        Check.True(json.Length > 0,
            "et pourtant : le JSON se lit dans un editeur, se compare dans un diff git, se corrige a la main quand un designer s'est trompe, et survit a un champ ajoute au milieu. Le binaire, non : il faut un outil, et il faut versionner le format comme dans version1");

        Check.Equal(EntitySize, 16,
            "la reponse d'un vrai jeu n'est donc pas 'l'un ou l'autre' : JSON pour ce qu'un humain edite - reglages, tables d'objets, dialogues - et binaire pour ce qu'une machine ecrit en masse : sauvegardes, replays, paquets reseau, terrain");
    }
}
