namespace Csharplings;

public static class Methods3
{
    public const bool NotDone = true;

    public static bool TryDivide(int a, int b, out int result)
    {
        if (b == 0)
            return false;

        result = a / b;
        return true;
    }

    public static void AddPoints(ref int score, int points)
    {
        score = score + points;
    }

    public static void Run()
    {
        Check.True(TryDivide(10, 2, out int ok), "10 divise par 2 fonctionne");
        Check.Equal(ok, 5, "et donne 5");

        Check.False(TryDivide(10, 0, out int failed), "diviser par zero echoue proprement");
        Check.Equal(failed, 0, "en cas d'echec, out doit valoir 0");

        int score = 100;
        AddPoints(ref score, 50);
        Check.Equal(score, 150, "ref modifie la vraie variable, pas une copie");
    }
}
