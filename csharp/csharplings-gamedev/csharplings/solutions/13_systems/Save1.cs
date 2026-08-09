using System.Globalization;

namespace Csharplings;

public sealed class SaveData
{
    public string PlayerName { get; set; } = "sans nom";
    public int Level { get; set; } = 1;
    public Vector2 Position { get; set; }
    public List<string> Flags { get; set; } = new();
}

public static class Save1
{
    public const bool NotDone = false;

    public static string Serialize(SaveData data)
    {
        return string.Join(";",
            $"name={data.PlayerName}",
            $"level={data.Level}",
            $"x={data.Position.X.ToString(CultureInfo.InvariantCulture)}",
            $"y={data.Position.Y.ToString(CultureInfo.InvariantCulture)}",
            $"flags={string.Join(",", data.Flags)}");
    }

    public static SaveData Deserialize(string text)
    {
        var fields = new Dictionary<string, string>();

        foreach (string pair in text.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = pair.IndexOf('=');

            if (separator < 0)
                throw new FormatException($"champ illisible : {pair}");

            fields[pair.Substring(0, separator)] = pair.Substring(separator + 1);
        }

        var data = new SaveData();

        if (fields.TryGetValue("name", out string name) && name.Length > 0)
            data.PlayerName = name;

        if (fields.TryGetValue("level", out string level) && int.TryParse(level, out int parsed))
            data.Level = parsed;

        data.Position = new Vector2(ReadFloat(fields, "x"), ReadFloat(fields, "y"));

        if (fields.TryGetValue("flags", out string flags))
            data.Flags = flags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        return data;
    }

    private static float ReadFloat(Dictionary<string, string> fields, string key)
    {
        if (fields.TryGetValue(key, out string raw)
            && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            return value;

        return 0f;
    }

    public static void Run()
    {
        var save = new SaveData
        {
            PlayerName = "anna",
            Level = 7,
            Position = new Vector2(12.5f, -40.25f),
            Flags = new List<string> { "boss", "cave" },
        };

        string text = Serialize(save);

        Check.True(text.Contains("name=anna"), "le nom part dans le fichier");
        Check.True(text.Contains("level=7"), "le niveau aussi");
        Check.True(text.Contains("12.5"), "et les decimales s'ecrivent avec un point, pas une virgule");

        SaveData loaded = Deserialize(text);

        Check.Equal(loaded.PlayerName, "anna", "on relit le nom");
        Check.Equal(loaded.Level, 7, "le niveau");
        Check.Near(loaded.Position, new Vector2(12.5f, -40.25f), "la position au pixel pres");
        Check.Sequence(loaded.Flags, new[] { "boss", "cave" }, "et la liste de drapeaux");

        Check.Equal(Serialize(loaded), text, "sauvegarder ce qu'on vient de charger doit redonner le meme texte");

        SaveData partial = Deserialize("level=3");

        Check.Equal(partial.Level, 3, "un fichier incomplet reste lisible");
        Check.Equal(partial.PlayerName, "sans nom", "les champs absents prennent leur valeur par defaut");
        Check.Near(partial.Position, Vector2.Zero, "y compris la position");
        Check.Equal(partial.Flags.Count, 0, "et la liste vide");

        SaveData empty = Deserialize("name=bob;flags=");

        Check.Equal(empty.Flags.Count, 0, "une liste vide n'est pas une liste avec un element vide");
        Check.Equal(empty.Level, 1, "et le niveau par defaut est 1");

        SaveData damaged = Deserialize("name=bob;level=beaucoup");

        Check.Equal(damaged.Level, 1, "un nombre illisible ne doit pas faire perdre la partie");
        Check.Equal(damaged.PlayerName, "bob", "le reste du fichier passe quand meme");

        Check.Throws<FormatException>(() => Deserialize("name=bob;jenesaispas"),
            "en revanche un champ sans '=' est une vraie corruption, on le signale");
    }
}
