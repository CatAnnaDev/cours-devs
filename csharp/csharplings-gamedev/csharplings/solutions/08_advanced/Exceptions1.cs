namespace Csharplings;

public sealed class OutOfManaException : Exception
{
    public OutOfManaException(int missing) : base($"il manque {missing} mana")
    {
        Missing = missing;
    }

    public int Missing { get; }
}

public static class Exceptions1
{
    public const bool NotDone = false;

    public static int SafeDivide(int a, int b, int fallback)
    {
        try
        {
            return a / b;
        }
        catch (DivideByZeroException)
        {
            return fallback;
        }
    }

    public static int CastSpell(int currentMana, int cost)
    {
        if (cost > currentMana)
            throw new OutOfManaException(cost - currentMana);

        return currentMana - cost;
    }

    public static string Attempt(int mana, int cost)
    {
        string outcome = "?";

        try
        {
            CastSpell(mana, cost);
            outcome = "lance";
        }
        catch (OutOfManaException failure)
        {
            outcome = $"echec : {failure.Missing} manquants";
        }

        return outcome;
    }

    public static void Run()
    {
        Check.Equal(SafeDivide(10, 2, -1), 5, "une division normale");
        Check.Equal(SafeDivide(10, 0, -1), -1, "diviser par zero renvoie la valeur de repli");

        Check.Equal(CastSpell(100, 30), 70, "il reste 70 mana");
        Check.Throws<OutOfManaException>(() => CastSpell(10, 30), "pas assez de mana : on leve une exception");

        Check.Equal(Attempt(100, 30), "lance", "le sort part");
        Check.Equal(Attempt(10, 30), "echec : 20 manquants", "l'exception est attrapee et racontee");
    }
}
