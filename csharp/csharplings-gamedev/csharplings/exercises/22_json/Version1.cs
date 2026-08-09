using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Csharplings;

public enum Difficulty
{
    Easy,
    Normal,
    Hard,
}

public sealed class Health
{
    public int Current { get; set; }

    public int Max { get; set; } = 100;
}

public sealed class SaveFile
{
    public int Version { get; set; } = Version1.CurrentVersion;

    public string Name { get; set; } = "sans nom";

    public Health Health { get; set; } = new();

    [JsonConverter(typeof(JsonStringEnumConverter<Difficulty>))]
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Unknown { get; set; }
}

public static class Version1
{
    public const bool NotDone = true;

    public const int CurrentVersion = 3;

    public static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static int VersionOf(JsonNode root) =>
        root["version"] is JsonNode version ? version.GetValue<int>() : 0;

    public static JsonNode ToVersion2(JsonNode root)
    {
        root["health"] = new JsonObject
        {
            ["current"] = root["hp"].GetValue<int>(),
            ["max"] = 100,
        };

        root["version"] = 2;

        return root;
    }

    public static JsonNode ToVersion3(JsonNode root)
    {
        root["difficulty"] = Difficulty.Normal.ToString();
        root["version"] = 3;

        return root;
    }

    public static SaveFile Load(string json)
    {
        JsonNode root = JsonNode.Parse(json);

        if (VersionOf(root) < 2)
            root = ToVersion2(root);

        return root.Deserialize<SaveFile>(Options);
    }

    public static string Save(SaveFile file)
    {
        file.Version = CurrentVersion;

        return JsonSerializer.Serialize(file, Options);
    }

    public static void Run()
    {
        const string v1 = "{\"name\":\"anna\",\"hp\":50}";
        const string v2 = "{\"version\":2,\"name\":\"anna\",\"health\":{\"current\":50,\"max\":80}}";
        const string v3 = "{\"version\":3,\"name\":\"anna\",\"health\":{\"current\":50,\"max\":80},\"difficulty\":\"Hard\"}";

        Check.Equal(VersionOf(JsonNode.Parse(v1)), 1,
            "un fichier sans champ version EST une version 1 : c'est la premiere decision a prendre, et elle se prend une seule fois");

        Check.Equal(VersionOf(JsonNode.Parse(v3)), 3, "les suivants le disent eux-memes");

        SaveFile fromV1 = Load(v1);

        Check.Equal(fromV1.Name, "anna", "un fichier de la toute premiere version se charge encore");
        Check.Equal(fromV1.Health.Current, 50, "les points de vie ont survecu au changement de forme : un entier est devenu un objet");
        Check.Equal(fromV1.Health.Max, 100, "avec le maximum que cette version-la n'avait pas et qu'on remplit a la migration");
        Check.Equal(fromV1.Difficulty, Difficulty.Normal, "et la difficulte ajoutee en version 3, a sa valeur par defaut");
        Check.Equal(fromV1.Version, CurrentVersion,
            "le fichier charge est TOUJOURS a la version courante : le reste du jeu n'a jamais a savoir d'ou il vient");

        Check.True(fromV1.Unknown is null || fromV1.Unknown.Count == 0,
            "et l'ancien champ 'hp' a bien ete RETIRE : une migration qui ajoute sans enlever traine ses vieux champs de version en version, pour toujours");

        SaveFile fromV2 = Load(v2);

        Check.Equal(fromV2.Health.Max, 80,
            "une version 2 saute la premiere migration : son maximum a elle est conserve, la migration ne repasse pas dessus");
        Check.Equal(fromV2.Difficulty, Difficulty.Normal, "et ne subit que celle qui la separe de la version courante");

        SaveFile fromV3 = Load(v3);

        Check.Equal(fromV3.Difficulty, Difficulty.Hard, "une version courante traverse sans etre touchee");
        Check.Equal(fromV3.Health.Max, 80, "rien n'est ecrase");

        string written = Save(fromV1);

        Check.True(written.Contains("\"version\":3"), "ce qu'on reecrit porte le numero de la version courante");
        Check.True(written.Contains("\"difficulty\":\"Normal\""),
            "et la difficulte part en TEXTE : un enum ecrit en nombre se decale des qu'on insere une valeur au milieu");

        Check.Equal(Load(written).Health.Current, 50, "et ce fichier-la se recharge sans passer par aucune migration");
        Check.Equal(Save(Load(written)), written, "un fichier deja a jour est un point fixe : le recharger et le reecrire ne le change plus");

        const string future = "{\"version\":4,\"name\":\"anna\",\"health\":{\"current\":50,\"max\":80},\"difficulty\":\"Hard\",\"pet\":\"renard\"}";

        SaveFile fromFuture = Load(future);

        Check.Equal(fromFuture.Name, "anna", "un fichier venu d'une version PLUS RECENTE se charge quand meme");
        Check.Equal(fromFuture.Unknown["pet"].GetString(), "renard", "et son champ inconnu est mis de cote au lieu d'etre jete");
        Check.True(Save(fromFuture).Contains("\"pet\":\"renard\""),
            "puis reecrit : le joueur qui revient a la version d'apres retrouve son renard. Sans ca, lancer une vieille version une seule fois detruit la sauvegarde");

        Check.Throws<JsonException>(() => Load("{\"version\":3,\"health\":\"beaucoup\"}"),
            "en revanche un fichier de la bonne version mais au contenu impossible doit echouer franchement, pas rendre un objet a moitie rempli");
    }
}
