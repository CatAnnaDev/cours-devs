namespace Csharplings;

public static class Variables3
{
    public const bool NotDone = true;

    private const int MaxLevel = 0;
    private const string GameName = "sans nom";

    private static readonly DateTime StartedAt = DateTime.UnixEpoch;

    public static void Run()
    {
        Check.Equal(MaxLevel, 99, "le niveau maximum est 99");
        Check.Equal(GameName, "Donjon", "le jeu s'appelle Donjon");
        Check.True(StartedAt.Year == 1970, "readonly : fixe une fois, plus modifiable ensuite");
    }
}
