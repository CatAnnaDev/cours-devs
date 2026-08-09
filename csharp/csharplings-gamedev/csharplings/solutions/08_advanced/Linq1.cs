namespace Csharplings;

public sealed record Monster(string Name, int Level, int Loot);

public static class Linq1
{
    public const bool NotDone = false;

    private static readonly List<Monster> Bestiary = new()
    {
        new Monster("slime", 1, 5),
        new Monster("gobelin", 3, 12),
        new Monster("golem", 8, 40),
        new Monster("dragon", 20, 300),
        new Monster("rat", 1, 2),
    };

    public static List<string> NamesAboveLevel(int level)
    {
        return Bestiary
            .Where(monster => monster.Level > level)
            .Select(monster => monster.Name)
            .ToList();
    }

    public static int TotalLoot()
    {
        return Bestiary.Sum(monster => monster.Loot);
    }

    public static string Strongest()
    {
        return Bestiary.OrderByDescending(monster => monster.Level).First().Name;
    }

    public static void Run()
    {
        Check.Sequence(NamesAboveLevel(5), new[] { "golem", "dragon" }, "les monstres au-dessus du niveau 5");
        Check.Sequence(NamesAboveLevel(100), Array.Empty<string>(), "aucun monstre aussi fort");

        Check.Equal(TotalLoot(), 359, "le butin total du bestiaire");
        Check.Equal(Strongest(), "dragon", "le monstre du plus haut niveau");

        Check.True(Bestiary.Any(m => m.Level == 1), "Any repond juste oui ou non");
        Check.Equal(Bestiary.Count(m => m.Level == 1), 2, "Count avec condition");
    }
}
