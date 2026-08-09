namespace Csharplings;

public sealed record WeaponStats(string Name, int Damage, float Weight)
{
    public string Label => $"{Name} ({Damage})";
}

public static class Records1
{
    public const bool NotDone = false;

    public static WeaponStats Upgrade(WeaponStats weapon, int bonus)
    {
        return weapon with { Damage = weapon.Damage + bonus };
    }

    public static void Run()
    {
        var sword = new WeaponStats("epee", 12, 3.5f);
        var twin = new WeaponStats("epee", 12, 3.5f);

        Check.True(sword == twin, "deux records aux memes valeurs sont egaux");
        Check.False(ReferenceEquals(sword, twin), "alors que ce ne sont pas le meme objet");

        WeaponStats upgraded = Upgrade(sword, 5);

        Check.Equal(upgraded.Damage, 17, "'with' fabrique une copie modifiee");
        Check.Equal(sword.Damage, 12, "l'original n'a pas bouge");
        Check.Equal(upgraded.Name, "epee", "les champs non cites sont recopies");
        Check.Equal(upgraded.Label, "epee (17)", "un record peut porter des membres calcules");

        (string name, int damage, float weight) = upgraded;

        Check.Equal(name, "epee", "un record positionnel se deconstruit");
        Check.Equal(damage, 17, "champ par champ");
        Check.Near(weight, 3.5, "dans l'ordre de declaration");

        var seen = new HashSet<WeaponStats> { sword, twin, upgraded };

        Check.Equal(seen.Count, 2, "l'egalite de valeur vaut aussi pour GetHashCode");
        Check.True(sword.ToString().Contains("Damage = 12"), "et le ToString liste les champs, gratuitement");
    }
}
