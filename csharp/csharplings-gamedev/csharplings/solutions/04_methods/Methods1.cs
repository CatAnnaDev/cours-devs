namespace Csharplings;

public static class Methods1
{
    public const bool NotDone = false;

    public static int Double(int value)
    {
        return value * 2;
    }

    public static int Biggest(int a, int b, int c)
    {
        int biggest = a;

        if (b > biggest)
            biggest = b;

        if (c > biggest)
            biggest = c;

        return biggest;
    }

    public static bool IsEven(int value) => value % 2 == 0;

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
