using System.Text.Json;
using System.Text.Json.Serialization;

namespace Csharplings;

public readonly struct Cell
{
    public int Column { get; }

    public int Row { get; }

    public Cell(int column, int row)
    {
        Column = column;
        Row = row;
    }

    public override string ToString() => $"({Column}, {Row})";
}

public sealed class PatrolPath
{
    public string Name { get; set; }

    public List<Vector2> Points { get; set; } = new();
}

public sealed class Vector2Converter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        reader.Read();
        float x = reader.GetSingle();

        reader.Read();
        float y = reader.GetSingle();

        reader.Read();

        if (reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("un Vector2 a exactement deux composantes");

        return new Vector2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteEndObject();
    }
}

public static class Custom1
{
    public const bool NotDone = true;

    public static readonly JsonSerializerOptions Plain = new();

    public static readonly JsonSerializerOptions Options = new() { Converters = { new Vector2Converter() } };

    public static void Run()
    {
        var position = new Vector2(1.5f, -2f);

        Check.Equal(JsonSerializer.Serialize(position, Plain), "{\"X\":1.5,\"Y\":-2}",
            "sans convertisseur, un Vector2 s'ecrit tres bien : le fichier a l'air parfait");

        Check.Equal(JsonSerializer.Deserialize<Vector2>("{\"X\":1.5,\"Y\":-2}", Plain), Vector2.Zero,
            "et il se relit a ZERO, sans une seule erreur. Un struct readonly n'a que des proprietes en lecture seule : le serialiseur construit le struct vide et n'a aucun moyen de le remplir");

        Check.Equal(JsonSerializer.Serialize(position, Options), "[1.5,-2]",
            "le convertisseur choisit la forme : deux nombres dans un tableau, moitie moins de caracteres");

        Check.Equal(JsonSerializer.Deserialize<Vector2>("[1.5,-2]", Options), position,
            "et il sait relire ce qu'il a ecrit, ce qui est le seul but de l'operation");

        Check.Equal(JsonSerializer.Serialize(position, Options).Length * 2, JsonSerializer.Serialize(position, Plain).Length,
            "exactement deux fois moins de place. Sur dix mille points de navigation, ce sont des centaines de kilooctets");

        var path = new PatrolPath
        {
            Name = "ronde",
            Points = { new Vector2(0f, 0f), new Vector2(10f, 0f), new Vector2(10f, 10f) },
        };

        Check.Equal(JsonSerializer.Serialize(path, Options), "{\"Name\":\"ronde\",\"Points\":[[0,0],[10,0],[10,10]]}",
            "un convertisseur s'applique partout ou le type apparait, y compris au fond d'une liste");

        PatrolPath reloaded = JsonSerializer.Deserialize<PatrolPath>(JsonSerializer.Serialize(path, Options), Options);

        Check.Equal(reloaded.Points.Count, 3, "les trois points reviennent");
        Check.Near(reloaded.Points[2], new Vector2(10f, 10f), "et le dernier est au bon endroit");

        Check.Throws<JsonException>(() => JsonSerializer.Deserialize<Vector2>("{\"X\":1,\"Y\":2}", Options),
            "un convertisseur doit VERIFIER ce qu'il lit : l'ancien format doit lever une JsonException, pas une exception de bas niveau venue du lecteur");

        Check.Throws<JsonException>(() => JsonSerializer.Deserialize<Vector2>("[1,2,3]", Options),
            "et trois composantes ne sont pas un Vector2 : sans le controle de fin de tableau, le lecteur se retrouve desynchronise au milieu du fichier");

        Check.Equal(JsonSerializer.Deserialize<Cell>("{\"Column\":3,\"Row\":4}", Plain), new Cell(3, 4),
            "pour TES propres structs il y a plus simple : JsonConstructor designe le constructeur a utiliser, et les parametres se branchent sur les proprietes de meme nom");

        Check.Equal(JsonSerializer.Serialize(new Cell(3, 4), Plain), "{\"Column\":3,\"Row\":4}",
            "sans rien changer a l'ecriture");

        Check.Equal(JsonSerializer.Serialize(new Dictionary<string, Vector2> { ["spawn"] = position }, Options),
            "{\"spawn\":[1.5,-2]}",
            "et un Vector2 en VALEUR de dictionnaire passe par le convertisseur comme le reste");
    }
}
