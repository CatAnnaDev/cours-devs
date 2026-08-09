namespace Csharplings;

public static class Methods2
{
    public const bool NotDone = true;

    public static float Damage(float baseDamage, float multiplier, bool critical)
    {
        float result = baseDamage * multiplier;
        return critical ? result * 2f : result;
    }

    public static void Run()
    {
        Check.Near(Damage(10f), 10f, "un seul argument : le reste prend ses valeurs par defaut");
        Check.Near(Damage(10f, 1.5f), 15f, "deux arguments");
        Check.Near(Damage(10f, 1.5f, true), 30f, "un coup critique double les degats");
        Check.Near(Damage(10f, critical: true), 20f, "argument nomme : on saute le multiplicateur");
    }
}
