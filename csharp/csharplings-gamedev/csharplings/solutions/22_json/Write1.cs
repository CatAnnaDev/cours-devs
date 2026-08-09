using System.Text.Json;
using System.Text.Json.Serialization;

namespace Csharplings;

public sealed class Coin
{
    public string Kind { get; set; }

    public int Count { get; set; }
}

public sealed class Hero
{
    public string Name { get; set; } = "sans nom";

    public int Level { get; set; } = 1;

    [JsonInclude]
    public int Gold;

    private string Password { get; set; } = "motdepasse";

    public int NextLevelCost => Level * 100;
}

public static class Write1
{
    public const bool NotDone = false;

    public static readonly JsonSerializerOptions Compact = new();

    public static readonly JsonSerializerOptions Readable = new() { WriteIndented = true };

    public static readonly JsonSerializerOptions Web = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string Write(Hero hero) => JsonSerializer.Serialize(hero, Compact);

    public static string WriteReadable(Hero hero) => JsonSerializer.Serialize(hero, Readable);

    public static string WriteWeb(Hero hero) => JsonSerializer.Serialize(hero, Web);

    public static void Run()
    {
        Check.Equal(JsonSerializer.Serialize(new Coin { Kind = "or", Count = 3 }, Compact),
            "{\"Kind\":\"or\",\"Count\":3}",
            "un objet devient une paire d'accolades, une propriete par champ, dans l'ordre de declaration");

        var hero = new Hero { Name = "anna", Level = 7, Gold = 300 };
        string json = Write(hero);

        Check.True(json.Contains("\"Name\":\"anna\""), "une propriete publique part dans le fichier");

        Check.True(json.Contains("\"Gold\":300"),
            "l'or est un CHAMP et pas une propriete : par defaut il ne part PAS, et la sauvegarde perd l'or du joueur sans une seule erreur");

        Check.False(json.Contains("Password"),
            "le prive ne sort jamais, meme quand c'est une propriete");

        Check.True(json.Contains("\"NextLevelCost\":700"),
            "une propriete calculee part, elle : du poids en plus pour une valeur qu'on sait recalculer, et qui sera ignoree a la relecture");

        string readable = WriteReadable(hero);

        Check.True(readable.Contains("\n"), "la version lisible met une propriete par ligne");
        Check.True(readable.Contains("  \"Name\": \"anna\""), "avec deux espaces d'indentation et un espace apres les deux-points");
        Check.True(readable.Length > json.Length,
            "elle est plus longue : lisible pendant le developpement, compacte pour le joueur");

        string web = WriteWeb(hero);

        Check.True(web.Contains("\"name\":\"anna\""), "une politique de nommage renomme TOUTES les proprietes d'un coup");
        Check.False(web.Contains("\"Name\""), "et l'ancien nom disparait, ce qui casse la relecture si les deux cotes ne sont pas d'accord");

        Check.Equal(JsonSerializer.Serialize(new[] { 1, 2, 3 }, Compact), "[1,2,3]",
            "un tableau devient des crochets");

        Check.Equal(JsonSerializer.Serialize(new List<string> { "boss", "cave" }, Compact), "[\"boss\",\"cave\"]",
            "une List aussi : le JSON ne fait pas la difference");

        Check.Equal(JsonSerializer.Serialize(new Dictionary<string, int> { ["potion"] = 3 }, Compact), "{\"potion\":3}",
            "un Dictionary a cle texte devient un objet, ses cles deviennent des noms de propriete");

        Check.Equal(JsonSerializer.Serialize(new Dictionary<int, string> { [7] = "arc" }, Compact), "{\"7\":\"arc\"}",
            "et une cle entiere devient un nom de propriete, donc du TEXTE : un nom de propriete JSON est toujours une chaine");

        Hero absent = null;

        Check.Equal(JsonSerializer.Serialize(absent, Compact), "null",
            "serialiser null donne le texte null, pas une chaine vide et pas une exception");

        Check.Equal(JsonSerializer.Serialize(1.5f, Compact), "1.5",
            "un flottant s'ecrit avec un POINT, sur la machine du joueur comme sur la tienne : le JSON n'a pas de langue");

        Check.Equal(JsonSerializer.Serialize(1f / 3f, Compact), "0.33333334",
            "et il ecrit le plus court texte qui redonne exactement le meme float : ni plus, ni moins");
    }
}
