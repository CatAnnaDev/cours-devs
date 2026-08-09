namespace Csharplings;

public static class Rays1
{
    public const bool NotDone = true;

    public static Vector2 ClosestPointOnSegment(Vector2 a, Vector2 b, Vector2 point)
    {
        Vector2 along = b - a;
        float t = (point - a).Dot(along) / along.LengthSquared();

        return a + along * t;
    }

    public static bool SegmentsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 hit)
    {
        hit = Vector2.Zero;

        Vector2 first = a2 - a1;
        Vector2 second = b2 - b1;
        float denominator = first.Cross(second);

        Vector2 gap = b1 - a1;
        float t = gap.Cross(second) / denominator;

        hit = a1 + first * t;

        return true;
    }

    public static bool SeesTarget(Vector2 eye, Vector2 target, Vector2 wallStart, Vector2 wallEnd)
    {
        return Todo.Value<bool>();
    }

    public static void Run()
    {
        var start = new Vector2(0f, 0f);
        var end = new Vector2(10f, 0f);

        Check.Near(ClosestPointOnSegment(start, end, new Vector2(5f, 7f)), new Vector2(5f, 0f),
            "on projette le point sur le segment");
        Check.Near(ClosestPointOnSegment(start, end, new Vector2(-5f, 3f)), start,
            "avant le debut, on s'arrete au debut");
        Check.Near(ClosestPointOnSegment(start, end, new Vector2(50f, 3f)), end,
            "apres la fin, on s'arrete a la fin : c'est ca, un segment et pas une droite");
        Check.Near(ClosestPointOnSegment(start, start, new Vector2(4f, 4f)), start,
            "un segment de longueur nulle ne doit pas diviser par zero");

        Check.True(SegmentsIntersect(start, end, new Vector2(5f, -5f), new Vector2(5f, 5f), out Vector2 crossing),
            "deux segments qui se croisent");
        Check.Near(crossing, new Vector2(5f, 0f), "et on recupere le point d'impact");

        Check.False(SegmentsIntersect(start, end, new Vector2(0f, 5f), new Vector2(10f, 5f), out _),
            "deux segments paralleles ne se croisent jamais");
        Check.False(SegmentsIntersect(start, end, new Vector2(20f, -5f), new Vector2(20f, 5f), out _),
            "les droites se croiseraient, mais pas les segments");
        Check.False(SegmentsIntersect(start, end, new Vector2(5f, 1f), new Vector2(5f, 5f), out _),
            "le mur s'arrete avant le rayon");

        var guard = new Vector2(0f, 0f);
        var player = new Vector2(20f, 0f);

        Check.True(SeesTarget(guard, player, new Vector2(10f, 1f), new Vector2(10f, 5f)),
            "le mur ne coupe pas la ligne de vue");
        Check.False(SeesTarget(guard, player, new Vector2(10f, -5f), new Vector2(10f, 5f)),
            "celui-la, si : le garde ne voit rien");
    }
}
