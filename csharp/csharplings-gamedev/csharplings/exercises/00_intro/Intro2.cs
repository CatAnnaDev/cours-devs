namespace Csharplings;

public static class Intro2
{
    public const bool NotDone = true;

    public static void Run()
    {
        string message = "une instruction se termine par un point-virgule"

        Check.True(message.EndsWith("point-virgule"), "le message est intact");
    }
}
