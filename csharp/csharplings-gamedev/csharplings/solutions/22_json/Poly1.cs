using System.Text.Json;
using System.Text.Json.Serialization;

namespace Csharplings;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(Sword), "sword")]
[JsonDerivedType(typeof(Potion), "potion")]
[JsonDerivedType(typeof(DoorKey), "key")]
public abstract class InventoryItem
{
    public string Name { get; set; }

    public abstract int Value();
}

public sealed class Sword : InventoryItem
{
    public int Damage { get; set; }

    public override int Value() => Damage * 3;
}

public sealed class Potion : InventoryItem
{
    public int Heal { get; set; }

    public override int Value() => Heal;
}

public sealed class DoorKey : InventoryItem
{
    public string Door { get; set; }

    public override int Value() => 0;
}

public sealed class Trinket : InventoryItem
{
    public override int Value() => 1;
}

public static class Poly1
{
    public const bool NotDone = false;

    public static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string WriteBag(List<InventoryItem> bag) => JsonSerializer.Serialize(bag, Options);

    public static List<InventoryItem> ReadBag(string json) =>
        JsonSerializer.Deserialize<List<InventoryItem>>(json, Options) ?? new List<InventoryItem>();

    public static void Run()
    {
        InventoryItem sword = new Sword { Name = "epee", Damage = 12 };

        Check.True(JsonSerializer.Serialize(sword, Options).StartsWith("{\"kind\":\"sword\""),
            "avec JsonPolymorphic, le fichier commence par un DISCRIMINANT : le nom du type reel, choisi par toi");

        Check.False(JsonSerializer.Serialize(sword, Options).Contains("$type"),
            "et ce nom se decide dans l'attribut, sinon le serialiseur en met un a lui, '$type', que tu n'as pas envie de voir dans une sauvegarde de joueur");

        var bag = new List<InventoryItem>
        {
            sword,
            new Potion { Name = "fiole", Heal = 30 },
            new DoorKey { Name = "cle rouillee", Door = "cave" },
        };

        string json = WriteBag(bag);

        Check.True(json.Contains("\"kind\":\"potion\""), "chaque element du sac emporte son propre discriminant");
        Check.True(json.Contains("\"kind\":\"key\""), "y compris la cle");
        Check.True(json.Contains("\"heal\":30"), "et ses proprietes a lui, celles que le type de base ne connait pas");

        List<InventoryItem> loaded = ReadBag(json);

        Check.Equal(loaded.Count, 3, "on relit les trois objets");
        Check.Equal(loaded[0].GetType().Name, "Sword", "et le premier est redevenu une VRAIE epee");
        Check.Equal(loaded[1].GetType().Name, "Potion", "la seconde une potion");
        Check.Equal(loaded[2].GetType().Name, "DoorKey", "la troisieme une cle");
        Check.Equal(loaded.Sum(item => item.Value()), 66,
            "et l'appel virtuel repart : 36 + 30 + 0. C'est ca qu'on achete, pas juste des champs qui reviennent");

        Check.Throws<NotSupportedException>(() => JsonSerializer.Serialize<InventoryItem>(new Trinket { Name = "babiole" }, Options),
            "un type derive qu'on a oublie de declarer echoue BRUYAMMENT a l'ecriture, ce qui vaut infiniment mieux qu'une sauvegarde tronquee");

        Check.Throws<JsonException>(() => ReadBag("[{\"kind\":\"arbalete\",\"name\":\"x\"}]"),
            "et un discriminant inconnu echoue a la lecture : c'est le cas d'une sauvegarde faite par une version plus recente du jeu");

        Check.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<InventoryItem>("{\"name\":\"x\",\"kind\":\"sword\",\"damage\":4}", Options),
            "le discriminant doit etre le PREMIER champ de l'objet : un outil qui reordonne tes fichiers par ordre alphabetique casse tous les chargements");

        Check.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<InventoryItem>("{\"name\":\"x\"}", Options),
            "et un objet sans discriminant ne peut pas etre reconstruit, faute de savoir en quoi");

        Check.False(JsonSerializer.Serialize(new Sword { Name = "epee", Damage = 12 }, Options).Contains("kind"),
            "dernier piege : ecrire depuis une variable declaree Sword n'ecrit AUCUN discriminant. Le fichier a l'air correct et ne se relira jamais en InventoryItem");

        Check.True(JsonSerializer.Serialize<InventoryItem>(new Sword { Name = "epee", Damage = 12 }, Options).Contains("kind"),
            "il faut serialiser depuis le type de BASE, celui qui porte l'attribut");

        Check.Equal(WriteBag(ReadBag(json)), json, "et l'aller-retour complet rend exactement le meme texte");
    }
}
