namespace Csharplings;

public static class Variables1
{
    public const bool NotDone = true;

    public static void Run()
    {
        int health = Todo.Value<int>();
        string playerName = Todo.Value<string>();
        bool isAlive = Todo.Value<bool>();

        Check.Equal(health, 100, "les points de vie valent 100");
        Check.Equal(playerName, "Anna", "le nom du joueur est Anna");
        Check.True(isAlive, "le joueur est vivant");
    }
}
