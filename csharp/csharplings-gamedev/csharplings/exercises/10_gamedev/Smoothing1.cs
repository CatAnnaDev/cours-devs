namespace Csharplings;

public static class Smoothing1
{
    public const bool NotDone = true;

    public static float Approach(float current, float target, float step)
    {
        return current + step;
    }

    public static float Smooth(float current, float target, float sharpness, double delta)
    {
        return Mathf.Lerp(current, target, 0.1f);
    }

    private static float SmoothOver(int frames, double delta)
    {
        float value = 0f;

        for (int frame = 0; frame < frames; frame++)
            value = Smooth(value, 100f, 5f, delta);

        return value;
    }

    public static void Run()
    {
        Check.Near(Approach(0f, 10f, 3f), 3.0, "on avance d'un pas de 3");
        Check.Near(Approach(9f, 10f, 3f), 10.0, "on s'arrete pile sur la cible, sans la depasser");
        Check.Near(Approach(10f, 0f, 3f), 7.0, "ca marche aussi vers le bas");
        Check.Near(Approach(10f, 10f, 3f), 10.0, "deja arrive : on ne bouge plus");

        Check.Near(SmoothOver(60, 1.0 / 60.0), 99.326, "une seconde de lissage a 60 images par seconde", 0.01);
        Check.Near(SmoothOver(120, 1.0 / 120.0), 99.326, "la MEME chose a 120 images par seconde", 0.01);
        Check.Near(SmoothOver(20, 1.0 / 20.0), 99.326, "et a 20 images par seconde sur un vieux PC", 0.01);
    }
}
