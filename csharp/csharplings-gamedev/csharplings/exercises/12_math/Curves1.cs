namespace Csharplings;

public static class Curves1
{
    public const bool NotDone = true;

    public static Vector2 QuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        return Todo.Value<Vector2>();
    }

    public static Vector2 CubicBezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        return Todo.Value<Vector2>();
    }

    public static Vector2 Arc(Vector2 start, Vector2 end, float height, float t)
    {
        Vector2 flat = start.Lerp(end, t);

        return flat + Vector2.Up * height * t;
    }

    public static float Length(Func<float, Vector2> curve, int samples)
    {
        return curve(0f).DistanceTo(curve(1f));
    }

    public static void Run()
    {
        var a = new Vector2(0f, 0f);
        var b = new Vector2(10f, 0f);
        var c = new Vector2(10f, 10f);

        Check.Near(QuadraticBezier(a, b, c, 0f), a, "une courbe de Bezier part de son premier point");
        Check.Near(QuadraticBezier(a, b, c, 1f), c, "et arrive au dernier");
        Check.Near(QuadraticBezier(a, b, c, 0.5f), new Vector2(7.5f, 2.5f),
            "elle ne passe PAS par le point de controle, elle est attiree par lui");

        Check.Near(QuadraticBezier(a, new Vector2(5f, 0f), b, 0.5f), new Vector2(5f, 0f),
            "avec un controle aligne, on retombe sur la ligne droite");

        Check.Near(CubicBezier(a, a, c, c, 0.5f), new Vector2(5f, 5f), "une cubique, c'est deux quadratiques melangees");
        Check.Near(CubicBezier(a, b, c, c, 0f), a, "meme depart");
        Check.Near(CubicBezier(a, b, c, c, 1f), c, "meme arrivee");

        var from = new Vector2(0f, 0f);
        var to = new Vector2(100f, 0f);

        Check.Near(Arc(from, to, 50f, 0f), from, "un saut part du sol");
        Check.Near(Arc(from, to, 50f, 1f), to, "et y revient");
        Check.Near(Arc(from, to, 50f, 0.5f), new Vector2(50f, -50f), "au sommet, on est a la hauteur demandee");
        Check.True(Arc(from, to, 50f, 0.25f).Y < 0f, "et entre les deux, on est en l'air");

        Check.Near(Length(t => from.Lerp(to, t), 10), 100.0, "la longueur d'une droite, c'est sa distance", 0.01);
        Check.True(Length(t => QuadraticBezier(a, b, c, t), 200) > a.DistanceTo(c),
            "une courbe est toujours plus longue que la corde qui la tend");
        Check.True(Length(t => Arc(from, to, 50f, t), 400) > 100f, "un saut parcourt plus que sa portee au sol");
    }
}
