namespace Csharplings;

public static class Types2
{
    public const bool NotDone = false;

    public static float HealthRatio(int current, int max)
    {
        return (float)current / max;
    }

    public static int Floor(float value)
    {
        return (int)value;
    }

    public static void Run()
    {
        Check.Near(HealthRatio(3, 4), 0.75, "3 PV sur 4 font 75 pour cent");
        Check.Near(HealthRatio(1, 3), 0.3333, "1 PV sur 3", 0.001);
        Check.Equal(Floor(7.9f), 7, "un cast vers int tronque, il n'arrondit pas");
        Check.Equal(Floor(-2.5f), -2, "il tronque aussi vers zero pour les negatifs");
    }
}
