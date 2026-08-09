namespace Csharplings;

public sealed class WeaponStats
{
    public WeaponStats(string name, int damage, float weight)
    {
        Name = name;
        Damage = damage;
        Weight = weight;
    }

    public string Name { get; }
    public int Damage { get; }
    public float Weight { get; }
}

public static class Records1
{
    public const bool NotDone = true;

    public static WeaponStats Upgrade(WeaponStats weapon, int bonus)
    {
        return Todo.Value<WeaponStats>();
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
