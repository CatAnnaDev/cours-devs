namespace Csharplings;

public static class Methods1
{
    public const bool NotDone = true;

    public static int Double(int value)
    {
    }

    public static int Biggest(int a, int b, int c)
    {
        return Todo.Value<int>();
    }

    public static bool IsEven(int value) => Todo.Value<bool>();

    public static void Run()
    {
        Check.Equal(Double(21), 42, "le double de 21");
        Check.Equal(Double(-3), -6, "le double d'un negatif");

        Check.Equal(Biggest(3, 9, 5), 9, "le plus grand des trois");
        Check.Equal(Biggest(10, 2, 4), 10, "le premier est le plus grand");

        Check.True(IsEven(4), "4 est pair");
        Check.False(IsEven(7), "7 est impair");
    }
}
