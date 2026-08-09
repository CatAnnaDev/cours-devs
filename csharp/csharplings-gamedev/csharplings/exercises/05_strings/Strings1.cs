namespace Csharplings;

public static class Strings1
{
    public const bool NotDone = true;

    public static string Hud(string player, int score, float seconds)
    {
        return "Joueur : " + player + " Score : " + score;
    }

    public static void Run()
    {
        Check.Equal(Hud("Anna", 1200, 12.5f), "Anna | 1200 pts | 12.50 s", "l'affichage du HUD");
        Check.Equal(Hud("Bob", 0, 3f), "Bob | 0 pts | 3.00 s", "toujours deux decimales pour le temps");
    }
}
