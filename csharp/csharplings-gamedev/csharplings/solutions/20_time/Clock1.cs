namespace Csharplings;

public static class Clock1
{
    public const bool NotDone = false;

    private const float Step = 1f / 60f;
    private const int Steps = 600_000;

    public static float AccumulatedInFloat(int steps)
    {
        float elapsed = 0f;

        for (int i = 0; i < steps; i++)
            elapsed += Step;

        return elapsed;
    }

    public static double AccumulatedInDouble(int steps)
    {
        double elapsed = 0.0;

        for (int i = 0; i < steps; i++)
            elapsed += Step;

        return elapsed;
    }

    public static double CountedInSteps(int steps) => steps * (double)Step;

    public static void Run()
    {
        float inFloat = AccumulatedInFloat(Steps);
        double inDouble = AccumulatedInDouble(Steps);
        double counted = CountedInSteps(Steps);

        Report("10 000 s accumulees en float", inFloat);
        Report("les memes accumulees en double", inDouble);
        Report("les memes comptees en pas entiers", counted);
        Report("derive du float", Math.Abs(inFloat - counted));
        Report("derive du double", Math.Abs(inDouble - counted));

        Check.Near(counted, 10000.0,
            "compter les pas puis multiplier une seule fois : une addition ne peut pas deriver s'il n'y en a pas", 0.01);
        Check.True(Math.Abs(inDouble - counted) < 0.001,
            "accumuler en double reste utilisable : la derive est invisible");
        Check.True(Math.Abs(inFloat - counted) > 20.0,
            "accumuler en float derive de VINGT-HUIT SECONDES sur 10 000 : chaque addition arrondit, et il y en a 600 000");

        float wall = 524_288f;

        Check.Equal(wall + Step, wall,
            "et il y a un mur : passe 524 288 secondes, un float ne peut plus representer un ecart aussi petit qu'une frame. L'horloge s'arrete net");
        Check.True(wall / 86_400f > 6f, "524 288 secondes, c'est six jours de fonctionnement : un serveur les fait");

        float justBefore = 262_144f;

        Check.True(justBefore + Step > justBefore,
            "trois jours plus tot elle avance encore, mais par bonds de 0.031 s au lieu de 0.017 : deja n'importe quoi");

        double preciseWall = 524_288.0;

        Check.True(preciseWall + Step > preciseWall,
            "en double, le meme instant se manipule sans y penser : c'est pour ca que les deux moteurs exposent leur temps absolu en double");

        Check.Equal(TickOf(0.0), 0L, "en pas entiers, l'instant zero est le pas zero");
        Check.Equal(TickOf(1.0), 60L, "une seconde, soixante pas");
        Check.Equal(TickOf(10_000.0), 600_000L, "dix mille secondes, six cent mille pas, sans le moindre arrondi");

        Check.Equal(TickOf(10_000.0) - TickOf(9_999.0), 60L,
            "et la difference entre deux instants est exacte, meme tres loin dans la partie");

        Check.Equal(TickOf(3.0) + TickOf(2.0), TickOf(5.0),
            "les pas s'additionnent sans perte, ce qu'aucun flottant ne garantit : c'est la seule base solide pour un replay ou du reseau");
    }

    private static long TickOf(double seconds) => (long)Math.Round(seconds * 60.0);

    private static void Report(string what, double value) =>
        Console.WriteLine($"      mesure  {what} : {value:0.######}");
}
