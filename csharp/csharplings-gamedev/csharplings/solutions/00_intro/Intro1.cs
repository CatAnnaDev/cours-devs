namespace Csharplings;

public static class Intro1
{
    public const bool NotDone = false;

    public static void Run()
    {
        Console.WriteLine("      Bienvenue. Ce code fonctionne deja.");
        Console.WriteLine("      Il ne te manque qu'une chose : valider l'exercice.");

        Check.Equal(1 + 1, 2, "les maths fonctionnent");
    }
}
