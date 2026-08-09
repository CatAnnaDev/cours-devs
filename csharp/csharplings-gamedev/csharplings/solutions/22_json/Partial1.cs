using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Csharplings;

public static class Partial1
{
    public const bool NotDone = false;

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static int ReadVersion(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        return document.RootElement.TryGetProperty("version", out JsonElement version) ? version.GetInt32() : 1;
    }

    public static string Retarget(string json, string zone)
    {
        JsonNode root = JsonNode.Parse(json);

        root["zone"] = zone;

        return root.ToJsonString();
    }

    public static int ScanLevel(byte[] utf8)
    {
        var reader = new Utf8JsonReader(utf8);

        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals("level"))
                continue;

            reader.Read();

            return reader.GetInt32();
        }

        return 0;
    }

    public static JsonElement Escaping(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        return document.RootElement;
    }

    public static JsonElement Snapshot(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        return document.RootElement.Clone();
    }

    public static void Run()
    {
        const string save = "{\"version\":2,\"zone\":\"cave\",\"level\":7,\"inventory\":[{\"id\":\"epee\"},{\"id\":\"cle\"}]}";

        Check.Equal(ReadVersion(save), 2, "lire UN champ sans construire l'objet complet");
        Check.Equal(ReadVersion("{\"zone\":\"cave\"}"), 1,
            "et TryGetProperty pour le champ absent : une sauvegarde d'avant le numero de version est une version 1, pas une exception");

        using (JsonDocument document = JsonDocument.Parse(save))
        {
            JsonElement root = document.RootElement;

            Check.Equal(root.GetProperty("zone").GetString(), "cave", "un JsonElement se lit sans classe en face");
            Check.Equal(root.GetProperty("version").ValueKind, JsonValueKind.Number, "et il sait dire ce qu'il contient");
            Check.Equal(root.GetProperty("inventory").GetArrayLength(), 2, "compter un tableau ne demande pas de le materialiser");
            Check.Equal(root.GetProperty("inventory")[0].GetProperty("id").GetString(), "epee", "on descend a l'indice voulu et pas plus loin");
            Check.Equal(root.GetProperty("inventory").GetRawText(), "[{\"id\":\"epee\"},{\"id\":\"cle\"}]",
                "GetRawText rend le morceau tel quel, de quoi le recopier ailleurs sans le comprendre");
            Check.Sequence(root.EnumerateObject().Select(property => property.Name), new[] { "version", "zone", "level", "inventory" },
                "et on peut parcourir les noms sans savoir a l'avance ce qu'il y a dedans");
            Check.Throws<KeyNotFoundException>(() => root.GetProperty("absent"),
                "GetProperty sur un champ absent leve KeyNotFoundException, pas JsonException : ce n'est plus le serialiseur qui parle");
        }

        Check.Throws<ObjectDisposedException>(() => Escaping(save).GetProperty("version").GetInt32(),
            "un JsonElement rendu depuis l'interieur du using est MORT : il ne contient pas les donnees, il pointe dedans, et le tampon est deja reparti au pool");

        Check.Equal(Snapshot(save).GetProperty("version").GetInt32(), 2,
            "Clone() en fait une copie autonome : c'est la seule facon de faire sortir un morceau de JSON de son using");

        string moved = Retarget(save, "surface");

        Check.True(moved.Contains("\"zone\":\"surface\""), "JsonNode sert quand il faut MODIFIER : on change un champ");
        Check.True(moved.Contains("\"inventory\":[{\"id\":\"epee\"},{\"id\":\"cle\"}]"),
            "et tout le reste ressort intact, y compris ce que le jeu ne sait pas interpreter");
        Check.Equal(ReadVersion(moved), 2, "le fichier reste un fichier");

        byte[] utf8 = Encoding.UTF8.GetBytes(save);

        Check.Equal(ScanLevel(utf8), 7, "le lecteur bas niveau retrouve le meme champ");

        Check.Equal(Measure(() => ScanLevel(utf8)), 0L,
            "en ZERO octet : Utf8JsonReader est un struct qui avance dans les octets sans rien construire. C'est ce qu'on utilise quand on doit trier mille fichiers de sauvegarde pour afficher un menu");

        Check.True(Measure(() => ReadVersion(save)) > 0L,
            "alors que JsonDocument construit un index de tout le document, meme pour n'en lire qu'un champ");

        Check.True(Measure(() => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(save)) > Measure(() => ReadVersion(save)),
            "et le serialiseur complet coute encore plus cher : trois outils, du plus economique au plus confortable");
    }
}
