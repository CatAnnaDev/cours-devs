namespace Csharplings;

public static class Flow1
{
    public const bool NotDone = false;

    public static string Rank(int score)
    {
        if (score >= 90)
            return "or";

        if (score >= 75)
            return "argent";

        if (score >= 50)
            return "bronze";

        return "aucun";
    }

    public static void Run()
    {
        Check.Equal(Rank(95), "or", "95 points donnent l'or");
        Check.Equal(Rank(80), "argent", "80 points donnent l'argent");
        Check.Equal(Rank(60), "bronze", "60 points donnent le bronze");
        Check.Equal(Rank(10), "aucun", "10 points ne donnent rien");
    }
}
