namespace Csharplings;

public static class Variables1
{
    public const bool NotDone = false;

    public static void Run()
    {
        int health = 100;
        string playerName = "Anna";
        bool isAlive = true;

        Check.Equal(health, 100, "les points de vie valent 100");
        Check.Equal(playerName, "Anna", "le nom du joueur est Anna");
        Check.True(isAlive, "le joueur est vivant");
    }
}
