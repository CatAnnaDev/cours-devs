using System.Text.Json;

namespace Csharplings;

public class Item
{
    public string Name { get; set; } = "objet";
}

public sealed class Weapon : Item
{
    public int Damage { get; set; }
}

public sealed class SaveSlot<T>
{
    public int Version { get; set; } = 1;

    public string Label { get; set; }

    public T Payload { get; set; }
}

public sealed class LooseSlot
{
    public object Payload { get; set; }
}

public static class Generic1
{
    public const bool NotDone = true;

    public static readonly JsonSerializerOptions Options = new();

    public static string Save<T>(T value) =>
        JsonSerializer.Serialize(value, Options);

    public static string SaveExact<T>(T value) =>
        JsonSerializer.Serialize(value, Options);

    public static T Load<T>(string json) where T : new() =>
        JsonSerializer.Deserialize<T>(json, Options);

    public static List<T> LoadList<T>(string json) =>
        JsonSerializer.Deserialize<List<T>>(json, Options);

    public static void Run()
    {
        var weapon = new Weapon { Name = "epee", Damage = 12 };

        Check.Equal(Save(weapon), "{\"Damage\":12,\"Name\":\"epee\"}",
            "une variable declaree Weapon donne T = Weapon : tout part");

        Item asItem = weapon;

        Check.Equal(Save(asItem), "{\"Name\":\"epee\"}",
            "LA surprise : la MEME arme, rangee dans une variable declaree Item, perd ses degats. Le serialiseur suit le type STATIQUE de T, pas l'objet qu'il a en main");

        Check.Equal(SaveExact(asItem), "{\"Damage\":12,\"Name\":\"epee\"}",
            "premiere parade : lui donner le type reel a l'execution avec value.GetType()");

        Check.Equal(Save<object>(asItem), "{\"Damage\":12,\"Name\":\"epee\"}",
            "deuxieme parade : 'object' est le seul type pour lequel le serialiseur va CHERCHER le type reel");

        Check.Equal(Save(new List<Item> { weapon }), "[{\"Name\":\"epee\"}]",
            "et la parade s'arrete au premier niveau : dans une liste d'Item, chaque element repart en Item");

        Check.Equal(SaveExact(new List<Item> { weapon }), "[{\"Name\":\"epee\"}]",
            "GetType() ne change rien ici : le type REEL de la liste est bien List<Item>, ce sont ses elements qui sont declares trop haut");

        Check.Equal(Save(new SaveSlot<Weapon> { Label = "auto", Payload = weapon }),
            "{\"Version\":1,\"Label\":\"auto\",\"Payload\":{\"Damage\":12,\"Name\":\"epee\"}}",
            "un conteneur generique ferme sur Weapon ecrit une arme complete");

        Check.Equal(Save(new SaveSlot<Item> { Label = "auto", Payload = weapon }),
            "{\"Version\":1,\"Label\":\"auto\",\"Payload\":{\"Name\":\"epee\"}}",
            "le meme conteneur ferme sur Item la tronque : le type statique decide a CHAQUE niveau, pas seulement a la racine");

        Check.True(Save(new LooseSlot { Payload = weapon }).Contains("\"Damage\":12"),
            "une propriete declaree object ecrit bien l'objet entier");

        Check.Equal(JsonSerializer.Deserialize<LooseSlot>("{\"Payload\":{\"Name\":\"epee\"}}", Options).Payload.GetType().Name,
            "JsonElement",
            "mais elle ne le relit JAMAIS : object rend un JsonElement, du texte analyse et rien de plus. 'object' est un aller sans retour, et c'est pour ca qu'on ecrit du generique");

        SaveSlot<Weapon> reloaded = Load<SaveSlot<Weapon>>(Save(new SaveSlot<Weapon> { Label = "auto", Payload = weapon }));

        Check.Equal(reloaded.Payload.Damage, 12, "un conteneur generique fait l'aller-retour complet, lui");
        Check.Equal(reloaded.Label, "auto", "avec le reste du conteneur");

        Check.True(Load<SaveSlot<Weapon>>("null") is not null,
            "un fichier vide doit rendre un objet NEUF et pas null : c'est ce que la contrainte 'where T : new()' rend possible");

        Check.Equal(Load<SaveSlot<Weapon>>("null").Version, 1, "avec les valeurs par defaut de la classe");

        Check.True(LoadList<Item>("null") is not null,
            "et une liste vide plutot que null : un chargement rate ne doit jamais faire tomber la scene suivante");

        Check.Equal(LoadList<Item>("null").Count, 0, "vide, mais parcourable");

        Check.Equal(LoadList<Item>("[{\"Name\":\"arc\"},{\"Name\":\"cle\"}]").Count, 2, "une vraie liste se relit normalement");
    }
}
