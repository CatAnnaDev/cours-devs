namespace Csharplings;

public sealed record Item(string Name, string Kind, int Price, int Weight);

public static class Linq2
{
    public const bool NotDone = false;

    private static readonly List<Item> Shop = new()
    {
        new Item("potion", "consommable", 50, 1),
        new Item("ether", "consommable", 80, 1),
        new Item("epee", "arme", 300, 8),
        new Item("hache", "arme", 300, 12),
        new Item("bouclier", "armure", 220, 15),
    };

    public static Dictionary<string, int> CountByKind()
    {
        return Shop
            .GroupBy(item => item.Kind)
            .ToDictionary(group => group.Key, group => group.Count());
    }

    public static List<string> ByPriceThenName()
    {
        return Shop
            .OrderByDescending(item => item.Price)
            .ThenBy(item => item.Name)
            .Select(item => item.Name)
            .ToList();
    }

    public static int TotalWeight()
    {
        return Shop.Aggregate(0, (total, item) => total + item.Weight);
    }

    public static List<string> Label(List<string> names, List<int> scores)
    {
        return names.Zip(scores, (name, score) => $"{name}:{score}").ToList();
    }

    public static void Run()
    {
        Dictionary<string, int> byKind = CountByKind();

        Check.Equal(byKind["consommable"], 2, "GroupBy range les objets par famille");
        Check.Equal(byKind["arme"], 2, "et ToDictionary transforme le resultat en table");
        Check.Equal(byKind["armure"], 1, "une seule armure au magasin");
        Check.Equal(byKind.Count, 3, "trois familles en tout");

        Check.Sequence(ByPriceThenName(), new[] { "epee", "hache", "bouclier", "ether", "potion" },
            "du plus cher au moins cher, l'alphabet departage les ex aequo");

        Check.Equal(TotalWeight(), 37, "Aggregate replie une collection en une seule valeur");
        Check.Sequence(Label(new List<string> { "a", "b", "c" }, new List<int> { 1, 2 }),
            new[] { "a:1", "b:2" }, "Zip s'arrete des que l'une des deux listes est finie");

        var levels = new List<int> { 1, 2, 3 };
        IEnumerable<int> strong = levels.Where(level => level >= 3);

        levels.Add(9);

        Check.Sequence(strong, new[] { 3, 9 }, "une requete se rejoue a chaque parcours : elle a vu l'ajout");

        List<int> frozen = strong.ToList();
        levels.Add(50);

        Check.Sequence(frozen, new[] { 3, 9 }, "ToList fige le resultat, lui ne bouge plus");

        Check.Equal(Shop.FirstOrDefault(item => item.Price > 10000), null, "FirstOrDefault rend null quand rien ne colle");
        Check.Throws<InvalidOperationException>(() => Shop.First(item => item.Price > 10000),
            "First, lui, refuse de rendre quoi que ce soit et plante");
    }
}
