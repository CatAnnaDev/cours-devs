namespace Csharplings;

public static class Async1
{
    public const bool NotDone = true;

    public static int LoadLevel()
    {
        Task.Delay(20);
        return 7;
    }

    public static async Task<string> LoadNameAsync()
    {
        await Task.Delay(10);
        return "donjon";
    }

    public static async Task<int> LoadEverythingAsync()
    {
        int level = LoadLevel();
        string name = LoadNameAsync();

        return level + name.Length;
    }

    public static void Run()
    {
        int level = LoadLevel().GetAwaiter().GetResult();
        Check.Equal(level, 7, "le niveau charge");

        string name = LoadNameAsync().GetAwaiter().GetResult();
        Check.Equal(name, "donjon", "le nom charge");

        int total = LoadEverythingAsync().GetAwaiter().GetResult();
        Check.Equal(total, 13, "7 plus les 6 lettres de 'donjon'");
    }
}
