using System.Text.Json;
using System.Text.Json.Serialization;

namespace Csharplings;

public readonly record struct Stat<T>(string Key, T Value);

public sealed class StatConverter<T> : JsonConverter<Stat<T>>
{
    public override Stat<T> Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        string raw = reader.GetString();
        int separator = raw.IndexOf('=');

        return new Stat<T>(
            raw.Substring(0, separator),
            JsonSerializer.Deserialize<T>(raw.Substring(separator + 1), options));
    }

    public override void Write(Utf8JsonWriter writer, Stat<T> value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Key + "=" + value.Value);
}

public sealed class StatConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type type) => type == typeof(Stat<int>);

    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(
            typeof(StatConverter<>).MakeGenericType(type.GetGenericArguments()[0]));
}

public static class Generic2
{
    public const bool NotDone = true;

    public static readonly JsonSerializerOptions Plain = new();

    public static readonly JsonSerializerOptions Options = new() { Converters = { new StatConverterFactory() } };

    public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T Read<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);

    public static void Run()
    {
        Check.True(typeof(Stat<>).IsGenericTypeDefinition,
            "Stat<> sans argument est un TYPE OUVERT : un moule, pas un type. On ne peut pas en fabriquer d'instance");

        Check.False(typeof(Stat<int>).IsGenericTypeDefinition, "Stat<int> est ferme : un vrai type, avec sa propre table de methodes");

        Check.Equal(typeof(Stat<int>).GetGenericTypeDefinition(), typeof(Stat<>),
            "remonter du ferme a l'ouvert, c'est ce qui permet de reconnaitre TOUS les Stat d'un coup");

        Check.Equal(typeof(Stat<>).MakeGenericType(typeof(float)), typeof(Stat<float>),
            "et redescendre de l'ouvert au ferme, c'est ce qui permet de fabriquer le convertisseur qui va bien");

        var factory = new StatConverterFactory();

        Check.True(factory.CanConvert(typeof(Stat<int>)), "la fabrique reconnait Stat<int>");
        Check.True(factory.CanConvert(typeof(Stat<string>)), "et Stat<string>, sans qu'on ait ecrit une ligne de plus");
        Check.False(factory.CanConvert(typeof(int)), "elle ne reconnait pas un type non generique");
        Check.False(factory.CanConvert(typeof(List<int>)), "ni un autre generique : c'est la definition ouverte qu'elle compare, pas le nom");

        Check.Equal(JsonSerializer.Serialize(new Stat<int>("force", 12), Plain), "{\"Key\":\"force\",\"Value\":12}",
            "sans convertisseur, une stat pese 25 caracteres");

        Check.Equal(Write(new Stat<int>("force", 12)), "\"force=12\"",
            "avec, elle en pese 10. Sur mille stats dans une sauvegarde, ce n'est plus un detail");

        Check.Equal(Write(new Stat<float>("vitesse", 2.5f)), "\"vitesse=2.5\"", "le meme convertisseur sert pour les flottants");

        Check.Equal(Write(new Stat<string>("classe", "voleur")), "\"classe=\\u0022voleur\\u0022\"",
            "et pour le texte, parce que la valeur interne passe par le SERIALISEUR et pas par un ToString : c'est lui qui sait qu'une chaine se met entre guillemets, et que des guillemets a l'interieur d'une chaine s'echappent en \\u0022");

        Check.Equal(Read<Stat<int>>("\"force=12\""), new Stat<int>("force", 12), "l'aller-retour rend la stat entiere");
        Check.Equal(Read<Stat<float>>("\"vitesse=2.5\""), new Stat<float>("vitesse", 2.5f), "quel que soit le T");
        Check.Equal(Read<Stat<string>>(Write(new Stat<string>("classe", "voleur"))), new Stat<string>("classe", "voleur"),
            "y compris celui qui a besoin de guillemets : ce que le convertisseur ecrit, il doit savoir le relire");

        Check.Throws<JsonException>(() => Read<Stat<int>>("\"forcedouze\""),
            "une valeur mal formee doit lever une JsonException, comme le reste du serialiseur : c'est ce que le code appelant attrape deja");

        var sheet = new List<Stat<int>>
        {
            new("force", 12),
            new("agilite", 7),
        };

        Check.Equal(Write(sheet), "[\"force=12\",\"agilite=7\"]", "la fabrique s'applique aussi aux stats rangees dans une liste");

        Check.Sequence(Read<List<Stat<int>>>(Write(sheet)), sheet, "et l'aller-retour de la liste entiere");

        var byZone = new Dictionary<string, Stat<float>> { ["cave"] = new("humidite", 0.75f) };

        Check.Equal(Write(byZone), "{\"cave\":\"humidite=0.75\"}", "et aux valeurs d'un dictionnaire, sans rien declarer de plus");

        Check.Equal(Read<Dictionary<string, Stat<float>>>(Write(byZone))["cave"], new Stat<float>("humidite", 0.75f),
            "un seul convertisseur ecrit une fois, valable pour tous les T d'aujourd'hui et ceux de demain");
    }
}
