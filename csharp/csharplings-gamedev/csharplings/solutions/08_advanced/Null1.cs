namespace Csharplings;

public sealed class Weapon
{
    public string Name { get; set; }
    public Weapon Upgrade { get; set; }
}

public sealed class Hero
{
    public string Name { get; set; }
    public Weapon Equipped { get; set; }
    public List<string> Titles { get; set; }
}

public static class Null1
{
    public const bool NotDone = false;

    public static string UpgradeName(Hero hero)
    {
        return hero?.Equipped?.Upgrade?.Name ?? "aucune";
    }

    public static int TitleCount(Hero hero)
    {
        return hero?.Titles?.Count ?? 0;
    }

    public static void GrantTitle(Hero hero, string title)
    {
        hero.Titles ??= new List<string>();
        hero.Titles.Add(title);
    }

    public static void Run()
    {
        var bare = new Hero { Name = "Anna" };
        var armed = new Hero
        {
            Name = "Bob",
            Equipped = new Weapon { Name = "epee", Upgrade = new Weapon { Name = "epee+1" } },
        };

        Check.Equal(UpgradeName(armed), "epee+1", "le heros arme a une amelioration");
        Check.Equal(UpgradeName(bare), "aucune", "sans arme, on renvoie 'aucune' au lieu de planter");

        Check.Equal(TitleCount(bare), 0, "pas de liste de titres : on compte 0");

        GrantTitle(bare, "Novice");
        Check.Equal(TitleCount(bare), 1, "accorder un titre cree la liste si besoin");

        GrantTitle(bare, "Vaillante");
        Check.Equal(TitleCount(bare), 2, "et ne la recree pas ensuite");
    }
}
