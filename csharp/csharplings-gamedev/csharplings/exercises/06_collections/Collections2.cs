namespace Csharplings;

public static class Collections2
{
    public const bool NotDone = true;

    public static List<int> RemoveZeros(List<int> values)
    {
        foreach (int value in values)
        {
            if (value == 0)
                values.Remove(value);
        }

        return values;
    }

    public static void Run()
    {
        var inventory = new List<string>();
        inventory.Add("potion");
        inventory.Add("epee");
        inventory.Remove("potion");

        Check.Equal(inventory.Count, 1, "il reste un objet");
        Check.True(inventory.Contains("epee"), "l'epee est toujours la");
        Check.Equal(inventory[0], "epee", "on accede par index comme un tableau");

        Check.Sequence(RemoveZeros(new List<int> { 1, 0, 2, 0, 3 }), new[] { 1, 2, 3 }, "retirer tous les zeros");
        Check.Sequence(RemoveZeros(new List<int> { 0, 0 }), Array.Empty<int>(), "tout retirer");
        Check.Sequence(RemoveZeros(new List<int> { 5 }), new[] { 5 }, "rien a retirer");
    }
}
