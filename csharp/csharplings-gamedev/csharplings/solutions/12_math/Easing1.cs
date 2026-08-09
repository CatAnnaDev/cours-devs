namespace Csharplings;

public static class Easing1
{
    public const bool NotDone = false;

    public static float Clamp01(float value) => Mathf.Clamp(value, 0f, 1f);

    public static float InverseLerp(float from, float to, float value)
    {
        if (Mathf.IsEqualApprox(from, to))
            return 0f;

        return (value - from) / (to - from);
    }

    public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return Mathf.Lerp(toMin, toMax, InverseLerp(fromMin, fromMax, value));
    }

    public static float EaseOut(float t)
    {
        float clamped = Clamp01(t);

        return 1f - (1f - clamped) * (1f - clamped);
    }

    public static float EaseInOut(float t)
    {
        float clamped = Clamp01(t);

        if (clamped < 0.5f)
            return 2f * clamped * clamped;

        return 1f - Mathf.Pow(-2f * clamped + 2f, 2f) / 2f;
    }

    public static float Damp(float current, float target, float strength, float delta)
    {
        return Mathf.Lerp(current, target, 1f - Mathf.Exp(-strength * delta));
    }

    public static void Run()
    {
        Check.Near(Clamp01(-3f), 0.0, "en dessous on colle a zero");
        Check.Near(Clamp01(0.4f), 0.4, "au milieu on ne touche a rien");
        Check.Near(Clamp01(12f), 1.0, "au dessus on colle a un");

        Check.Near(InverseLerp(0f, 200f, 50f), 0.25, "50 points de vie sur 200, c'est un quart de barre");
        Check.Near(InverseLerp(100f, 200f, 150f), 0.5, "l'intervalle ne part pas forcement de zero");
        Check.Near(InverseLerp(5f, 5f, 5f), 0.0, "et un intervalle vide ne doit pas diviser par zero");

        Check.Near(Remap(50f, 0f, 200f, 0f, 64f), 16.0, "une barre de vie de 200 points affichee sur 64 pixels");
        Check.Near(Remap(0f, -1f, 1f, 0f, 100f), 50.0, "un joystick centre affiche 50 pour cent");

        Check.Near(EaseOut(0f), 0.0, "toute courbe part de zero");
        Check.Near(EaseOut(1f), 1.0, "et arrive a un");
        Check.True(EaseOut(0.5f) > 0.5f, "un ease-out demarre vite et ralentit a la fin");
        Check.Near(EaseOut(5f), 1.0, "au dela de un, on reste a un");

        Check.Near(EaseInOut(0f), 0.0, "meme depart");
        Check.Near(EaseInOut(0.5f), 0.5, "un ease-in-out est symetrique");
        Check.Near(EaseInOut(1f), 1.0, "meme arrivee");
        Check.True(EaseInOut(0.25f) < 0.25f, "il demarre doucement");
        Check.True(EaseInOut(0.75f) > 0.75f, "et finit doucement");

        float oneStep = Damp(0f, 100f, 5f, 0.2f);
        float twoHalfSteps = Damp(Damp(0f, 100f, 5f, 0.1f), 100f, 5f, 0.1f);

        Check.Near(oneStep, twoHalfSteps, "un lissage exponentiel donne le meme resultat quel que soit le decoupage", 0.001);
        Check.True(oneStep < 100f, "on approche la cible sans jamais la depasser");
        Check.True(Damp(0f, 100f, 5f, 10f) > 99f, "mais avec le temps on finit par y etre");
    }
}
