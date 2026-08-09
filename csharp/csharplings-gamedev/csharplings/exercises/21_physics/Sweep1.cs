namespace Csharplings;

public static class Sweep1
{
    public const bool NotDone = true;

    public static bool SweepAgainstRect(Vector2 from, Vector2 to, Rect2 rect, out float entry)
    {
        Vector2 motion = to - from;
        float tMin = 0f;
        float tMax = 1f;

        bool crossed =
            SlabRange(from.X, motion.X, rect.Position.X, rect.End.X, ref tMin, ref tMax)
            && SlabRange(from.Y, motion.Y, rect.Position.Y, rect.End.Y, ref tMin, ref tMax);

        entry = crossed ? tMin : 1f;

        return crossed;
    }

    public static int StepsFor(Vector2 motion, float maxStep) =>
        Mathf.FloorToInt(motion.Length() / maxStep);

    public static int FirstOverlappingStep(Vector2 from, Vector2 to, Rect2 rect, int steps)
    {
        Vector2 motion = to - from;

        for (int step = 1; step <= steps; step++)
        {
            if (rect.HasPoint(from + motion * ((float)step / steps)))
                return step;
        }

        return -1;
    }

    public static void Run()
    {
        var wall = new Rect2(100f, 0f, 10f, 100f);
        var from = new Vector2(50f, 50f);
        var to = new Vector2(150f, 50f);

        Check.False(wall.HasPoint(from), "au debut de la frame la balle est avant le mur");
        Check.False(wall.HasPoint(to), "a la fin de la frame elle est deja derriere");
        Check.Equal(FirstOverlappingStep(from, to, wall, steps: 1),  -1,
            "un test de chevauchement une fois par frame ne voit RIEN : la balle a traverse le mur entre deux images");

        Check.True(SweepAgainstRect(from, to, wall, out float entry),
            "le balayage, lui, teste le TRAJET et non les deux extremites : il touche");
        Check.Near(entry, 0.5,
            "et il dit quand : a la moitie de la frame, parce que le mur est a 50 pixels sur les 100 parcourus");
        Check.Near(from + (to - from) * entry, new Vector2(100f, 50f),
            "de quoi replacer la balle exactement au point de contact au lieu de la laisser derriere");

        Check.False(SweepAgainstRect(from, new Vector2(90f, 50f), wall, out float shortEntry),
            "un deplacement qui s'arrete avant le mur ne touche pas");
        Check.Near(shortEntry, 1.0, "et rend la frame entiere : rien ne l'a interrompue");

        Check.True(SweepAgainstRect(new Vector2(105f, 50f), new Vector2(200f, 50f), wall, out float inside),
            "un deplacement qui part DEDANS touche aussi");
        Check.Near(inside, 0.0, "des le debut de la frame");

        Check.False(SweepAgainstRect(new Vector2(50f, 200f), new Vector2(150f, 200f), wall, out _),
            "et passer a cote sans jamais croiser la tranche verticale ne touche pas");

        Check.True(SweepAgainstRect(new Vector2(105f, -50f), new Vector2(105f, 200f), wall, out float vertical),
            "le balayage marche dans les deux axes");
        Check.Near(vertical, 0.2, "le mur commence a 50 des 250 pixels parcourus");

        Check.Equal(StepsFor(to - from, maxStep: 10f), 10,
            "l'autre parade : decouper le deplacement pour qu'aucun morceau ne depasse l'epaisseur du plus fin des murs");
        Check.Equal(StepsFor(new Vector2(3f, 4f), maxStep: 10f), 1, "un petit deplacement reste en un seul morceau");
        Check.Equal(StepsFor(Vector2.Zero, maxStep: 10f), 1, "et un deplacement nul ne fait pas zero morceau");

        Check.Equal(FirstOverlappingStep(from, to, wall, StepsFor(to - from, 10f)), 5,
            "en dix morceaux, le cinquieme tombe pile dans le mur : plus de traversee");

        Check.True(SweepAgainstRect(from, to, wall, out _),
            "mais le balayage reste preferable : UN test au lieu de dix, et il donne l'instant exact du contact");
    }

    private static bool SlabRange(float origin, float direction, float min, float max, ref float tMin, ref float tMax)
    {
        if (Mathf.IsZeroApprox(direction))
            return true;

        float t1 = (min - origin) / direction;
        float t2 = (max - origin) / direction;

        if (t1 > t2)
            (t1, t2) = (t2, t1);

        tMin = t1;
        tMax = t2;

        return tMin <= tMax;
    }
}
