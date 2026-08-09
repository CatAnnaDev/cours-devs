namespace Csharplings;

public static class Stable1
{
    public const bool NotDone = true;

    private const float Duration = 0.25f;
    private const float Gravity = 980f;

    public static float NaiveSmooth(float value, float target, float delta) =>
        Mathf.Lerp(value, target, 0.1f);

    public static float StableSmooth(float value, float target, float strength, float delta) =>
        Mathf.Lerp(value, target, strength * delta);

    public static float ExplicitEulerHeight(float delta)
    {
        float velocity = 0f;
        float height = 0f;

        for (int step = 0; step < StepsFor(delta); step++)
        {
            height += velocity * delta;
            velocity += Gravity * delta;
        }

        return height;
    }

    public static float TrapezoidHeight(float delta)
    {
        float velocity = 0f;
        float height = 0f;

        for (int step = 0; step < StepsFor(delta); step++)
        {
            velocity += Gravity * delta;
            height += velocity * delta;
        }

        return height;
    }

    public static void Run()
    {
        float naiveSlow = SmoothTo(NaiveSmooth, 1f / 60f);
        float naiveFast = SmoothTo(NaiveSmooth, 1f / 240f);

        Report("lissage naif a 60 images par seconde", naiveSlow);
        Report("lissage naif a 240 images par seconde", naiveFast);

        Check.True(Mathf.Abs(naiveFast - naiveSlow) > 15f,
            "Lerp(valeur, cible, 0.1f) applique une fois par frame depend ENTIEREMENT du framerate : plus de 15 unites d'ecart sur un quart de seconde");
        Check.True(naiveFast > naiveSlow,
            "et c'est le joueur avec le meilleur ecran qui voit sa camera coller plus vite : ton reglage ne veut plus rien dire");

        float stableSlow = SmoothTo((value, target, delta) => StableSmooth(value, target, 8f, delta), 1f / 60f);
        float stableFast = SmoothTo((value, target, delta) => StableSmooth(value, target, 8f, delta), 1f / 240f);

        Report("lissage exponentiel a 60 images par seconde", stableSlow);
        Report("lissage exponentiel a 240 images par seconde", stableFast);

        Check.Near(stableSlow, stableFast,
            "1 - Exp(-force * delta) donne le MEME resultat aux deux framerates : c'est ca, un lissage reglable", 0.2);
        Check.Near(stableSlow, 100.0 * (1.0 - Math.Exp(-2.0)),
            "et ce resultat est exactement celui que la formule continue predit", 0.2);

        float towardSlow = SmoothTo((value, target, delta) => Mathf.MoveToward(value, target, 300f * delta), 1f / 60f);
        float towardFast = SmoothTo((value, target, delta) => Mathf.MoveToward(value, target, 300f * delta), 1f / 240f);

        Check.Near(towardSlow, towardFast,
            "MoveToward est stable par construction : la vitesse est multipliee par delta, donc la distance parcourue ne depend que du temps", 0.01);
        Check.Near(towardSlow, 75.0, "300 unites par seconde pendant un quart de seconde, ca fait 75", 0.01);

        float eulerSlow = ExplicitEulerHeight(1f / 60f);
        float eulerFast = ExplicitEulerHeight(1f / 240f);
        float exact = 0.5f * Gravity * Duration * Duration;

        Report("chute en Euler explicite a 60 images par seconde", eulerSlow);
        Report("chute en Euler explicite a 240 images par seconde", eulerFast);
        Report("la hauteur exacte", exact);

        Check.True(Mathf.Abs(eulerSlow - exact) > 1f,
            "l'integration naive se trompe : elle utilise l'ANCIENNE vitesse pendant tout le pas, donc elle tombe trop lentement");
        Check.True(Mathf.Abs(eulerSlow - exact) > Mathf.Abs(eulerFast - exact),
            "l'erreur diminue quand le pas diminue, mais elle ne disparait jamais : ta hauteur de saut depend du framerate");

        float trapezoidSlow = TrapezoidHeight(1f / 60f);
        float trapezoidFast = TrapezoidHeight(1f / 240f);

        Report("chute avec la vitesse moyenne a 60 images par seconde", trapezoidSlow);
        Report("chute avec la vitesse moyenne a 240 images par seconde", trapezoidFast);

        Check.Near(trapezoidSlow, exact,
            "prendre la MOYENNE de l'ancienne et de la nouvelle vitesse tombe pile sur la valeur exacte", 0.01);
        Check.Near(trapezoidFast, exact, "a n'importe quel pas de temps", 0.01);
        Check.Near(trapezoidSlow, trapezoidFast, "donc les deux framerates sautent exactement a la meme hauteur", 0.01);
    }

    private static int StepsFor(float delta) => Mathf.RoundToInt(Duration / delta);

    private static float SmoothTo(Func<float, float, float, float> smooth, float delta)
    {
        float value = 0f;

        for (int step = 0; step < StepsFor(delta); step++)
            value = smooth(value, 100f, delta);

        return value;
    }

    private static void Report(string what, float value) =>
        Console.WriteLine($"      mesure  {what} : {value:0.###}");
}
