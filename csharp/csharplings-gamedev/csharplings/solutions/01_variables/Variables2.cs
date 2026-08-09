namespace Csharplings;

public static class Variables2
{
    public const bool NotDone = false;

    public static void Run()
    {
        var count = 3;
        count = 4;

        var enemyName = "slime";

        var position = 1.5f;

        Check.Equal(count, 4, "var a devine int, la variable reste modifiable");
        Check.Equal(enemyName.ToUpper(), "SLIME", "var a devine string");
        Check.Near(position, 1.5, "position vaut 1.5");
    }
}
