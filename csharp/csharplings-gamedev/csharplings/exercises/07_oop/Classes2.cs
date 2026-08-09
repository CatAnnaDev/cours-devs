namespace Csharplings;

public sealed class Chest
{
    public bool IsOpen { get; private set; }
    public int Coins { get; private set; }

    public bool IsEmpty => Todo.Value<bool>();

    public Chest(int coins)
    {
        Coins = coins;
    }

    public int Open()
    {
        IsOpen = true;
        int taken = Coins;
        Coins = 0;
        return taken;
    }
}

public static class Classes2
{
    public const bool NotDone = true;

    public static void Run()
    {
        var chest = new Chest(50);

        Check.False(chest.IsOpen, "un coffre neuf est ferme");
        Check.False(chest.IsEmpty, "et il n'est pas vide");

        Check.Equal(chest.Open(), 50, "l'ouvrir donne 50 pieces");
        Check.True(chest.IsOpen, "il est maintenant ouvert");
        Check.True(chest.IsEmpty, "et vide");

        Check.Equal(chest.Open(), 0, "l'ouvrir une seconde fois ne donne rien");
    }
}
