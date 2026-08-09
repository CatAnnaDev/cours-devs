namespace Csharplings;

public static class Collections3
{
    public const bool NotDone = true;

    public static int CountOf(Dictionary<string, int> inventory, string item)
    {
        return inventory[item];
    }

    public static void Give(Dictionary<string, int> inventory, string item, int amount)
    {
        inventory.Add(item, amount);
    }

    public static void Run()
    {
        var inventory = new Dictionary<string, int> { ["potion"] = 3 };

        Check.Equal(CountOf(inventory, "potion"), 3, "on a 3 potions");
        Check.Equal(CountOf(inventory, "epee"), 0, "un objet absent compte pour 0, il ne plante pas");

        Give(inventory, "epee", 1);
        Check.Equal(CountOf(inventory, "epee"), 1, "on recoit une epee");

        Give(inventory, "potion", 2);
        Check.Equal(CountOf(inventory, "potion"), 5, "recevoir s'ajoute a ce qu'on a deja");
    }
}
