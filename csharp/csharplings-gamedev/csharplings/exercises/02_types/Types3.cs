namespace Csharplings;

public static class Types3
{
    public const bool NotDone = true;

    public static int ReadScore(string input, int fallback)
    {
        return int.Parse(input);
    }

    public static void Run()
    {
        Check.Equal(ReadScore("42", 0), 42, "un texte valide devient un nombre");
        Check.Equal(ReadScore("", 0), 0, "un texte vide renvoie la valeur de repli");
        Check.Equal(ReadScore("bonjour", -1), -1, "un texte invalide ne doit PAS faire planter le jeu");
        Check.Equal(ReadScore("7", 99), 7, "la valeur de repli sert seulement en cas d'echec");
    }
}
