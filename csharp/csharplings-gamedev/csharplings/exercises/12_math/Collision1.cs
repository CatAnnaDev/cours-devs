namespace Csharplings;

public static class Collision1
{
    public const bool NotDone = true;

    public static bool CirclesOverlap(Vector2 a, float radiusA, Vector2 b, float radiusB)
    {
        return a.DistanceTo(b) < radiusA;
    }

    public static Vector2 ClosestPointOnRect(Rect2 rect, Vector2 point)
    {
        return Todo.Value<Vector2>();
    }

    public static bool CircleHitsRect(Vector2 center, float radius, Rect2 rect)
    {
        return rect.Intersects(new Rect2(center.X - radius, center.Y - radius, radius * 2f, radius * 2f));
    }

    public static Vector2 MinimumPush(Rect2 moving, Rect2 solid)
    {
        if (!moving.Intersects(solid))
            return Vector2.Zero;

        float right = solid.End.X - moving.Position.X;
        float down = solid.End.Y - moving.Position.Y;

        return new Vector2(right, down);
    }

    public static void Run()
    {
        Check.True(CirclesOverlap(Vector2.Zero, 5f, new Vector2(8f, 0f), 5f), "deux cercles qui se chevauchent");
        Check.True(CirclesOverlap(Vector2.Zero, 5f, new Vector2(10f, 0f), 5f), "deux cercles qui se touchent tout juste");
        Check.False(CirclesOverlap(Vector2.Zero, 5f, new Vector2(11f, 0f), 5f), "et un pixel plus loin, plus rien");

        var box = new Rect2(0f, 0f, 10f, 10f);

        Check.True(box.HasPoint(new Vector2(5f, 5f)), "un point au milieu est dedans");
        Check.True(box.HasPoint(new Vector2(0f, 0f)), "le coin haut gauche appartient au rectangle");
        Check.False(box.HasPoint(new Vector2(10f, 5f)), "mais le bord droit, non : sinon deux cases voisines se marchent dessus");

        Check.True(box.Intersects(new Rect2(5f, 5f, 10f, 10f)), "deux rectangles qui se recouvrent");
        Check.False(box.Intersects(new Rect2(10f, 0f, 10f, 10f)), "deux rectangles colles ne se recouvrent pas");

        Check.Near(ClosestPointOnRect(box, new Vector2(20f, 5f)), new Vector2(10f, 5f), "le point du rectangle le plus proche");
        Check.Near(ClosestPointOnRect(box, new Vector2(5f, 5f)), new Vector2(5f, 5f), "un point interieur est son propre plus proche");
        Check.Near(ClosestPointOnRect(box, new Vector2(-4f, 30f)), new Vector2(0f, 10f), "sinon on tombe sur un coin");

        Check.True(CircleHitsRect(new Vector2(13f, 5f), 4f, box), "un cercle qui mord le bord droit");
        Check.False(CircleHitsRect(new Vector2(15f, 5f), 4f, box), "et un qui reste dehors");
        Check.False(CircleHitsRect(new Vector2(13f, 14f), 4f, box),
            "en diagonale du coin, la boite englobante dirait oui a tort : c'est la distance au coin qui tranche");
        Check.True(CircleHitsRect(new Vector2(13f, 14f), 6f, box), "avec un rayon plus grand, cette fois ca touche");

        Check.Near(MinimumPush(new Rect2(8f, 0f, 10f, 10f), box), new Vector2(2f, 0f),
            "on repousse par le plus petit cote : 2 pixels vers la droite");
        Check.Near(MinimumPush(new Rect2(0f, 8f, 10f, 10f), box), new Vector2(0f, 2f),
            "ici c'est la verticale qui coute le moins cher");
        Check.Near(MinimumPush(new Rect2(50f, 50f, 10f, 10f), box), Vector2.Zero,
            "et sans collision, on ne pousse pas");
    }
}
