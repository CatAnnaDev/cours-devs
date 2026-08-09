using System.Text.Json;
using System.Text.Json.Serialization;

namespace Csharplings;

public enum Rarity
{
    Common,
    Rare,
    Epic,
}

public sealed class LootEntry
{
    [JsonPropertyOrder(-1)]
    [JsonPropertyName("v")]
    public int Version { get; set; } = 3;

    public required string Identifier { get; set; }

    public Rarity Rarity { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Note { get; set; }

    public float DropChanceCache { get; set; }

    [JsonInclude]
    public int Count;

    public Dictionary<string, JsonElement> Unknown { get; set; }
}

public static class Attributes1
{
    public const bool NotDone = true;

    public static readonly JsonSerializerOptions Options =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string Write(LootEntry entry) => JsonSerializer.Serialize(entry, Options);

    public static LootEntry Read(string json) => JsonSerializer.Deserialize<LootEntry>(json, Options);

    public static void Run()
    {
        var entry = new LootEntry
        {
            Identifier = "epee_rouillee",
            Rarity = Rarity.Epic,
            Count = 2,
            DropChanceCache = 0.25f,
        };

        string json = Write(entry);

        Check.True(json.Contains("\"id\":\"epee_rouillee\""),
            "JsonPropertyName decouple le nom C# du nom dans le fichier : tu peux renommer la propriete sans casser une seule sauvegarde");

        Check.False(json.Contains("Identifier") || json.Contains("identifier"),
            "le nom C# ne doit apparaitre nulle part");

        Check.True(json.StartsWith("{\"v\":3"),
            "JsonPropertyOrder sort la version en premier, ce qui permet de la lire sans parcourir tout le fichier");

        Check.True(json.Contains("\"rarity\":\"Epic\""),
            "une rarete ecrite en TEXTE. Par defaut un enum part en nombre, et le jour ou tu inseres une valeur au milieu de l'enum, toutes les sauvegardes decalent d'un cran en silence");

        Check.False(json.Contains("DropChanceCache") || json.Contains("dropChanceCache"),
            "JsonIgnore garde les caches et les valeurs recalculables hors du fichier");

        Check.False(json.Contains("note"),
            "et JsonIgnore avec WhenWritingDefault n'ecrit le champ que s'il vaut autre chose que sa valeur par defaut : des fichiers deux fois plus courts");

        Check.True(json.Contains("\"count\":2"),
            "un champ marque JsonInclude part comme une propriete, politique de nommage comprise : dans le fichier, rien ne distingue plus un champ d'une propriete");

        Check.True(Write(new LootEntry { Identifier = "arc", Note = "du boss" }).Contains("\"note\":\"du boss\""),
            "des que la note vaut quelque chose, elle repart dans le fichier");

        Check.Throws<JsonException>(() => Read("{\"v\":3}"),
            "un 'required' absent du fichier est une erreur, pas un objet a moitie construit");

        const string future = "{\"v\":4,\"id\":\"arc\",\"rarity\":\"Rare\",\"skin\":\"dore\",\"enchant\":{\"feu\":3}}";

        LootEntry loaded = Read(future);

        Check.Equal(loaded.Identifier, "arc", "un fichier venu d'une version plus recente se relit");
        Check.Equal(loaded.Rarity, Rarity.Rare, "la rarete se relit depuis son nom");
        Check.Equal(loaded.Unknown.Count, 2,
            "et les deux champs que cette version ne connait pas atterrissent dans le dictionnaire JsonExtensionData au lieu d'etre jetes");
        Check.Equal(loaded.Unknown["skin"].GetString(), "dore", "on peut meme les lire");

        string again = Write(loaded);

        Check.True(again.Contains("\"skin\":\"dore\""),
            "et surtout ils repartent a l'ecriture : sans ca, un joueur qui lance une ancienne version perd son skin pour toujours");
        Check.True(again.Contains("\"enchant\":{\"feu\":3}"), "y compris les objets entiers");

        Check.Equal(Read(again).Unknown.Count, 2, "un aller-retour complet ne perd rien");

        Check.Equal(JsonSerializer.Serialize(Rarity.Epic, Options), "2",
            "sans convertisseur, la rarete n'est qu'un numero d'ordre dans l'enum");
    }
}
