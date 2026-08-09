using System.Text.Json;
using System.Text.Json.Serialization;

namespace Csharplings;

public sealed class Character
{
    public string Name { get; set; }

    public int Level { get; set; }

    public string Title { get; set; }
}

public static class JsonConvert
{
    private static readonly JsonSerializerOptions Settings = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public static string SerializeObject(object value) =>
        JsonSerializer.Serialize(value, value?.GetType() ?? typeof(object), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });

    public static string SerializeObject(object value, bool indented) =>
        JsonSerializer.Serialize(value, value?.GetType() ?? typeof(object), indented ? Indented : Settings);

    public static T DeserializeObject<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Settings);

    public static object DeserializeObject(string json, Type type) =>
        JsonSerializer.Deserialize(json, type, Settings);

    public static bool TryDeserializeObject<T>(string json, out T value)
    {
        value = JsonSerializer.Deserialize<T>(json, Settings);

        return value is not null;
    }
}

public static class Convert1
{
    public const bool NotDone = true;

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static string SerializeEachTime(object value) =>
        JsonSerializer.Serialize(value, value.GetType(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });

    public static void Run()
    {
        var hero = new Character { Name = "anna", Level = 7 };

        Check.Equal(JsonConvert.SerializeObject(hero), "{\"name\":\"anna\",\"level\":7}",
            "une facade : un seul endroit qui decide du nommage pour tout le jeu");

        Check.False(JsonConvert.SerializeObject(hero).Contains("title"),
            "et un seul endroit qui decide que les null ne partent pas dans le fichier");

        Check.Equal(JsonConvert.SerializeObject(hero, indented: true), "{\n  \"name\": \"anna\",\n  \"level\": 7\n}",
            "la variante indentee doit HERITER de la configuration de base : le constructeur de copie recopie tout, puis on change une chose");

        Check.Equal(JsonConvert.DeserializeObject<Character>("{\"name\":\"bob\",\"level\":3}").Name, "bob",
            "et la relecture passe par la meme configuration, sinon la casse ne tombe pas en face");

        Check.Equal(((Character)JsonConvert.DeserializeObject("{\"name\":\"bob\"}", typeof(Character))).Name, "bob",
            "la version non generique sert quand le type ne se connait qu'a l'execution");

        Check.Equal(JsonConvert.SerializeObject(null), "null", "et serialiser null ne doit pas planter");

        Check.True(JsonConvert.TryDeserializeObject("{\"name\":\"bob\"}", out Character parsed) && parsed.Name == "bob",
            "la version qui rend un bool, pour tout ce qui vient du disque ou du reseau");

        Check.False(JsonConvert.TryDeserializeObject("{\"name\":", out Character _),
            "un fichier corrompu rend false au lieu de faire remonter une exception jusqu'a la boucle de jeu");

        long shared = Measure(() => JsonConvert.SerializeObject(hero));
        long each = Measure(() => SerializeEachTime(hero));

        Check.True(each > shared * 3,
            $"des options RECREEES a chaque appel coutent {each} octets contre {shared} pour la facade, et c'est le cas favorable : elles sont identiques, donc le cache de metadonnees les reconnait");

        long distinct = MeasureDistinctConfigurations();

        Check.True(distinct > 5000,
            $"des qu'elles different vraiment, chaque configuration neuve reconstruit tout le cache de reflexion : {distinct} octets pour UNE. C'est pour ca que JsonSerializerOptions se declare static readonly, jamais dans la methode");

        var frozen = new JsonSerializerOptions();

        JsonSerializer.Serialize(hero, frozen);

        Check.Throws<InvalidOperationException>(() => frozen.WriteIndented = true,
            "et le premier usage GELE les options : les modifier apres coup leve une exception. Une variante se fabrique avec new JsonSerializerOptions(autre)");
    }

    private static long MeasureDistinctConfigurations()
    {
        const int count = 10;
        var hero = new Character { Name = "anna", Level = 7 };
        var configurations = new JsonSerializerOptions[count];

        for (int i = 0; i < count; i++)
            configurations[i] = new JsonSerializerOptions { MaxDepth = 20 + i, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        long before = GC.GetAllocatedBytesForCurrentThread();

        foreach (JsonSerializerOptions configuration in configurations)
            JsonSerializer.Serialize(hero, configuration);

        return (GC.GetAllocatedBytesForCurrentThread() - before) / count;
    }
}
