using Csharplings.Unity;

namespace Csharplings;

public sealed class LootTable : ISerializationCallbackReceiver
{
    public string Label = "coffre";

    public List<string> ItemKeys = new();

    public List<int> ItemCounts = new();

    public int Version { get; set; } = 3;

    public readonly Dictionary<string, int> Items = new();

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
    }
}

public static class Serialize1
{
    public const bool NotDone = true;

    public static void Run()
    {
        Check.True(UnitySerializer.CanSerialize(typeof(int)), "un entier se serialise");
        Check.True(UnitySerializer.CanSerialize(typeof(List<string>)), "une liste de chaines aussi");
        Check.False(UnitySerializer.CanSerialize(typeof(Dictionary<string, int>)),
            "un Dictionary, NON. Et Unity ne previent pas : le champ disparait simplement de l'inspecteur et de la sauvegarde");

        var table = new LootTable { Label = "coffre du boss", Version = 7 };

        table.Items["potion"] = 3;
        table.Items["cle"] = 1;

        Dictionary<string, string> asset = UnitySerializer.Save(table);

        Check.True(asset.ContainsKey("Label"), "un CHAMP public est serialise");
        Check.False(asset.ContainsKey("Version"),
            "une propriete auto, non : Unity serialise les champs, jamais les proprietes. C'est la surprise numero un");
        Check.False(asset.ContainsKey("Items"), "et le dictionnaire n'y est pas non plus, comme annonce");

        Check.Sequence(table.ItemKeys.OrderBy(key => key), new[] { "cle", "potion" },
            "d'ou l'aplatissement du dictionnaire en DEUX LISTES, fait dans OnBeforeSerialize juste avant l'ecriture");
        Check.Equal(table.ItemCounts.Count, 2, "une entree de chaque cote");
        Check.True(asset.ContainsKey("ItemKeys") && asset.ContainsKey("ItemCounts"),
            "et ce sont les listes, elles, qui partent dans l'asset");

        var reloaded = new LootTable();

        UnitySerializer.Load(reloaded, asset);

        Check.Equal(reloaded.Label, "coffre du boss", "au rechargement, le champ revient");
        Check.Equal(reloaded.Version, 3,
            "la propriete auto repart a sa valeur par defaut, en silence : c'est comme ca qu'on perd un reglage sans comprendre pourquoi");

        Check.Equal(reloaded.Items.Count, 2,
            "et le dictionnaire est reconstruit par OnAfterDeserialize, juste apres la lecture");
        Check.Equal(reloaded.Items["potion"], 3, "avec ses valeurs");
        Check.Equal(reloaded.Items["cle"], 1, "toutes ses valeurs");

        var empty = new LootTable();
        Dictionary<string, string> emptyAsset = UnitySerializer.Save(empty);

        Check.Equal(emptyAsset["ItemKeys"], string.Empty, "un dictionnaire vide donne des listes vides");

        var restoredEmpty = new LootTable();
        UnitySerializer.Load(restoredEmpty, emptyAsset);

        Check.Equal(restoredEmpty.Items.Count, 0, "et se recharge sans planter");

        table.Items["epee"] = 1;

        Check.Equal(table.ItemKeys.Count, 2,
            "attention : modifier le dictionnaire ne met PAS les listes a jour tout seul");

        Dictionary<string, string> refreshed = UnitySerializer.Save(table);

        Check.Equal(table.ItemKeys.Count, 3,
            "c'est la sauvegarde qui declenche OnBeforeSerialize et resynchronise. Les listes sont un cache, pas la verite");
        Check.True(refreshed["ItemKeys"].Contains("epee"), "et l'asset contient bien la nouveaute");
    }
}
