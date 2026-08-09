namespace Csharplings;

public static class Vectors1
{
    public const bool NotDone = false;

    public static Vector2 DirectionTo(Vector2 from, Vector2 to)
    {
        return (to - from).Normalized();
    }

    public static float DistanceBetween(Vector2 a, Vector2 b)
    {
        return a.DistanceTo(b);
    }

    public static bool InRange(Vector2 attacker, Vector2 target, float range)
    {
        return DistanceBetween(attacker, target) <= range;
    }

    public static Vector2 MoveTowardTarget(Vector2 position, Vector2 target, float speed, double delta)
    {
        return position + DirectionTo(position, target) * speed * (float)delta;
    }

    public static void Run()
    {
        Vector2 direction = DirectionTo(new Vector2(0f, 0f), new Vector2(300f, 0f));
        Check.Near(direction.Length(), 1.0, "une direction a TOUJOURS une longueur de 1");
        Check.Near(direction.X, 1.0, "elle pointe vers la droite");

        Vector2 loin = DirectionTo(new Vector2(0f, 0f), new Vector2(0f, 5000f));
        Check.Near(loin.Length(), 1.0, "meme vers une cible tres loin");

        Check.Near(DistanceBetween(new Vector2(0f, 0f), new Vector2(3f, 4f)), 5.0, "le bon vieux triangle 3-4-5");

        Check.True(InRange(new Vector2(0f, 0f), new Vector2(30f, 40f), 60f), "50 pixels, portee 60 : a portee");
        Check.False(InRange(new Vector2(0f, 0f), new Vector2(30f, 40f), 40f), "50 pixels, portee 40 : trop loin");

        Vector2 avance = MoveTowardTarget(new Vector2(0f, 0f), new Vector2(1000f, 0f), 100f, 1.0);
        Check.Near(avance.X, 100.0, "a 100 pixels par seconde, on avance de 100 en une seconde");
    }
}
