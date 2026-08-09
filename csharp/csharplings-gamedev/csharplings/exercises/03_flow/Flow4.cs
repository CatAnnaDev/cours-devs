namespace Csharplings;

public static class Flow4
{
    public const bool NotDone = true;

    public static int TotalLoot(List<int> drops)
    {
        int total = 0;

        foreach (int drop in drops)
        {
            if (drop == 0)
                break;

            if (drop < 0)
                break;

            total += drop;
        }

        return total;
    }

    public static void Run()
    {
        Check.Equal(TotalLoot(new List<int> { 5, 10, 3 }), 18, "tout ramasser");
        Check.Equal(TotalLoot(new List<int> { 5, 0, 3 }), 8, "un 0 se saute, on continue");
        Check.Equal(TotalLoot(new List<int> { 5, -1, 3 }), 5, "un negatif est un piege : on arrete la");
        Check.Equal(TotalLoot(new List<int>()), 0, "une liste vide ne rapporte rien");
    }
}
